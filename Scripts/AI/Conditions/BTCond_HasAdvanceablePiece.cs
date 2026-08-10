using System.Collections.Generic;
using JunqiGame.AI.BehaviorTree;
using JunqiGame.Core;

namespace JunqiGame.AI.Conditions
{
    public class BTCond_HasAdvanceablePiece : BTCondition
    {
        public BTCond_HasAdvanceablePiece(string name) : base(name) { }

        public override bool Check(AIContext context)
        {
            if (context.ValidMoves == null || context.ValidMoves.Count == 0)
                return false;

            BoardPosition enemyFlagEst = context.GetEstimatedEnemyFlagPos();

            foreach (string move in context.ValidMoves)
            {
                if (move.Contains("x"))
                    continue;

                string[] parts = move.Split('-');
                if (parts.Length != 2)
                    continue;

                BoardPosition fromPos = BoardPosition.FromString(parts[0]);
                BoardPosition toPos = BoardPosition.FromString(parts[1]);

                Piece piece = context.Board.GetPiece(fromPos);
                if (piece == null || piece.Color != context.AIColor)
                    continue;

                if (piece.Rank == PieceRank.Bomb || piece.Rank == PieceRank.Sapper)
                    continue;

                int currentDist = context.ManhattanDistance(fromPos, enemyFlagEst);
                int newDist = context.ManhattanDistance(toPos, enemyFlagEst);

                if (newDist < currentDist)
                    return true;
            }
            return false;
        }
    }
}
