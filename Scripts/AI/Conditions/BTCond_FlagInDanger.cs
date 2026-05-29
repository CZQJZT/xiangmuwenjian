using JunqiGame.AI.BehaviorTree;
using JunqiGame.Core;

namespace JunqiGame.AI.Conditions
{
    public class BTCond_FlagInDanger : BTCondition
    {
        private int threatRadius;

        public BTCond_FlagInDanger(string name, int threatRadius = 3) : base(name)
        {
            this.threatRadius = threatRadius;
        }

        public override bool Check(AIContext context)
        {
            BoardPosition flagPos = context.GetOwnFlagPosition();
            var enemies = context.GetEnemyPieces();

            foreach (var kvp in enemies)
            {
                if (!kvp.Value.CanMove())
                    continue;

                int dist = context.ManhattanDistance(kvp.Key, flagPos);
                if (dist <= threatRadius)
                    return true;
            }
            return false;
        }
    }
}
