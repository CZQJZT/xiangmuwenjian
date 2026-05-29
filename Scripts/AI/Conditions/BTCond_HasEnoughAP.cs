using JunqiGame.AI.BehaviorTree;

namespace JunqiGame.AI.Conditions
{
    public class BTCond_HasEnoughAP : BTCondition
    {
        private float threshold;

        public BTCond_HasEnoughAP(string name, float threshold = 0.5f) : base(name)
        {
            this.threshold = threshold;
        }

        public override bool Check(AIContext context)
        {
            if (context.APMax <= 0)
                return true;
            return context.CurrentAP >= context.APMax * threshold;
        }
    }
}
