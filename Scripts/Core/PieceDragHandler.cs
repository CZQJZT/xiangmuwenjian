
using UnityEngine;
using UnityEngine.EventSystems;
using JunqiGame.Core;
using JunqiGame.MonoBehaviours;

namespace JunqiGame.UI
{
    /// <summary>
    /// 棋子拖动处理器 - 处理布阵阶段的棋子拖动
    /// </summary>
    public class PieceDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector3 originalPosition;
        private Transform originalParent;
        
        private GameUIManager uiManager;
        private JunqiGameManager gameManager;
        
        // 当前棋子代表的棋盘位置
        private BoardPosition boardPosition;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            uiManager = FindObjectOfType<GameUIManager>();
            gameManager = JunqiGameManager.Instance;
        }
        
        /// <summary>
        /// 初始化棋子拖动器
        /// </summary>
        public void Initialize(BoardPosition position)
        {
            boardPosition = position;
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (gameManager == null) return;
            
            GameState gameState = gameManager.GetGameState();
            if (gameState == null || gameState.Status != GameStatus.Setup)
            {
                // 只有在布阵阶段才能拖动
                return;
            }
            
            // 记录原始位置和父对象
            originalPosition = rectTransform.anchoredPosition;
            originalParent = transform.parent;
            
            // 设置为可穿透，方便检测下方的格子
            canvasGroup.blocksRaycasts = false;
            
            // 将棋子提升到最高层级
            transform.SetAsLastSibling();
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            // 跟随鼠标移动
            rectTransform.anchoredPosition += eventData.delta;
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            if (gameManager == null)
            {
                ResetPosition();
                return;
            }
            
            GameState gameState = gameManager.GetGameState();
            if (gameState == null || gameState.Status != GameStatus.Setup)
            {
                ResetPosition();
                return;
            }
            
            // 恢复射线检测
            canvasGroup.blocksRaycasts = true;
            
            // 检测鼠标下方的格子
            BoardPosition? targetPosition = GetTargetBoardPosition(eventData);
            
            if (targetPosition.HasValue)
            {
                // 尝试移动棋子到新位置
                bool success = TryMovePiece(boardPosition, targetPosition.Value);
                
                if (success)
                {
                    // 移动成功，销毁旧的棋子对象，UI会在UpdateBoardPieces时重新创建
                    Destroy(gameObject);
                    return;
                }
            }
            
            // 移动失败或无效，恢复原位
            ResetPosition();
        }
        
        /// <summary>
        /// 获取鼠标下方的棋盘位置
        /// </summary>
        private BoardPosition? GetTargetBoardPosition(PointerEventData eventData)
        {
            if (uiManager == null) return null;
            
            // 使用EventSystem的射线检测
            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            
            foreach (var result in raycastResults)
            {
                // 查找BoardCellClickHandler组件
                BoardCellClickHandler cellHandler = result.gameObject.GetComponent<BoardCellClickHandler>();
                if (cellHandler != null)
                {
                    // 通过反射或公开字段获取行列信息
                    // 这里我们需要修改BoardCellClickHandler来暴露这些信息
                    return GetBoardPositionFromCell(result.gameObject);
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 从格子GameObject获取棋盘位置
        /// </summary>
        private BoardPosition? GetBoardPositionFromCell(GameObject cellObj)
        {
            if (cellObj == null || uiManager == null) return null;
            
            // 遍历 boardCells 数组查找匹配的格子
            for (int col = 0; col < 5; col++)
            {
                for (int row = 0; row < 13; row++)
                {
                    if (uiManager.boardCells[col, row] == cellObj)
                    {
                        char columnChar = (char)('a' + col);
                        int rowNum = row + 1;
                        return new BoardPosition(columnChar, rowNum);
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 尝试移动棋子
        /// </summary>
        private bool TryMovePiece(BoardPosition fromPos, BoardPosition toPos)
        {
            if (gameManager == null) return false;
            
            Board board = gameManager.GetBoard();
            if (board == null) return false;
            
            // 检查目标位置是否在己方区域
            GameState gameState = gameManager.GetGameState();
            if (gameState == null) return false;
            
            PlayerColor playerColor = gameState.ActivePlayer?.Color ?? PlayerColor.Blue;
            int minRow = playerColor == PlayerColor.Blue ? 1 : 8;
            int maxRow = playerColor == PlayerColor.Blue ? 6 : 13;
            
            if (toPos.Row < minRow || toPos.Row > maxRow)
            {
                Debug.LogWarning("不能将棋子移动到对方区域");
                return false;
            }
            
            // 检查目标位置是否是行营
            if (Board.IsCamp(toPos))
            {
                Debug.LogWarning("不能在行营中放置棋子");
                return false;
            }
            
            // 检查目标位置是否有其他棋子
            Piece targetPiece = board.GetPiece(toPos);
            if (targetPiece != null)
            {
                // 如果目标位置有自己的棋子，交换位置
                if (targetPiece.Color == playerColor)
                {
                    // 交换两个位置的棋子
                    Piece fromPiece = board.GetPiece(fromPos);
                    if (fromPiece != null)
                    {
                        board.PlacePiece(toPos, fromPiece);
                        board.PlacePiece(fromPos, targetPiece);
                        
                        Debug.Log($"交换棋子：{fromPos} <-> {toPos}");
                        
                        // 刷新棋盘显示
                        uiManager.UpdateBoardPieces();
                        return true;
                    }
                }
                else
                {
                    Debug.LogWarning("目标位置有敌方棋子");
                    return false;
                }
            }
            else
            {
                // 目标位置为空，直接移动
                Piece fromPiece = board.GetPiece(fromPos);
                if (fromPiece != null)
                {
                    board.RemovePiece(fromPos);
                    board.PlacePiece(toPos, fromPiece);
                    
                    Debug.Log($"移动棋子：{fromPos} -> {toPos}");
                    
                    // 刷新棋盘显示
                    uiManager.UpdateBoardPieces();
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 恢复原始位置
        /// </summary>
        private void ResetPosition()
        {
            rectTransform.anchoredPosition = originalPosition;
            canvasGroup.blocksRaycasts = true;
        }
    }
}