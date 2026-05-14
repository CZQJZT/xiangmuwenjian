/* ─── 暂注释：未使用的 CombatEngine（游戏使用 GameRules.ResolveCombat 直接战斗） ───
using UnityEngine;
using JunqiGame.Core;
using JunqiGame.RTS.Interfaces;

namespace JunqiGame.RTS
{
    public static class CombatEngine
    {
        public static CombatResult ExecuteFullCombat(ICombatEntity attacker, ICombatEntity defender, float tickInterval = 0.1f)
        {
            if (attacker == null || defender == null)
            {
                Debug.LogWarning("⚠️ [CombatEngine] Null entity detected");
                return CombatResult.BothDie;
            }

            Piece attackerPiece = CreatePieceFromEntity(attacker);
            Piece defenderPiece = CreatePieceFromEntity(defender);
            
            CombatResult specialResult = GameRules.ResolveCombat(attackerPiece, defenderPiece);
            
            if (specialResult != CombatResult.BothDie || attacker.IsBomb || defender.IsBomb)
            {
                Debug.Log($"🎯 [CombatEngine] Special rule applied: {specialResult}");
                return specialResult;
            }

            int attHP = attacker.Health;
            int defHP = defender.Health;
            int tickCount = 0;

            while (attHP > 0 && defHP > 0)
            {
                tickCount++;
                defHP -= Mathf.Max(1, attacker.Attack);
                if (defHP <= 0)
                {
                    Debug.Log($"⚔️ [CombatEngine] Attacker wins after {tickCount} ticks");
                    return CombatResult.AttackerWin;
                }

                attHP -= Mathf.Max(1, defender.Attack);
                if (attHP <= 0)
                {
                    Debug.Log($"⚔️ [CombatEngine] Defender wins after {tickCount} ticks");
                    return CombatResult.DefenderWin;
                }
            }

            Debug.Log($"⚔️ [CombatEngine] Both died after {tickCount} ticks");
            return CombatResult.BothDie;
        }

        private static Piece CreatePieceFromEntity(ICombatEntity entity)
        {
            Piece piece = new Piece(entity.Color, entity.Rank);
            piece.Health = entity.Health;
            piece.Attack = entity.Attack;
            return piece;
        }

        public static CombatResult TickFight(ICombatEntity attacker, ICombatEntity defender, float tickInterval)
        {
            return ExecuteFullCombat(attacker, defender, tickInterval);
        }
    }
}
─── 暂注释结束 ───*/