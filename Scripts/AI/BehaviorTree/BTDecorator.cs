namespace JunqiGame.AI.BehaviorTree
{
    public abstract class BTDecorator : BTNode
    {
        protected BTNode child;

        protected BTDecorator(string name, BTNode child) : base(name)
        {
            this.child = child;
        }

        public void SetChild(BTNode node)
        {
            child = node;
        }
    }
}
