// 新建文件：Scripts/Core/PathFinder.cs
using System.Collections.Generic;
using UnityEngine;

namespace JunqiGame.Core
{
    /// <summary>
    /// 路径查找器 - 专门处理铁路网路径搜索
    /// </summary>
    public static class PathFinder
    {
        private static readonly List<BoardPosition> CachedRailwayPositions = new List<BoardPosition>();
        private static bool railwayCacheBuilt = false;
        /// <summary>
        /// 获取所有铁路位置（缓存，避免每次分配）
        /// </summary>
        public static List<BoardPosition> GetAllRailwayPositions()
        {
            if (!railwayCacheBuilt)
            {
                CachedRailwayPositions.Clear();
                char[] cols = { 'a', 'b', 'c', 'd', 'e' };
                for (int r = 1; r <= 13; r++)
                {
                    foreach (char c in cols)
                    {
                        BoardPosition pos = new BoardPosition(c, r);
                        if (pos.IsValid() && Board.IsRailway(pos))
                            CachedRailwayPositions.Add(pos);
                    }
                }
                railwayCacheBuilt = true;
            }
            return CachedRailwayPositions;
        }

        /// <summary>
        /// 重置铁路缓存（Board布局改变时调用）
        /// </summary>
        public static void ResetRailwayCache()
        {
            railwayCacheBuilt = false;
            CachedRailwayPositions.Clear();
        }

        /// <summary>
        /// 查找从 from 出发所有可达的铁路位置（单次BFS，替代N*M次IsValidMove）
        /// </summary>
        public static List<BoardPosition> FindAllReachablePositions(Board board, BoardPosition from, Piece mover)
        {
            if (!from.IsValid() || !Board.IsRailway(from)) return null;

            var result = new List<BoardPosition>();
            var visited = new HashSet<BoardPosition>();
            var queue = new Queue<BoardPosition>();
            var adjBuffer = new BoardPosition[4];

            queue.Enqueue(from);
            visited.Add(from);

            while (queue.Count > 0)
            {
                BoardPosition current = queue.Dequeue();

                if (!current.Equals(from))
                    result.Add(current);

                int adjCount = current.GetAdjacentPositions(adjBuffer);
                for (int i = 0; i < adjCount; i++)
                {
                    BoardPosition next = adjBuffer[i];
                    if (!visited.Add(next)) continue;
                    if (!Board.IsRailway(next)) continue;
                    if (current.Row == 7 && next.Row == 7) continue;
                    if ((next.Column == 'b' || next.Column == 'd') && next.Row == 7) continue;

                    if (!board.IsEmpty(next))
                    {
                        Piece blocker = board.GetPiece(next);
                        if (blocker != null && mover != null && blocker.Color != mover.Color)
                            result.Add(next);
                        continue;
                    }

                    result.Add(next);
                    queue.Enqueue(next);
                }
            }

            return result.Count > 0 ? result : null;
        }

        /// <summary>
        /// 查找从 from 到 to 的路径（优先铁路网）
        /// </summary>
        /// <param name="board">棋盘</param>
        /// <param name="from">起点</param>
        /// <param name="to">终点</param>
        /// <param name="mover">移动的棋子</param>
        /// <returns>路径列表，如果找不到返回 null</returns>
        public static List<BoardPosition> FindPath(Board board, BoardPosition from, BoardPosition to, Piece mover)
        {
            if (!from.IsValid() || !to.IsValid())
            {
#if UNITY_EDITOR
                Debug.LogWarning($"⚠️ [PathFinder] Invalid positions: from={from}, to={to}");
#endif
                return null;
            }

            if (from.Equals(to))
                return new List<BoardPosition> { from };

            bool fromIsRailway = Board.IsRailway(from);
            bool toIsRailway = Board.IsRailway(to);

            if (!fromIsRailway || !toIsRailway)
                return null;

            if (mover != null && mover.IsSapper())
            {
#if UNITY_EDITOR
                Debug.Log($"🔧 [PathFinder] Sapper detected, using BFS");
#endif
                return FindSapperPath(board, from, to);
            }

#if UNITY_EDITOR
            Debug.Log($"🚂 [PathFinder] Normal piece, checking straight line");
#endif
            return FindStraightRailwayPath(board, from, to);
        }

        private static List<BoardPosition> FindSapperPath(Board board, BoardPosition from, BoardPosition to)
        {
            var visited = new HashSet<BoardPosition>();
            var queue = new Queue<BoardPosition>();
            var parent = new Dictionary<BoardPosition, BoardPosition>();
            var adjBuffer = new BoardPosition[4];

            queue.Enqueue(from);
            visited.Add(from);

            while (queue.Count > 0)
            {
                BoardPosition current = queue.Dequeue();

                if (current.Equals(to))
                {
                    var path = new List<BoardPosition>();
                    BoardPosition step = to;
                    path.Add(step);
                    while (parent.TryGetValue(step, out step))
                        path.Add(step);
                    path.Reverse();
                    return path;
                }

                int adjCount = current.GetAdjacentPositions(adjBuffer);
                for (int i = 0; i < adjCount; i++)
                {
                    BoardPosition next = adjBuffer[i];

                    if (!visited.Add(next))
                        continue;

                    if (!Board.IsRailway(next))
                        continue;

                    if (current.Row == 7 && next.Row == 7)
                        continue;

                    if ((next.Column == 'b' || next.Column == 'd') && next.Row == 7)
                        continue;

                    if (!next.Equals(to) && !board.IsEmpty(next))
                        continue;

                    parent[next] = current;
                    queue.Enqueue(next);
                }
            }

            return null;
        }

        private static List<BoardPosition> FindStraightRailwayPath(Board board, BoardPosition from, BoardPosition to)
        {
            if (from.Row != to.Row && from.Column != to.Column)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"❌ [PathFinder] Not on same line: {from} -> {to}");
#endif
                return null;
            }

            if ((from.Row == 7 || to.Row == 7) && from.Column != to.Column)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"❌ [PathFinder] Horizontal move on row 7 blocked");
#endif
                return null;
            }

            if ((from.Column == 'b' || from.Column == 'd') && from.Column == to.Column)
            {
                if ((from.Row < 7 && to.Row > 7) || (from.Row > 7 && to.Row < 7))
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"❌ [PathFinder] Crossing row 7 on column {from.Column} blocked");
#endif
                    return null;
                }
            }

            var path = new List<BoardPosition>();
            int colStep = 0;
            int rowStep = 0;

            if (from.Column != to.Column)
                colStep = (to.Column > from.Column) ? 1 : -1;
            else if (from.Row != to.Row)
                rowStep = (to.Row > from.Row) ? 1 : -1;

            BoardPosition current = from;
            path.Add(current);

            while (!current.Equals(to))
            {
                current = new BoardPosition((char)(current.Column + colStep), current.Row + rowStep);

                if (!Board.IsRailway(current))
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"❌ [PathFinder] {current} is not railway");
#endif
                    return null;
                }

                if (!current.Equals(to) && !board.IsEmpty(current))
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"🚧 [PathFinder] Blocked at {current}");
#endif
                    return null;
                }

                path.Add(current);
            }

#if UNITY_EDITOR
            Debug.Log($"✅ [PathFinder] Straight path found with {path.Count} steps");
#endif
            return path;
        }
    }
}