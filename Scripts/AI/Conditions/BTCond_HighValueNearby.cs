using System.Collections.Generic;
using JunqiGame.AI.BehaviorTree;
using JunqiGame.Core;

namespace JunqiGame.AI.Conditions
{
    public class BTCond_HighValueNearby : BTCondition
    {
        private int searchRadius;

        public BTCond_HighValueNearby(string name, int searchRadius = 5) : base(name)
        {
            this.searchRadius = searchRadius;
        }

        public override bool Check(AIContext context)
        {
            var allies = context.GetAllyPieces();
            var enemies = context.GetEnemyPieces();

            foreach (var allyKvp in allies)
            {
                if (allyKvp.Value.Rank != PieceRank.Bomb)
                    continue;

                foreach (var enemyKvp in enemies)
                {
                    if (!context.IsHighValue(enemyKvp.Value))
                        continue;

                    int dist = context.ManhattanDistance(allyKvp.Key, enemyKvp.Key);
                    if (dist <= searchRadius)
                        return true;
                }
            }
            return false;
        }
    }
}
