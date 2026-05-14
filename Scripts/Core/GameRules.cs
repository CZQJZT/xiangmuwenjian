using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunqiGame.Core
{
    /// <summary>
    /// 游戏规则引擎 - 处理战斗判定和规则验证
    /// </summary>
    public class GameRules
    {
        /// <summary>
        /// 解析移动字符串（如"b2-b3"或"b2xa3"）
        /// </summary>
        public static MoveParseResult ParseMove(string moveString)
        {
            if (string.IsNullOrEmpty(moveString))
                throw new ArgumentException("Move string cannot be null or empty");

            char separator = moveString.Contains('x') ? 'x' : '-';
            string[] parts = moveString.Split(separator);

            if (parts.Length != 2)
                throw new ArgumentException($"Invalid move format: {moveString}");

            BoardPosition from = BoardPosition.FromString(parts[0]);
            BoardPosition to = BoardPosition.FromString(parts[1]);
            MoveType type = separator == 'x' ? MoveType.Capture : MoveType.Normal;

            return new MoveParseResult(from, to, type);
        }

        /// <summary>
        /// 验证移动是否合法
        /// </summary>
        public static bool IsValidMove(
            BoardPosition from,
            BoardPosition to,
            Board board,
            PlayerColor currentPlayer)
        {
            // 检查位置有效性
            if (!from.IsValid() || !to.IsValid())
                return false;

            // 获取棋子
            Piece piece = board.GetPiece(from);
            if (piece == null)
                return false;

            // 检查是否是当前玩家的棋子
            if (piece.Color != currentPlayer)
                return false;

            // 检查棋子是否可以移动
            if (!piece.CanMove())
                return false;

            // 检查目标位置
            Piece targetPiece = board.GetPiece(to);
            
            // 不能吃自己的棋子
            if (targetPiece != null && targetPiece.Color == currentPlayer)
                return false;

            // 行营内的敌方棋子不能被攻击（免疫）
            if (targetPiece != null && targetPiece.Color != currentPlayer)
            {
                if (Board.IsCamp(to))
                {
                    return false; // 不能攻击行营里的棋子
                }
            }

            // 检查路径合法性
            if (!IsValidPath(from, to, board, piece))
                return false;

            return true;
        }

        // ─── 以下 3 个方法已被 PathFinder 替代，暂注释 ───
        // IsValidSapperRailwayPath, IsValidRailwayStraightMove, IsRailwayPathClear
        // ─────────────────────────────────────────────

        /// <summary>
        /// 验证路径是否合法（总入口）
        /// </summary>
        public static bool IsValidPath(
            BoardPosition from,
            BoardPosition to,
            Board board,
            Piece piece)
        {
            // --- 诊断日志（编辑器下才输出）---
#if UNITY_EDITOR
            Debug.Log($"🔍 [DIAG] Move: {piece.Rank} from {from} to {to}");
            Debug.Log($"   📍 IsSapper: {piece.IsSapper()}");
            Debug.Log($"   🛤️ IsRailway(From): {Board.IsRailway(from)}, IsRailway(To): {Board.IsRailway(to)}");
            Debug.Log($"   🏰 IsCamp(From): {Board.IsCamp(from)}, IsCamp(To): {Board.IsCamp(to)}");
#endif

            // 1. 纯行营逻辑：只有当起点或终点是行营，且【不满足铁路移动条件】时，才使用行营相邻规则
            bool isFromCamp = Board.IsCamp(from);
            bool isToCamp = Board.IsCamp(to);

            // 2. 工兵铁路逻辑（优先级最高，使用 PathFinder）
            if (piece.IsSapper() && Board.IsRailway(from))
            {
                // 🔑 特殊处理：如果目标是行营，不使用 PathFinder，直接用相邻规则
                if (Board.IsCamp(to))
                {
#if UNITY_EDITOR
                    Debug.Log("   ✅ Branch: SAPPER TO CAMP (Adjacent rule)");
#endif
                    return IsAdjacentIncludingDiagonal(from, to);
                }
                
#if UNITY_EDITOR
                Debug.Log("   ✅ Branch: SAPPER RAILWAY (Calling PathFinder)");
#endif
                var path = PathFinder.FindPath(board, from, to, piece);
                return path != null;
            }

            // 3. 普通棋子铁路逻辑（直线移动，使用 PathFinder）
            if (Board.IsRailway(from))
            {
#if UNITY_EDITOR
                Debug.Log($"   🚂 Branch: RAILWAY MOVE (From {from} is railway). To is Camp: {Board.IsCamp(to)}");
#endif
                var path = PathFinder.FindPath(board, from, to, piece);
                
                if (path != null) return true;

                if (Board.IsCamp(to))
                {
#if UNITY_EDITOR
                    Debug.Log("   ⛺ Branch: RAILWAY TO ADJACENT CAMP");
#endif
                    return IsAdjacentIncludingDiagonal(from, to);
                }
            }

            // 4. 行营相邻逻辑
            if (isFromCamp || isToCamp)
            {
#if UNITY_EDITOR
                Debug.Log("   ⛔ Branch: CAMP LOGIC (Adjacent only)");
#endif
                return IsAdjacentIncludingDiagonal(from, to);
            }

            // 5. 普通相邻移动
#if UNITY_EDITOR
            Debug.Log("   🚶 Branch: NORMAL ADJACENT");
#endif
            return IsAdjacent(from, to);
        }

        /// <summary>
        /// 检查两个位置是否相邻
        /// </summary>
        public static bool IsAdjacent(BoardPosition from, BoardPosition to)
        {
            int colDiff = Math.Abs(from.Column - to.Column);
            int rowDiff = Math.Abs(from.Row - to.Row);

            // 上下左右相邻（不包括对角线）
            return (colDiff == 1 && rowDiff == 0) || (colDiff == 0 && rowDiff == 1);
        }

        /// <summary>
        /// 检查两个位置是否相邻（包括对角线，用于行营）
        /// </summary>
        public static bool IsAdjacentIncludingDiagonal(BoardPosition from, BoardPosition to)
        {
            int colDiff = Math.Abs(from.Column - to.Column);
            int rowDiff = Math.Abs(from.Row - to.Row);
            return colDiff <= 1 && rowDiff <= 1 && !(colDiff == 0 && rowDiff == 0);
        }

        /// <summary>
        /// 解决战斗 - 返回战斗结果（O(1)数学计算）
        /// </summary>
        public static CombatResult ResolveCombat(Piece attacker, Piece defender)
        {
            if (attacker == null || defender == null) return CombatResult.BothDie;

            // 1. 军旗规则
            if (defender.IsFlag())
                return CombatResult.AttackerWin;

            // 2. 炸弹规则
            if (attacker.IsBomb() || defender.IsBomb())
                return CombatResult.BothDie;

            // 3. 地雷规则
            if (defender.IsMine())
                return attacker.IsSapper() ? CombatResult.AttackerWin : CombatResult.DefenderWin;

            // 4. O(1) 计算：攻击方先手，计算各自需要多少次攻击击倒对方
            int attTicks = (defender.Health + attacker.Attack - 1) / attacker.Attack;
            int defTicks = (attacker.Health + defender.Attack - 1) / defender.Attack;

            // 攻击方先手，所以 <= 时攻击方赢（同时死也算攻击方赢）
            return attTicks <= defTicks ? CombatResult.AttackerWin : CombatResult.DefenderWin;
        }

        /// <summary>
        /// 执行移动（新版本：只验证合法性并返回路径，不实际执行移动）
        /// UI 层负责根据路径播放动画，并在动画每步完成后调用 ApplyMoveStep
        /// </summary>
        public static MoveResult ExecuteMove(
            Board board,
            BoardPosition from,
            BoardPosition to,
            PlayerColor currentPlayer)
        {
            Piece attacker = board.GetPiece(from);
            
            // 验证移动合法性
            if (!IsValidMove(from, to, board, currentPlayer))
            {
                return new MoveResult(false, "Invalid move");
            }

            // 🔑 查找路径（使用 PathFinder）
            List<BoardPosition> path = null;
            if (Board.IsRailway(from) && Board.IsRailway(to))
            {
                path = PathFinder.FindPath(board, from, to, attacker);
            }

            // 如果没有找到路径，使用默认路径（起点+终点）
            if (path == null || path.Count < 2)
            {
                path = new List<BoardPosition> { from, to };
            }

            Piece defender = board.GetPiece(to);

            // 🔑 关键：不执行任何移动，只返回路径信息
            // UI 层会：
            // 1. 根据 path 播放逐格动画
            // 2. 每步动画完成后调用 ApplyMoveStep 更新棋盘
            // 3. 最后一步完成后调用 FinalizeMove 处理战斗
            
            return new MoveResult(
                true,
                path.Count > 2 ? $"Success via railway path ({path.Count - 1} steps)" : "Success",
                attacker,
                    defender, // include original defender (if any) so UI can preserve it before animation
                    null, // no combat result yet; FinalizeMove will compute it after animation
                path  // ← 返回完整路径供 UI 层使用
            );
        }

        /// <summary>
        /// 应用单步移动（供 UI 层在动画每步完成后调用）
        /// 🔑 阶段 A：只移动棋子，不处理战斗
        /// </summary>
        public static void ApplyMoveStep(Board board, BoardPosition from, BoardPosition to)
        {
            Piece captured = board.MovePiece(from, to);
            
            // 🔑 移除每步移动的日志（太频繁）
            // Debug.Log($"🔄 [ApplyMoveStep] Moved piece from {from} to {to}");
        }

        // ─── GameRules.FinalizeMove 已移除（战斗逻辑分散在 ApplyVisualCollisionResult / RTS 中，更简洁） ───

        /// <summary>
        /// 检查游戏是否结束
        /// </summary>
        public static GameEndResult CheckGameEnd(Board board)
        {
            // 查找双方的军旗
            BoardPosition? blueFlagPos = FindPiece(board, PlayerColor.Blue, PieceRank.Flag);
            BoardPosition? redFlagPos = FindPiece(board, PlayerColor.Red, PieceRank.Flag);

            // 检查军旗是否被扛
            if (!blueFlagPos.HasValue)
            {
                return new GameEndResult(true, PlayerColor.Red, "Red captured Blue's flag");
            }

            if (!redFlagPos.HasValue)
            {
                return new GameEndResult(true, PlayerColor.Blue, "Blue captured Red's flag");
            }

            // 检查是否无子可动
            var bluePieces = board.GetPiecesByColor(PlayerColor.Blue);
            var redPieces = board.GetPiecesByColor(PlayerColor.Red);

            bool blueCanMove = false;
            foreach (var pos in bluePieces)
            {
                Piece piece = board.GetPiece(pos);
                if (piece != null && piece.CanMove() && HasValidMoves(board, pos, PlayerColor.Blue))
                {
                    blueCanMove = true;
                    break;
                }
            }

            bool redCanMove = false;
            foreach (var pos in redPieces)
            {
                Piece piece = board.GetPiece(pos);
                if (piece != null && piece.CanMove() && HasValidMoves(board, pos, PlayerColor.Red))
                {
                    redCanMove = true;
                    break;
                }
            }

            if (!blueCanMove)
            {
                return new GameEndResult(true, PlayerColor.Red, "Blue has no valid moves");
            }

            if (!redCanMove)
            {
                return new GameEndResult(true, PlayerColor.Blue, "Red has no valid moves");
            }

            return new GameEndResult(false, PlayerColor.None, "Game continues");
        }

        /// <summary>
        /// 查找指定颜色的特定等级棋子
        /// </summary>
        public static BoardPosition? FindPiece(Board board, PlayerColor color, PieceRank rank)
        {
            var positions = board.GetPiecesByColor(color);
            foreach (var pos in positions)
            {
                Piece piece = board.GetPiece(pos);
                if (piece != null && piece.Rank == rank)
                {
                    return pos;
                }
            }
            return null;
        }

        /// <summary>
        /// 检查某个位置是否有合法的移动
        /// </summary>
        public static bool HasValidMoves(Board board, BoardPosition position, PlayerColor playerColor)
        {
            var adjBuffer = new BoardPosition[4];
            int adjCount = position.GetAdjacentPositions(adjBuffer);
            for (int i = 0; i < adjCount; i++)
            {
                if (IsValidMove(position, adjBuffer[i], board, playerColor))
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 移动解析结果
    /// </summary>
    public struct MoveParseResult
    {
        public BoardPosition From;
        public BoardPosition To;
        public MoveType Type;

        public MoveParseResult(BoardPosition from, BoardPosition to, MoveType type)
        {
            From = from;
            To = to;
            Type = type;
        }
    }

    /// <summary>
    /// 移动执行结果
    /// </summary>
    public class MoveResult
    {
        public bool Success;
        public string Message;
        public Piece Attacker;
        public Piece CapturedPiece;
        public CombatResult? CombatResult;
        
        /// <summary>
        /// 使用的路径（用于铁路网多格移动）
        /// </summary>
        public List<BoardPosition> PathUsed;

        public MoveResult(bool success, string message, 
            Piece attacker = null, Piece capturedPiece = null, 
            CombatResult? combatResult = null,
            List<BoardPosition> pathUsed = null)
        {
            Success = success;
            Message = message;
            Attacker = attacker;
            CapturedPiece = capturedPiece;
            CombatResult = combatResult;
            PathUsed = pathUsed;
        }
    }

    /// <summary>
    /// 游戏结束结果
    /// </summary>
    public class GameEndResult
    {
        public bool IsGameOver;
        public PlayerColor Winner;
        public string Reason;

        public GameEndResult(bool isGameOver, PlayerColor winner, string reason)
        {
            IsGameOver = isGameOver;
            Winner = winner;
            Reason = reason;
        }
    }
}
