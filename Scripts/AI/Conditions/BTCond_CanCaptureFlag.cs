using System.Collections.Generic;
using JunqiGame.AI.BehaviorTree;
using JunqiGame.Core;

namespace JunqiGame.AI.Conditions
{
    public class BTCond_CanCaptureFlag : BTCondition
    {
        public BTCond_CanCaptureFlag(string name) : base(name) { }

        public override bool Check(AIContext context)
        {
            if (context.ValidMoves == null || context.ValidMoves.Count == 0)
                return false;

            var enemies = context.GetEnemyPieces();
            BoardPosition? flagPos = null;
            foreach (var kvp in enemies)
            {
                if (kvp.Value.Rank == PieceRank.Flag)
                {
                    flagPos = kvp.Key;
                    break;
                }
            }

            if (!flagPos.HasValue)
                return false;

            string flagPosStr = flagPos.Value.ToString();
            foreach (string move in context.ValidMoves)
            {
                if (move.Contains("x") && move.EndsWith(flagPosStr))
                    return true;
            }
            return false;
        }
    }
}
