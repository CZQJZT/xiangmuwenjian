/* ─── 暂注释：未使用的 CombatEngine 配套接口 ───
using JunqiGame.Core;

namespace JunqiGame.RTS.Interfaces
{
    public interface ICombatEntity
    {
        int Health { get; set; }
        int Attack { get; set; }
        bool IsFlag { get; }
        bool IsMine { get; }
        bool IsBomb { get; }
        bool IsSapper { get; }
        PlayerColor Color { get; }
        PieceRank Rank { get; }
    }
}
─── 暂注释结束 ───*/