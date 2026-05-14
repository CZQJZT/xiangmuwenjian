// 新建文件：Scripts/Core/BoardUtils.cs
using UnityEngine;

namespace JunqiGame.Core
{
    /// <summary>
    /// 棋盘工具类 - 提供坐标映射等功能
    /// </summary>
    public static class BoardUtils
    {
        /// <summary>
        /// 将棋盘位置转换为世界坐标
        /// </summary>
        public static Vector3 ToWorldPosition(BoardPosition pos, GameObject[,] boardCells)
        {
            if (!pos.IsValid() || boardCells == null)
                return Vector3.zero;

            int col = pos.Column - 'a';
            int row = pos.Row - 1;

            if (col < 0 || col >= 5 || row < 0 || row >= 13)
                return Vector3.zero;

            GameObject cell = boardCells[col, row];
            if (cell == null)
                return Vector3.zero;

            return cell.transform.position;
        }

        /// <summary>
        /// 将路径转换为世界坐标数组
        /// </summary>
        public static Vector3[] PathToWorldPositions(System.Collections.Generic.List<BoardPosition> path, GameObject[,] boardCells)
        {
            if (path == null || path.Count == 0)
                return new Vector3[0];

            Vector3[] positions = new Vector3[path.Count];
            for (int i = 0; i < path.Count; i++)
            {
                positions[i] = ToWorldPosition(path[i], boardCells);
            }
            return positions;
        }

        /// <summary>
        /// 从世界坐标反推棋盘位置（近似）
        /// </summary>
        public static BoardPosition FromWorldPosition(Vector3 worldPos, GameObject[,] boardCells)
        {
            if (boardCells == null)
                return new BoardPosition('\0', 0);

            float minDistance = float.MaxValue;
            BoardPosition closestPos = new BoardPosition('a', 1);

            for (int col = 0; col < 5; col++)
            {
                for (int row = 0; row < 13; row++)
                {
                    GameObject cell = boardCells[col, row];
                    if (cell == null)
                        continue;

                    float distance = Vector3.Distance(worldPos, cell.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestPos = new BoardPosition((char)('a' + col), row + 1);
                    }
                }
            }

            // 如果距离太远，认为无效
            if (minDistance > 1.0f)
                return new BoardPosition('\0', 0);

            return closestPos;
        }
    }
}