namespace JunqiGame.AI.BehaviorTree
{
    public abstract class BTAction : BTLeaf
    {
        protected BTAction(string name) : base(name) { }

        public override BTStatus Execute(AIContext context)
        {
            return DoAction(context) ? BTStatus.Success : BTStatus.Failure;
        }

        public abstract bool DoAction(AIContext context);
    }
}
