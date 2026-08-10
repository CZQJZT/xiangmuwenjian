using System.Collections.Generic;
using JunqiGame.AI.BehaviorTree;
using JunqiGame.Core;
using JunqiGame.RTS;

namespace JunqiGame.AI.Actions
{
    public class BTAct_AdvanceTowardFlag : BTAction
    {
        public BTAct_AdvanceTowardFlag(string name) : base(name) { }

        public override bool DoAction(AIContext context)
        {
            BoardPosition enemyFlagEst = context.GetEstimatedEnemyFlagPos();

            List<(BoardPosition from, BoardPosition to, string moveStr, int distReduction, int pieceValue)> candidates =
                new List<(BoardPosition, BoardPosition, string, int, int)>();

            foreach (string move in context.ValidMoves)
            {
                if (move.Contains("x"))
                    continue;

                string[] parts = move.Split('-');
                if (parts.Length != 2) continue;

                BoardPosition fromPos = BoardPosition.FromString(parts[0]);
                BoardPosition toPos = BoardPosition.FromString(parts[1]);

                Piece piece = context.Board.GetPiece(fromPos);
                if (piece == null || piece.Color != context.AIColor)
                    continue;

                if (piece.Rank == PieceRank.Bomb || piece.Rank == PieceRank.Sapper)
                    continue;

                if (context.BusyPieceKeys != null && context.BusyPieceKeys.Contains(fromPos.ToString()))
                    continue;

                int currentDist = context.ManhattanDistance(fromPos, enemyFlagEst);
                int newDist = context.ManhattanDistance(toPos, enemyFlagEst);
                int distReduction = currentDist - newDist;

                if (distReduction <= 0)
                    continue;

                bool underThreat = context.IsPositionUnderThreat(toPos, context.EnemyColor);
                int threatPenalty = underThreat ? context.GetPieceValue(piece) / 2 : 0;

                candidates.Add((fromPos, toPos, move, distReduction - threatPenalty, context.GetPieceValue(piece)));
            }

            if (candidates.Count == 0)
                return false;

            candidates.Sort((a, b) =>
            {
                int distComp = b.distReduction.CompareTo(a.distReduction);
                if (distComp != 0) return distComp;
                return b.pieceValue.CompareTo(a.pieceValue);
            });

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
