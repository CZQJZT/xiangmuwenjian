using System.Collections.Generic;
using JunqiGame.Core;
using JunqiGame.RTS;

namespace JunqiGame.AI
{
    public class AIContext
    {
        public Board Board;
        public PlayerColor AIColor;
        public float CurrentAP;
        public float APMax;
        public List<string> ValidMoves;
        public AIDifficulty Difficulty;
        public PlayMode PlayMode;
        public HashSet<string> BusyPieceKeys;

        public RTSMoveAction SelectedAction;

        private List<KeyValuePair<BoardPosition, Piece>> enemyPiecesCache;
        private List<KeyValuePair<BoardPosition, Piece>> allyPiecesCache;
        private BoardPosition? estimatedEnemyFlagPos;
        private BoardPosition? ownFlagPos;
        private Dictionary<PieceRank, int> pieceValueCache;

        public PlayerColor EnemyColor => AIColor == PlayerColor.Blue ? PlayerColor.Red : PlayerColor.Blue;

        public List<KeyValuePair<BoardPosition, Piece>> GetEnemyPieces()
        {
            if (enemyPiecesCache != null)
                return enemyPiecesCache;

            enemyPiecesCache = new List<KeyValuePair<BoardPosition, Piece>>();
            var positions = Board.GetPiecesByColor(EnemyColor);
            foreach (var pos in positions)
            {
                Piece piece = Board.GetPiece(pos);
                if (piece != null)
                    enemyPiecesCache.Add(new KeyValuePair<BoardPosition, Piece>(pos, piece));
            }
            return enemyPiecesCache;
        }

        public List<KeyValuePair<BoardPosition, Piece>> GetAllyPieces()
        {
            if (allyPiecesCache != null)
                return allyPiecesCache;

            allyPiecesCache = new List<KeyValuePair<BoardPosition, Piece>>();
            var positions = Board.GetPiecesByColor(AIColor);
            foreach (var pos in positions)
            {
                Piece piece = Board.GetPiece(pos);
                if (piece != null)
                    allyPiecesCache.Add(new KeyValuePair<BoardPosition, Piece>(pos, piece));
            }
            return allyPiecesCache;
        }

        public BoardPosition GetOwnFlagPosition()
        {
            if (ownFlagPos.HasValue)
                return ownFlagPos.Value;

            var allies = GetAllyPieces();
            foreach (var kvp in allies)
            {
                if (kvp.Value.Rank == PieceRank.Flag)
                {
                    ownFlagPos = kvp.Key;
                    return kvp.Key;
                }
            }
            return new BoardPosition('b', AIColor == PlayerColor.Red ? 13 : 1);
        }

        public BoardPosition GetEstimatedEnemyFlagPos()
        {
            if (estimatedEnemyFlagPos.HasValue)
                return estimatedEnemyFlagPos.Value;

            var enemies = GetEnemyPieces();
            foreach (var kvp in enemies)
            {
                if (kvp.Value.Rank == PieceRank.Flag)
                {
                    estimatedEnemyFlagPos = kvp.Key;
                    return kvp.Key;
                }
            }

            if (Difficulty == AIDifficulty.Cheating)
            {
                foreach (var kvp in enemies)
                {
                    if (kvp.Value.Rank == PieceRank.Flag)
                    {
                        estimatedEnemyFlagPos = kvp.Key;
                        return kvp.Key;
                    }
                }
            }

            int estRow = EnemyColor == PlayerColor.Red ? 13 : 1;
            estimatedEnemyFlagPos = new BoardPosition('b', estRow);
            return estimatedEnemyFlagPos.Value;
        }

        public int GetPieceValue(PieceRank rank)
        {
            if (pieceValueCache == null)
            {
                pieceValueCache = new Dictionary<PieceRank, int>
                {
                    { PieceRank.Flag, 1000 },
                    { PieceRank.Marshal, 100 },
                    { PieceRank.General, 90 },
                    { PieceRank.MajorGeneral, 70 },
                    { PieceRank.Brigadier, 60 },
                    { PieceRank.Colonel, 50 },
                    { PieceRank.Major, 40 },
                    { PieceRank.Captain, 30 },
                    { PieceRank.Lieutenant, 20 },
                    { PieceRank.Sapper, 35 },
                    { PieceRank.Bomb, 80 },
                    { PieceRank.Mine, 45 }
                };
            }
            return pieceValueCache.TryGetValue(rank, out int val) ? val : 10;
        }

        public int GetPieceValue(Piece piece)
        {
            return piece != null ? GetPieceValue(piece.Rank) : 0;
        }

        public bool IsExpendable(Piece piece)
        {
            return piece.Rank == PieceRank.Lieutenant
                || piece.Rank == PieceRank.Captain
                || piece.Rank == PieceRank.Major;
        }

        public bool IsHighValue(Piece piece)
        {
            return piece.Rank == PieceRank.Marshal
                || piece.Rank == PieceRank.General
                || piece.Rank == PieceRank.MajorGeneral
                || piece.Rank == PieceRank.Brigadier;
        }

        public int ManhattanDistance(BoardPosition a, BoardPosition b)
        {
            return System.Math.Abs(a.Column - b.Column) + System.Math.Abs(a.Row - b.Row);
        }

        public bool CanSeeEnemyPiece(Piece enemyPiece)
        {
            if (Difficulty == AIDifficulty.Cheating)
                return true;
            if (PlayMode == PlayMode.Revealed)
                return true;
            return false;
        }

        public Piece TryIdentifyEnemy(BoardPosition pos)
        {
            Piece piece = Board.GetPiece(pos);
            if (piece == null || piece.Color != EnemyColor)
                return null;

            if (CanSeeEnemyPiece(piece))
                return piece;

            return null;
        }

        public bool IsPositionUnderThreat(BoardPosition pos, PlayerColor fromColor)
        {
            var adjBuffer = new BoardPosition[4];
            int adjCount = pos.GetAdjacentPositions(adjBuffer);
            for (int i = 0; i < adjCount; i++)
            {
                Piece neighbor = Board.GetPiece(adjBuffer[i]);
                if (neighbor != null && neighbor.Color == fromColor && neighbor.CanMove())
                    return true;
            }
            return false;
        }

        public void Reset()
        {
            enemyPiecesCache = null;
            allyPiecesCache = null;
            estimatedEnemyFlagPos = null;
            ownFlagPos = null;
            SelectedAction = null;
        }
    }
}
