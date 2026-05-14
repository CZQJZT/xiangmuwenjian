using System.Collections.Generic;
using UnityEngine;
using JunqiGame.UI;

namespace JunqiGame.Core
{
    public static class PathCombatDetector
    {
        /// <summary>
        /// 视觉碰撞检测 — 检查目标位置是否有敌方棋子 GameObject
        /// </summary>
        public static bool CheckVisualCollision(
            GameObject[,] pieceObjects,
            BoardPosition targetPos,
            PlayerColor attackerColor,
            out Piece defender)
        {
            defender = null;

            int col = targetPos.Column - 'a';
            int row = targetPos.Row - 1;

            if (col < 0 || col >= 5 || row < 0 || row >= 13)
                return false;

            GameObject obj = pieceObjects[col, row];
            if (obj == null) return false;

            PieceDisplay display = obj.GetComponent<PieceDisplay>();
            if (display == null || display.CurrentPiece == null) return false;

            Piece targetPiece = display.CurrentPiece;
            if (targetPiece.Color != attackerColor && targetPiece.Color != PlayerColor.None)
            {
                defender = targetPiece;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 视觉碰撞检测 — 同时返回 defender GameObject
        /// </summary>
        public static bool CheckVisualCollision(
            GameObject[,] pieceObjects,
            BoardPosition targetPos,
            PlayerColor attackerColor,
            out Piece defender,
            out GameObject defenderObj)
        {
            defender = null;
            defenderObj = null;

            int col = targetPos.Column - 'a';
            int row = targetPos.Row - 1;

            if (col < 0 || col >= 5 || row < 0 || row >= 13)
                return false;

            GameObject obj = pieceObjects[col, row];
            if (obj == null) return false;

            PieceDisplay display = obj.GetComponent<PieceDisplay>();
            if (display == null || display.CurrentPiece == null) return false;

            Piece targetPiece = display.CurrentPiece;
            if (targetPiece.Color != attackerColor && targetPiece.Color != PlayerColor.None)
            {
                defender = targetPiece;
                defenderObj = obj;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 己方阻挡检测 - 检查目标位置是否有己方棋子 GameObject
        /// </summary>
        public static bool CheckFriendlyBlock(
            GameObject[,] pieceObjects,
            BoardPosition targetPos,
            PlayerColor attackerColor)
        {
            int col = targetPos.Column - 'a';
            int row = targetPos.Row - 1;

            if (col < 0 || col >= 5 || row < 0 || row >= 13)
                return false;

            GameObject obj = pieceObjects[col, row];
            if (obj == null) return false;

            PieceDisplay display = obj.GetComponent<PieceDisplay>();
            if (display == null || display.CurrentPiece == null) return false;

            return display.CurrentPiece.Color == attackerColor;
        }
    }
}
