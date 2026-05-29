using System.Collections.Generic;
using JunqiGame.AI.BehaviorTree;
using JunqiGame.Core;

namespace JunqiGame.AI.Conditions
{
    public class BTCond_HasFavorableCombat : BTCondition
    {
        public BTCond_HasFavorableCombat(string name) : base(name) { }

        public override bool Check(AIContext context)
        {
            if (context.ValidMoves == null || context.ValidMoves.Count == 0)
                return false;

            foreach (string move in context.ValidMoves)
            {
                if (!move.Contains("x"))
                    continue;

                string[] parts = move.Split(new char[] { '-', 'x' });
                if (parts.Length != 2)
                    continue;

                BoardPosition fromPos = BoardPosition.FromString(parts[0]);
                BoardPosition toPos = BoardPosition.FromString(parts[1]);

                Piece attacker = context.Board.GetPiece(fromPos);
                Piece defender = context.Board.GetPiece(toPos);

                if (attacker == null || defender == null)
                    continue;

                if (attacker.Color != context.AIColor)
                    continue;

                CombatResult result = GameRules.ResolveCombat(attacker, defender);
                if (result == CombatResult.AttackerWin)
                    return true;
            }
            return false;
        }
    }
}
