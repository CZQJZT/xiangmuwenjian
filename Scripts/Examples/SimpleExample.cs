using UnityEngine;
using JunqiGame.Core;

namespace JunqiGame.Examples
{
    /// <summary>
    /// 简单示例 - 展示如何使用军棋游戏核心API
    /// </summary>
    public class SimpleExample : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("=== 军棋游戏简单示例 ===\n");
            
            // 示例1: 创建基本游戏
            Example1_BasicGame();
            
            // 示例2: AI对战
            Example2_AIGame();
            
            // 示例3: 手动布阵
            Example3_ManualLayout();
        }

        /// <summary>
        /// 示例1: 基本游戏流程
        /// </summary>
        void Example1_BasicGame()
        {
            Debug.Log("--- 示例1: 基本游戏流程 ---");
            
            // 1. 创建游戏状态
            var game = new GameState();
            game.SetPlayMode(JunqiGame.Core.PlayMode.Concealed);
            
            // 2. 添加玩家
            game.AddPlayer(new PlayerInfo(PlayerColor.Blue, "张三", "player1"));
            game.AddPlayer(new PlayerInfo(PlayerColor.Red, "李四", "player2"));
            
            // 3. 初始化布阵（这里使用AI自动生成）
            game.InitializeAILayout(PlayerColor.Blue, AIDifficulty.Medium);
            game.InitializeAILayout(PlayerColor.Red, AIDifficulty.Medium);
            
            // 4. 完成布阵，开始游戏
            game.FinishSetup(game.Players[PlayerColor.Blue]);
            game.FinishSetup(game.Players[PlayerColor.Red]);
            
            Debug.Log($"游戏状态: {game.Status}");
            Debug.Log($"当前玩家: {game.ActivePlayer.Name}");
            Debug.Log($"棋盘棋子数: {game.Board.PieceCount}\n");
        }

        /// <summary>
        /// 示例2: AI对战
        /// </summary>
        void Example2_AIGame()
        {
            Debug.Log("--- 示例2: AI对战 ---");
            
            var game = new GameState();
            game.SetPlayMode(JunqiGame.Core.PlayMode.Revealed);  // 明棋模式
            
            // 人类玩家 vs AI
            game.AddPlayer(new PlayerInfo(PlayerColor.Blue, "Human", "human-1"));
            game.AddPlayer(new PlayerInfo(PlayerColor.Red, "AI", "ai-1"));
            
            // 生成不同难度的AI布阵
            game.InitializeAILayout(PlayerColor.Blue, AIDifficulty.Easy);
            game.InitializeAILayout(PlayerColor.Red, AIDifficulty.Hard);
            
            game.FinishSetup(game.Players[PlayerColor.Blue]);
            game.FinishSetup(game.Players[PlayerColor.Red]);
            
            // 模拟几步游戏
            for (int i = 0; i < 3; i++)
            {
                var currentPlayer = game.ActivePlayer.Color;
                var validMoves = game.GetValidMoves(currentPlayer);
                
                if (validMoves.Count > 0)
                {
                    // 随机选择一个移动
                    string move = validMoves[Random.Range(0, validMoves.Count)];
                    var result = game.Move(move, currentPlayer);
                    
                    Debug.Log($"第{i + 1}步: {currentPlayer} 执行 {move}, 成功: {result.Success}");
                }
            }
            
            Debug.Log("");
        }

        /// <summary>
        /// 示例3: 手动布阵
        /// </summary>
        void Example3_ManualLayout()
        {
            Debug.Log("--- 示例3: 手动布阵 ---");
            
            var board = new Board();
            
            // 手动放置一些棋子
            var marshal = new Piece(PlayerColor.Blue, PieceRank.Marshal);
            var general = new Piece(PlayerColor.Blue, PieceRank.General);
            var flag = new Piece(PlayerColor.Blue, PieceRank.Flag);
            var mine = new Piece(PlayerColor.Blue, PieceRank.Mine);
            
            board.PlacePiece(BoardPosition.FromString("b6"), marshal);
            board.PlacePiece(BoardPosition.FromString("c6"), general);
            board.PlacePiece(BoardPosition.FromString("b5"), flag);
            board.PlacePiece(BoardPosition.FromString("a5"), mine);
            
            Debug.Log("手动布阵完成:");
            Debug.Log($"  b6: {board.GetPiece(BoardPosition.FromString("b6"))}");
            Debug.Log($"  c6: {board.GetPiece(BoardPosition.FromString("c6"))}");
            Debug.Log($"  b5: {board.GetPiece(BoardPosition.FromString("b5"))}");
            Debug.Log($"  a5: {board.GetPiece(BoardPosition.FromString("a5"))}");
            
            // 测试移动
            var moveResult = GameRules.ExecuteMove(
                board,
                BoardPosition.FromString("b6"),
                BoardPosition.FromString("b5"),
                PlayerColor.Blue
            );
            
            Debug.Log($"\n移动 b6->b5: {moveResult.Message}");
            Debug.Log($"b5 现在是: {board.GetPiece(BoardPosition.FromString("b5"))}");
            Debug.Log($"b6 现在是: {(board.IsEmpty(BoardPosition.FromString("b6")) ? "空" : "有棋子")}\n");
        }

        /// <summary>
        /// 示例4: 战斗判定测试
        /// </summary>
        void Example4_CombatTest()
        {
            Debug.Log("--- 示例4: 战斗判定 ---");
            
            // 测试各种战斗场景
            TestCombat("司令 vs 军长", 
                new Piece(PlayerColor.Blue, PieceRank.Marshal),
                new Piece(PlayerColor.Red, PieceRank.General));
            
            TestCombat("炸弹 vs 司令",
                new Piece(PlayerColor.Blue, PieceRank.Bomb),
                new Piece(PlayerColor.Red, PieceRank.Marshal));
            
            TestCombat("工兵 vs 地雷",
                new Piece(PlayerColor.Blue, PieceRank.Sapper),
                new Piece(PlayerColor.Red, PieceRank.Mine));
            
            TestCombat("排长 vs 地雷",
                new Piece(PlayerColor.Blue, PieceRank.Lieutenant),
                new Piece(PlayerColor.Red, PieceRank.Mine));
            
            TestCombat("同级对战",
                new Piece(PlayerColor.Blue, PieceRank.Colonel),
                new Piece(PlayerColor.Red, PieceRank.Colonel));
        }

        void TestCombat(string scenario, Piece attacker, Piece defender)
        {
            var result = GameRules.ResolveCombat(attacker, defender);
            Debug.Log($"{scenario}: {result}");
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("军棋游戏示例");
            GUILayout.Label("查看Console输出了解详细信息");
            
            if (GUILayout.Button("运行示例"))
            {
                Start();
            }
            
            GUILayout.EndArea();
        }
    }
}
