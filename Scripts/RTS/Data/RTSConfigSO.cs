using UnityEngine;

namespace JunqiGame.RTS.Data
{
    /// <summary>
    /// 全局 RTS 配置 - 数据驱动的全局开关与参数
    /// </summary>
    [CreateAssetMenu(fileName = "RTSConfig", menuName = "Junqi/RTS/Global Config")]
    public class RTSConfigSO : ScriptableObject
    {
        [Header("RTS 模式开关")]
        [Tooltip("是否启用 RTS 模式")]
        public bool RTSModeEnabled = false;

        [Header("行动点(AP)系统")]
        [Tooltip("双方初始行动点（AP 上限）")]
        public int APMax = 3;
        
        [Tooltip("每次 Tick 的行动点回复量")]
        public float APRegenPerTick = 0.1f;
        
        [Tooltip("Tick 间隔（用于 AP 回复与行动调度）")]
        public float RTSTickIntervalSeconds = 0.1f;

        [Header("战斗系统")]
        [Tooltip("战斗的基础 Tick 间隔")]
        public float CombatTickIntervalSeconds = 0.1f;
        
        [Tooltip("是否启用沿途碰撞检测")]
        public bool EnablePathCollisionDetection = true;
        
        [Tooltip("移动每格的 AP 消耗")]
        public float MoveCostPerStep = 0.5f;
        
        [Tooltip("攻击的 AP 消耗")]
        public float AttackCost = 1.0f;
    }
}