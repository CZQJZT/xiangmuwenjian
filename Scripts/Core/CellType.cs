namespace JunqiGame.Core
{
    /// <summary>
    /// 棋盘格子类型枚举
    /// </summary>
    public enum CellType
    {
        /// <summary>
        /// 普通格子 - 可以放置棋子，可以移动
        /// </summary>
        Normal = 0,

        /// <summary>
        /// 行营 - 不能初始布阵，进入后免疫攻击
        /// </summary>
        Camp = 1,

        /// <summary>
        /// 铁路线 - 工兵可以飞行，其他棋子可以连续移动
        /// </summary>
        Railway = 2,

        /// <summary>
        /// 公路线 - 普通移动路径
        /// </summary>
        Road = 3,

        /// <summary>
        /// 无效格子 - 不能放置棋子（如第7行的偶数列）
        /// </summary>
        Invalid = 4
    }
}