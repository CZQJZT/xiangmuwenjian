using UnityEngine;
using JunqiGame.Core;
using JunqiGame.UI;
using JunqiGame.RTS;
using JunqiGame.RTS.Data;

namespace JunqiGame.MonoBehaviours
{
    /// <summary>
    /// 军棋游戏管理器 - Unity MonoBehaviour控制器
    /// 用于在Unity场景中管理游戏逻辑
    /// </summary>
    public class JunqiGameManager : MonoBehaviour
    {
        [Header("游戏设置")]
        [Tooltip("游戏模式")]
        public JunqiGame.Core.PlayMode GameMode = JunqiGame.Core.PlayMode.Concealed;
        
        [Tooltip("AI难度（如果是AI对战）")]
        public JunqiGame.Core.AIDifficulty AIDifficulty = JunqiGame.Core.AIDifficulty.Medium;
        
        [Tooltip("是否是AI对战")]
        public bool IsAIGame = true;

        [Header("玩家信息")]
        [Tooltip("蓝方玩家名称")]
        public string BluePlayerName = "Player";
        
        [Tooltip("红方玩家名称（AI）")]
        public string RedPlayerName = "Computer";

        [Header("RTS 设置")]
        [Tooltip("RTS 配置文件")]
        public RTSConfigSO RTSConfig;

        // 游戏状态
        private GameState gameState;
        
        // 单例
        private static JunqiGameManager instance;

        // 缓存引用（避免每步 FindObjectOfType）
        private RTSController cachedRTSController;
        private GameUIManager cachedUIManager;
        public static JunqiGameManager Instance => instance;

        private void Awake()
        {
            Application.targetFrameRate = 24;
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeGame();

            // 缓存引用
            cachedRTSController = FindObjectOfType<RTSController>();
            cachedUIManager = FindObjectOfType<GameUIManager>();

            // RTS 模式初始化
            if (RTSConfig != null && cachedRTSController != null)
            {
                cachedRTSController.Config = RTSConfig;
                cachedRTSController.gameManager = this;
                cachedRTSController.uiManager = cachedUIManager;
                cachedRTSController.EnableRTSMode(true);
                cachedRTSController.ResetState();
                Debug.Log($"✅ [JunqiGameManager] RTS mode enabled. APMax={RTSConfig.APMax}, Regen={RTSConfig.APRegenPerTick}/tick");
            }
        }

        /// <summary>
        /// 初始化游戏
        /// </summary>
        public void InitializeGame()
        {
            Debug.Log("Initializing Junqi Game...");
            
            // 创建游戏状态
            gameState = new GameState();
            gameState.SetPlayMode(GameMode);

            // 添加玩家
            var bluePlayer = new PlayerInfo(PlayerColor.Blue, BluePlayerName, "player-1");
            var redPlayer = new PlayerInfo(PlayerColor.Red, RedPlayerName, IsAIGame ? "ai-player" : "player-2");
            
            gameState.AddPlayer(bluePlayer);
            gameState.AddPlayer(redPlayer);

            // 注册事件
            gameState.OnStateChange += HandleStateChange;
            gameState.OnMoveExecuted += HandleMoveExecuted;
            gameState.OnGameEnded += HandleGameEnded;

            Debug.Log("Game initialized successfully!");
        }

        /// <summary>
        /// 开始AI布阵
        /// </summary>
        public void StartAILayout()
        {
            if (gameState == null)
            {
                Debug.LogError("Game state not initialized!");
                return;
            }

            Debug.Log("Generating AI layout...");
            
            // 为AI生成布阵
            gameState.InitializeAILayout(PlayerColor.Red, AIDifficulty);
            
            // 为玩家生成布阵（这里简化处理，实际应该让玩家自己布阵）
            gameState.InitializeAILayout(PlayerColor.Blue, AIDifficulty);

            // 完成布阵
            gameState.FinishSetup(gameState.Players[PlayerColor.Blue]);
            gameState.FinishSetup(gameState.Players[PlayerColor.Red]);

            Debug.Log($"Game started! Status: {gameState.Status}");
        }

        /// <summary>
        /// 执行移动
        /// </summary>
        public MoveResult MakeMove(string moveString)
        {
            if (gameState == null || gameState.Status != GameStatus.Ongoing)
            {
                Debug.LogWarning("Cannot make move: game is not ongoing");
                return new MoveResult(false, "Game is not ongoing");
            }

            PlayerColor currentPlayer = gameState.ActivePlayer?.Color ?? PlayerColor.None;
            Debug.Log($"Player {currentPlayer} making move: {moveString}");

            if (RTSConfig != null && RTSConfig.RTSModeEnabled)
            {
                if (cachedRTSController != null)
                {
                    if (!cachedRTSController.ConsumeAP(currentPlayer, 1f))
                    {
                        Debug.LogWarning($"⛔ [AP] Not enough AP for {currentPlayer}!");
                        return new MoveResult(false, "行动点不足！");
                    }
                }
            }

            MoveResult result = gameState.Move(moveString, currentPlayer);

            if (result.Success)
            {
                Debug.Log($"Move successful! {result.Message}");
            }
            else
            {
                Debug.LogWarning($"Move failed: {result.Message}");
            }

            return result;
        }

        /// <summary>
        /// AI执行移动（简化版本，随机选择合法移动）
        /// </summary>
        private void AIMakeMove()
        {
            if (gameState == null || gameState.Status != GameStatus.Ongoing)
            {
                Debug.LogWarning("⚠️ [AI] Cannot move: game state invalid");
                return;
            }

            if (cachedUIManager != null && cachedUIManager.IsAnimating)
            {
                Debug.LogWarning("⚠️ [AI] Animation in progress, waiting...");
                Invoke(nameof(AIMakeMove), 0.5f);
                return;
            }

            if (RTSConfig != null && RTSConfig.RTSModeEnabled)
            {
                if (cachedRTSController != null)
                {
                    if (!cachedRTSController.ConsumeAP(PlayerColor.Red, 1f))
                    {
                        Debug.LogWarning($"⚠️ [AI] Not enough AP! Skipping turn.");
                        return;
                    }
                }
            }

            var validMoves = gameState.GetValidMoves(PlayerColor.Red);
            
            if (validMoves.Count > 0)
            {
                int randomIndex = Random.Range(0, validMoves.Count);
                string aiMove = validMoves[randomIndex];
                
                Debug.Log($"🤖 [AI] Choosing move: {aiMove} (total {validMoves.Count} options)");
                
                MoveResult result = MakeMove(aiMove);
                
                if (result.Success && result.PathUsed != null)
                {
                    Debug.Log($"✅ [AI] Move validated, triggering animation with {result.PathUsed.Count} steps");
                    
                    if (cachedUIManager != null)
                    {
                        cachedUIManager.HandleMoveExecuted(result);
                    }
                }
                else if (result.Success)
                {
                    Debug.LogWarning("⚠️ [AI] Move success but no path data");
                }
                else
                {
                    Debug.LogError($"❌ [AI] Move failed: {result.Message}");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ [AI] No valid moves available!");
            }
        }

        /// <summary>
        /// 认输
        /// </summary>
        public void Forfeit()
        {
            if (gameState == null || gameState.Status != GameStatus.Ongoing)
                return;

            PlayerColor currentPlayer = gameState.ActivePlayer?.Color ?? PlayerColor.None;
            var result = gameState.Forfeit(currentPlayer);
            
            Debug.Log($"Game ended: {result.Reason}. Winner: {result.Winner}");
        }

        /// <summary>
        /// 重置游戏
        /// </summary>
        public void ResetGame()
        {
            if (gameState != null)
            {
                gameState.Reset();
            }
            
            InitializeGame();
            Debug.Log("Game reset!");
        }

        /// <summary>
        /// 获取当前游戏状态
        /// </summary>
        public GameState GetGameState()
        {
            return gameState;
        }

        /// <summary>
        /// 获取棋盘
        /// </summary>
        public Board GetBoard()
        {
            return gameState?.Board;
        }

        // 事件处理
        private void HandleStateChange(GameState state, string changeType)
        {
            Debug.Log($"State changed: {changeType}, Status: {state.Status}");
        }

        private void HandleMoveExecuted(MoveResult result)
        {
            Debug.Log($"Move executed: {result.Message}");
            if (result.CapturedPiece != null)
            {
                Debug.Log($"Captured: {result.CapturedPiece}");
            }
            if (result.CombatResult.HasValue)
            {
                Debug.Log($"Combat result: {result.CombatResult.Value}");
            }
            
            // RTS mode: AI moves are scheduled by RTSController, not by event chain
            // No automatic AI triggering here
        }

        private void HandleGameEnded(GameEndResult result)
        {
            Debug.Log($"=== GAME ENDED ===");
            Debug.Log($"Winner: {result.Winner}");
            Debug.Log($"Reason: {result.Reason}");
        }
    }
}
