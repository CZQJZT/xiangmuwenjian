namespace JunqiGame.AI.BehaviorTree
{
    public class BTInverter : BTDecorator
    {
        public BTInverter(string name, BTNode child) : base(name, child) { }

        public override BTStatus Execute(AIContext context)
        {
            BTStatus status = child.Execute(context);
            if (status == BTStatus.Success)
                return BTStatus.Failure;
            if (status == BTStatus.Failure)
                return BTStatus.Success;
            return BTStatus.Running;
        }
    }
}
