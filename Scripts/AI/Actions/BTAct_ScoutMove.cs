using System.Collections.Generic;
using JunqiGame.AI.BehaviorTree;
using JunqiGame.Core;
using JunqiGame.RTS;

namespace JunqiGame.AI.Actions
{
    public class BTAct_ScoutMove : BTAction
    {
        public BTAct_ScoutMove(string name) : base(name) { }

        public override bool DoAction(AIContext context)
        {
            BoardPosition enemyFlagEst = context.GetEstimatedEnemyFlagPos();

            List<(BoardPosition from, BoardPosition to, string moveStr, int distReduction, bool isRailway)> candidates =
                new List<(BoardPosition, BoardPosition, string, int, bool)>();

            foreach (string move in context.ValidMoves)
            {
                string[] parts = move.Split(new char[] { '-', 'x' });
                if (parts.Length != 2) continue;

                BoardPosition fromPos = BoardPosition.FromString(parts[0]);
                BoardPosition toPos = BoardPosition.FromString(parts[1]);

                Piece piece = context.Board.GetPiece(fromPos);
                if (piece == null || piece.Color != context.AIColor)
                    continue;

                if (!context.IsExpendable(piece))
                    continue;

                if (context.BusyPieceKeys != null && context.BusyPieceKeys.Contains(fromPos.ToString()))
                    continue;

                int currentDist = context.ManhattanDistance(fromPos, enemyFlagEst);
                int newDist = context.ManhattanDistance(toPos, enemyFlagEst);
                int distReduction = currentDist - newDist;

                bool isRailway = Board.IsRailway(fromPos) && Board.IsRailway(toPos);

                candidates.Add((fromPos, toPos, move, distReduction, isRailway));
            }

            if (candidates.Count == 0)
                return false;

            candidates.Sort((a, b) =>
            {
                if (a.isRailway != b.isRailway)
                    return b.isRailway.CompareTo(a.isRailway);
                return b.distReduction.CompareTo(a.distReduction);
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
