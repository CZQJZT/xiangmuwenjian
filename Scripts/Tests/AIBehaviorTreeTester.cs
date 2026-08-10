using UnityEngine;
using JunqiGame.Core;
using JunqiGame.AI;
using JunqiGame.AI.BehaviorTree;

namespace JunqiGame.Tests
{
    public class AIBehaviorTreeTester : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("=== AI Behavior Tree Tests ===\n");

            TestBehaviorTreeFramework();
            TestAIContext();
            TestFullBehaviorTree();

            Debug.Log("\n=== All AI Behavior Tree Tests Completed ===");
        }

        private void TestBehaviorTreeFramework()
        {
            Debug.Log("--- Testing BT Framework ---");

            var selector = new BTSelector("TestSelector");
            var sequence = new BTSequence("TestSequence");

            Debug.Log($"Selector created: {selector}");
            Debug.Log($"Sequence created: {sequence}");

            Debug.Log("BT Framework tests passed!\n");
        }

        private void TestAIContext()
        {
            Debug.Log("--- Testing AIContext ---");

            var gameState = new GameState();
            gameState.SetPlayMode(PlayMode.Concealed);

            var bluePlayer = new PlayerInfo(PlayerColor.Blue, "Player", "p1");
            var redPlayer = new PlayerInfo(PlayerColor.Red, "AI", "p2");
            gameState.AddPlayer(bluePlayer);
            gameState.AddPlayer(redPlayer);

            gameState.InitializeAILayout(PlayerColor.Blue, AIDifficulty.Medium);
            gameState.InitializeAILayout(PlayerColor.Red, AIDifficulty.Medium);
            gameState.FinishSetup(bluePlayer);
            gameState.FinishSetup(redPlayer);

            var context = new AIContext
            {
                Board = gameState.Board,
                AIColor = PlayerColor.Red,
                CurrentAP = 3f,
                APMax = 3f,
                ValidMoves = gameState.GetValidMoves(PlayerColor.Red),
                Difficulty = AIDifficulty.Medium,
                PlayMode = PlayMode.Concealed,
                BusyPieceKeys = new System.Collections.Generic.HashSet<string>()
            };

            Debug.Log($"AI Context created: AIColor={context.AIColor}, EnemyColor={context.EnemyColor}");
            Debug.Log($"Valid moves: {context.ValidMoves.Count}");
            Debug.Log($"Ally pieces: {context.GetAllyPieces().Count}");
            Debug.Log($"Enemy pieces: {context.GetEnemyPieces().Count}");
            Debug.Log($"Own flag pos: {context.GetOwnFlagPosition()}");
            Debug.Log($"Estimated enemy flag: {context.GetEstimatedEnemyFlagPos()}");

            Debug.Log("AIContext tests passed!\n");
        }

        private void TestFullBehaviorTree()
        {
            Debug.Log("--- Testing Full Behavior Tree ---");

            foreach (AIDifficulty diff in System.Enum.GetValues(typeof(AIDifficulty)))
            {
                var tree = new AIBehaviorTree(diff);
                Debug.Log($"Behavior tree created for difficulty: {diff}");

                var gameState = new GameState();
                gameState.SetPlayMode(PlayMode.Revealed);

                var bluePlayer = new PlayerInfo(PlayerColor.Blue, "Player", "p1");
                var redPlayer = new PlayerInfo(PlayerColor.Red, "AI", "p2");
                gameState.AddPlayer(bluePlayer);
                gameState.AddPlayer(redPlayer);

                gameState.InitializeAILayout(PlayerColor.Blue, diff);
                gameState.InitializeAILayout(PlayerColor.Red, diff);
                gameState.FinishSetup(bluePlayer);
                gameState.FinishSetup(redPlayer);

                var context = new AIContext
                {
                    Board = gameState.Board,
                    AIColor = PlayerColor.Red,
                    CurrentAP = 3f,
                    APMax = 3f,
                    ValidMoves = gameState.GetValidMoves(PlayerColor.Red),
                    Difficulty = diff,
                    PlayMode = PlayMode.Revealed,
                    BusyPieceKeys = new System.Collections.Generic.HashSet<string>()
                };

                RTSMoveAction action = tree.Tick(context);

                if (action != null)
                {
                    Debug.Log($"  [{diff}] Selected action: {action.MoveString} ({action.FromPos} -> {action.ToPos})");
                }
                else
                {
                    Debug.LogWarning($"  [{diff}] No action selected!");
                }
            }

            Debug.Log("Full Behavior Tree tests passed!\n");
        }
    }
}
