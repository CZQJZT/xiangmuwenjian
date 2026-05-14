#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using JunqiGame.MonoBehaviours;

namespace JunqiGame.UI.Editor
{
    /// <summary>
    /// UI设置助手 - 在编辑器中快速创建UI结构
    /// </summary>
    public class UISetupHelper : EditorWindow
    {
        private Vector2 scrollPosition;

        [MenuItem("Junqi Game/Setup UI Structure")]
        public static void ShowWindow()
        {
            GetWindow<UISetupHelper>("军棋UI设置助手");
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("军棋游戏UI设置助手", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("创建完整UI结构", GUILayout.Height(40)))
            {
                CreateCompleteUIStructure();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("此工具将自动创建所需的UI结构和GameObject", MessageType.Info);

            EditorGUILayout.Space();
            GUILayout.Label("单独创建组件:", EditorStyles.boldLabel);

            if (GUILayout.Button("创建Canvas和EventSystem"))
            {
                CreateCanvasAndEventSystem();
            }

            if (GUILayout.Button("创建GameManager"))
            {
                CreateGameManager();
            }

            if (GUILayout.Button("创建AudioManager"))
            {
                CreateAudioManager();
            }

            if (GUILayout.Button("创建UIManager"))
            {
                CreateUIManager();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 创建完整的UI结构
        /// </summary>
        private void CreateCompleteUIStructure()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Create Complete UI Structure");

            // 1. 创建Canvas和EventSystem
            GameObject canvas = CreateCanvasAndEventSystem();
            
            // 2. 创建GameManager
            CreateGameManager();
            
            // 3. 创建AudioManager
            CreateAudioManager();
            
            // 4. 创建UIManager
            CreateUIManager();

            Selection.activeGameObject = canvas;
            Debug.Log("✅ 完整UI结构创建成功！");
        }

        /// <summary>
        /// 创建Canvas和EventSystem
        /// </summary>
        private GameObject CreateCanvasAndEventSystem()
        {
            // 检查是否已存在
            Canvas existingCanvas = FindObjectOfType<Canvas>();
            if (existingCanvas != null)
            {
                Debug.LogWarning("Canvas已存在");
                return existingCanvas.gameObject;
            }

            // 创建Canvas
            GameObject canvas = new GameObject("GameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvasComponent = canvas.GetComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;

            // 配置Canvas Scaler
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // 创建EventSystem
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // 创建TopPanel
            GameObject topPanel = CreatePanel(canvas.transform, "TopPanel");
            topPanel.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
            topPanel.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
            topPanel.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1);
            topPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 80);

            // 添加文本元素
            CreateTextMeshPro(topPanel.transform, "StatusText", "游戏状态: 准备中", new Vector2(0, -25));
            CreateTextMeshPro(topPanel.transform, "CurrentPlayerText", "当前玩家: -", new Vector2(0, -50));
            CreateTextMeshPro(topPanel.transform, "MessageText", "", new Vector2(0, -70));

            // 创建BoardPanel
            GameObject boardPanel = CreatePanel(canvas.transform, "BoardPanel");
            boardPanel.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            boardPanel.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            boardPanel.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            boardPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 800);
            boardPanel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            // 创建BoardGrid
            GameObject boardGrid = new GameObject("BoardGrid");
            boardGrid.transform.SetParent(boardPanel.transform);
            boardGrid.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            // 创建BottomPanel
            GameObject bottomPanel = CreatePanel(canvas.transform, "BottomPanel");
            bottomPanel.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
            bottomPanel.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0);
            bottomPanel.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0);
            bottomPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 100);

            // 创建按钮
            CreateButton(bottomPanel.transform, "StartButton", "开始游戏", new Vector2(-200, 50));
            CreateButton(bottomPanel.transform, "ForfeitButton", "认输", new Vector2(0, 50));
            CreateButton(bottomPanel.transform, "ResetButton", "重置", new Vector2(200, 50));

            Debug.Log("✅ Canvas和UI结构创建成功");
            return canvas;
        }

        /// <summary>
        /// 创建GameManager
        /// </summary>
        private GameObject CreateGameManager()
        {
            // 检查是否已存在
            JunqiGameManager existingManager = FindObjectOfType<JunqiGameManager>();
            if (existingManager != null)
            {
                Debug.LogWarning("JunqiGameManager已存在");
                return existingManager.gameObject;
            }

            GameObject manager = new GameObject("GameManager");
            manager.AddComponent<JunqiGame.MonoBehaviours.JunqiGameManager>();

            Debug.Log("✅ GameManager创建成功");
            return manager;
        }

        /// <summary>
        /// 创建AudioManager
        /// </summary>
        private GameObject CreateAudioManager()
        {
            // 检查是否已存在
            AudioManager existingManager = FindObjectOfType<AudioManager>();
            if (existingManager != null)
            {
                Debug.LogWarning("AudioManager已存在");
                return existingManager.gameObject;
            }

            GameObject manager = new GameObject("AudioManager");
            manager.AddComponent<AudioManager>();

            Debug.Log("✅ AudioManager创建成功");
            return manager;
        }

        /// <summary>
        /// 创建UIManager
        /// </summary>
        private GameObject CreateUIManager()
        {
            // 检查是否已存在
            GameUIManager existingManager = FindObjectOfType<GameUIManager>();
            if (existingManager != null)
            {
                Debug.LogWarning("GameUIManager已存在");
                return existingManager.gameObject;
            }

            GameObject manager = new GameObject("UIManager");
            GameUIManager uiManager = manager.AddComponent<GameUIManager>();

            // 自动查找并分配引用
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                // 查找文本组件
                Transform topPanel = canvas.transform.Find("TopPanel");
                if (topPanel != null)
                {
                    uiManager.statusText = topPanel.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
                    uiManager.currentPlayerText = topPanel.Find("CurrentPlayerText")?.GetComponent<TextMeshProUGUI>();
                    uiManager.messageText = topPanel.Find("MessageText")?.GetComponent<TextMeshProUGUI>();
                }

                // 查找按钮
                Transform bottomPanel = canvas.transform.Find("BottomPanel");
                if (bottomPanel != null)
                {
                    uiManager.startButton = bottomPanel.Find("StartButton")?.GetComponent<UnityEngine.UI.Button>();
                    uiManager.forfeitButton = bottomPanel.Find("ForfeitButton")?.GetComponent<UnityEngine.UI.Button>();
                    uiManager.resetButton = bottomPanel.Find("ResetButton")?.GetComponent<UnityEngine.UI.Button>();
                }

                // 查找棋盘网格
                Transform boardPanel = canvas.transform.Find("BoardPanel");
                if (boardPanel != null)
                {
                    Transform boardGrid = boardPanel.Find("BoardGrid");
                    if (boardGrid != null)
                    {
                        uiManager.boardParent = boardGrid;
                    }
                }
            }

            Debug.Log("✅ UIManager创建成功（请手动分配Prefab引用）");
            return manager;
        }

        // 辅助方法
        private GameObject CreatePanel(Transform parent, string name)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(parent);
            
            var rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = Vector2.zero;

            var image = panel.GetComponent<UnityEngine.UI.Image>();
            image.color = new Color(0, 0, 0, 0.3f);

            return panel;
        }

        private void CreateText(Transform parent, string name, string text, Vector2 position)
        {
            GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text));
            textObj.transform.SetParent(parent);

            var rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1);
            rectTransform.anchorMax = new Vector2(0.5f, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            rectTransform.sizeDelta = new Vector2(400, 20);
            rectTransform.anchoredPosition = position;

            var textComponent = textObj.GetComponent<UnityEngine.UI.Text>();
            textComponent.text = text;
            textComponent.fontSize = 20;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.white;
        }

        private void CreateTextMeshPro(Transform parent, string name, string text, Vector2 position)
        {
            GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(parent);

            var rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1);
            rectTransform.anchorMax = new Vector2(0.5f, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            rectTransform.sizeDelta = new Vector2(400, 30);
            rectTransform.anchoredPosition = position;

            var textComponent = textObj.GetComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 24;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.color = Color.white;
            
            // 设置字体（如果有的话）
            if (textComponent.font == null)
            {
                textComponent.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }
        }

        private void CreateButton(Transform parent, string name, string buttonText, Vector2 position)
        {
            GameObject buttonObj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Button), 
                typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Text));
            buttonObj.transform.SetParent(parent);

            var rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(150, 60);
            rectTransform.anchoredPosition = position;

            var image = buttonObj.GetComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.2f, 0.5f, 0.8f, 1f);

            var button = buttonObj.GetComponent<UnityEngine.UI.Button>();
            button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;

            var textComponent = buttonObj.GetComponent<UnityEngine.UI.Text>();
            textComponent.text = buttonText;
            textComponent.fontSize = 18;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.white;
        }
    }
}
#endif
