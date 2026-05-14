using System;  // ← 添加这个引用
using System.Collections.Generic;
using UnityEngine;
using JunqiGame.Core;
using JunqiGame.RTS.Data;
using JunqiGame.UI;
using JunqiGame.MonoBehaviours;

namespace JunqiGame.RTS
{
    /// <summary>
    /// RTS 调度器核心 - 实时并发行动调度器
    /// 玩家和 AI 同时操作，每次移动扣 1 点 AP，每 0.5s 恢复 0.1 点
    /// </summary>
    public class RTSController : MonoBehaviour
    {
        public static RTSController Instance { get; private set; }

        [Header("配置引用")]
        public RTSConfigSO Config;
        public HealthAttackConfigSO HealthConfig;

        [Header("场景引用")]
        public JunqiGameManager gameManager;
        public GameUIManager uiManager;

        private RTSState state;
        private float tickAccumulator = 0f;

        private Queue<RTSMoveAction> actionQueue = new Queue<RTSMoveAction>();

        private HashSet<string> busyPieceKeys = new HashSet<string>();
        private Dictionary<string, BoardPosition> pieceDestinations = new Dictionary<string, BoardPosition>();

        private bool aiActionThisTick = false;
        private bool isProcessingQueue = false;
        private bool combatTriggered = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (Config == null)
            {
                Console.WriteLine("⚠️ [RTS] RTSConfigSO is not assigned. RTS mode will be disabled.");
                enabled = false;
                return;
            }

            InitializeState();
        }

        private void InitializeState()
        {
            state = new RTSState
            {
                APMax = Config.APMax,
                APBlue = Config.APMax,
                APRed = Config.APMax
            };

            Console.WriteLine($"✅ [RTS] Initialized: APMax={Config.APMax}, Tick={Config.RTSTickIntervalSeconds}s");

            if (HealthConfig != null)
                HealthConfig.ValidateConfig();
        }

        private void FixedUpdate()
        {
            if (Config == null || !Config.RTSModeEnabled) return;

            tickAccumulator += Time.fixedDeltaTime;
            if (tickAccumulator < Config.RTSTickIntervalSeconds) return;
            tickAccumulator = 0f;

            state.APBlue = Mathf.Min(state.APBlue + Config.APRegenPerTick, state.APMax);
            state.APRed = Mathf.Min(state.APRed + Config.APRegenPerTick, state.APMax);

            if (AnimationManager.Instance != null)
            {
                AnimationManager.Instance.ResetCombatTriggered();
            }

            aiActionThisTick = false;
            TryGenerateAIAction();

            ProcessActionQueue();
        }

        /// <summary>
        /// 重置 RTS 状态
        /// </summary>
        public void ResetState()
        {
            if (state == null) state = new RTSState();
            state.APMax = Config != null ? Config.APMax : 3f;
            state.APBlue = state.APMax;
            state.APRed = state.APMax;
            tickAccumulator = 0f;
            actionQueue.Clear();
            busyPieceKeys.Clear();
            pieceDestinations.Clear();
            combatTriggered = false;
            isProcessingQueue = false;
            aiActionThisTick = false;
            Console.WriteLine($"🔄 [RTS] State reset. AP Blue={state.APBlue}, AP Red={state.APRed}");
        }

        /// <summary>
        /// 玩家尝试加入动作到队列
        /// </summary>
        public bool TryEnqueuePlayerAction(BoardPosition from, BoardPosition to, PlayerColor player)
        {
            if (Config == null || !Config.RTSModeEnabled) return false;
            if (state == null) return false;
            if (player == PlayerColor.None) return false;

            string fromKey = from.ToString();
            if (busyPieceKeys.Contains(fromKey))
            {
                Console.WriteLine($"⛔ [RTS] {from} already has an action in queue!");
                return false;
            }

            var board = GetBoard();
            if (board == null) return false;

            // 检查目标是否已被某个动画中棋子锁定
            string toKey = to.ToString();
            if (pieceDestinations.ContainsValue(to))
            {
                Console.WriteLine($"⛔ [RTS] {to} is already targeted by another moving piece!");
                return false;
            }

            if (!GameRules.IsValidMove(from, to, board, player))
            {
                Console.WriteLine($"⛔ [RTS] Move {from}-{to} is not valid.");
                return false;
            }

            if (gameManager != null && gameManager.IsAIGame)
            {
                if (!ConsumeAP(player, 1f))
                {
                    Console.WriteLine($"⛔ [RTS] {player} not enough AP!");
                    return false;
                }
            }

            busyPieceKeys.Add(fromKey);
            pieceDestinations[fromKey] = to;

            string moveStr = $"{from}-{to}";
            if (!board.IsEmpty(to))
                moveStr = $"{from}x{to}";

            var action = new RTSMoveAction
            {
                FromPos = from,
                ToPos = to,
                MoveString = moveStr,
                Player = player
            };

            actionQueue.Enqueue(action);
            // 🔑 简化日志：只在队列长度变化时输出
            if (actionQueue.Count == 1)
            {
                Console.WriteLine($"💙 [RTS] Player enqueued: {moveStr}. Queue: {actionQueue.Count}");
            }
            return true;
        }

        private void TryGenerateAIAction()
        {
            if (gameManager == null || !gameManager.IsAIGame) return;
            if (gameManager.GetGameState() == null) return;
            if (gameManager.GetGameState().Status != GameStatus.Ongoing) return;
            if (aiActionThisTick) return;

            var board = GetBoard();
            if (board == null) return;

            // 🔑 关键修复：AP为0时不生成AI动作
            if (state.APRed <= 0f)
            {
                Console.WriteLine($"🤖 [RTS AI] Red AP is 0, skipping AI action this tick");
                return;
            }

            var validMoves = gameManager.GetGameState().GetValidMoves(PlayerColor.Red);
            if (validMoves.Count == 0) return;

            if (!ConsumeAP(PlayerColor.Red, 1f)) return;

            int startIdx = UnityEngine.Random.Range(0, validMoves.Count);
            RTSMoveAction selectedAction = null;

            for (int i = 0; i < validMoves.Count; i++)
            {
                int idx = (startIdx + i) % validMoves.Count;
                var moveStr = validMoves[idx];
                var parts = moveStr.Split(new[] { '-', 'x' });
                var from = BoardPosition.FromString(parts[0]);
                var to = BoardPosition.FromString(parts[1]);

                if (busyPieceKeys.Contains(from.ToString())) continue;
                if (pieceDestinations.ContainsValue(to)) continue;
                var piece = board.GetPiece(from);
                if (piece == null || piece.Color != PlayerColor.Red) continue;
                if (!GameRules.IsValidMove(from, to, board, PlayerColor.Red)) continue;

                busyPieceKeys.Add(from.ToString());
                pieceDestinations[from.ToString()] = to;
                selectedAction = new RTSMoveAction
                {
                    FromPos = from,
                    ToPos = to,
                    MoveString = moveStr,
                    Player = PlayerColor.Red
                };
                break;
            }

            if (selectedAction != null)
            {
                actionQueue.Enqueue(selectedAction);
                aiActionThisTick = true;
                
                // 🔑 简化AI日志
                if (UnityEngine.Random.Range(0, 10) == 0)  // 10%概率输出
                {
                    Console.WriteLine($"❤️ [RTS] AI enqueued: {selectedAction.MoveString}. Queue: {actionQueue.Count}");
                }
                return;
            }
            else
            {
                Console.WriteLine($"⚠️ [AI] All moves invalid or blocked, AP consumed but no action queued");
            }
        }

        private void ProcessActionQueue()
        {
            if (isProcessingQueue) return;
            if (actionQueue.Count == 0) return;

            isProcessingQueue = true;
            combatTriggered = false;

            if (AnimationManager.Instance != null)
            {
                AnimationManager.Instance.ResetCombatTriggered();
            }

            // 每 tick 只处理 1 个动作，避免协程回调与同步逻辑之间的碰撞判断竞态
            var action = actionQueue.Dequeue();
            ExecuteActionImmediate(action);

            isProcessingQueue = false;
        }

        private void ExecuteActionImmediate(RTSMoveAction action)
        {
            var board = GetBoard();
            if (board == null) return;

            var attacker = board.GetPiece(action.FromPos);
            if (attacker == null) return;

            // 原路径：走到 action.ToPos，路上视觉碰撞检测到防御方就停下
            List<BoardPosition> path = null;
            if (Board.IsRailway(action.FromPos) && Board.IsRailway(action.ToPos))
            {
                path = PathFinder.FindPath(board, action.FromPos, action.ToPos, attacker);
            }
            if (path == null || path.Count < 2)
            {
                path = new List<BoardPosition> { action.FromPos, action.ToPos };
            }

            var result = new MoveResult(true, $"RTS: {action.MoveString}", attacker, null, null, path);
            TriggerAnimationParallel(action, result);
        }

        private void CheckFlagCapture(Piece capturedPiece, PlayerColor attackerColor)
        {
            if (capturedPiece == null) return;
            if (!capturedPiece.IsFlag()) return;

            Console.WriteLine($"🏴 [GAME OVER] {attackerColor} captured the flag!");

            if (gameManager != null && gameManager.GetGameState() != null)
            {
                var gameState = gameManager.GetGameState();
                var endResult = new GameEndResult(true, attackerColor, "Flag captured!");
                PlayerColor loserColor = attackerColor == PlayerColor.Blue ? PlayerColor.Red : PlayerColor.Blue;
                gameManager.GetGameState().Forfeit(loserColor);
            }

            actionQueue.Clear();
            busyPieceKeys.Clear();
            pieceDestinations.Clear();
            Config.RTSModeEnabled = false;
        }

        private void TriggerAnimationParallel(RTSMoveAction action, MoveResult result)
        {
            if (result.PathUsed == null || result.PathUsed.Count < 2) return;

            GameObject pieceObj = uiManager != null ? uiManager.GetPieceObjectAt(action.FromPos) : null;
            if (pieceObj == null) return;

            var board = GetBoard();
            string pieceKey = action.FromPos.ToString();
            Piece attacker = board?.GetPiece(action.FromPos);
            if (attacker == null) return;

            AnimationManager.Instance.PlayMoveAlongPath(
                pieceObj,
                result.PathUsed,
                uiManager.boardCells,
                15f,
                (stepIdx, curPos) =>
                {
                    // ═══ 视觉碰撞检测（行营免疫） ═══
                    Piece visualDefender;
                    if (uiManager != null && !Board.IsCamp(curPos) && PathCombatDetector.CheckVisualCollision(
                            uiManager.pieceObjects, curPos, action.Player, out visualDefender))
                    {
                        BoardPosition collisionFromPos = result.PathUsed[stepIdx - 1];
                        CombatResult cr = GameRules.ResolveCombat(attacker, visualDefender);
                        combatTriggered = true;

                        Console.WriteLine($"💥 [RTS VisualCollision] {attacker.Rank}({action.Player}) hit {visualDefender.Rank} at {curPos}, Result={cr}");

                        // 回收防守方 GameObject
                        if (uiManager != null)
                        {
                            var defObj = uiManager.GetPieceObjectAt(curPos);
                            if (defObj != null) uiManager.ReturnPieceToPool(defObj);
                        }
                        string defenderKey = curPos.ToString();
                        busyPieceKeys.Remove(defenderKey);
                        pieceDestinations.Remove(defenderKey);

                        // 数据层：移动攻击方到碰撞位置
                        if (board != null) GameRules.ApplyMoveStep(board, collisionFromPos, curPos);

                        // 根据战斗结果处理
                        switch (cr)
                        {
                            case CombatResult.AttackerWin:
                                // UI：将攻击方的 GameObject 引用移动到碰撞位置
                                if (uiManager != null) uiManager.SyncPiecePosition(collisionFromPos, curPos);
                                if (board != null) { board.RemovePiece(curPos); board.PlacePiece(curPos, attacker.Clone()); }
                                CheckFlagCapture(visualDefender, action.Player);
                                break;

                            case CombatResult.DefenderWin:
                                if (uiManager != null) uiManager.ReturnPieceToPool(pieceObj);
                                if (board != null) { board.RemovePiece(curPos); board.PlacePiece(curPos, visualDefender.Clone()); }
                                if (uiManager != null)
                                {
                                    int c = curPos.Column - 'a', r = curPos.Row - 1;
                                    uiManager.DisplayPieceAtPosition(c, r, visualDefender, curPos.ToString());
                                }
                                break;

                            case CombatResult.BothDie:
                                if (uiManager != null) uiManager.ReturnPieceToPool(pieceObj);
                                if (board != null) board.RemovePiece(curPos);
                                if (uiManager != null)
                                {
                                    int c = curPos.Column - 'a', r = curPos.Row - 1;
                                    uiManager.ClearPieceReference(c, r);
                                }
                                CheckFlagCapture(visualDefender, action.Player);
                                break;
                        }

                        return false;
                    }

                    // ═══ 行营阻挡检测 ═══
                    BoardPosition blockFromPos = result.PathUsed[stepIdx - 1];
                    if (uiManager != null && Board.IsCamp(curPos))
                    {
                        int cc = curPos.Column - 'a', rr = curPos.Row - 1;
                        if (uiManager.pieceObjects[cc, rr] != null)
                        {
                            Console.WriteLine($"⛔ [RTS CampBlock] blocked at {curPos}");
                            GameObject prevCell = uiManager.boardCells[blockFromPos.Column - 'a', blockFromPos.Row - 1];
                            if (prevCell != null)
                            {
                                pieceObj.transform.SetParent(prevCell.transform);
                                pieceObj.transform.localPosition = Vector3.zero;
                            }
                            combatTriggered = true;
                            return false;
                        }
                    }

                    // ═══ 己方阻挡检测 ═══
                    if (uiManager != null && PathCombatDetector.CheckFriendlyBlock(
                            uiManager.pieceObjects, curPos, action.Player))
                    {
                        Console.WriteLine($"🚫 [RTS FriendlyBlock] {attacker.Rank}({action.Player}) blocked at {curPos}");
                        GameObject prevCell = uiManager.boardCells[blockFromPos.Column - 'a', blockFromPos.Row - 1];
                        if (prevCell != null)
                        {
                            pieceObj.transform.SetParent(prevCell.transform);
                            pieceObj.transform.localPosition = Vector3.zero;
                        }
                        combatTriggered = true;
                        return false;
                    }

                    // ═══ 无阻挡：正常移动 ═══
                    if (stepIdx > 0 && stepIdx < result.PathUsed.Count)
                    {
                        BoardPosition normalFromPos = result.PathUsed[stepIdx - 1];
                        if (board != null) GameRules.ApplyMoveStep(board, normalFromPos, curPos);
                        if (uiManager != null) uiManager.SyncPiecePosition(normalFromPos, curPos);
                    }
                    return true;
                },
                () =>
                {
                    busyPieceKeys.Remove(pieceKey);
                    pieceDestinations.Remove(pieceKey);
                }
            );
        }

        /// <summary>
        /// 获取当前 AP
        /// </summary>
        public float GetCurrentAP(PlayerColor color)
        {
            if (state == null) return 0f;
            return color == PlayerColor.Blue ? state.APBlue : state.APRed;
        }

        /// <summary>
        /// 检查 AP 是否足够
        /// </summary>
        public bool HasEnoughAP(PlayerColor color, float cost)
        {
            return GetCurrentAP(color) >= cost;
        }

        /// <summary>
        /// 消耗 AP
        /// </summary>
        public bool ConsumeAP(PlayerColor color, float cost)
        {
            if (state == null) return false;
            if (cost <= 0) return false;

            if (color == PlayerColor.Blue)
            {
                if (state.APBlue >= cost)
                {
                    state.APBlue -= cost;
                    return true;
                }
            }
            else
            {
                if (state.APRed >= cost)
                {
                    state.APRed -= cost;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查棋子是否正在忙碌（从入队到动画完成）
        /// </summary>
        public bool IsPieceBusy(BoardPosition pos)
        {
            return busyPieceKeys.Contains(pos.ToString());
        }

        /// <summary>
        /// 启用/禁用 RTS 模式
        /// </summary>
        public void EnableRTSMode(bool enable)
        {
            if (Config != null)
            {
                Config.RTSModeEnabled = enable;
                Console.WriteLine($"🎮 [RTS] Mode {(enable ? "ENABLED" : "DISABLED")}");
            }
        }

        private Board GetBoard()
        {
            return gameManager != null ? gameManager.GetBoard() : null;
        }

        public RTSState GetState() => state;
    }

    /// <summary>
    /// RTS 移动动作
    /// </summary>
    public class RTSMoveAction
    {
        public BoardPosition FromPos;
        public BoardPosition ToPos;
        public string MoveString;
        public PlayerColor Player;
    }
}
