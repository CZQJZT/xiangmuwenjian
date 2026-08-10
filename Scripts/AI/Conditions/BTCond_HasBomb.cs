using System.Collections.Generic;
using JunqiGame.AI.BehaviorTree;
using JunqiGame.Core;

namespace JunqiGame.AI.Conditions
{
    public class BTCond_HasBomb : BTCondition
    {
        public BTCond_HasBomb(string name) : base(name) { }

        public override bool Check(AIContext context)
        {
            var allies = context.GetAllyPieces();
            foreach (var kvp in allies)
            {
                if (kvp.Value.Rank == PieceRank.Bomb && kvp.Value.CanMove())
                {
                    if (context.BusyPieceKeys != null && context.BusyPieceKeys.Contains(kvp.Key.ToString()))
                        continue;
                    return true;
                }
            }
            return false;
        }
    }
}
