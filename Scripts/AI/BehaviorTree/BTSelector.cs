namespace JunqiGame.AI.BehaviorTree
{
    public class BTSelector : BTComposite
    {
        public BTSelector(string name) : base(name) { }

        public override BTStatus Execute(AIContext context)
        {
            for (int i = 0; i < children.Count; i++)
            {
                BTStatus status = children[i].Execute(context);
                if (status == BTStatus.Success)
                    return BTStatus.Success;
                if (status == BTStatus.Running)
                    return BTStatus.Running;
            }
            return BTStatus.Failure;
        }
    }
}
