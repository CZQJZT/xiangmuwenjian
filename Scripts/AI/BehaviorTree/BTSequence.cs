namespace JunqiGame.AI.BehaviorTree
{
    public class BTSequence : BTComposite
    {
        public BTSequence(string name) : base(name) { }

        public override BTStatus Execute(AIContext context)
        {
            for (int i = 0; i < children.Count; i++)
            {
                BTStatus status = children[i].Execute(context);
                if (status == BTStatus.Failure)
                    return BTStatus.Failure;
                if (status == BTStatus.Running)
                    return BTStatus.Running;
            }
            return BTStatus.Success;
        }
    }
}
