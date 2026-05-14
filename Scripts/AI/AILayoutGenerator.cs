using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunqiGame.AI
{
    /// <summary>
    /// AI布阵生成器
    /// 对应原始代码中的generateAILayout函数
    /// </summary>
    public class AILayoutGenerator
    {
        // 每个玩家的25个棋子配置
        private static readonly string[] PIECE_RANKS = new string[]
        {
            "0", "0", "0",      // 地雷 x3
            "1",                // 司令 x1
            "2",                // 军长 x1
            "3", "3",           // 师长 x2
            "4", "4",           // 旅长 x2
            "5", "5",           // 团长 x2
            "6", "6",           // 营长 x2
            "7", "7", "7",      // 连长 x3
            "8", "8", "8",      // 排长 x3
            "9", "9", "9",      // 工兵 x3
            "10", "10",         // 炸弹 x2
            "11"                // 军旗 x1
        };

        // 6种预设的军旗和地雷布局方案（适配13行棋盘）
        private static readonly FlagMineLayout[] FLAG_MINE_LAYOUTS = new FlagMineLayout[]
        {
            new FlagMineLayout("b13", new string[] { "a13", "c13", "b12" }),
            new FlagMineLayout("b13", new string[] { "a13", "a12", "c12" }),
            new FlagMineLayout("b13", new string[] { "a13", "c12", "e12" }),
            new FlagMineLayout("d13", new string[] { "e13", "c13", "d12" }),
            new FlagMineLayout("d13", new string[] { "e13", "e12", "c12" }),
            new FlagMineLayout("d13", new string[] { "e13", "c13", "a12" })
        };

        // 棋子放置优先级策略（与原始代码一致）
        private static readonly PlacementStrategy[] PLACEMENT_STRATEGIES = new PlacementStrategy[]
        {
            new PlacementStrategy(new string[] { "1", "2" }, new int[] { 9, 10, 11 }),
            new PlacementStrategy(new string[] { "3", "3", "4", "4" }, new int[] { 8, 9, 10 }),
            new PlacementStrategy(new string[] { "5", "5", "6", "6" }, new int[] { 7, 8, 9, 10 }),
            new PlacementStrategy(new string[] { "7", "7", "7", "8", "8", "8" }, new int[] { 7, 8 }),
            new PlacementStrategy(new string[] { "9", "9", "9" }, new int[] { 7, 8, 11 }),
            new PlacementStrategy(new string[] { "10", "10", "10" }, new int[] { 8, 9, 10 })  // 添加炸弹策略
        };

        /// <summary>
        /// 生成AI布阵
        /// </summary>
        /// <param name="playerColor">玩家颜色</param>
        /// <param name="difficulty">AI难度</param>
        /// <returns>棋盘状态字典</returns>
        public static Dictionary<string, Core.Piece> GenerateLayout(
            Core.PlayerColor playerColor, 
            Core.AIDifficulty difficulty)
        {
            // 1. 随机选择一个布局模板
            int layoutIndex = UnityEngine.Random.Range(0, FLAG_MINE_LAYOUTS.Length);
            FlagMineLayout layout = FLAG_MINE_LAYOUTS[layoutIndex];

            // 2. 如果是蓝方(上方)，需要翻转坐标
            if (playerColor == Core.PlayerColor.Blue)
            {
                layout = FlipLayout(layout);
            }

            // 3. 初始化棋盘状态和可用位置集合
            var boardState = new Dictionary<string, Core.Piece>();
            var availablePositions = GetAllValidPositions(playerColor);

            // 4. 放置军旗 (rank "11")
            if (availablePositions.Contains(layout.FlagSquare))
            {
                var flagPiece = new Core.Piece(playerColor, Core.PieceRank.Flag);
                boardState[layout.FlagSquare] = flagPiece;
                availablePositions.Remove(layout.FlagSquare);
            }

            // 5. 放置地雷 (rank "0")
            foreach (string mineSquare in layout.MineSquares)
            {
                if (!availablePositions.Contains(mineSquare))
                {
                    continue;
                }
                
                var minePiece = new Core.Piece(playerColor, Core.PieceRank.Mine);
                boardState[mineSquare] = minePiece;
                availablePositions.Remove(mineSquare);
            }

            // 6. 准备剩余棋子列表（21个棋子）
            List<string> remainingPieces = new List<string>();
            bool flagPlaced = false;
            int skippedMines = 0;

            foreach (string rank in PIECE_RANKS)
            {
                if (rank == "11" && !flagPlaced)
                {
                    flagPlaced = true;
                    continue;
                }
                if (rank == "0" && skippedMines < 3)
                {
                    skippedMines++;
                    continue;
                }
                remainingPieces.Add(rank);
            }

            // 7. 根据难度选择布阵策略
            if (difficulty == Core.AIDifficulty.Easy)
            {
                PlacePiecesRandomly(boardState, remainingPieces, availablePositions, playerColor);
            }
            else
            {
                PlacePiecesByStrategy(boardState, remainingPieces, availablePositions, playerColor);

                if (difficulty == Core.AIDifficulty.Medium)
                {
                    PerformRandomSwaps(boardState, playerColor);
                }
            }

            // 验证：检查是否有棋子在行营中
            HashSet<string> campPositions = playerColor == Core.PlayerColor.Blue
                ? new HashSet<string> { "b3", "d3", "c4", "b5", "d5" }
                : new HashSet<string> { "b9", "d9", "c10", "b11", "d11" };
            
            foreach (var kvp in boardState)
            {
                if (campPositions.Contains(kvp.Key))
                {
                    Debug.LogError($"ERROR: Piece {kvp.Value.Rank} placed in camp at {kvp.Key}!");
                }
            }

            return boardState;
        }

        /// <summary>
        /// 翻转布局（用于蓝方）
        /// </summary>
        private static FlagMineLayout FlipLayout(FlagMineLayout layout)
        {
            string FlipRow(string square)
            {
                char col = square[0];
                int row = int.Parse(square.Substring(1));
                int newRow = 14 - row; // 13行棋盘的翻转公式
                return $"{col}{newRow}";
            }

            return new FlagMineLayout(
                FlipRow(layout.FlagSquare),
                Array.ConvertAll(layout.MineSquares, FlipRow)
            );
        }

        /// <summary>
        /// 获取所有有效位置（排除行营、第7行和无效格子）
        /// </summary>
        private static List<string> GetAllValidPositions(Core.PlayerColor playerColor)
        {
            char[] columns = { 'a', 'b', 'c', 'd', 'e' };
            
            // 行营位置（不能放棋子）
            HashSet<string> campPositions = playerColor == Core.PlayerColor.Blue
                ? new HashSet<string> { "b3", "d3", "c4", "b5", "d5" }
                : new HashSet<string> { "b9", "d9", "c10", "b11", "d11" };

            // 确定行的范围（13行棋盘，排除第7行）
            int startRow = playerColor == Core.PlayerColor.Blue ? 1 : 8;
            int endRow = playerColor == Core.PlayerColor.Blue ? 6 : 13;

            List<string> positions = new List<string>();
            for (int row = startRow; row <= endRow; row++)
            {
                foreach (char col in columns)
                {
                    string pos = $"{col}{row}";
                    
                    // 跳过行营
                    if (campPositions.Contains(pos))
                        continue;
                    
                    positions.Add(pos);
                }
            }

            return positions;
        }

        /// <summary>
        /// 随机放置（简单模式）
        /// </summary>
        private static void PlacePiecesRandomly(
            Dictionary<string, Core.Piece> boardState,
            List<string> pieces,
            List<string> availablePositions,
            Core.PlayerColor playerColor)
        {
            List<string> shuffledPieces = ShuffleList(pieces);
            
            int restrictedRow = playerColor == Core.PlayerColor.Blue ? 6 : 7;
            int[] restrictedRows = playerColor == Core.PlayerColor.Blue ? new[] { 1, 2 } : new[] { 12, 13 };
            string[] restrictedPositions = playerColor == Core.PlayerColor.Blue 
                ? new[] { "b1", "d1" } 
                : new[] { "b13", "d13" };
            
            foreach (string pieceRank in shuffledPieces)
            {
                bool placed = false;
                List<string> candidatePositions = ShuffleList(new List<string>(availablePositions));
                
                foreach (string pos in candidatePositions)
                {
                    int rowNum = int.Parse(pos.Substring(1));
                    
                    if (IsValidPlacement(pieceRank, pos, restrictedRow, restrictedRows, restrictedPositions))
                    {
                        var piece = new Core.Piece(playerColor, Core.Piece.StringToRank(pieceRank));
                        boardState[pos] = piece;
                        availablePositions.Remove(pos);
                        placed = true;
                        break;
                    }
                }

                if (!placed && availablePositions.Count > 0)
                {
                    string pos = availablePositions[0];
                    var piece = new Core.Piece(playerColor, Core.Piece.StringToRank(pieceRank));
                    boardState[pos] = piece;
                    availablePositions.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// 按策略放置（中等/困难模式）
        /// </summary>
        private static void PlacePiecesByStrategy(
            Dictionary<string, Core.Piece> boardState,
            List<string> pieces,
            List<string> availablePositions,
            Core.PlayerColor playerColor)
        {
            List<string> remainingPieces = new List<string>(pieces);

            int FlipRow(int row) => playerColor == Core.PlayerColor.Blue ? 14 - row : row;
            
            foreach (var strategy in PLACEMENT_STRATEGIES)
            {
                List<string> strategyRanks = new List<string>(strategy.Ranks);
                strategyRanks.RemoveAll(r => !remainingPieces.Contains(r));
                
                if (strategyRanks.Count == 0)
                {
                    continue;
                }
                
                foreach (string rank in ShuffleList(strategyRanks))
                {
                    int index = remainingPieces.IndexOf(rank);
                    if (index == -1)
                    {
                        continue;
                    }

                    // 获取该组的首选行
                    List<int> preferredRows = new List<int>();
                    foreach (int row in strategy.PreferredRows)
                    {
                        preferredRows.Add(FlipRow(row));
                    }

                    // 在可用位置中筛选出首选行的位置
                    List<string> candidates = availablePositions.FindAll(pos =>
                    {
                        int rowNum = int.Parse(pos.Substring(1));
                        return preferredRows.Contains(rowNum);
                    });

                    string chosenPos = null;
                    
                    if (candidates.Count > 0)
                    {
                        chosenPos = ShuffleList(candidates)[0];
                    }
                    else if (availablePositions.Count > 0)
                    {
                        chosenPos = availablePositions[0];
                    }

                    if (chosenPos != null)
                    {
                        var piece = new Core.Piece(playerColor, Core.Piece.StringToRank(rank));
                        boardState[chosenPos] = piece;
                        availablePositions.Remove(chosenPos);
                        remainingPieces.RemoveAt(index);
                    }
                }
            }

            // 放置剩余的棋子（如果有的话）
            if (remainingPieces.Count > 0)
            {
                List<string> finalRemaining = ShuffleList(new List<string>(remainingPieces));
                
                foreach (string rank in finalRemaining)
                {
                    if (availablePositions.Count == 0)
                    {
                        break;
                    }
                    
                    string pos = availablePositions[0];
                    var piece = new Core.Piece(playerColor, Core.Piece.StringToRank(rank));
                    boardState[pos] = piece;
                    availablePositions.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// 验证放置是否合法
        /// </summary>
        private static bool IsValidPlacement(
            string piece, 
            string position, 
            int restrictedRow, 
            int[] restrictedRows, 
            string[] restrictedPositions)
        {
            int rowNum = int.Parse(position.Substring(1));

            return (piece != "0" || rowNum != restrictedRow) &&
                   (piece != "10" || Array.IndexOf(restrictedRows, rowNum) == -1) &&
                   (piece != "11" || Array.IndexOf(restrictedPositions, position) == -1);
        }

        /// <summary>
        /// 随机交换已放置的棋子位置（中等难度）
        /// </summary>
        private static void PerformRandomSwaps(
            Dictionary<string, Core.Piece> boardState,
            Core.PlayerColor playerColor,
            int swapCount = 3)
        {
            List<string> positions = new List<string>(boardState.Keys);
            int restrictedRow = playerColor == Core.PlayerColor.Blue ? 6 : 7;
            int[] restrictedRows = playerColor == Core.PlayerColor.Blue ? new[] { 1, 2 } : new[] { 12, 13 };
            string[] restrictedPositions = playerColor == Core.PlayerColor.Blue
                ? new[] { "b1", "d1" }
                : new[] { "b13", "d13" };
            
            for (int i = 0; i < swapCount; i++)
            {
                int idx1 = UnityEngine.Random.Range(0, positions.Count);
                int idx2 = UnityEngine.Random.Range(0, positions.Count);

                if (idx1 == idx2) continue;

                string pos1 = positions[idx1];
                string pos2 = positions[idx2];
                Core.Piece piece1 = boardState[pos1];
                Core.Piece piece2 = boardState[pos2];

                string rank1 = piece1.RankStr;
                string rank2 = piece2.RankStr;

                if (IsValidPlacement(rank1, pos2, restrictedRow, restrictedRows, restrictedPositions) &&
                    IsValidPlacement(rank2, pos1, restrictedRow, restrictedRows, restrictedPositions))
                {
                    boardState[pos1] = piece2;
                    boardState[pos2] = piece1;
                }
            }
        }

        /// <summary>
        /// Fisher-Yates洗牌算法
        /// </summary>
        private static List<T> ShuffleList<T>(List<T> list)
        {
            List<T> shuffled = new List<T>(list);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                T temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }
            return shuffled;
        }

        // 辅助数据结构
        private struct FlagMineLayout
        {
            public string FlagSquare;
            public string[] MineSquares;

            public FlagMineLayout(string flagSquare, string[] mineSquares)
            {
                FlagSquare = flagSquare;
                MineSquares = mineSquares;
            }
        }

        private struct PlacementStrategy
        {
            public string[] Ranks;
            public int[] PreferredRows;

            public PlacementStrategy(string[] ranks, int[] preferredRows)
            {
                Ranks = ranks;
                PreferredRows = preferredRows;
            }
        }
    }
}
