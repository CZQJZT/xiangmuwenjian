using System.Collections.Generic;

namespace JunqiGame.AI.BehaviorTree
{
    public abstract class BTComposite : BTNode
    {
        protected List<BTNode> children = new List<BTNode>();

        protected BTComposite(string name) : base(name) { }

        public BTComposite AddChild(BTNode child)
        {
            children.Add(child);
            return this;
        }

        public List<BTNode> GetChildren()
        {
            return children;
        }
    }
}
