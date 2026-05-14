using JunqiGame.Core;

namespace JunqiGame.RTS
{
    /// <summary>
    /// 记录双方当前 AP、最大 AP、当前激活方等状态
    /// </summary>
    public class RTSState
    {
        public PlayerColor ActiveColor = PlayerColor.Blue;
        public float APBlue = 3.0f;
        public float APRed = 3.0f;
        public float APMax;
        
        // 未来扩展：待执行的 RTSAction 队列
        // public System.Collections.Generic.Queue<RTSAction> ActionQueue = new Queue<RTSAction>();
    }
}