namespace JunqiGame.AI.BehaviorTree
{
    public abstract class BTNode
    {
        public string Name;

        protected BTNode(string name)
        {
            Name = name;
        }

        public abstract BTStatus Execute(AIContext context);

        public override string ToString()
        {
            return $"[{GetType().Name}] {Name}";
        }
    }
}
