using UnityEngine;
using JunqiGame.Core;

namespace JunqiGame.Tests
{
    /// <summary>
    /// 游戏逻辑测试脚本
    /// 用于验证核心功能是否正常工作
    /// </summary>
    public class GameLogicTester : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("=== Starting Junqi Game Logic Tests ===\n");
            
            TestBoardPosition();
            TestPiece();
            TestBoard();
            TestAILayout();
            TestGameRules();
            TestGameState();
            
            Debug.Log("\n=== All Tests Completed ===");
        }

        /// <summary>
        /// 测试棋盘位置
        /// </summary>
        private void TestBoardPosition()
        {
            Debug.Log("--- Testing BoardPosition ---");
            
            // 测试创建位置
            var pos1 = new BoardPosition('a', 1);
            Debug.Log($"Created position: {pos1}");
            
            // 测试从字符串解析
            var pos2 = BoardPosition.FromString("b12");
            Debug.Log($"Parsed from string: {pos2}");
            
            // 测试有效性
            Debug.Log($"Is valid: {pos2.IsValid()}");
            
            // 测试相等性
            var pos3 = BoardPosition.FromString("b12");
            Debug.Log($"pos2 == pos3: {pos2 == pos3}");
            
            Debug.Log("BoardPosition tests passed!\n");
        }

        /// <summary>
        /// 测试棋子
        /// </summary>
        private void TestPiece()
        {
            Debug.Log("--- Testing Piece ---");
            
            var piece = new Piece(PlayerColor.Blue, PieceRank.Marshal);
            Debug.Log($"Created piece: {piece}");
            Debug.Log($"Can move: {piece.CanMove()}");
            Debug.Log($"Is bomb: {piece.IsBomb()}");
            Debug.Log($"Rank string: {piece.RankStr}");
            
            // 测试地雷
            var mine = new Piece(PlayerColor.Red, PieceRank.Mine);
            Debug.Log($"Mine can move: {mine.CanMove()}");
            
            // 测试军旗
            var flag = new Piece(PlayerColor.Blue, PieceRank.Flag);
            Debug.Log($"Flag can move: {flag.CanMove()}");
            
            Debug.Log("Piece tests passed!\n");
        }

        /// <summary>
        /// 测试棋盘
        /// </summary>
        private void TestBoard()
        {
            Debug.Log("--- Testing Board ---");
            
            var board = new Board();
            
            // 测试放置棋子
            var pos1 = BoardPosition.FromString("a1");
            var piece1 = new Piece(PlayerColor.Blue, PieceRank.Marshal);
            board.PlacePiece(pos1, piece1);
            Debug.Log($"Placed piece at {pos1}");
            
            // 测试获取棋子
            var retrieved = board.GetPiece(pos1);
            Debug.Log($"Retrieved piece: {retrieved}");
            
            // 测试移动棋子
            var pos2 = BoardPosition.FromString("a2");
            board.MovePiece(pos1, pos2);
            Debug.Log($"Moved piece from {pos1} to {pos2}");
            
            var moved = board.GetPiece(pos2);
            Debug.Log($"Piece at new position: {moved}");
            Debug.Log($"Old position empty: {board.IsEmpty(pos1)}");
            
            // 测试行营检测
            var campPos = BoardPosition.FromString("b3");
            Debug.Log($"Is b3 a camp: {Board.IsCamp(campPos)}");
            
            // 测试铁路检测
            var railwayPos = BoardPosition.FromString("b4");
            Debug.Log($"Is b4 railway: {Board.IsRailway(railwayPos)}");
            
            Debug.Log("Board tests passed!\n");
        }

        /// <summary>
        /// 测试AI布阵
        /// </summary>
        private void TestAILayout()
        {
            Debug.Log("--- Testing AI Layout Generation ---");
            
            // 测试红方布阵
            var redLayout = AI.AILayoutGenerator.GenerateLayout(
                PlayerColor.Red, 
                AIDifficulty.Medium
            );
            Debug.Log($"Red layout generated with {redLayout.Count} pieces");
            
            // 统计棋子类型
            int mineCount = 0, flagCount = 0;
            foreach (var kvp in redLayout)
            {
                if (kvp.Value.Rank == PieceRank.Mine) mineCount++;
                if (kvp.Value.Rank == PieceRank.Flag) flagCount++;
            }
            Debug.Log($"Mines: {mineCount}, Flag: {flagCount}");
            
            // 测试蓝方布阵
            var blueLayout = AI.AILayoutGenerator.GenerateLayout(
                PlayerColor.Blue, 
                AIDifficulty.Easy
            );
            Debug.Log($"Blue layout generated with {blueLayout.Count} pieces");
            
            Debug.Log("AI Layout tests passed!\n");
        }

        /// <summary>
        /// 测试游戏规则
        /// </summary>
        private void TestGameRules()
        {
            Debug.Log("--- Testing Game Rules ---");
            
            // 测试移动解析
            var moveResult = GameRules.ParseMove("b2-b3");
            Debug.Log($"Parsed move: {moveResult.From} -> {moveResult.To}, Type: {moveResult.Type}");
            
            var captureResult = GameRules.ParseMove("b2xa3");
            Debug.Log($"Parsed capture: {captureResult.From} x {captureResult.To}, Type: {captureResult.Type}");
            
            // 测试战斗判定
            var attacker = new Piece(PlayerColor.Blue, PieceRank.Marshal);
            var defender = new Piece(PlayerColor.Red, PieceRank.General);
            var combatResult = GameRules.ResolveCombat(attacker, defender);
            Debug.Log($"Marshal vs General: {combatResult}");
            
            // 炸弹测试
            var bomb = new Piece(PlayerColor.Blue, PieceRank.Bomb);
            var target = new Piece(PlayerColor.Red, PieceRank.Marshal);
            var bombResult = GameRules.ResolveCombat(bomb, target);
            Debug.Log($"Bomb vs Marshal: {bombResult}");
            
            // 地雷测试
            var sapper = new Piece(PlayerColor.Blue, PieceRank.Sapper);
            var mine = new Piece(PlayerColor.Red, PieceRank.Mine);
            var mineResult1 = GameRules.ResolveCombat(sapper, mine);
            Debug.Log($"Sapper vs Mine: {mineResult1}");
            
            var soldier = new Piece(PlayerColor.Blue, PieceRank.Lieutenant);
            var mineResult2 = GameRules.ResolveCombat(soldier, mine);
            Debug.Log($"Lieutenant vs Mine: {mineResult2}");
            
            Debug.Log("Game Rules tests passed!\n");
        }

        /// <summary>
        /// 测试游戏状态
        /// </summary>
        private void TestGameState()
        {
            Debug.Log("--- Testing GameState ---");
            
            var gameState = new GameState();
            gameState.SetPlayMode(JunqiGame.Core.PlayMode.Concealed);
            
            // 添加玩家
            var bluePlayer = new PlayerInfo(PlayerColor.Blue, "Player1", "uid-1");
            var redPlayer = new PlayerInfo(PlayerColor.Red, "Player2", "uid-2");
            gameState.AddPlayer(bluePlayer);
            gameState.AddPlayer(redPlayer);
            
            Debug.Log($"Initial status: {gameState.Status}");
            
            // 初始化AI布阵
            gameState.InitializeAILayout(PlayerColor.Blue, AIDifficulty.Medium);
            gameState.InitializeAILayout(PlayerColor.Red, AIDifficulty.Medium);
            Debug.Log($"Board has {gameState.Board.PieceCount} pieces after layout");
            
            // 完成布阵
            gameState.FinishSetup(bluePlayer);
            gameState.FinishSetup(redPlayer);
            Debug.Log($"Status after setup: {gameState.Status}");
            Debug.Log($"Active player: {gameState.ActivePlayer?.Name}");
            
            // 获取合法移动
            var validMoves = gameState.GetValidMoves(PlayerColor.Blue);
            Debug.Log($"Valid moves for Blue: {validMoves.Count}");
            
            if (validMoves.Count > 0)
            {
                // 执行一个移动
                var firstMove = validMoves[0];
                Debug.Log($"Executing move: {firstMove}");
                
                var moveResult = gameState.Move(firstMove, PlayerColor.Blue);
                Debug.Log($"Move result: {moveResult.Success}, {moveResult.Message}");
                Debug.Log($"New active player: {gameState.ActivePlayer?.Name}");
            }
            
            Debug.Log("GameState tests passed!\n");
        }
    }
}
