using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JunqiGame.Core;
using JunqiGame.MonoBehaviours;
using JunqiGame.RTS;
namespace JunqiGame.UI
{
    public class GameUIManager : MonoBehaviour
    {
        [Header("References")]
        public JunqiGameManager gameManager;
        public GameObject piecePrefab;
        public Transform boardCellsParent;
        
        [Header("Board References")]
        public Transform boardParent;
        
        [Header("UI Elements")]
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI messageText;
        public TextMeshProUGUI currentPlayerText;
        public TextMeshProUGUI apDisplayText;
        public Button finishLayoutButton;
        public Button cancelLayoutButton;
        public Button startButton;
        public Button forfeitButton;
        public Button resetButton;
        
        [Header("Animation Settings")]
        [Tooltip("棋子移动速度（单位/秒）")]
        public float moveSpeed = 2f;
        
        [Tooltip("是否启用移动动画")]
        public bool enableMoveAnimation = true;

        // 对象池 + 追踪
        private Dictionary<string, GameObject> pieceGameObjects = new Dictionary<string, GameObject>();
        private Stack<GameObject> piecePool = new Stack<GameObject>();
        private const int PoolPreAlloc = 50;

        public GameObject[,] boardCells = new GameObject[5, 13];
        public GameObject[,] pieceObjects = new GameObject[5, 13];
        private PieceSelectionManager selectionManager;
        private RTSController cachedRTSController;
        
        // 动画队列，防止多个动画同时执行
        private bool isAnimating = false;
        private bool isHandlingMove = false;  // 防止 OnMoveExecuted 重入
        
        /// <summary>
        /// 是否正在播放动画（供外部查询）
        /// </summary>
        public bool IsAnimating => isAnimating;

        private float lastAPUpdateTime = 0f;
        private float lastReconnectCheckTime = 0f;
        private float lastDisplayedBlueAP = -1f;
        private float lastDisplayedRedAP = -1f;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<JunqiGameManager>();
            }
            
            if (gameManager == null)
            {
                Debug.LogError("❌ JunqiGameManager 未找到！\n" +
                      "请确保场景中有包含 JunqiGameManager 组件的 GameObject。\n" +
                      "或者使用菜单：Junqi Game → Setup UI Structure 自动创建");
                enabled = false; // 禁用此组件
                return;
            }
            
            selectionManager = FindObjectOfType<PieceSelectionManager>();
            if (selectionManager == null)
            {
                Debug.LogWarning("⚠️ PieceSelectionManager not found! Creating one...");
                GameObject selectionObj = new GameObject("PieceSelectionManager");
                selectionManager = selectionObj.AddComponent<PieceSelectionManager>();
            }
            
            Debug.Log("✅ GameManager 找到成功！");
            SetupUI();
        }

        private void Start()
        {
            // 检查必要的引用
            if (piecePrefab == null)
            {
                Debug.LogError("❌ piecePrefab is not assigned in Inspector!");
            }
            else
            {
                Debug.Log($"✅ piecePrefab assigned: {piecePrefab.name}");
                
                // 检查 Prefab 是否有 PieceDisplay 组件
                PieceDisplay testDisplay = piecePrefab.GetComponent<PieceDisplay>();
                if (testDisplay == null)
                {
                    Debug.LogError("❌ piecePrefab does NOT have PieceDisplay component!");
                }
                else
                {
                    Debug.Log("✅ piecePrefab has PieceDisplay component");
                }
            }
            
            if (boardParent == null)
            {
                Debug.LogError("❌ boardParent is not assigned in Inspector!");
            }
            else
            {
                Debug.Log($"✅ boardParent assigned: {boardParent.name}, Children: {boardParent.childCount}");
            }

            // 🔑 确保 AnimationManager 存在
            if (AnimationManager.Instance == null)
            {
                Debug.LogWarning("⚠️ AnimationManager not found in scene, creating one...");
                GameObject animObj = new GameObject("AnimationManager");
                animObj.AddComponent<AnimationManager>();
                Debug.Log("✅ AnimationManager created automatically");
            }
            else
            {
                Debug.Log("✅ AnimationManager found in scene");
            }

            // 🔑 缓存 RTSController 引用
            cachedRTSController = FindObjectOfType<RTSController>();

            // 🔑 预分配对象池
            for (int i = 0; i < PoolPreAlloc; i++)
            {
                GameObject go = Instantiate(piecePrefab);
                go.SetActive(false);
                go.transform.SetParent(null);
                piecePool.Push(go);
            }

            // 🔑 注册游戏事件
            if (gameManager != null && gameManager.GetGameState() != null)
            {
                var gameState = gameManager.GetGameState();
                gameState.OnMoveExecuted += HandleMoveExecuted;
                Debug.Log("✅ OnMoveExecuted event registered successfully");
            }
            else
            {
                Debug.LogError("❌ Cannot register events: gameManager or gameState is null");
            }

            UpdateUI();
        }


private void Update()
        {
            float now = Time.time;

            // 定期检查 GameManager 是否仍然有效（每秒一次，不每帧）
            if (gameManager == null && now - lastReconnectCheckTime >= 1f)
            {
                lastReconnectCheckTime = now;
                gameManager = JunqiGameManager.Instance;
                
                if (gameManager != null)
                {
                    var gameState = gameManager.GetGameState();
                    if (gameState != null)
                    {
                        gameState.OnStateChange += HandleStateChange;
                        gameState.OnMoveExecuted += HandleMoveExecuted;
                        gameState.OnGameEnded += HandleGameEnded;
                    }
                }
            }

            // RTS 模式：节流更新 AP 显示（每0.3秒一次，减少UI更新开销）
            if (gameManager != null && gameManager.RTSConfig != null && gameManager.RTSConfig.RTSModeEnabled)
            {
                if (now - lastAPUpdateTime >= 0.3f)
                {
                    UpdateAPDisplay();
                    lastAPUpdateTime = now;
                }
            }
        }
        /// <summary>
        /// 设置UI
        /// </summary>
        private void SetupUI()
        {
            // 绑定按钮事件
            if (startButton != null)
                startButton.onClick.AddListener(OnStartButtonClick);
            
            if (forfeitButton != null)
                forfeitButton.onClick.AddListener(OnForfeitButtonClick);
            
            if (resetButton != null)
                resetButton.onClick.AddListener(OnResetButtonClick);
            
            if (finishLayoutButton != null)
            {
                finishLayoutButton.onClick.AddListener(OnFinishLayoutButtonClick);
                finishLayoutButton.gameObject.SetActive(false); // 初始隐藏
            }
            
            if (cancelLayoutButton != null)
            {
                cancelLayoutButton.onClick.AddListener(OnCancelLayoutButtonClick);
                cancelLayoutButton.gameObject.SetActive(false); // 初始隐藏
            }

            // 初始化棋盘显示
            InitializeBoardDisplay();
        }

        /// <summary>
        /// 初始化棋盘显示
        /// </summary>
   
        private void InitializeBoardDisplay()
        {
            if (boardParent == null)
            {
                Debug.LogError("❌ Board parent is not assigned!");
                return;
            }

            int successCount = 0;
            int childCount = boardParent.childCount;

            Debug.Log($"🔍 Initializing board display with {childCount} children");

            // 检查是否有命名规范的格子
            bool hasNamedCells = false;
            foreach (Transform child in boardParent)
            {
                string cellName = child.name.ToLower().Trim();
                if (cellName.Length >= 2 && char.IsLetter(cellName[0]) && char.IsDigit(cellName[1]))
                {
                    hasNamedCells = true;
                    break;
                }
            }

            if (hasNamedCells)
            {
                // 方法1：根据名称解析位置
                Debug.Log("📍 Using name-based assignment");
                
                foreach (Transform child in boardParent)
                {
                    string cellName = child.name.ToLower().Trim();
                    
                    if (cellName.Length < 2) continue;
                    
                    char columnChar = cellName[0];
                    string rowStr = new string(cellName.Skip(1).TakeWhile(char.IsDigit).ToArray());
                    
                    if (string.IsNullOrEmpty(rowStr)) continue;
                    if (!int.TryParse(rowStr, out int rowNum)) continue;
                    
                    int col = char.ToLower(columnChar) - 'a';
                    int row = rowNum - 1;
                    
                    if (col >= 0 && col < 5 && row >= 0 && row < 13)
                    {
                        RegisterCell(child.gameObject, col, row, columnChar, rowNum);
                        successCount++;
                    }
                }
            }
            else
            {
                // 方法2：根据子对象顺序自动分配（从左到右，从上到下）
                Debug.Log("📍 Using position-based auto assignment");
                
                // 首先按 Y 坐标排序（从上到下），再按 X 坐标排序（从左到右）
                List<Transform> sortedChildren = new List<Transform>();
                foreach (Transform child in boardParent)
                {
                    sortedChildren.Add(child);
                }
                
                // 排序：先按行（Y从大到小），再按列（X从小到大）
                sortedChildren.Sort((a, b) => {
                    float yDiff = b.position.y - a.position.y;
                    if (Mathf.Abs(yDiff) > 0.1f)
                        return yDiff.CompareTo(0);
                    
                    float xDiff = a.position.x - b.position.x;
                    return xDiff.CompareTo(0);
                });
                
                // 调试：打印排序后的前几个格子
                Debug.Log("📋 Sorted children order (first 20):");
                for (int i = 0; i < Mathf.Min(20, sortedChildren.Count); i++)
                {
                    Debug.Log($"   [{i}] {sortedChildren[i].name} at pos ({sortedChildren[i].position.x:F2}, {sortedChildren[i].position.y:F2})");
                }
                
                // 创建有效位置列表（跳过第7行的偶数列）
                List<(int col, int row, char columnChar, int rowNum)> validPositions = new List<(int, int, char, int)>();
                
                for (int r = 0; r < 13; r++)
                {
                    for (int c = 0; c < 5; c++)
                    {
                        char colChar = (char)('a' + c);
                        int rowNum = r + 1;
                        
                        // 跳过第7行的偶数列（b7, d7）
                        if (rowNum == 7 && (colChar == 'b' || colChar == 'd'))
                        {
                            continue;
                        }
                        
                        validPositions.Add((c, r, colChar, rowNum));
                    }
                }
                
                Debug.Log($"✅ Generated {validPositions.Count} valid positions");
                
                // 调试：打印有效位置列表的前几个
                Debug.Log("📋 Valid positions order (first 20):");
                for (int i = 0; i < Mathf.Min(20, validPositions.Count); i++)
                {
                    var pos = validPositions[i];
                    Debug.Log($"   [{i}] {pos.columnChar}{pos.rowNum} at boardCells[{pos.col},{pos.row}]");
                }
                
                // 按顺序分配位置
                for (int i = 0; i < sortedChildren.Count && i < validPositions.Count; i++)
                {
                    var pos = validPositions[i];
                    Transform child = sortedChildren[i];
                    
                    RegisterCell(child.gameObject, pos.col, pos.row, pos.columnChar, pos.rowNum);
                    
                    // 重命名为标准格式
                    child.name = $"{pos.columnChar}{pos.rowNum}";
                    
                    successCount++;
                }
                
                // 统计结果
                Debug.Log($"\n📊 Registration Summary:");
                Debug.Log($"   Total children: {sortedChildren.Count}");
                Debug.Log($"   Successfully registered: {successCount}");
                Debug.Log($"   Expected: 64 cells (5×13 - 2 invalid)");
                
                if (successCount < 64)
                {
                    Debug.LogWarning($"⚠️ Only {successCount} cells found. Expected 64.");
                }
                else
                {
                    Debug.Log("🎉 Board initialization complete!");
                }
            }
            
            // 统计结果
            Debug.Log($"\n📊 Registration Summary:");
            Debug.Log($"   Total children: {childCount}");
            Debug.Log($"   Successfully registered: {successCount}");
            Debug.Log($"   Expected: 65 cells (5×13)");
            
            if (successCount < 65)
            {
                Debug.LogWarning($"⚠️ Only {successCount} cells found. Expected 65.");
            }
            else
            {
                Debug.Log("🎉 Board initialization complete!");
            }
        }
        
        /// <summary>
        /// 注册单个格子
        /// </summary>
        private void RegisterCell(GameObject cellObj, int col, int row, char columnChar, int rowNum)
        {
            // 跳过第7行的偶数列（b7, d7）
            if (rowNum == 7 && (columnChar == 'b' || columnChar == 'd'))
            {
                Debug.Log($"⏭️ Skipping invalid position: {columnChar}{rowNum}");
                return;
            }
            
            boardCells[col, row] = cellObj;
            
            // 添加点击处理器
            BoardCellClickHandler clickHandler = cellObj.GetComponent<BoardCellClickHandler>();
            if (clickHandler == null)
            {
                clickHandler = cellObj.AddComponent<BoardCellClickHandler>();
            }
            clickHandler.Initialize(columnChar, rowNum, this);
            
            Debug.Log($"✅ Registered: {columnChar}{rowNum} at [{col},{row}]");
        }
        /// <summary>
        /// 更新UI显示
        /// </summary>
        public void UpdateUI()
        {
            if (gameManager == null) return;

            GameState gameState = gameManager.GetGameState();
            if (gameState == null) return;

            // 更新状态文本
            if (statusText != null)
            {
                statusText.text = $"游戏状态: {GetStatusText(gameState.Status)}";
            }

            // 更新当前玩家
            if (currentPlayerText != null && !gameManager.RTSConfig?.RTSModeEnabled == true)
            {
                if (gameState.ActivePlayer != null)
                {
                    currentPlayerText.text = $"当前玩家: {gameState.ActivePlayer.Name} ({gameState.ActivePlayer.Color})";
                }
            }
            else if (currentPlayerText != null)
            {
                currentPlayerText.text = "RTS 模式 - 双方同时行动";
            }

            UpdateAPDisplay();
        }

        /// <summary>
        /// 更新 AP 显示（仅值改变时更新TextMeshPro，减少GC）
        /// </summary>
        private void UpdateAPDisplay()
        {
            if (apDisplayText == null) return;
            if (gameManager == null || gameManager.RTSConfig == null || !gameManager.RTSConfig.RTSModeEnabled)
            {
                apDisplayText.text = "";
                return;
            }

            if (cachedRTSController == null)
            {
                cachedRTSController = FindObjectOfType<RTSController>();
                if (cachedRTSController == null)
                {
                    apDisplayText.text = "";
                    return;
                }
            }

            float blueAP = cachedRTSController.GetCurrentAP(PlayerColor.Blue);
            float redAP = cachedRTSController.GetCurrentAP(PlayerColor.Red);

            if (Mathf.Approximately(blueAP, lastDisplayedBlueAP) && Mathf.Approximately(redAP, lastDisplayedRedAP))
                return;

            lastDisplayedBlueAP = blueAP;
            lastDisplayedRedAP = redAP;
            int apMax = gameManager.RTSConfig.APMax;
            apDisplayText.text = $"blueAP: {blueAP:F1}/{apMax}   redAP: {redAP:F1}/{apMax}";
        }

        /// <summary>
        /// 更新棋盘上的棋子显示
        /// </summary>
        public void UpdateBoardPieces()
        {
            if (gameManager == null) return;

            Board board = gameManager.GetBoard();
            if (board == null) return;

            // 使用销毁重建方式
            ClearPieceDisplay();
            DisplayAllPieces(board);
        }

        /// <summary>
        /// 从对象池获取或创建一个棋子 GameObject
        /// </summary>
        private GameObject SpawnPiece(Transform parent)
        {
            GameObject go;
            if (piecePool.Count > 0)
            {
                go = piecePool.Pop();
                go.SetActive(true);
            }
            else
            {
                go = Instantiate(piecePrefab);
            }
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null) rt.localScale = new Vector3(40f, 40f, 1f);
            return go;
        }

        /// <summary>
        /// 将棋子返还对象池（替代 Destroy）
        /// </summary>
        public void ReturnPieceToPool(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            go.transform.SetParent(null);
            piecePool.Push(go);
        }

        /// <summary>
        /// 在指定位置显示棋子
        /// </summary>
        public void DisplayPieceAtPosition(int col, int row, Piece piece, string posKey)
        {
            if (piecePrefab == null || boardCells[col, row] == null) return;

            GameObject pieceObj = SpawnPiece(boardCells[col, row].transform);
            pieceObj.name = $"{piece.Color}_{piece.Rank}";
            
            BoardPosition pos = new BoardPosition((char)('a' + col), row + 1);
            SetupPieceDisplay(pieceObj, piece, pos);
            pieceGameObjects[posKey] = pieceObj;
            pieceObjects[col, row] = pieceObj;
        }

        /// <summary>
        /// 设置棋子显示
        /// </summary>
        private void SetupPieceDisplay(GameObject pieceObj, Piece piece, BoardPosition position)
        {
            PieceDisplay display = pieceObj.GetComponent<PieceDisplay>();
            if (display != null) display.SetPiece(piece);
            
            // 添加拖动处理器（仅在布阵阶段启用）
            PieceDragHandler dragHandler = pieceObj.GetComponent<PieceDragHandler>();
            if (dragHandler == null)
            {
                dragHandler = pieceObj.AddComponent<PieceDragHandler>();
            }
            dragHandler.Initialize(position);
        }

        /// <summary>
        /// 显示所有棋子
        /// </summary>
        private void DisplayAllPieces(Board board)
        {
            pieceGameObjects.Clear();
            for (int col = 0; col < 5; col++)
            {
                for (int row = 0; row < 13; row++)
                {
                    if (boardCells[col, row] == null) continue;
                    char columnChar = (char)('a' + col);
                    int rowNum = row + 1;
                    string posKey = $"{columnChar}{rowNum}";
                    
                    BoardPosition pos = new BoardPosition(columnChar, rowNum);
                    Piece piece = board.GetPiece(pos);

                    if (piece != null && piecePrefab != null)
                    {
                        GameObject pieceObj = SpawnPiece(boardCells[col, row].transform);
                        pieceObj.name = $"{piece.Color}_{piece.Rank}";
                        SetupPieceDisplay(pieceObj, piece, pos);
                        pieceGameObjects[posKey] = pieceObj;
                        pieceObjects[col, row] = pieceObj;
                    }
                }
            }
        }

        /// <summary>
        /// 清除所有棋子显示
        /// </summary>
        private void ClearPieceDisplay()
        {
            foreach (var kvp in pieceGameObjects)
            {
                if (kvp.Value != null) ReturnPieceToPool(kvp.Value);
            }
            pieceGameObjects.Clear();
            
            for (int col = 0; col < 5; col++)
            {
                for (int row = 0; row < 13; row++)
                {
                    pieceObjects[col, row] = null;
                }
            }
        }

        /// <summary>
        /// 执行路径移动动画（调用 AnimationManager）
        /// </summary>
        public void ExecutePathAnimation(List<BoardPosition> path)
        {
            Debug.Log($"🔍 [ExecutePathAnimation] enableMoveAnimation={enableMoveAnimation}, path={(path == null ? "null" : $"Count={path.Count}")}");
            
            if (!enableMoveAnimation || path == null || path.Count < 2)
            {
                Debug.Log("⚠️ [Animation] Animation disabled or invalid path, updating UI directly");
                UpdateUI();
                return;
            }

            if (isAnimating)
            {
                Debug.LogWarning("⚠️ [Animation] Another animation is running, updating UI directly");
                UpdateUI();
                return;
            }

            // 获取起点棋子
            BoardPosition startPos = path[0];
            int startCol = startPos.Column - 'a';
            int startRow = startPos.Row - 1;
            
            Debug.Log($"🔍 [ExecutePathAnimation] Looking for piece at [{startCol},{startRow}] = {startPos}");
            
            GameObject pieceObj = pieceObjects[startCol, startRow];
            
            if (pieceObj == null)
            {
                Debug.LogError($"❌ [Animation] Piece object not found at {startPos}");
                UpdateUI();
                return;
            }

            Debug.Log($"✅ [ExecutePathAnimation] Found piece: {pieceObj.name}");

            // 🔑 关键：检查 AnimationManager 是否存在
            if (AnimationManager.Instance == null)
            {
                Debug.LogError("❌ [Animation] AnimationManager.Instance is null! Cannot play animation.");
                UpdateUI();
                return;
            }

            // 🔑 将路径转换为世界坐标数组
            Vector3[] worldPositions = Board.PathToWorldPositions(path, boardCells);
            
            // 检查是否有无效坐标
            bool hasInvalid = false;
            for (int i = 0; i < worldPositions.Length; i++)
            {
                if (worldPositions[i] == Vector3.zero)
                {
                    Debug.LogError($"❌ [Animation] Invalid world position at index {i} ({path[i]})");
                    hasInvalid = true;
                }
            }

            if (hasInvalid)
            {
                Debug.LogError("❌ [Animation] Some world positions are invalid, cannot play animation");
                UpdateUI();
                return;
            }

            Debug.Log($"✅ [Animation] Calling AnimationManager.PlayMoveAlongPath...");
            
            // 🔑 设置动画状态
            isAnimating = true;
            
            AnimationManager.Instance.PlayMoveAlongPath(
                pieceObj,
                path,
                boardCells,
                moveSpeed,
                (stepIdx, curPos) => {
                    Debug.Log($"📍 [Animation] Step {stepIdx} completed");
                    return true;
                },
                () => {
                    Debug.Log($"🎉 [Animation] Animation completed callback");
                    isAnimating = false;
                    UpdateUI();
                }
            );
            
            Debug.Log($"✅ [Animation] PlayMoveAlongPath called successfully");
        }

        /// <summary>
        /// 处理格子点击
        /// </summary>
         public void HandleCellClick(char column, int row)
        {
            BoardPosition clickedPos = new BoardPosition(column, row);
            
            // 检查是否在布阵阶段
            if (gameManager != null)
            {
                GameState gameState = gameManager.GetGameState();
                if (gameState != null && gameState.Status == GameStatus.Setup)
                {
                    // 布阵阶段：AI已自动布阵，不允许玩家手动放置
                    Debug.Log("⚠️ Setup phase: AI layout is automatic, no manual placement");
                    return;
                }
            }

            // 游戏阶段：转发给 PieceSelectionManager 处理
            if (selectionManager != null)
            {
                selectionManager.HandleCellClick(clickedPos);
            }
            else
            {
                Debug.LogWarning("⚠️ SelectionManager is null!");
            }
        }

        /// <summary>
        /// 显示消息
        /// </summary>
        public void ShowMessage(string message, float duration = 2f)
        {
            if (messageText != null)
            {
                messageText.text = message;
                
                if (duration > 0)
                {
                    Invoke(nameof(ClearMessage), duration);
                }
            }
        }

        /// <summary>
        /// 清除消息
        /// </summary>
       public GameObject GetPieceObject(int col, int row)
        {
            if (col >= 0 && col < 5 && row >= 0 && row < 13)
            {
                return pieceObjects[col, row];
            }
            return null;
        }

        /// <summary>
        /// 获取指定棋盘位置的棋子对象
        /// </summary>
        public GameObject GetPieceObjectAt(BoardPosition position)
        {
            if (!position.IsValid())
                return null;

            int col = position.Column - 'a';
            int row = position.Row - 1;

            return GetPieceObject(col, row);
        }

        /// <summary>
        /// 获取指定棋盘位置的格子对象
        /// </summary>
        public GameObject GetCellObjectAt(BoardPosition position)
        {
            if (!position.IsValid())
                return null;

            int col = position.Column - 'a';
            int row = position.Row - 1;

            if (col >= 0 && col < 5 && row >= 0 && row < 13)
            {
                return boardCells[col, row];
            }
            return null;
        }

                /// <summary>
        /// 同步棋子位置（动画每步完成后调用）
        /// </summary>
        public void SyncPiecePosition(BoardPosition from, BoardPosition to)
        {
            int fromCol = from.Column - 'a';
            int fromRow = from.Row - 1;
            int toCol = to.Column - 'a';
            int toRow = to.Row - 1;

            if (fromCol < 0 || fromCol >= 5 || fromRow < 0 || fromRow >= 13) return;
            if (toCol < 0 || toCol >= 5 || toRow < 0 || toRow >= 13) return;

            GameObject pieceObj = pieceObjects[fromCol, fromRow];
            if (pieceObj != null)
            {
                // 🔑 关键修复：直接从GameObject的PieceDisplay获取棋子数据
                // 而不是查询棋盘数据（因为棋盘数据可能已经更新到最终位置）
                PieceDisplay display = pieceObj.GetComponent<PieceDisplay>();
                Piece movingPiece = null;
                
                if (display != null && display.CurrentPiece != null)
                {
                    movingPiece = display.CurrentPiece;
                    Debug.Log($"🔄 [UI] Got piece from display: {movingPiece.Rank} ({movingPiece.Color})");
                }
                else
                {
                    // 备用方案：尝试从棋盘数据获取
                    GameState gameState = gameManager != null ? gameManager.GetGameState() : null;
                    if (gameState != null)
                    {
                        movingPiece = gameState.Board.GetPiece(from);
                        if (movingPiece == null)
                        {
                            movingPiece = gameState.Board.GetPiece(to);
                        }
                    }
                }

                // 移动GameObject引用
                pieceObjects[fromCol, fromRow] = null;
                pieceObjects[toCol, toRow] = pieceObj;

                // 改变父对象（跟随格子）
                GameObject targetCell = boardCells[toCol, toRow];
                if (targetCell != null)
                {
                    pieceObj.transform.SetParent(targetCell.transform);
                    pieceObj.transform.localPosition = Vector3.zero;
                }

                // 🔑 关键修复：使用获取到的棋子数据更新显示
                if (movingPiece != null)
                {
                    if (display != null)
                    {
                        display.SetPiece(movingPiece);
                        Debug.Log($"🔄 [UI] Updated display at {to}: {movingPiece.Rank}");
                    }
                }
                else
                {
                    Debug.LogWarning($"⚠️ [UI] No piece data found for move {from} -> {to}. Keeping GameObject visible.");
                }

                Debug.Log($"🔄 [UI] Synced piece: {from} -> {to}");
            }
            else
            {
                Debug.LogWarning($"⚠️ [UI] No GameObject at {from}, cannot sync");
            }
        }
         private void ClearMessage()
        {
            if (messageText != null)
            {
                messageText.text = "";
            }
        }

        // UI事件处理
        public void OnStartButtonClick()
        {
            Debug.Log("Start button clicked");
            
            // 进入布阵阶段
            EnterLayoutPhase();
        }
        
        /// <summary>
        /// 进入布阵阶段
        /// </summary>
        private void EnterLayoutPhase()
        {
            Debug.Log("🎮 EnterLayoutPhase called");
            
            if (gameManager == null)
            {
                Debug.LogError("❌ gameManager is null in EnterLayoutPhase");
                return;
            }
            
            GameState gameState = gameManager.GetGameState();
            if (gameState == null)
            {
                Debug.LogError("❌ gameState is null in EnterLayoutPhase");
                return;
            }
            
            Debug.Log($"Current game status before layout: {gameState.Status}");
            
            // 重置游戏状态到Setup
            gameState.Reset();
            
            // 重新添加玩家
            var bluePlayer = new PlayerInfo(PlayerColor.Blue, gameManager.BluePlayerName, "player-1");
            var redPlayer = new PlayerInfo(PlayerColor.Red, gameManager.RedPlayerName, gameManager.IsAIGame ? "ai-player" : "player-2");
            gameState.AddPlayer(bluePlayer);
            gameState.AddPlayer(redPlayer);
            
            Debug.Log($"Game status after reset: {gameState.Status}");
            
            // 为AI生成布阵（红方）
            gameState.InitializeAILayout(PlayerColor.Red, gameManager.AIDifficulty);
            Debug.Log("Red AI layout completed");
            
            // 为玩家生成初始布阵（蓝方）
            gameState.InitializeAILayout(PlayerColor.Blue, gameManager.AIDifficulty);
            Debug.Log("Blue AI layout completed");
            
            Debug.Log("AI布阵完成，玩家可以调整棋子位置");
            ShowMessage("请调整你的棋子位置，然后点击'开始对局'");
            
            // 显示"开始对局"按钮
            if (finishLayoutButton != null)
            {
                finishLayoutButton.gameObject.SetActive(true);
                Debug.Log("✅ finishLayoutButton activated");
                
                // 修改按钮文本为"开始对局"
                UnityEngine.UI.Text buttonText = finishLayoutButton.GetComponent<UnityEngine.UI.Text>();
                if (buttonText != null)
                {
                    buttonText.text = "开始对局";
                }
            }
            else
            {
                Debug.LogError("❌ finishLayoutButton is not assigned!");
            }
            
            // 隐藏取消布阵按钮（因为不需要重新布阵）
            if (cancelLayoutButton != null)
            {
                cancelLayoutButton.gameObject.SetActive(false);
            }
            
            // 更新棋盘显示（直接刷新，不经过 UpdateUI）
            UpdateBoardPieces();
            
            Debug.Log($"Final game status: {gameState.Status}, Board pieces: {gameState.Board.PieceCount}");
        }
        
        /// <summary>
        /// 完成布阵按钮点击（验证并开始对局）
        /// </summary>
        private void OnFinishLayoutButtonClick()
        {
            if (gameManager == null) return;
            
            GameState gameState = gameManager.GetGameState();
            if (gameState == null) return;
            
            Board board = gameManager.GetBoard();
            if (board == null) return;
            
            // 验证玩家的布阵是否合法
            bool isValid = ValidatePlayerLayout(board, PlayerColor.Blue);
            
            if (!isValid)
            {
                ShowMessage("布阵不合法，请检查棋子位置！", 3f);
                return;
            }
            
            // 布阵合法，完成布阵并开始游戏
            gameState.FinishSetup(gameState.Players[PlayerColor.Blue]);
            gameState.FinishSetup(gameState.Players[PlayerColor.Red]);
            
            Debug.Log("布阵验证通过，游戏开始");
            ShowMessage("游戏开始！");
            
            // 隐藏布阵按钮
            if (finishLayoutButton != null)
                finishLayoutButton.gameObject.SetActive(false);
            
            // 更新棋盘显示（直接刷新，不经过 UpdateUI）
            UpdateBoardPieces();
        }
        
        /// <summary>
        /// 验证玩家布阵是否合法
        /// </summary>
        private bool ValidatePlayerLayout(Board board, PlayerColor playerColor)
        {
            // 获取玩家所有棋子的位置
            var positions = board.GetPiecesByColor(playerColor);
            
            // 统计各种棋子的数量
            int flagCount = 0;
            int mineCount = 0;
            int bombCount = 0;
            int sapperCount = 0;
            int marshalCount = 0;
            int generalCount = 0;
            int majorGeneralCount = 0;
            int brigadierCount = 0;
            int colonelCount = 0;
            int majorCount = 0;
            int captainCount = 0;
            int lieutenantCount = 0;
            
            // 确定玩家的行范围
            int minRow = playerColor == PlayerColor.Blue ? 1 : 8;
            int maxRow = playerColor == PlayerColor.Blue ? 6 : 13;
            
            foreach (var pos in positions)
            {
                // 检查是否在合法区域内
                if (pos.Row < minRow || pos.Row > maxRow)
                {
                    Debug.LogWarning($"棋子 {pos} 不在合法区域内");
                    return false;
                }
                
                // 检查是否在行营中
                if (Board.IsCamp(pos))
                {
                    Debug.LogWarning($"棋子 {pos} 在行营中，不允许");
                    return false;
                }
                
                Piece piece = board.GetPiece(pos);
                if (piece == null) continue;
                
                // 统计棋子数量
                switch (piece.Rank)
                {
                    case PieceRank.Flag:
                        flagCount++;
                        break;
                    case PieceRank.Mine:
                        mineCount++;
                        break;
                    case PieceRank.Bomb:
                        bombCount++;
                        break;
                    case PieceRank.Sapper:
                        sapperCount++;
                        break;
                    case PieceRank.Marshal:
                        marshalCount++;
                        break;
                    case PieceRank.General:
                        generalCount++;
                        break;
                    case PieceRank.MajorGeneral:
                        majorGeneralCount++;
                        break;
                    case PieceRank.Brigadier:
                        brigadierCount++;
                        break;
                    case PieceRank.Colonel:
                        colonelCount++;
                        break;
                    case PieceRank.Major:
                        majorCount++;
                        break;
                    case PieceRank.Captain:
                        captainCount++;
                        break;
                    case PieceRank.Lieutenant:
                        lieutenantCount++;
                        break;
                }
            }
            
            // 验证棋子数量是否正确
            bool isValid = true;
            string errorMessage = "";
            
            if (flagCount != 1)
            {
                isValid = false;
                errorMessage += $"军旗数量错误：{flagCount}（应为1）\n";
            }
            
            if (mineCount != 3)
            {
                isValid = false;
                errorMessage += $"地雷数量错误：{mineCount}（应为3）\n";
            }
            
            if (bombCount != 2)
            {
                isValid = false;
                errorMessage += $"炸弹数量错误：{bombCount}（应为2）\n";
            }
            
            if (sapperCount != 3)
            {
                isValid = false;
                errorMessage += $"工兵数量错误：{sapperCount}（应为3）\n";
            }
            
            if (marshalCount != 1)
            {
                isValid = false;
                errorMessage += $"司令数量错误：{marshalCount}（应为1）\n";
            }
            
            if (generalCount != 1)
            {
                isValid = false;
                errorMessage += $"军长数量错误：{generalCount}（应为1）\n";
            }
            
            if (majorGeneralCount != 2)
            {
                isValid = false;
                errorMessage += $"师长数量错误：{majorGeneralCount}（应为2）\n";
            }
            
            if (brigadierCount != 2)
            {
                isValid = false;
                errorMessage += $"旅长数量错误：{brigadierCount}（应为2）\n";
            }
            
            if (colonelCount != 2)
            {
                isValid = false;
                errorMessage += $"团长数量错误：{colonelCount}（应为2）\n";
            }
            
            if (majorCount != 2)
            {
                isValid = false;
                errorMessage += $"营长数量错误：{majorCount}（应为2）\n";
            }
            
            if (captainCount != 3)
            {
                isValid = false;
                errorMessage += $"连长数量错误：{captainCount}（应为3）\n";
            }
            
            if (lieutenantCount != 3)
            {
                isValid = false;
                errorMessage += $"排长数量错误：{lieutenantCount}（应为3）\n";
            }
            
            if (!isValid)
            {
                Debug.LogWarning($"布阵验证失败：\n{errorMessage}");
            }
            else
            {
                Debug.Log("布阵验证通过");
            }
            
            return isValid;
        }
        
        /// <summary>
        /// 取消布阵按钮点击
        /// </summary>
        private void OnCancelLayoutButtonClick()
        {
            Debug.Log("⚠️ Cancel layout not supported in AI layout mode");
            ShowMessage("AI 布阵模式不支持取消");
        }

        private void OnForfeitButtonClick()
        {
            Debug.Log("Forfeit button clicked");
            gameManager.Forfeit();
            UpdateUI();
        }

        private void OnResetButtonClick()
        {
            Debug.Log("Reset button clicked");
            gameManager.ResetGame();
            UpdateUI();
            ShowMessage("游戏已重置");
        }

        // 游戏事件处理
        private void HandleStateChange(GameState state, string changeType)
        {
            UpdateUI();
        }

        /// <summary>
        /// 处理移动执行事件（公共方法，供 PieceSelectionManager 调用）
        /// </summary>
        public void HandleMoveExecuted(MoveResult result)
        {
            if (isHandlingMove) return;  // 防止动画完成回调重入
            isHandlingMove = true;
            Debug.Log($"🔔 [HandleMoveExecuted] Success={result.Success}");
            
            if (result.PathUsed != null)
            {
                Debug.Log($"   📍 PathUsed Count: {result.PathUsed.Count}");
            }
            
            if (result.Success)
            {
                string message = result.Message;
                
                // 🔑 关键：只要有路径就播放动画
                if (enableMoveAnimation && result.PathUsed != null && result.PathUsed.Count >= 2)
                {
                    Debug.Log($"✅ Will play animation: {result.PathUsed[0]} -> {result.PathUsed[result.PathUsed.Count - 1]}");
                    
                    if (result.PathUsed.Count > 2)
                    {
                        message += $" (路径: {result.PathUsed.Count - 1} 步)";
                    }
                    
                    // 🔑 播放动画，不立即刷新UI
                    ExecutePathAnimationWithSync(result.PathUsed, result.Attacker);
                    
                    ShowMessage(message);
                }
                else
                {
                    // 没有路径或动画禁用，直接更新
                    if (selectionManager != null)
                    {
                        selectionManager.ClearSelection();
                    }
                    UpdateUI();
                    ShowMessage(message);
                }
            }
            else
            {
                if (selectionManager != null)
                {
                    selectionManager.ClearSelection();
                }
                UpdateUI();
                ShowMessage($"移动失败: {result.Message}");
            }
            isHandlingMove = false;
        }

        /// <summary>
        /// 执行路径动画 — 视觉碰撞驱动（动画层先移动，接触到棋子后再更新数据层）
        /// 取代旧的 pre-check + truncate 模式
        /// </summary>
        private void ExecutePathAnimationWithSync(System.Collections.Generic.List<BoardPosition> path, Piece attacker)
        {
            if (path == null || path.Count < 2)
            {
                Debug.LogError("❌ Invalid path");
                UpdateUI();
                return;
            }

            BoardPosition startPos = path[0];
            int startCol = startPos.Column - 'a';
            int startRow = startPos.Row - 1;

            GameObject pieceObj = pieceObjects[startCol, startRow];
            if (pieceObj == null)
            {
                Debug.LogError($"❌ Piece not found at {startPos}");
                UpdateUI();
                return;
            }

            if (AnimationManager.Instance == null)
            {
                Debug.LogError("❌ AnimationManager not found");
                UpdateUI();
                return;
            }

            // 状态（在 per-step 闭包中更新）
            bool collisionHandled = false;
            bool friendlyBlocked = false;
            BoardPosition collisionPos = new BoardPosition('\0', 0);
            Piece collisionDefenderPiece = null;
            CombatResult collisionCombatResult = CombatResult.BothDie;

            isAnimating = true;

            AnimationManager.Instance.PlayMoveAlongPath(
                pieceObj,
                path,
                boardCells,
                moveSpeed,
                (stepIdx, curPos) =>
                {
                    BoardPosition stepFrom = path[stepIdx - 1];

                    // ═══ Phase 1: 视觉碰撞检测（敌方，行营免疫） ═══
                    Piece visualDefender;
                    if (!Board.IsCamp(curPos) && PathCombatDetector.CheckVisualCollision(pieceObjects, curPos, attacker.Color, out visualDefender))
                    {
                        collisionHandled = true;
                        collisionPos = curPos;
                        collisionDefenderPiece = visualDefender;
                        collisionCombatResult = GameRules.ResolveCombat(attacker, visualDefender);

                        Debug.Log($"💥 [VisualCollision] {attacker.Rank}({attacker.Color}) hit {visualDefender.Rank}({visualDefender.Color}) at {curPos}, Result={collisionCombatResult}");

                        ApplyVisualCollisionResult(pieceObj, stepFrom, curPos, attacker, visualDefender, collisionCombatResult);
                        return false;
                    }

                    // ═══ Phase 1.5: 行营阻挡（行营内有棋子时不可进入） ═══
                    if (Board.IsCamp(curPos))
                    {
                        int cc = curPos.Column - 'a', rr = curPos.Row - 1;
                        if (pieceObjects[cc, rr] != null)
                        {
                            friendlyBlocked = true;
                            Debug.Log($"⛔ [CampBlock] {attacker.Rank}({attacker.Color}) blocked by camp at {curPos}");
                            GameObject prevCell = boardCells[stepFrom.Column - 'a', stepFrom.Row - 1];
                            if (prevCell != null)
                            {
                                pieceObj.transform.SetParent(prevCell.transform);
                                pieceObj.transform.localPosition = Vector3.zero;
                            }
                            return false;
                        }
                    }

                    // ═══ Phase 1.75: 己方阻挡检测 ═══
                    if (PathCombatDetector.CheckFriendlyBlock(pieceObjects, curPos, attacker.Color))
                    {
                        friendlyBlocked = true;
                        Debug.Log($"🚫 [FriendlyBlock] {attacker.Rank}({attacker.Color}) blocked by friendly at {curPos}");
                        GameObject prevCell = boardCells[stepFrom.Column - 'a', stepFrom.Row - 1];
                        if (prevCell != null)
                        {
                            pieceObj.transform.SetParent(prevCell.transform);
                            pieceObj.transform.localPosition = Vector3.zero;
                        }
                        return false;
                    }

                    // ═══ Phase 2: 无阻挡 — 正常移动 ═══
                    if (stepIdx < path.Count)
                    {
                        GameRules.ApplyMoveStep(gameManager.GetGameState().Board, stepFrom, curPos);
                        UpdatePieceObjectPosition(stepFrom, curPos);
                    }
                    return true;
                },
                () =>
                {
                    var gs = gameManager.GetGameState();

                    if (friendlyBlocked)
                    {
                        // 己方阻挡：数据不动，不切换回合，仅刷新 UI
                        Debug.Log("🚫 [Complete] Friendly block, no state change");
                    }
                    else if (collisionHandled)
                    {
                        MoveResult result = new MoveResult(true, $"Collision at {collisionPos}", attacker, collisionDefenderPiece, collisionCombatResult, path);
                        gs.FinalizeMove(result, attacker.Color);
                    }
                    else
                    {
                        Piece finalPiece = gs.Board.GetPiece(path[path.Count - 1]);
                        MoveResult result = new MoveResult(true, "Move completed", finalPiece, null, null, path);
                        gs.FinalizeMove(result, attacker.Color);
                    }

                    isAnimating = false;
                    UpdateUI();
                }
            );
        }

        /// <summary>
        /// 应用视觉碰撞结果 — 处理数据层 + UI 层的战斗后果
        /// </summary>
        private void ApplyVisualCollisionResult(
            GameObject attackerObj,
            BoardPosition from,
            BoardPosition collisionPos,
            Piece attacker,
            Piece defender,
            CombatResult combatResult)
        {
            int fromCol = from.Column - 'a';
            int fromRow = from.Row - 1;
            int colCol = collisionPos.Column - 'a';
            int colRow = collisionPos.Row - 1;

            // 先移动攻击方到碰撞位置（数据层会覆盖防守方）
            GameRules.ApplyMoveStep(gameManager.GetGameState().Board, from, collisionPos);

            switch (combatResult)
            {
                case CombatResult.AttackerWin:
                    DestroyPieceObjectAt(colCol, colRow);
                    UpdatePieceObjectPosition(from, collisionPos);
                    break;

                case CombatResult.DefenderWin:
                    ReturnPieceToPool(attackerObj);
                    DestroyPieceObjectAt(colCol, colRow);
                    gameManager.GetGameState().Board.RemovePiece(collisionPos);
                    gameManager.GetGameState().Board.PlacePiece(collisionPos, defender.Clone());
                    DisplayPieceAtPosition(colCol, colRow, defender, collisionPos.ToString());
                    pieceObjects[fromCol, fromRow] = null;
                    { string fk = from.ToString(); if (pieceGameObjects.ContainsKey(fk)) pieceGameObjects.Remove(fk); }
                    break;

                case CombatResult.BothDie:
                    ReturnPieceToPool(attackerObj);
                    DestroyPieceObjectAt(colCol, colRow);
                    gameManager.GetGameState().Board.RemovePiece(collisionPos);
                    pieceObjects[fromCol, fromRow] = null;
                    { string fk = from.ToString(); if (pieceGameObjects.ContainsKey(fk)) pieceGameObjects.Remove(fk); }
                    break;
            }
        }


        

        /// <summary>
        /// 更新棋子对象的内部数据结构
        /// </summary>
        private void UpdatePieceObjectPosition(BoardPosition from, BoardPosition to)
        {
            int fromCol = from.Column - 'a';
            int fromRow = from.Row - 1;
            int toCol = to.Column - 'a';
            int toRow = to.Row - 1;

            // 移动 pieceObjects 引用
            GameObject pieceObj = pieceObjects[fromCol, fromRow];
            pieceObjects[fromCol, fromRow] = null;
            pieceObjects[toCol, toRow] = pieceObj;

            // 更新 pieceGameObjects 字典
            string fromKey = $"{from.Column}{from.Row}";
            string toKey = $"{to.Column}{to.Row}";
            
            if (pieceGameObjects.ContainsKey(fromKey))
            {
                pieceGameObjects.Remove(fromKey);
            }
            pieceGameObjects[toKey] = pieceObj;
        }

        /// <summary>
        /// 清空指定位置在 pieceObjects / pieceGameObjects 中的引用（不销毁 GameObject）
        /// </summary>
        public void ClearPieceReference(int col, int row)
        {
            if (col < 0 || col >= 5 || row < 0 || row >= 13) return;
            pieceObjects[col, row] = null;
            string key = $"{(char)('a' + col)}{row + 1}";
            if (pieceGameObjects.ContainsKey(key))
                pieceGameObjects.Remove(key);
        }

        /// <summary>
        /// 销毁指定位置的棋子 GameObject
        /// </summary>
        private void DestroyPieceObjectAt(int col, int row)
        {
            if (col < 0 || col >= 5 || row < 0 || row >= 13) return;

            GameObject pieceObj = pieceObjects[col, row];
            if (pieceObj != null)
            {
                pieceObjects[col, row] = null;
                string key = $"{(char)('a' + col)}{row + 1}";
                if (pieceGameObjects.ContainsKey(key))
                    pieceGameObjects.Remove(key);
                ReturnPieceToPool(pieceObj);
            }
            else
            {
                Debug.LogWarning($"⚠️ [Destroy] No piece object at ({col}, {row})");
            }
        }

        /// <summary>
        /// 高亮显示路径（可选功能）
        /// </summary>
        private void HighlightPath(List<BoardPosition> path)
        {
            // TODO: 实现路径高亮显示
            // 可以临时改变路径上格子的颜色或添加特效
            Debug.Log($"🛣️ [HighlightPath] Path with {path.Count} positions");
        }

        private void HandleGameEnded(GameEndResult result)
        {
            UpdateUI();
            ShowMessage($"游戏结束! 胜利者: {result.Winner}", 5f);
        }

        /// <summary>
        /// 获取状态文本
        /// </summary>
        private string GetStatusText(GameStatus status)
        {
            switch (status)
            {
                case GameStatus.Setup:
                    return "布阵阶段";
                case GameStatus.Ongoing:
                    return "游戏中";
                case GameStatus.Finished:
                    return "已结束";
                default:
                    return "未知";
            }
        }
    }
}
