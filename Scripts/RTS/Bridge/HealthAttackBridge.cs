/* ─── 暂注释：未使用的 CombatEngine 配套桥接 ───
using JunqiGame.Core;
using JunqiGame.RTS.Interfaces;

namespace JunqiGame.RTS.Bridge
{
    public static class HealthAttackBridge
    {
        public static ICombatEntity ToCombatEntity(Piece piece)
        {
            return new CombatEntityWrapper(piece);
        }

        private class CombatEntityWrapper : ICombatEntity
        {
            private Piece piece;
            public CombatEntityWrapper(Piece p) { piece = p; }

            public int Health { get => piece.Health; set { piece.Health = value; } }
            public int Attack { get => piece.Attack; set { piece.Attack = value; } }
            
            public bool IsFlag => piece.IsFlag();
            public bool IsMine => piece.IsMine();
            public bool IsBomb => piece.IsBomb();
            public bool IsSapper => piece.IsSapper();
            public PlayerColor Color => piece.Color;
            public PieceRank Rank => piece.Rank;
        }
    }
}
─── 暂注释结束 ───*/