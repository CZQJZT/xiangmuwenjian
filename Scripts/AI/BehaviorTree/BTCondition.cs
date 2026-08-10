namespace JunqiGame.AI.BehaviorTree
{
    public abstract class BTCondition : BTLeaf
    {
        protected BTCondition(string name) : base(name) { }

        public override BTStatus Execute(AIContext context)
        {
            return Check(context) ? BTStatus.Success : BTStatus.Failure;
        }

        public abstract bool Check(AIContext context);
    }
}
