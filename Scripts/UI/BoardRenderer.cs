using UnityEngine;
using JunqiGame.Core;

namespace JunqiGame.UI
{
    /// <summary>
    /// 棋盘渲染器
    /// 负责绘制棋盘的视觉效果（铁路、行营等）
    /// </summary>
    public class BoardRenderer : MonoBehaviour
    {
        [Header("棋盘设置")]
        [Tooltip("棋盘宽度")]
        public float boardWidth = 350f;
        
        [Tooltip("棋盘高度")]
        public float boardHeight = 780f;
        
        [Tooltip("格子大小")]
        public float cellSize = 60f;
        
        [Tooltip("格子间距")]
        public float cellSpacing = 5f;

        [Header("颜色设置")]
        [Tooltip("普通格子颜色")]
        public Color normalCellColor = new Color(0.9f, 0.85f, 0.75f, 1f);
        
        [Tooltip("铁路格子颜色")]
        public Color railwayCellColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        
        [Tooltip("行营格子颜色")]
        public Color campCellColor = new Color(0.6f, 0.8f, 0.6f, 1f);
        
        [Tooltip("蓝方区域背景色")]
        public Color blueAreaColor = new Color(0.2f, 0.3f, 0.5f, 0.1f);
        
        [Tooltip("红方区域背景色")]
        public Color redAreaColor = new Color(0.5f, 0.2f, 0.2f, 0.1f);

        [Header("线条设置")]
        [Tooltip("线条颜色")]
        public Color lineColor = Color.black;
        
        [Tooltip("线条宽度")]
        public float lineWidth = 2f;

        private void OnDrawGizmos()
        {
            DrawBoard();
        }

        /// <summary>
        /// 绘制棋盘
        /// </summary>
        private void DrawBoard()
        {
            Vector3 startPos = transform.position;
            
            // 绘制背景区域
            DrawAreaBackground(startPos);
            
            // 绘制格子
            for (int col = 0; col < 5; col++)
            {
                for (int row = 0; row < 12; row++)
                {
                    char columnChar = (char)('a' + col);
                    int rowNum = row + 1;
                    
                    BoardPosition pos = new BoardPosition(columnChar, rowNum);
                    Vector3 cellPos = GetCellPosition(startPos, col, row);
                    
                    // 确定格子类型和颜色
                    Color cellColor = GetCellColor(pos);
                    
                    // 绘制格子
                    Gizmos.color = cellColor;
                    Gizmos.DrawCube(cellPos, new Vector3(cellSize, cellSize, 1));
                    
                    // 绘制边框
                    Gizmos.color = lineColor;
                    Gizmos.DrawWireCube(cellPos, new Vector3(cellSize, cellSize, 1));
                }
            }
            
            // 绘制连接线
            DrawConnectionLines(startPos);
        }

        /// <summary>
        /// 绘制区域背景
        /// </summary>
        private void DrawAreaBackground(Vector3 startPos)
        {
            // 蓝方区域 (行1-6)
            Gizmos.color = blueAreaColor;
            Vector3 blueCenter = startPos + new Vector3(boardWidth / 2, boardHeight * 5/12, 0);
            Vector3 blueSize = new Vector3(boardWidth, boardHeight / 2, 1);
            Gizmos.DrawCube(blueCenter, blueSize);
            
            // 红方区域 (行7-12)
            Gizmos.color = redAreaColor;
            Vector3 redCenter = startPos + new Vector3(boardWidth / 2, boardHeight * 11/12, 0);
            Vector3 redSize = new Vector3(boardWidth, boardHeight / 2, 1);
            Gizmos.DrawCube(redCenter, redSize);
        }

        /// <summary>
        /// 获取格子颜色
        /// </summary>
        private Color GetCellColor(BoardPosition pos)
        {
            // 检查是否是行营
            if (Board.IsCamp(pos))
            {
                return campCellColor;
            }
            
            // 检查是否是铁路
            if (Board.IsRailway(pos))
            {
                return railwayCellColor;
            }
            
            return normalCellColor;
        }

        /// <summary>
        /// 获取格子位置
        /// </summary>
        private Vector3 GetCellPosition(Vector3 startPos, int col, int row)
        {
            float x = startPos.x + col * (cellSize + cellSpacing) + cellSize / 2;
            float y = startPos.y + row * (cellSize + cellSpacing) + cellSize / 2;
            return new Vector3(x, y, startPos.z);
        }

        /// <summary>
        /// 绘制连接线（显示可移动路径）
        /// </summary>
        private void DrawConnectionLines(Vector3 startPos)
        {
            Gizmos.color = lineColor;
            
            // 绘制横向连接
            for (int row = 0; row < 12; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    Vector3 from = GetCellPosition(startPos, col, row);
                    Vector3 to = GetCellPosition(startPos, col + 1, row);
                    Gizmos.DrawLine(from + Vector3.right * cellSize / 2, 
                                   to - Vector3.right * cellSize / 2);
                }
            }
            
            // 绘制纵向连接
            for (int col = 0; col < 5; col++)
            {
                for (int row = 0; row < 11; row++)
                {
                    Vector3 from = GetCellPosition(startPos, col, row);
                    Vector3 to = GetCellPosition(startPos, col, row + 1);
                    Gizmos.DrawLine(from + Vector3.up * cellSize / 2, 
                                   to - Vector3.up * cellSize / 2);
                }
            }
        }

        /// <summary>
        /// 在Scene视图中绘制标签
        /// </summary>
        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 startPos = transform.position;
            
            for (int col = 0; col < 5; col++)
            {
                for (int row = 0; row < 12; row++)
                {
                    char columnChar = (char)('a' + col);
                    int rowNum = row + 1;
                    
                    Vector3 cellPos = GetCellPosition(startPos, col, row);
                    UnityEditor.Handles.Label(cellPos, $"{columnChar}{rowNum}");
                }
            }
        }
        #endif
    }
}
