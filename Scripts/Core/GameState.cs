using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunqiGame.Core
{
    /// <summary>
    /// 玩家信息
    /// </summary>
    [Serializable]
    public class PlayerInfo
    {
        public PlayerColor Color;
        public string Name;
        public string Uid;

        public PlayerInfo()
        {
            Color = PlayerColor.None;
            Name = "";
            Uid = "";
        }

        public PlayerInfo(PlayerColor color, string name, string uid)
        {
            Color = color;
            Name = name;
            Uid = uid;
        }
    }

    /// <summary>
    /// 游戏状态类 - 管理整个游戏的运行状态
    /// 对应原始代码中的GameState和新代码中的LocalGameConnection
    /// </summary>
    public class GameState
    {
        // 游戏基本属性
        public GameStatus Status { get; private set; }
        public PlayMode PlayMode { get; private set; }
        public PlayerInfo ActivePlayer { get; private set; }
        
        // 棋盘
        public Board Board { get; private set; }
        
        // 玩家信息
        public Dictionary<PlayerColor, PlayerInfo> Players { get; private set; }
        
        // 事件回调
        public event Action<GameState, string> OnStateChange;
        public event Action<MoveResult> OnMoveExecuted;
        public event Action<GameEndResult> OnGameEnded;

        // 构造函数
        public GameState()
        {
            Status = GameStatus.Setup;
            PlayMode = PlayMode.Concealed;
            Board = new Board();
            Players = new Dictionary<PlayerColor, PlayerInfo>();
            ActivePlayer = null;
        }

        /// <summary>
        /// 设置游戏模式
        /// </summary>
        public void SetPlayMode(PlayMode mode)
        {
            PlayMode = mode;
        }

        /// <summary>
        /// 添加玩家
        /// </summary>
        public void AddPlayer(PlayerInfo playerInfo)
        {
            Players[playerInfo.Color] = playerInfo;
        }

        /// <summary>
        /// 完成布阵（双方都完成后开始游戏）
        /// 对应原始代码: game.finishSetup()
        /// </summary>
        public void FinishSetup(PlayerInfo playerInfo)
        {
            // 检查是否所有玩家都已准备好
            bool allReady = true;
            foreach (var kvp in Players)
            {
                // 这里可以添加更复杂的检查逻辑
            }

            if (allReady && Players.Count >= 2)
            {
                Status = GameStatus.Ongoing;
                // 蓝方先手
                ActivePlayer = Players.ContainsKey(PlayerColor.Blue)
                    ? Players[PlayerColor.Blue]
                    : (Players.Count > 0 ? new System.Collections.Generic.List<PlayerInfo>(Players.Values)[0] : null);
                
                NotifyStateChange("FINISH_SETUP");
            }
        }

    /// <summary>
    /// 执行移动（新版本：只验证并返回路径，不立即执行）
    /// 实时模式下：任何有 AP 的一方都能移动，不再检查 ActivePlayer
    /// </summary>
    public MoveResult Move(string moveString, PlayerColor playerColor)
    {
        // 移除"不是你的回合"的检查
        // RTS 模式下，任何有 AP 的一方都能发起移动

        // 解析移动
        MoveParseResult parseResult;
        try
        {
            parseResult = GameRules.ParseMove(moveString);
        }
        catch (Exception e)
        {
            return new MoveResult(false, $"Invalid move format: {e.Message}");
        }

        // 执行移动验证并获取路径
        MoveResult result = GameRules.ExecuteMove(
            Board,
            parseResult.From,
            parseResult.To,
            playerColor
        );

        if (result.Success)
        {
            Debug.Log($"✅ [GameState] Move validated, path count: {result.PathUsed?.Count ?? 0}");
        }

        return result;
    }

        /// <summary>
        /// 完成移动（实时模式下不切换玩家）
        /// </summary>
        public void FinalizeMove(MoveResult result, PlayerColor currentPlayer)
        {
            if (!result.Success)
            {
                Debug.LogWarning("⚠️ [GameState] Cannot finalize failed move");
                return;
            }

            Debug.Log($"✅ [GameState] Move finalized by {currentPlayer}");

            // 触发事件
            OnMoveExecuted?.Invoke(result);
            NotifyStateChange("MOVE_PIECE");

            Debug.Log($"🎉 [GameState] Move finalized");
        }

        /// <summary>
        /// 完成移动并切换玩家（仅保留兼容性，不推荐使用）
        /// </summary>
        [System.Obsolete("Use FinalizeMove instead. RTS mode does not switch players.")]
        public void FinalizeMoveAndSwitch(MoveResult result, PlayerColor currentPlayer)
        {
            FinalizeMove(result, currentPlayer);
        }

        /// <summary>
        /// 认输
        /// 对应原始代码: game.forfeit(playerColor)
        /// </summary>
        public GameEndResult Forfeit(PlayerColor playerColor)
        {
            PlayerColor winner = playerColor == PlayerColor.Blue ? PlayerColor.Red : PlayerColor.Blue;
            var result = new GameEndResult(true, winner, $"{playerColor} forfeited");
            
            Status = GameStatus.Finished;
            OnGameEnded?.Invoke(result);
            
            return result;
        }

        /// <summary>
        /// 获取当前玩家的所有合法移动（使用单次BFS替代N*M次IsValidMove）
        /// </summary>
        public List<string> GetValidMoves(PlayerColor playerColor)
        {
            var validMoves = new List<string>();
            var positions = Board.GetPiecesByColor(playerColor);
            var adjBuffer = new BoardPosition[4];

            foreach (var fromPos in positions)
            {
                Piece piece = Board.GetPiece(fromPos);
                if (piece == null || !piece.CanMove())
                    continue;

                // 1. 基础检查：相邻位置
                int adjCount = fromPos.GetAdjacentPositions(adjBuffer);
                for (int i = 0; i < adjCount; i++)
                {
                    var toPos = adjBuffer[i];
                    if (GameRules.IsValidMove(fromPos, toPos, Board, playerColor))
                        AddMoveIfValid(validMoves, fromPos, toPos);
                }

                // 2. 铁路：单次BFS找所有可达位置（比逐个检查快O(N)倍）
                if (Board.IsRailway(fromPos))
                {
                    var reachable = PathFinder.FindAllReachablePositions(Board, fromPos, piece);
                    if (reachable != null)
                    {
                        foreach (var toPos in reachable)
                        {
                            bool isAdjacent = false;
                            for (int j = 0; j < adjCount; j++)
                            {
                                if (adjBuffer[j].Equals(toPos)) { isAdjacent = true; break; }
                            }
                            if (isAdjacent) continue;

                            if (GameRules.IsValidMove(fromPos, toPos, Board, playerColor))
                                AddMoveIfValid(validMoves, fromPos, toPos);
                        }
                    }
                }
            }

            return validMoves;
        }

        /// <summary>
        /// 辅助方法：添加移动字符串
        /// </summary>
        private void AddMoveIfValid(List<string> moves, BoardPosition from, BoardPosition to)
        {
            Piece targetPiece = Board.GetPiece(to);
            string moveStr = targetPiece != null 
                ? $"{from}x{to}" 
                : $"{from}-{to}";
            
            if (!moves.Contains(moveStr))
            {
                moves.Add(moveStr);
            }
        }

        /// <summary>
        /// 初始化AI布阵
        /// </summary>
        public void InitializeAILayout(PlayerColor aiColor, AIDifficulty difficulty)
        {
            var aiLayout = JunqiGame.AI.AILayoutGenerator.GenerateLayout(aiColor, difficulty);
            Board.Merge(aiLayout);
        }

        /// <summary>
        /// 切换当前玩家
        /// </summary>
        private void SwitchPlayer()
        {
            if (ActivePlayer == null)
                return;

            PlayerColor nextColor = ActivePlayer.Color == PlayerColor.Blue 
                ? PlayerColor.Red 
                : PlayerColor.Blue;

            if (Players.ContainsKey(nextColor))
            {
                ActivePlayer = Players[nextColor];
            }
        }

        /// <summary>
        /// 检查游戏是否结束
        /// </summary>
        private void CheckGameEnd()
        {
            GameEndResult result = GameRules.CheckGameEnd(Board);
            
            if (result.IsGameOver)
            {
                Status = GameStatus.Finished;
                OnGameEnded?.Invoke(result);
            }
        }

        /// <summary>
        /// 通知状态变化
        /// </summary>
        private void NotifyStateChange(string changeType)
        {
            OnStateChange?.Invoke(this, changeType);
        }

        /// <summary>
        /// 获取棋盘的静态副本（用于AI计算）
        /// </summary>
        public Dictionary<string, Piece> GetBoardState()
        {
            return Board.GetStaticBoardState();
        }

        /// <summary>
        /// 重置游戏
        /// </summary>
        public void Reset()
        {
            Status = GameStatus.Setup;
            Board.Clear();
            Players.Clear();
            ActivePlayer = null;
        }

        /// <summary>
        /// 克隆游戏状态
        /// </summary>
        public GameState Clone()
        {
            var cloned = new GameState();
            cloned.Status = this.Status;
            cloned.PlayMode = this.PlayMode;
            cloned.Board = this.Board.Clone();
            cloned.ActivePlayer = this.ActivePlayer;
            
            foreach (var kvp in Players)
            {
                cloned.Players[kvp.Key] = new PlayerInfo(kvp.Value.Color, kvp.Value.Name, kvp.Value.Uid);
            }

            return cloned;
        }
    }
}
