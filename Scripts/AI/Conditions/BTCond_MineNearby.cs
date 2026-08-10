using System.Collections.Generic;
using JunqiGame.AI.BehaviorTree;
using JunqiGame.Core;

namespace JunqiGame.AI.Conditions
{
    public class BTCond_MineNearby : BTCondition
    {
        private int searchRadius;

        public BTCond_MineNearby(string name, int searchRadius = 6) : base(name)
        {
            this.searchRadius = searchRadius;
        }

        public override bool Check(AIContext context)
        {
            var allies = context.GetAllyPieces();
            var enemies = context.GetEnemyPieces();

            BoardPosition? sapperPos = null;
            foreach (var kvp in allies)
            {
                if (kvp.Value.Rank == PieceRank.Sapper)
                {
                    if (context.BusyPieceKeys == null || !context.BusyPieceKeys.Contains(kvp.Key.ToString()))
                    {
                        sapperPos = kvp.Key;
                        break;
                    }
                }
            }

            if (!sapperPos.HasValue)
                return false;

            foreach (var kvp in enemies)
            {
                if (kvp.Value.Rank == PieceRank.Mine)
                {
                    int dist = context.ManhattanDistance(sapperPos.Value, kvp.Key);
                    if (dist <= searchRadius)
                        return true;
                }
            }

            if (context.PlayMode == PlayMode.Concealed && context.Difficulty != AIDifficulty.Cheating)
            {
                foreach (var kvp in enemies)
                {
                    if (!kvp.Value.CanMove())
                    {
                        int dist = context.ManhattanDistance(sapperPos.Value, kvp.Key);
                        if (dist <= searchRadius)
                            return true;
                    }
                }
            }

            return false;
        }
    }
}
