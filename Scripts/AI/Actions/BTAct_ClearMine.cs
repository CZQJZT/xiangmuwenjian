using System.Collections.Generic;
using JunqiGame.AI.BehaviorTree;
using JunqiGame.Core;
using JunqiGame.RTS;

namespace JunqiGame.AI.Actions
{
    public class BTAct_ClearMine : BTAction
    {
        public BTAct_ClearMine(string name) : base(name) { }

        public override bool DoAction(AIContext context)
        {
            var enemies = context.GetEnemyPieces();
            List<BoardPosition> minePositions = new List<BoardPosition>();

            foreach (var kvp in enemies)
            {
                if (kvp.Value.Rank == PieceRank.Mine)
                    minePositions.Add(kvp.Key);
            }

            if (context.PlayMode == PlayMode.Concealed && context.Difficulty != AIDifficulty.Cheating)
            {
                foreach (var kvp in enemies)
                {
                    if (!kvp.Value.CanMove() && kvp.Value.Rank != PieceRank.Flag)
                    {
                        if (!minePositions.Contains(kvp.Key))
                            minePositions.Add(kvp.Key);
                    }
                }
            }

            if (minePositions.Count == 0)
                return false;

            List<(BoardPosition from, BoardPosition to, string moveStr, int dist)> candidates =
                new List<(BoardPosition, BoardPosition, string, int)>();

            foreach (string move in context.ValidMoves)
            {
                string[] parts = move.Split(new char[] { '-', 'x' });
                if (parts.Length != 2) continue;

                BoardPosition fromPos = BoardPosition.FromString(parts[0]);
                BoardPosition toPos = BoardPosition.FromString(parts[1]);

                Piece piece = context.Board.GetPiece(fromPos);
                if (piece == null || piece.Rank != PieceRank.Sapper)
                    continue;

                if (context.BusyPieceKeys != null && context.BusyPieceKeys.Contains(fromPos.ToString()))
                    continue;

                foreach (var minePos in minePositions)
                {
                    int dist = context.ManhattanDistance(toPos, minePos);
                    candidates.Add((fromPos, toPos, move, dist));
                }
            }

            if (candidates.Count == 0)
                return false;

            candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

            var chosen = candidates[0];
            context.SelectedAction = new RTSMoveAction
            {
                FromPos = chosen.from,
                ToPos = chosen.to,
                MoveString = chosen.moveStr,
                Player = context.AIColor
            };
            return true;
        }
    }
}
