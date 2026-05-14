namespace JunqiGame.Core
{
    /// <summary>
    /// 棋子颜色枚举
    /// </summary>
    public enum PlayerColor
    {
        None = 0,
        Blue = 1,   // 上方玩家
        Red = 2     // 下方玩家
    }

    /// <summary>
    /// 棋子等级枚举（对应原始代码中的rankStr）
    /// </summary>
    public enum PieceRank
    {
        Mine = 0,       // 地雷
        Marshal = 1,    // 司令
        General = 2,    // 军长
        MajorGeneral = 3,  // 师长
        Brigadier = 4,  // 旅长
        Colonel = 5,    // 团长
        Major = 6,      // 营长
        Captain = 7,    // 连长
        Lieutenant = 8, // 排长
        Sapper = 9,     // 工兵
        Bomb = 10,      // 炸弹
        Flag = 11       // 军旗
    }

    /// <summary>
    /// 游戏状态枚举
    /// </summary>
    public enum GameStatus
    {
        Setup,      // 布阵阶段
        Ongoing,    // 游戏中
        Finished    // 已结束
    }

    /// <summary>
    /// 游戏模式枚举
    /// </summary>
    public enum PlayMode
    {
        Concealed,  // 暗棋模式（默认）
        Revealed    // 明棋模式
    }

    /// <summary>
    /// AI难度级别
    /// </summary>
    public enum AIDifficulty
    {
        Easy,       // 简单 - 随机走法
        Medium,     // 中等 - MCTS有限搜索
        Hard,       // 困难 - MCTS深度搜索
        Cheating    // 作弊 - 完全信息
    }
    /// <summary>
    /// 移动类型
    /// </summary>
    public enum MoveType
    {
        Normal,     // 普通移动
        Capture     // 吃子移动
    }

    /// <summary>
    /// 战斗结果
    /// </summary>
    public enum CombatResult
    {
        AttackerWin,    // 攻击方胜利
        DefenderWin,    // 防守方胜利
        BothDie         // 同归于尽
    }
}
