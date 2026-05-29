using System.Collections.Generic;
using JunqiGame.AI.BehaviorTree;
using JunqiGame.Core;
using JunqiGame.RTS;

namespace JunqiGame.AI.Actions
{
    public class BTAct_CaptureFlag : BTAction
    {
        public BTAct_CaptureFlag(string name) : base(name) { }

        public override bool DoAction(AIContext context)
        {
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
                {
                    string[] parts = move.Split(new char[] { '-', 'x' });
                    if (parts.Length != 2) continue;

                    BoardPosition fromPos = BoardPosition.FromString(parts[0]);

                    if (context.BusyPieceKeys != null && context.BusyPieceKeys.Contains(fromPos.ToString()))
                        continue;

                    context.SelectedAction = new RTSMoveAction
                    {
                        FromPos = fromPos,
                        ToPos = flagPos.Value,
                        MoveString = move,
                        Player = context.AIColor
                    };
                    return true;
                }
            }
            return false;
        }
    }
}
