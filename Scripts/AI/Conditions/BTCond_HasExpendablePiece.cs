using System.Collections.Generic;
using JunqiGame.AI.BehaviorTree;
using JunqiGame.Core;

namespace JunqiGame.AI.Conditions
{
    public class BTCond_HasExpendablePiece : BTCondition
    {
        public BTCond_HasExpendablePiece(string name) : base(name) { }

        public override bool Check(AIContext context)
        {
            var allies = context.GetAllyPieces();
            foreach (var kvp in allies)
            {
                if (!kvp.Value.CanMove())
                    continue;
                if (!context.IsExpendable(kvp.Value))
                    continue;
                if (context.BusyPieceKeys != null && context.BusyPieceKeys.Contains(kvp.Key.ToString()))
                    continue;
                return true;
            }
            return false;
        }
    }
}
