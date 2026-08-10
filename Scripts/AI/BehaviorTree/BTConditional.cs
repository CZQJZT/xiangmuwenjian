using System;

namespace JunqiGame.AI.BehaviorTree
{
    public class BTConditional : BTDecorator
    {
        private Func<AIContext, bool> condition;

        public BTConditional(string name, Func<AIContext, bool> condition, BTNode child)
            : base(name, child)
        {
            this.condition = condition;
        }

        public override BTStatus Execute(AIContext context)
        {
            if (condition(context))
                return child.Execute(context);
            return BTStatus.Failure;
        }
    }
}
