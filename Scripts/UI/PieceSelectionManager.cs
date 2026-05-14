using System.Collections.Generic;
using UnityEngine;
using JunqiGame.Core;
using JunqiGame.MonoBehaviours;
using JunqiGame.RTS;

namespace JunqiGame.UI
{
    /// <summary>
    /// 棋子选择管理器
    /// 处理玩家选择棋子和目标位置的逻辑
    /// </summary>
    public class PieceSelectionManager : MonoBehaviour
    {
        [Header("视觉反馈")]
        [Tooltip("选中高亮颜色")]
        public Color selectedHighlightColor = Color.yellow;
        
        [Tooltip("可移动位置高亮颜色")]
        public Color validMoveColor = new Color(0f, 1f, 0f, 0.5f);
        
        [Tooltip("无效位置颜色")]
        public Color invalidMoveColor = new Color(1f, 0f, 0f, 0.3f);

        // 当前选中的位置
        private BoardPosition? selectedPosition = null;
        
        // 当前玩家的合法移动列表
        private List<string> validMoves = new List<string>();
        
        // 游戏管理器引用
        private JunqiGameManager gameManager;
        
        // UI管理器引用
        private GameUIManager uiManager;

        private RTSController rtsController;

        private void Start()
        {
            gameManager = JunqiGameManager.Instance;
            uiManager = FindObjectOfType<GameUIManager>();
            rtsController = FindObjectOfType<RTSController>();
        }

        /// <summary>
        /// 处理格子点击
        /// </summary>
        public void HandleCellClick(BoardPosition clickedPosition)
        {
            if (gameManager == null) return;

            GameState gameState = gameManager.GetGameState();
            if (gameState == null || gameState.Status != GameStatus.Ongoing)
            {
                return;
            }

            Board board = gameManager.GetBoard();
            if (board == null) return;

            Piece clickedPiece = board.GetPiece(clickedPosition);

            // 如果已经选中了棋子
            if (selectedPosition.HasValue)
            {
                // 情况1：点击的是同一个位置 - 取消选中
                if (clickedPosition.Equals(selectedPosition.Value))
                {
                    ClearSelection();
                    return;
                }

                // 情况2：点击的是自己的其他棋子 - 取消当前选中，选中新棋子
                if (clickedPiece != null && clickedPiece.Color == gameState.ActivePlayer.Color)
                {
                    if (clickedPiece.CanMove())
                    {
                        SelectPiece(clickedPosition);
                    }
                    else
                    {
                        ClearSelection();
                    }
                    return;
                }

                // 情况3：点击的是空位置或敌方棋子 - 尝试移动
                TryMoveTo(clickedPosition);
            }
            else
            {
                // 没有选中棋子时，尝试选中棋子
                TrySelectPiece(clickedPosition);
            }
        }

        /// <summary>
        /// 尝试选中棋子
        /// </summary>
        private void TrySelectPiece(BoardPosition position)
        {
            GameState gameState = gameManager.GetGameState();
            if (gameState == null) return;

            Board board = gameManager.GetBoard();
            if (board == null) return;

            Piece piece = board.GetPiece(position);
            
            if (piece == null)
            {
                // 🔑 增强调试信息：输出该位置的详细信息
                Debug.Log($"⚪ [TrySelectPiece] Empty cell at {position}");
                Debug.Log($"   - Position: {position.Column}{position.Row}");
                Debug.Log($"   - IsCamp: {Board.IsCamp(position)}");
                
                // 检查UI层是否有GameObject
                if (uiManager != null)
                {
                    int col = position.Column - 'a';
                    int row = position.Row - 1;
                    GameObject uiPiece = uiManager.GetPieceObject(col, row);
                    Debug.Log($"   - UI has GameObject: {uiPiece != null}");
                    if (uiPiece != null)
                    {
                        Debug.Log($"   - UI GameObject name: {uiPiece.name}");
                        PieceDisplay display = uiPiece.GetComponent<PieceDisplay>();
                        if (display != null && display.CurrentPiece != null)
                        {
                            Debug.Log($"   - ⚠️ WARNING: UI shows {display.CurrentPiece.Rank} but board data is empty!");
                            Debug.Log($"   - This indicates a sync issue between UI and board data.");
                        }
                    }
                }
                
                return;
            }

            // 🔑 RTS模式：如果AI已禁用，允许选择任意颜色的棋子（手动控制双方）
            PlayerColor currentPlayer;
            bool isRTSMode = gameManager.RTSConfig?.RTSModeEnabled == true;
            bool aiDisabled = !gameManager.IsAIGame;
            
            if (isRTSMode && aiDisabled)
            {
                // AI禁用时，允许选择任何棋子
                currentPlayer = piece.Color;  // 放宽限制
                Debug.Log($"🎮 [Manual Control] AI disabled, can select any piece");
            }
            else if (isRTSMode)
            {
                // RTS模式且AI启用，只允许选择蓝方（玩家）
                currentPlayer = PlayerColor.Blue;
            }
            else
            {
                // 传统回合制模式
                currentPlayer = gameState.ActivePlayer.Color;
            }

            if (piece.Color != currentPlayer)
            {
                Debug.Log($"Not your piece! Current player: {currentPlayer}, Piece color: {piece.Color}");
                return;
            }

            if (!piece.CanMove())
            {
                Debug.Log("This piece cannot move");
                return;
            }

            // RTS 模式：检查棋子是否正在动画中
            if (gameManager.RTSConfig != null && gameManager.RTSConfig.RTSModeEnabled)
            {
                if (rtsController != null)
                {
                    if (rtsController.IsPieceBusy(position))
                    {
                        Debug.LogWarning($"⛔ [RTS] {position} is busy (animating/moving), cannot select!");
                        return;
                    }
                }
            }

            SelectPiece(position);
        }

        /// <summary>
        /// 选中棋子
        /// </summary>
        private void SelectPiece(BoardPosition position)
        {
            GameState gameState = gameManager.GetGameState();
            if (gameState == null) return;

            // 清除之前的选中
            ClearSelection();

            // 设置新的选中
            selectedPosition = position;
            
            // 获取合法移动
            validMoves = gameState.GetValidMoves(gameState.ActivePlayer.Color);
            
            // 显示合法移动提示
            HighlightValidMoves();
            
            // 更新棋子的视觉选中状态
            UpdatePieceVisualSelection(position, true);
            
            // 播放选中音效
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayClickSound();
            }

            Debug.Log($"✅ Selected piece at: {position}");
        }

        /// <summary>
        /// 更新棋子的视觉选中状态
        /// </summary>
        private void UpdatePieceVisualSelection(BoardPosition position, bool selected)
        {
            // 查找对应的棋子 GameObject
            int col = position.Column - 'a';
            int row = position.Row - 1;
            
            // 需要通过 GameUIManager 获取棋子对象
            if (uiManager != null)
            {
                GameObject pieceObj = uiManager.GetPieceObject(col, row);
                if (pieceObj != null)
                {
                    PieceDisplay display = pieceObj.GetComponent<PieceDisplay>();
                    if (display != null)
                    {
                        display.SetSelected(selected);
                    }
                }
            }
        }

        /// <summary>
        /// 清除选中状态（公共方法，供外部调用）
        /// </summary>
        public void ClearSelection()
        {
            if (selectedPosition.HasValue)
            {
                // 取消棋子的选中视觉效果
                if (uiManager != null)
                {
                    int col = selectedPosition.Value.Column - 'a';
                    int row = selectedPosition.Value.Row - 1;
                    
                    GameObject pieceObj = uiManager.GetPieceObject(col, row);
                    if (pieceObj != null)
                    {
                        PieceDisplay display = pieceObj.GetComponent<PieceDisplay>();
                        if (display != null)
                        {
                            display.SetSelected(false);
                        }
                    }
                }
                
                selectedPosition = null;
                Debug.Log("Selection cleared");
            }
        }
        /// <summary>
        /// 尝试移动到目标位置
        /// </summary>
        private void TryMoveTo(BoardPosition targetPosition)
        {
            Debug.Log($"🎯 [ClickTarget] Selected: {selectedPosition} (Type: {Board.GetCellType(selectedPosition.Value)})");
            Debug.Log($"🎯 [ClickTarget] Target: {targetPosition} (Type: {Board.GetCellType(targetPosition)})");

            GameState gameState = gameManager.GetGameState();
            if (gameState == null || !selectedPosition.HasValue) return;

            // 🔑 RTS模式：如果AI禁用，根据选中棋子的颜色确定当前玩家
            PlayerColor currentPlayer;
            bool isRTSMode = gameManager.RTSConfig?.RTSModeEnabled == true;
            bool aiDisabled = !gameManager.IsAIGame;
            
            if (isRTSMode && aiDisabled)
            {
                // AI禁用时，根据选中棋子的颜色确定玩家
                Board currentBoard = gameManager.GetBoard();
                Piece selectedPiece = currentBoard.GetPiece(selectedPosition.Value);
                currentPlayer = selectedPiece != null ? selectedPiece.Color : gameState.ActivePlayer.Color;
                Debug.Log($"🎮 [Manual Control] Moving for {currentPlayer}");
            }
            else if (isRTSMode)
            {
                // RTS模式且AI启用，只允许蓝方移动
                currentPlayer = PlayerColor.Blue;
            }
            else
            {
                // 传统回合制模式
                currentPlayer = gameState.ActivePlayer.Color;
            }

            // RTS 模式：使用队列接口加入动作
            if (gameManager.RTSConfig != null && gameManager.RTSConfig.RTSModeEnabled)
            {
                if (rtsController != null)
                {
                    // 🔑 AI禁用时，不检查AP（手动调试模式）
                    if (!aiDisabled && !rtsController.HasEnoughAP(currentPlayer, 1f))
                    {
                        Debug.LogWarning($"⛔ [AP] {currentPlayer} not enough AP to move!");
                        ClearSelection();
                        if (uiManager != null) uiManager.ShowMessage("行动点不足！");
                        return;
                    }

                    // 检查棋子是否正在移动（可能在动画中）
                    if (rtsController.IsPieceBusy(selectedPosition.Value))
                    {
                        Debug.LogWarning($"⛔ [RTS] {selectedPosition} is busy (animating/moving), cannot operate!");
                        ClearSelection();
                        return;
                    }

                    // 🔑 AI禁用时，仍然消耗AP以保持平衡，或者可以选择不消耗
                    bool enqueued = rtsController.TryEnqueuePlayerAction(selectedPosition.Value, targetPosition, currentPlayer);
                    if (enqueued)
                    {
                        Debug.Log($"✅ [RTS] Move enqueued: {selectedPosition.Value}-{targetPosition}");
                        ClearSelection();
                        return;
                    }
                    else
                    {
                        Debug.LogWarning($"⛔ [RTS] Failed to enqueue move!");
                        return;
                    }
                }
            }

            // 非 RTS 模式：直接执行移动（原有逻辑）
            string moveString = $"{selectedPosition.Value}-{targetPosition}";
            Board moveBoard = gameManager.GetBoard();
            Piece targetPiece = moveBoard.GetPiece(targetPosition);
            if (targetPiece != null && targetPiece.Color != gameState.ActivePlayer.Color)
            {
                moveString = $"{selectedPosition.Value}x{targetPosition}";
            }

            Debug.Log($"   📝 MoveString: {moveString}");
            Debug.Log($"   📋 ValidMoves Count: {validMoves.Count}, Contains Target: {validMoves.Contains(moveString)}");
            
            for(int i = 0; i < Mathf.Min(5, validMoves.Count); i++)
            {
                Debug.Log($"      - ValidMove[{i}]: {validMoves[i]}");
            }

            if (!IsValidMove(moveString))
            {
                Debug.LogWarning($"   ❌ Move INVALID! Clearing selection.");
                ClearSelection();
                if (uiManager != null) uiManager.ShowMessage("无效的移动！");
                return;
            }

            Debug.Log($"   ✅ Move VALID! Executing...");
            ExecuteMove(moveString);
        }

        /// <summary>
        /// 检查移动是否合法
        /// </summary>
        private bool IsValidMove(string moveString)
        {
            // 方案 A：使用实时规则判定（推荐，更稳健）
            if (selectedPosition.HasValue)
            {
                Board board = gameManager.GetBoard();
                GameState gameState = gameManager.GetGameState();
                
                // 解析目标位置字符串 (例如 "a6-c6" 或 "a6xc6")
                string[] parts = moveString.Split(new char[] { '-', 'x' });
                if (parts.Length == 2)
                {
                    try 
                    {
                        // 假设 BoardPosition 有一个能从字符串解析的构造函数或静态方法
                        // 如果 BoardPosition 没有 Parse 方法，我们需要手动解析
                        string toStr = parts[1];
                        char col = toStr[0];
                        int row = int.Parse(toStr.Substring(1));
                        
                        BoardPosition toPos = new BoardPosition(col, row);
                        return GameRules.IsValidMove(selectedPosition.Value, toPos, board, gameState.ActivePlayer.Color);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to parse move string: {moveString}, Error: {e.Message}");
                    }
                }
            }

            // 方案 B：兜底，依然检查列表（防止解析失败）
            return validMoves.Contains(moveString);
        }

        /// <summary>
        /// 执行移动
        /// </summary>
        private void ExecuteMove(string moveString)
        {
            Debug.Log($"Executing move: {moveString}");

            MoveResult result = gameManager.MakeMove(moveString);

            if (result.Success)
            {
                // 播放移动音效
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayMoveSound();
                }

                // 如果有吃子，播放吃子音效
                if (result.CapturedPiece != null)
                {
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayCaptureSound();
                    }
                }

                // 🔑 关键：如果有路径信息，触发动画播放
                if (result.PathUsed != null && result.PathUsed.Count >= 2)
                {
                    Debug.Log($"🎬 [PieceSelection] Triggering animation for path with {result.PathUsed.Count} steps");
                    
                    // 通知 GameUIManager 播放动画
                    if (uiManager != null)
                    {
                        uiManager.HandleMoveExecuted(result);
                    }
                }
                else
                {
                    // 没有路径，直接更新UI
                    ClearSelection();
                    if (uiManager != null)
                    {
                        uiManager.UpdateUI();
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Move failed: {result.Message}");
                
                // 移动失败，清除选中并更新UI
                ClearSelection();
                if (uiManager != null)
                {
                    uiManager.UpdateUI();
                    uiManager.ShowMessage($"移动失败: {result.Message}");
                }
            }
        }

        /// <summary>
        /// 高亮显示合法移动位置
        /// </summary>
        private void HighlightValidMoves()
        {
            // TODO: 实现合法移动的高亮显示
            // 这需要访问棋盘格子的UI元素并改变它们的颜色
            Debug.Log($"Valid moves count: {validMoves.Count}");
        }

        /// <summary>
        /// 取消选择
        /// </summary>
        public void Deselect()
        {
            ClearSelection();
        }

        /// <summary>
        /// 获取当前选中的位置
        /// </summary>
        public BoardPosition? GetSelectedPosition()
        {
            return selectedPosition;
        }

        /// <summary>
        /// 检查是否有选中的棋子
        /// </summary>
        public bool HasSelection()
        {
            return selectedPosition.HasValue;
        }
    }
}

