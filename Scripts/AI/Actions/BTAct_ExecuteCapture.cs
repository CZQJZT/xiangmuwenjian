using System.Collections.Generic;
using JunqiGame.AI.BehaviorTree;
using JunqiGame.Core;
using JunqiGame.RTS;

namespace JunqiGame.AI.Actions
{
    public class BTAct_ExecuteCapture : BTAction
    {
        public BTAct_ExecuteCapture(string name) : base(name) { }

        public override bool DoAction(AIContext context)
        {
            List<(BoardPosition from, BoardPosition to, string moveStr, int score)> candidates =
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
                Piece defender = context.Board.GetPiece(toPos);

                if (attacker == null || defender == null)
                    continue;

                if (attacker.Color != context.AIColor)
                    continue;

                if (context.BusyPieceKeys != null && context.BusyPieceKeys.Contains(fromPos.ToString()))
                    continue;

                CombatResult result = GameRules.ResolveCombat(attacker, defender);

                if (result == CombatResult.AttackerWin)
                {
                    int score = context.GetPieceValue(defender) - context.GetPieceValue(attacker) / 2;
                    candidates.Add((fromPos, toPos, move, score));
                }
                else if (result == CombatResult.BothDie && context.IsExpendable(attacker) && context.IsHighValue(defender))
                {
                    int score = context.GetPieceValue(defender) - context.GetPieceValue(attacker);
                    candidates.Add((fromPos, toPos, move, score));
                }
            }

            if (candidates.Count == 0)
                return false;

            candidates.Sort((a, b) => b.score.CompareTo(a.score));

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
