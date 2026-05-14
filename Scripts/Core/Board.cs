using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunqiGame.Core
{
    /// <summary>
    /// 棋盘类 - 管理所有棋子的位置和状态
    /// 对应原始代码中的board.merge(), getStaticBoardState()等方法
    /// </summary>
    [Serializable]
    public class Board
    {
        // 棋盘大小：5列 x 13行 = 65个位置（第7行只有3个有效位置）
        public const int Columns = 5;
        public const int Rows = 13;
        public const int TotalPositions = 64; // 实际有效位置：5*12 + 3 = 63，但预留64

        // 双层存储：字典兼容旧代码 + 扁平数组实现O(1)快速查询
        [SerializeField]
        private Dictionary<BoardPosition, Piece> pieces = new Dictionary<BoardPosition, Piece>();
        private Piece[] piecesFlat = new Piece[Columns * Rows];

        private static int PosToIndex(BoardPosition pos)
        {
            return (pos.Row - 1) * Columns + (pos.Column - 'a');
        }

        private static readonly BoardPosition[] BlueCampPositions = new BoardPosition[]
        {
            new BoardPosition('b', 3), new BoardPosition('d', 3),
            new BoardPosition('c', 4),
            new BoardPosition('b', 5), new BoardPosition('d', 5)
        };
        
        private static readonly BoardPosition[] RedCampPositions = new BoardPosition[]
        {
            new BoardPosition('b', 9), new BoardPosition('d', 9),
            new BoardPosition('c', 10),
            new BoardPosition('b', 11), new BoardPosition('d', 11)
        };

        private static readonly HashSet<BoardPosition> BlueCamps = new HashSet<BoardPosition>(BlueCampPositions);
        private static readonly HashSet<BoardPosition> RedCamps = new HashSet<BoardPosition>(RedCampPositions);

        private static readonly int[] HorizontalRailways = { 2, 6, 8, 12, 7 };
        private static readonly char[] VerticalRailways = { 'a', 'e' };

        public Board()
        {
            Clear();
        }

        public static bool IsCamp(BoardPosition position)
        {
            return BlueCamps.Contains(position) || RedCamps.Contains(position);
        }

        public static bool IsRailway(BoardPosition position)
        {
            if (position.Column == 'c' && position.Row == 7)
                return true;
            if (Array.IndexOf(HorizontalRailways, position.Row) != -1)
                return true;
            if (Array.IndexOf(VerticalRailways, position.Column) != -1)
                return true;
            return false;
        }

        public static CellType GetCellType(BoardPosition position)
        {
            if (!position.IsValid())
                return CellType.Invalid;
            if (IsCamp(position))
                return CellType.Camp;
            if (IsRailway(position))
                return CellType.Railway;
            return CellType.Normal;
        }

        public void Clear()
        {
            pieces.Clear();
            System.Array.Clear(piecesFlat, 0, piecesFlat.Length);
        }

        public void Merge(Dictionary<string, Piece> pieceMap)
        {
            foreach (var kvp in pieceMap)
            {
                if (kvp.Value != null)
                {
                    var pos = BoardPosition.FromString(kvp.Key);
                    pieces[pos] = kvp.Value;
                    piecesFlat[PosToIndex(pos)] = kvp.Value;
                }
            }
        }

        public void PlacePiece(BoardPosition position, Piece piece)
        {
            pieces[position] = piece;
            piecesFlat[PosToIndex(position)] = piece;
        }

        public Piece RemovePiece(BoardPosition position)
        {
            if (pieces.TryGetValue(position, out Piece piece))
            {
                pieces.Remove(position);
                piecesFlat[PosToIndex(position)] = null;
                return piece;
            }
            return null;
        }

        /// <summary>
        /// 获取指定位置的棋子（扁平数组O(1)查询）
        /// </summary>
        public Piece GetPiece(BoardPosition position)
        {
            int idx = PosToIndex(position);
            if (idx < 0 || idx >= piecesFlat.Length) return null;
            return piecesFlat[idx];
        }

        public bool IsEmpty(BoardPosition position)
        {
            int idx = PosToIndex(position);
            return idx < 0 || idx >= piecesFlat.Length || piecesFlat[idx] == null;
        }

        public Piece MovePiece(BoardPosition from, BoardPosition to)
        {
            int fromIdx = PosToIndex(from);
            int toIdx = PosToIndex(to);
            if (fromIdx < 0 || fromIdx >= piecesFlat.Length) return null;

            Piece piece = piecesFlat[fromIdx];
            if (piece == null)
            {
                Debug.LogWarning($"No piece at position: {from}");
                return null;
            }

            Piece capturedPiece = piecesFlat[toIdx];
            piecesFlat[toIdx] = piece;
            piecesFlat[fromIdx] = null;
            pieces[to] = piece;
            pieces.Remove(from);
            return capturedPiece;
        }

        public Dictionary<string, Piece> GetStaticBoardState()
        {
            var state = new Dictionary<string, Piece>();
            for (int i = 0; i < piecesFlat.Length; i++)
            {
                if (piecesFlat[i] != null)
                {
                    int row = i / Columns + 1;
                    int col = i % Columns;
                    var pos = new BoardPosition((char)('a' + col), row);
                    state[pos.ToString()] = piecesFlat[i].Clone();
                }
            }
            return state;
        }

        public void LoadFromState(Dictionary<string, Piece> state)
        {
            Clear();
            foreach (var kvp in state)
            {
                var pos = BoardPosition.FromString(kvp.Key);
                Piece clone = kvp.Value.Clone();
                pieces[pos] = clone;
                piecesFlat[PosToIndex(pos)] = clone;
            }
        }

        public List<BoardPosition> GetAllOccupiedPositions()
        {
            var positions = new List<BoardPosition>(pieces.Count);
            for (int i = 0; i < piecesFlat.Length; i++)
            {
                if (piecesFlat[i] != null)
                {
                    int row = i / Columns + 1;
                    int col = i % Columns;
                    positions.Add(new BoardPosition((char)('a' + col), row));
                }
            }
            return positions;
        }

        public List<BoardPosition> GetPiecesByColor(PlayerColor color)
        {
            var positions = new List<BoardPosition>();
            for (int i = 0; i < piecesFlat.Length; i++)
            {
                if (piecesFlat[i] != null && piecesFlat[i].Color == color)
                {
                    int row = i / Columns + 1;
                    int col = i % Columns;
                    positions.Add(new BoardPosition((char)('a' + col), row));
                }
            }
            return positions;
        }

        public static bool IsCamp(BoardPosition position, PlayerColor? playerColor = null)
        {
            if (playerColor == PlayerColor.Blue)
                return BlueCamps.Contains(position);
            else if (playerColor == PlayerColor.Red)
                return RedCamps.Contains(position);
            else
                return BlueCamps.Contains(position) || RedCamps.Contains(position);
        }

        public static HashSet<BoardPosition> GetCamps(PlayerColor color)
        {
            return color == PlayerColor.Blue ? BlueCamps : RedCamps;
        }

        public int PieceCount => pieces.Count;

        /// <summary>
        /// 获取指定位置的棋子（索引器，字符串版，兼容旧代码）
        /// </summary>
        public Piece this[string positionStr]
        {
            get => GetPiece(BoardPosition.FromString(positionStr));
            set
            {
                var pos = BoardPosition.FromString(positionStr);
                pieces[pos] = value;
                piecesFlat[PosToIndex(pos)] = value;
            }
        }

        public Board Clone()
        {
            var newBoard = new Board();
            var state = GetStaticBoardState();
            foreach (var kvp in state)
            {
                var pos = BoardPosition.FromString(kvp.Key);
                newBoard.pieces[pos] = kvp.Value;
                newBoard.piecesFlat[PosToIndex(pos)] = kvp.Value;
            }
            return newBoard;
        }

        public static Vector3 PositionToWorldPosition(BoardPosition position, GameObject[,] boardCells)
        {
            return BoardUtils.ToWorldPosition(position, boardCells);
        }

        public static Vector3[] PathToWorldPositions(System.Collections.Generic.List<BoardPosition> path, GameObject[,] boardCells)
        {
            return BoardUtils.PathToWorldPositions(path, boardCells);
        }
    }
}
