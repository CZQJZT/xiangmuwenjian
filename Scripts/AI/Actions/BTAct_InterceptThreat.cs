using System.Collections.Generic;
using JunqiGame.AI.BehaviorTree;
using JunqiGame.Core;
using JunqiGame.RTS;

namespace JunqiGame.AI.Actions
{
    public class BTAct_InterceptThreat : BTAction
    {
        public BTAct_InterceptThreat(string name) : base(name) { }

        public override bool DoAction(AIContext context)
        {
            BoardPosition flagPos = context.GetOwnFlagPosition();
            var enemies = context.GetEnemyPieces();

            BoardPosition? closestThreat = null;
            int closestDist = int.MaxValue;

            foreach (var kvp in enemies)
            {
                if (!kvp.Value.CanMove())
                    continue;
                int dist = context.ManhattanDistance(kvp.Key, flagPos);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestThreat = kvp.Key;
                }
            }

            if (!closestThreat.HasValue)
                return false;

            var allies = context.GetAllyPieces();
            List<(BoardPosition from, BoardPosition to, Piece piece, int value)> candidates =
                new List<(BoardPosition, BoardPosition, Piece, int)>();

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

                if (context.BusyPieceKeys != null && context.BusyPieceKeys.Contains(fromPos.ToString()))
                    continue;

                int distToThreat = context.ManhattanDistance(toPos, closestThreat.Value);
                int distToFlag = context.ManhattanDistance(toPos, flagPos);

                if (distToThreat <= 2 || distToFlag < closestDist)
                {
                    candidates.Add((fromPos, toPos, piece, context.GetPieceValue(piece)));
                }
            }

            if (candidates.Count == 0)
                return false;

            candidates.Sort((a, b) => a.value.CompareTo(b.value));

            var chosen = candidates[0];
            string moveStr = $"{chosen.from}-{chosen.to}";
            context.SelectedAction = new RTSMoveAction
            {
                FromPos = chosen.from,
                ToPos = chosen.to,
                MoveString = moveStr,
                Player = context.AIColor
            };
            return true;
        }
    }
}
