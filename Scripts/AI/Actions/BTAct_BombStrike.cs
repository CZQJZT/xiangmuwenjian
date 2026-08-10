using System.Collections.Generic;
using JunqiGame.AI.BehaviorTree;
using JunqiGame.Core;
using JunqiGame.RTS;

namespace JunqiGame.AI.Actions
{
    public class BTAct_BombStrike : BTAction
    {
        public BTAct_BombStrike(string name) : base(name) { }

        public override bool DoAction(AIContext context)
        {
            var enemies = context.GetEnemyPieces();
            List<(BoardPosition from, BoardPosition to, string moveStr, int targetValue)> candidates =
                new List<(BoardPosition, BoardPosition, string, int)>();

            foreach (string move in context.ValidMoves)
            {
                if (!move.Contains("x"))
                    continue;

                string[] parts = move.Split(new char[] { '-', 'x' });
                if (parts.Length != 2) continue;

                BoardPosition fromPos = BoardPosition.FromString(parts[0]);
                BoardPosition toPos = BoardPosition.FromString(parts[1]);

                Piece attacker = context.Board.GetPiece(fromPos);
                if (attacker == null || attacker.Rank != PieceRank.Bomb)
                    continue;

                if (context.BusyPieceKeys != null && context.BusyPieceKeys.Contains(fromPos.ToString()))
                    continue;

                Piece defender = context.Board.GetPiece(toPos);
                if (defender == null || defender.Color == context.AIColor)
                    continue;

                if (context.CanSeeEnemyPiece(defender))
                {
                    if (context.IsHighValue(defender))
                    {
                        candidates.Add((fromPos, toPos, move, context.GetPieceValue(defender)));
                    }
                }
                else
                {
                    int estRow = context.EnemyColor == PlayerColor.Red ? 8 : 6;
                    if ((context.EnemyColor == PlayerColor.Red && toPos.Row <= estRow)
                        || (context.EnemyColor == PlayerColor.Blue && toPos.Row >= estRow))
                    {
                        candidates.Add((fromPos, toPos, move, 60));
                    }
                }
            }

            if (candidates.Count == 0)
                return false;

            candidates.Sort((a, b) => b.targetValue.CompareTo(a.targetValue));

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
