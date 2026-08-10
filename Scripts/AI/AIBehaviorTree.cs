using JunqiGame.AI.BehaviorTree;
using JunqiGame.AI.Conditions;
using JunqiGame.AI.Actions;
using JunqiGame.Core;
using JunqiGame.RTS;

namespace JunqiGame.AI
{
    public class AIBehaviorTree
    {
        private BTNode root;
        private AIDifficulty difficulty;

        public AIBehaviorTree(AIDifficulty difficulty)
        {
            this.difficulty = difficulty;
            root = BuildTree(difficulty);
        }

        public RTSMoveAction Tick(AIContext context)
        {
            context.SelectedAction = null;

            BTStatus status = root.Execute(context);

            if (status == BTStatus.Success && context.SelectedAction != null)
            {
                return context.SelectedAction;
            }

            return null;
        }

        private BTNode BuildTree(AIDifficulty diff)
        {
            if (diff == AIDifficulty.Easy)
            {
                return BuildEasyTree();
            }

            return BuildFullTree(diff);
        }

        private BTNode BuildEasyTree()
        {
            var root = new BTSelector("EasyRoot");

            var captureSeq = new BTSequence("CaptureFlag")
                .AddChild(new BTCond_CanCaptureFlag("CanCaptureFlag"))
                .AddChild(new BTAct_CaptureFlag("DoCaptureFlag"));
            root.AddChild(captureSeq);

            root.AddChild(new BTAct_RandomMove("RandomMove"));

            return root;
        }

        private BTNode BuildFullTree(AIDifficulty diff)
        {
            var root = new BTSelector("AIRoot");

            var defendSeq = new BTSequence("DefendFlag")
                .AddChild(new BTCond_FlagInDanger("FlagInDanger", diff == AIDifficulty.Hard ? 4 : 3))
                .AddChild(new BTAct_InterceptThreat("InterceptThreat"));
            root.AddChild(defendSeq);

            var captureSeq = new BTSequence("CaptureFlag")
                .AddChild(new BTCond_CanCaptureFlag("CanCaptureFlag"))
                .AddChild(new BTAct_CaptureFlag("DoCaptureFlag"));
            root.AddChild(captureSeq);

            var bombSeq = new BTSequence("BombStrike")
                .AddChild(new BTCond_HasBomb("HasBomb"))
                .AddChild(new BTCond_HighValueNearby("HighValueNearby", diff == AIDifficulty.Hard ? 6 : 5))
                .AddChild(new BTAct_BombStrike("DoBombStrike"));
            root.AddChild(bombSeq);

            var sapperSeq = new BTSequence("ClearMine")
                .AddChild(new BTCond_HasSapper("HasSapper"))
                .AddChild(new BTCond_MineNearby("MineNearby", diff == AIDifficulty.Hard ? 8 : 6))
                .AddChild(new BTAct_ClearMine("DoClearMine"));
            root.AddChild(sapperSeq);

            var combatSeq = new BTSequence("FavorableCombat")
                .AddChild(new BTCond_HasFavorableCombat("HasFavorableCombat"))
                .AddChild(new BTAct_ExecuteCapture("DoExecuteCapture"));
            root.AddChild(combatSeq);

            var advanceSeq = new BTSequence("AdvanceTowardFlag")
                .AddChild(new BTCond_HasEnoughAP("HasEnoughAP", diff == AIDifficulty.Hard ? 0.3f : 0.5f))
                .AddChild(new BTCond_HasAdvanceablePiece("HasAdvanceablePiece"))
                .AddChild(new BTAct_AdvanceTowardFlag("DoAdvance"));
            root.AddChild(advanceSeq);

            var scoutSeq = new BTSequence("ScoutExplore")
                .AddChild(new BTCond_HasExpendablePiece("HasExpendablePiece"))
                .AddChild(new BTAct_ScoutMove("DoScoutMove"));
            root.AddChild(scoutSeq);

            root.AddChild(new BTAct_RandomMove("RandomMove"));

            return root;
        }
    }
}
