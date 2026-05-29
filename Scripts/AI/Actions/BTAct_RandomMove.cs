using System.Collections.Generic;
using JunqiGame.AI.BehaviorTree;
using JunqiGame.Core;
using JunqiGame.RTS;

namespace JunqiGame.AI.Actions
{
    public class BTAct_RandomMove : BTAction
    {
        private static System.Random rng = new System.Random();

        public BTAct_RandomMove(string name) : base(name) { }

        public override bool DoAction(AIContext context)
        {
            if (context.ValidMoves == null || context.ValidMoves.Count == 0)
                return false;

            List<string> availableMoves = new List<string>();
            foreach (string move in context.ValidMoves)
            {
                string[] parts = move.Split(new char[] { '-', 'x' });
                if (parts.Length != 2) continue;

                BoardPosition fromPos = BoardPosition.FromString(parts[0]);

                if (context.BusyPieceKeys != null && context.BusyPieceKeys.Contains(fromPos.ToString()))
                    continue;

                availableMoves.Add(move);
            }

            if (availableMoves.Count == 0)
                return false;

            int index = rng.Next(availableMoves.Count);
            string chosenMove = availableMoves[index];

            char separator = chosenMove.Contains("x") ? 'x' : '-';
            string[] moveParts = chosenMove.Split(separator);

            BoardPosition from = BoardPosition.FromString(moveParts[0]);
            BoardPosition to = BoardPosition.FromString(moveParts[1]);

            context.SelectedAction = new RTSMoveAction
            {
                FromPos = from,
                ToPos = to,
                MoveString = chosenMove,
                Player = context.AIColor
            };
            return true;
        }
    }
}
