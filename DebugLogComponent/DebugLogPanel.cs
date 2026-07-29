using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DebugLogPanel : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.F12;
    [SerializeField] private int _maxDisplayEntries = 500;
    [SerializeField] private Vector2 _panelSize = new Vector2(128, 90);
    [SerializeField] private Vector2 _minPanelSize = new Vector2(128, 90);

    [Header("Font")]
    [SerializeField] private TMP_FontAsset _font;

    [Header("Colors")]
    [SerializeField] private Color _colorLog = Color.white;
    [SerializeField] private Color _colorWarning = Color.yellow;
    [SerializeField] private Color _colorError = new Color(1f, 0.35f, 0.35f);
    [SerializeField] private Color _colorAssert = new Color(1f, 0.5f, 0f);

    [Header("Export")]
    [SerializeField] private string _exportFolder = "";

    private static DebugLogPanel _instance;
    public static DebugLogPanel Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DebugLogPanel>(true);
                if (_instance == null)
                {
                    var go = new GameObject("[DebugLogPanel]");
                    _instance = go.AddComponent<DebugLogPanel>();
                }
            }
            return _instance;
        }
    }

    private Canvas _canvas;
    private RectTransform _rootRt;
    private GameObject _rootPanel;
    private ScrollRect _scrollRect;
    private RectTransform _contentRoot;
    private TMP_InputField _searchInput;
    private TMP_InputField _fromInput, _toInput;
    private int _rangeFrom = 1, _rangeTo = int.MaxValue;
    private GameObject _countText;
    private Button _btnAll, _btnLog, _btnWarning, _btnError;
    private Button _btnExportCsv, _btnExportTxt, _btnClear;
    private GameObject _resizeHandle;
    private GameObject _headerGo, _searchGo, _filterGo, _bottomGo;

    private LogType? _currentFilter;
    private string _searchKeyword = "";
    private bool _isDirty = true;
    private bool _isDragging;
    private Vector2 _dragOffset;
    private bool _isResizing;
    private Vector2 _resizeStartMouse;
    private Vector2 _resizeStartSize;

    // ── 优化①：字符串构建器 ──────────────────────────
    private System.Text.StringBuilder _sb = new System.Text.StringBuilder();

    // ── 优化②：虚拟滚动参数 ──────────────────────────
    private float _entryHeight = 24f;
    private int _bufferEntries = 5;

    // ── 优化③：组件缓存数组 ──────────────────────────
    private EntryItem[] _entries = new EntryItem[0];

    // ── 优化④：刷新节流 ──────────────────────────────
    private float _nextRefreshTime;

    // ── 优化⑤：DebugLogEntry 对象池 ──────────────────
    // 条目滚动出可见区时 poolEntry 回池，进入时 Pop + CopyFrom
    private readonly Stack<DebugLogEntry> _dataPool = new Stack<DebugLogEntry>();

    private static readonly Color BgDark = new Color(0.08f, 0.08f, 0.08f, 0.97f);
    private static readonly Color BgMedium = new Color(0.13f, 0.13f, 0.13f);
    private static readonly Color BgInput = new Color(0.18f, 0.18f, 0.18f);
    private static readonly Color BgButton = new Color(0.25f, 0.25f, 0.25f);
    private static readonly Color BgButtonActive = new Color(0.45f, 0.45f, 0.45f);
    private static TMP_FontAsset _sharedFont;
    private static Canvas _debugCanvas;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(_instance.gameObject);
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        _sharedFont = _font;

        if (string.IsNullOrEmpty(_exportFolder))
            _exportFolder = DebugLogManager.DefaultExportPath;
        BuildUI();
        DebugLogManager.Instance.OnLogReceived += _ => MarkDirty();
    }

    // ── 优化④：带节流的 Update ───────────────────────
    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
            Toggle();
        if (_isDragging)
            HandleDrag();
        if (_isDirty && _rootPanel.activeInHierarchy && Time.unscaledTime >= _nextRefreshTime)
        {
            _isDirty = false;
            _nextRefreshTime = Time.unscaledTime + 0.05f;
            RefreshDisplay();
        }
        if (_isResizing)
            HandleResize();
    }

    public void MarkDirty() { _isDirty = true; }

    public void Show()
    {
        _rootPanel.SetActive(true);
        MarkDirty();
    }

    public void Hide() { _rootPanel.SetActive(false); }
    public void Toggle()
    {
        if (_rootPanel.activeSelf) Hide();
        else Show();
    }

    private static GameObject New(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static RectTransform Rect(GameObject go) => go.GetComponent<RectTransform>();

    private static TextMeshProUGUI Text(GameObject go, string text, int size, FontStyles style, TextAlignmentOptions align, Color color, bool raycast)
    {
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color = color;
        tmp.raycastTarget = raycast;
        if (_sharedFont != null)
            tmp.font = _sharedFont;
        return tmp;
    }

    private static Image Img(GameObject go, Color c)
    {
        var img = go.AddComponent<Image>();
        img.color = c;
        img.type = Image.Type.Sliced;
        return img;
    }

    private static void Fill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private static GameObject Label(GameObject parent, string text, int size, FontStyles style, TextAlignmentOptions align, Color color)
    {
        var go = New("l", parent.transform);
        Fill(Rect(go));
        Text(go, text, size, style, align, color, false);
        return go;
    }

    private static GameObject MakeBtn(GameObject parent, string label, Color c, int size, Action onClick)
    {
        var go = New("btn", parent.transform);
        Fill(Rect(go));
        Img(go, c);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        btn.onClick.AddListener(() => onClick());
        var lr = New("Label", go.transform);
        Fill(Rect(lr));
        Text(lr, label, size, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, false);
        return go;
    }

    private void BuildUI()
    {
        var cg = new GameObject("DebugLogCanvas");
        _canvas = cg.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 32766;
        cg.AddComponent<CanvasScaler>();
        cg.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(cg);
        _debugCanvas = _canvas;

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(es);
        }

        _rootPanel = New("DebugLogRoot", _canvas.transform);
        _rootRt = Rect(_rootPanel);
        _rootRt.anchorMin = new Vector2(0.5f, 0.5f);
        _rootRt.anchorMax = new Vector2(0.5f, 0.5f);
        _rootRt.sizeDelta = _panelSize;
        _rootRt.anchoredPosition = Vector2.zero;

        Img(_rootPanel, BgDark);

        var vlg = _rootPanel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 8, 8);
        vlg.spacing = 6;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        BuildHeader();
        BuildSearchBar();
        BuildFilterRow();
        BuildScrollView();
        BuildBottomBar();
        BuildResizeHandle();

        _rootPanel.SetActive(false);
    }

    private void BuildHeader()
    {
        _headerGo = New("Header", _rootPanel.transform);
        Fill(Rect(_headerGo));
        _headerGo.AddComponent<LayoutElement>().preferredHeight = 14;
        var headerImg = _headerGo.AddComponent<Image>();
        headerImg.color = new Color(0, 0, 0, 0.01f);
        var hlg = _headerGo.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.spacing = 0;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;

        var drag = _headerGo.AddComponent<HeaderDragHandler>();
        drag.onPointerDown = () =>
        {
            Vector2 lp;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_canvas.transform, Input.mousePosition, _canvas.worldCamera, out lp);
            _dragOffset = (Vector2)_rootRt.anchoredPosition - lp;
            _isDragging = true;
        };
        drag.onPointerUp = () => _isDragging = false;

        var title = New("Title", _headerGo.transform);
        title.AddComponent<LayoutElement>().preferredWidth = 110;
        Fill(Rect(title));
        Text(title, "Debug Log", 16, FontStyles.Bold, TextAlignmentOptions.Left, Color.white, false);

        var sp = New("Spacer", _headerGo.transform);
        sp.AddComponent<LayoutElement>().flexibleWidth = 1;

        var btnGo = New("CloseBtn", _headerGo.transform);
        btnGo.AddComponent<LayoutElement>().preferredWidth = 36;
        Fill(Rect(btnGo));
        var btnImg = Img(btnGo, new Color(0.5f, 0.15f, 0.15f));
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(Hide);
        var lr = New("Label", btnGo.transform);
        Fill(Rect(lr));
        Text(lr, "X", 15, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, false);
    }

    private void BuildSearchBar()
    {
        _searchGo = New("SearchBar", _rootPanel.transform);
        Fill(Rect(_searchGo));
        _searchGo.AddComponent<LayoutElement>().preferredHeight = 36;
        var inputGo = New("SearchInput", _searchGo.transform);
        Fill(Rect(inputGo));
        Img(inputGo, BgInput);

        _searchInput = inputGo.AddComponent<TMP_InputField>();

        var ta = New("TextArea", inputGo.transform);
        var taRt = Rect(ta);
        taRt.anchorMin = Vector2.zero;
        taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(8, 8);
        taRt.offsetMax = new Vector2(-8, -8);

        var text = ta.AddComponent<TextMeshProUGUI>();
        text.text = "";
        text.fontSize = 13;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = true;
        if (_font != null)
            text.font = _font;

        var ph = New("Placeholder", ta.transform);
        Fill(Rect(ph));
        Text(ph, "Search logs...", 13, FontStyles.Italic, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.5f, 0.7f), false);

        _searchInput.textViewport = taRt;
        _searchInput.textComponent = text;
        _searchInput.placeholder = ph.GetComponent<TextMeshProUGUI>();
        _searchInput.customCaretColor = true;
        _searchInput.caretColor = Color.white;
        _searchInput.onValueChanged.AddListener(val =>
        {
            _searchKeyword = val;
            MarkDirty();
        });
    }

    private void BuildFilterRow()
    {
        _filterGo = New("FilterRow", _rootPanel.transform);
        Fill(Rect(_filterGo));
        _filterGo.AddComponent<LayoutElement>().preferredHeight = 12;

        var hlg = _filterGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6;
        hlg.childForceExpandWidth = false;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        _btnAll = MakeBtnInner("All", 0.3f, 0.3f, 0.3f, () => { _currentFilter = null; UpdateFilterBtns(); MarkDirty(); }, true);
        _btnLog = MakeBtnInner("Log", 0.25f, 0.35f, 0.5f, () => { _currentFilter = LogType.Log; UpdateFilterBtns(); MarkDirty(); });
        _btnWarning = MakeBtnInner("Warning", 0.5f, 0.45f, 0.15f, () => { _currentFilter = LogType.Warning; UpdateFilterBtns(); MarkDirty(); });
        _btnError = MakeBtnInner("Error", 0.5f, 0.2f, 0.2f, () => { _currentFilter = LogType.Error; UpdateFilterBtns(); MarkDirty(); });
    }

    private Button MakeBtnInner(string label, float r, float g, float b, Action onClick, bool active = false)
    {
        var go = New("Btn" + label, _filterGo.transform);
        go.AddComponent<LayoutElement>().preferredWidth = 72;
        Rect(go).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 24);
        var c = active ? BgButtonActive : new Color(r, g, b);
        Img(go, c);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        btn.onClick.AddListener(() => onClick());
        var lr = New("Label", go.transform);
        Fill(Rect(lr));
        Text(lr, label, 11, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, false);
        return btn;
    }

    private void UpdateFilterBtns()
    {
        SetBtnActive(_btnAll, _currentFilter == null);
        SetBtnActive(_btnLog, _currentFilter == LogType.Log);
        SetBtnActive(_btnWarning, _currentFilter == LogType.Warning);
        SetBtnActive(_btnError, _currentFilter == LogType.Error);
    }

    private static void SetBtnActive(Button btn, bool active)
    {
        btn.GetComponent<Image>().color = active ? BgButtonActive : BgButton;
    }

    // ── 优化②：虚拟滚动 ScrollRect ──────────────────
    private void BuildScrollView()
    {
        var go = New("ScrollView", _rootPanel.transform);
        Fill(Rect(go));
        go.AddComponent<LayoutElement>().flexibleHeight = 56;

        _scrollRect = go.AddComponent<ScrollRect>();
        _scrollRect.vertical = true;
        _scrollRect.horizontal = false;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.scrollSensitivity = 30;

        var vp = New("Viewport", go.transform);
        Fill(Rect(vp));
        var vpRt = Rect(vp);
        vpRt.offsetMax = new Vector2(-22, 0);
        Img(vp, BgMedium);
        vp.AddComponent<Mask>().showMaskGraphic = false;

        _contentRoot = New("Content", vp.transform).GetComponent<RectTransform>();
        _contentRoot.anchorMin = new Vector2(0, 1);
        _contentRoot.anchorMax = Vector2.one;
        _contentRoot.pivot = new Vector2(0.5f, 1);
        _contentRoot.sizeDelta = new Vector2(0, 0);

        _scrollRect.viewport = vpRt;
        _scrollRect.content = _contentRoot;

        _scrollRect.onValueChanged.AddListener(pos =>
        {
            if (_isDirty == false)
                MarkDirty();
        });

        var sb = New("Scrollbar", go.transform);
        var sbRt = Rect(sb);
        sbRt.anchorMin = new Vector2(1, 0);
        sbRt.anchorMax = Vector2.one;
        sbRt.sizeDelta = new Vector2(22, 0);

        var scrollbar = sb.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        var sa = New("SlidingArea", sb.transform);
        Fill(Rect(sa));
        var hd = New("Handle", sa.transform);
        Fill(Rect(hd));
        Img(hd, new Color(0.5f, 0.5f, 0.5f));
        scrollbar.handleRect = Rect(hd);
        scrollbar.targetGraphic = hd.GetComponent<Image>();
        _scrollRect.verticalScrollbar = scrollbar;
    }

    private void BuildBottomBar()
    {
        _bottomGo = New("BottomBar", _rootPanel.transform);
        Fill(Rect(_bottomGo));
        _bottomGo.AddComponent<LayoutElement>().preferredHeight = 14;

        var hlg = _bottomGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6;
        hlg.childForceExpandWidth = false;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        MakeBtnInner2("Export CSV", 0.2f, 0.4f, 0.2f, 11, () =>
        {
            string path = string.IsNullOrEmpty(_exportFolder) ? null : _exportFolder;
            ExportWithRange(path, isCsv: true);
        });
        MakeBtnInner2("Export TXT", 0.2f, 0.3f, 0.4f, 11, () =>
        {
            string path = string.IsNullOrEmpty(_exportFolder) ? null : _exportFolder;
            ExportWithRange(path, isCsv: false);
        });
        MakeBtnInner2("Clear", 0.4f, 0.2f, 0.2f, 11, () =>
        {
            DebugLogManager.Instance.ClearLogs();
            _dataPool.Clear();
            MarkDirty();
        });

        BuildRangeField(_bottomGo.transform, "F", ref _fromInput, 1);
        BuildRangeField(_bottomGo.transform, "T", ref _toInput, int.MaxValue);

        var sp = New("Spacer", _bottomGo.transform);
        sp.AddComponent<LayoutElement>().flexibleWidth = 1;

        _countText = New("Count", _bottomGo.transform);
        _countText.AddComponent<LayoutElement>().preferredWidth = 72;
        Text(_countText, "0", 11, FontStyles.Normal, TextAlignmentOptions.Right, new Color(0.6f, 0.6f, 0.6f), false);
        Fill(Rect(_countText));
    }

    private void BuildRangeField(Transform parent, string label, ref TMP_InputField field, int defaultValue)
    {
        var go = New("Range" + label, parent);
        go.AddComponent<LayoutElement>().preferredWidth = 44;
        Rect(go).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 14);
        var iImg = Img(go, BgInput);

        field = go.AddComponent<TMP_InputField>();

        var ta = New("TextArea", go.transform);
        var taRt = Rect(ta);
        taRt.anchorMin = Vector2.zero;
        taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(3, 2);
        taRt.offsetMax = new Vector2(-2, -2);

        var text = ta.AddComponent<TextMeshProUGUI>();
        text.text = defaultValue == int.MaxValue ? "" : defaultValue.ToString();
        text.fontSize = 10;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = true;
        if (_font != null) text.font = _font;

        var ph = New("Placeholder", ta.transform);
        Fill(Rect(ph));
        Text(ph, label, 10, FontStyles.Italic, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.5f, 0.7f), false);

        field.textViewport = taRt;
        field.textComponent = text;
        field.placeholder = ph.GetComponent<TextMeshProUGUI>();
        field.customCaretColor = true;
        field.caretColor = Color.white;
        field.contentType = TMP_InputField.ContentType.IntegerNumber;
        field.onValueChanged.AddListener(val =>
        {
            if (int.TryParse(val, out int n))
            {
                if (label == "F") _rangeFrom = n;
                else _rangeTo = n;
            }
        });
    }

    private void ExportWithRange(string folderPath, bool isCsv)
    {
        var logs = DebugLogManager.Instance.GetFilteredLogs(_searchKeyword, _currentFilter);
        int from = Mathf.Max(1, _rangeFrom);
        int to = Mathf.Min(logs.Count, _rangeTo);
        if (from > to || logs.Count == 0) return;
        var range = logs.GetRange(from - 1, to - from + 1);

        string suffix = $"_{from}-{to}";
        if (isCsv)
            DebugLogManager.Instance.ExportToCsv(range, folderPath, suffix);
        else
            DebugLogManager.Instance.ExportToTxt(range, folderPath, suffix);
    }

    private void MakeBtnInner2(string label, float r, float g, float b, int size, Action onClick)
    {
        var go = New("Btn" + label.Replace(" ", ""), _bottomGo.transform);
        go.AddComponent<LayoutElement>().preferredWidth = 80;
        Rect(go).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 24);
        Img(go, new Color(r, g, b));
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        btn.onClick.AddListener(() => onClick());
        var lr = New("Label", go.transform);
        Fill(Rect(lr));
        Text(lr, label, size, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, false);
    }

    private void BuildResizeHandle()
    {
        _resizeHandle = New("ResizeHandle", _rootPanel.transform);
        var rt = Rect(_resizeHandle);
        rt.anchorMin = Vector2.one;
        rt.anchorMax = Vector2.one;
        rt.pivot = Vector2.one;
        rt.sizeDelta = new Vector2(18, 18);
        rt.anchoredPosition = Vector2.zero;

        var img = Img(_resizeHandle, new Color(0.4f, 0.4f, 0.4f, 0.6f));
        try
        {
            var tex = new Texture2D(3, 3);
            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    tex.SetPixel(x, y, x + y >= 2 ? Color.white : Color.clear);
            tex.Apply();
            img.sprite = Sprite.Create(tex, new Rect(0, 0, 3, 3), Vector2.zero);
        }
        catch { }

        var trigger = _resizeHandle.AddComponent<EventTrigger>();
        var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener(_ =>
        {
            _isResizing = true;
            _resizeStartMouse = Input.mousePosition;
            _resizeStartSize = _rootRt.sizeDelta;
        });
        trigger.triggers.Add(down);

        var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        up.callback.AddListener(_ => _isResizing = false);
        trigger.triggers.Add(up);

        _resizeHandle.AddComponent<LayoutElement>().ignoreLayout = true;
    }

    private void HandleDrag()
    {
        var rt = (RectTransform)_canvas.transform;
        Vector2 lp;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, Input.mousePosition, _canvas.worldCamera, out lp);
        _rootRt.anchoredPosition = lp + _dragOffset;
    }

    private void HandleResize()
    {
        var delta = (Vector2)Input.mousePosition - _resizeStartMouse;
        var newSize = _resizeStartSize + delta;
        newSize.x = Mathf.Max(newSize.x, _minPanelSize.x);
        newSize.y = Mathf.Max(newSize.y, _minPanelSize.y);
        _rootRt.sizeDelta = newSize;
    }

    // ── 优化②③④⑤：虚拟滚动 + 组件缓存 + 节流 + 数据池 ──
    //
    // 每帧流程：
    // 1. 回收当前可见条目的 poolEntry → _dataPool
    // 2. 计算 content 总高度 = 条目数 × 每行高度
    // 3. 根据滚动位置计算可见范围的 start / end 索引
    // 4. 确保 _entries 数组足够容纳需要的条目数
    // 5. 循环可见范围：_dataPool.Pop(或 new) + CopyFrom + 赋值给 item
    // 6. 超出范围的条目 / 多余的 entries → SetActive(false)
    //
    // 池生命周期：
    //   看不见 → poolEntry 回 _dataPool
    //   看得见 → _dataPool.Pop → CopyFrom(_logs[logIdx]) → handler.Entry
    //   _logs 中的所有 canonical 对象永不进入池
    private void RefreshDisplay()
    {
        var logs = DebugLogManager.Instance.GetFilteredLogs(_searchKeyword, _currentFilter);

        // 回收当前可见条目的 poolEntry
        for (int i = 0; i < _entries.Length; i++)
        {
            if (_entries[i].poolEntry != null)
            {
                _dataPool.Push(_entries[i].poolEntry);
                _entries[i].poolEntry = null;
            }
        }

        float viewH = _scrollRect.viewport.rect.height;
        float totalH = logs.Count * _entryHeight;
        _contentRoot.sizeDelta = new Vector2(0, totalH);

        float maxScroll = Mathf.Max(0, totalH - viewH);
        float scrollY = (1f - _scrollRect.verticalNormalizedPosition) * maxScroll;
        int start = Mathf.Max(0, Mathf.FloorToInt(scrollY / _entryHeight) - _bufferEntries);
        int need = Mathf.CeilToInt(viewH / _entryHeight) + _bufferEntries * 2;
        int end = Mathf.Min(logs.Count, start + need);

        EnsureCapacity(need);

        for (int i = 0; i < _entries.Length; i++)
        {
            int logIdx = start + i;

            if (i < need && logIdx < end)
            {
                var item = _entries[i];
                var src = logs[logIdx];
                var color = src.Type switch
                {
                    LogType.Warning => _colorWarning,
                    LogType.Error => _colorError,
                    LogType.Assert => _colorAssert,
                    LogType.Exception => _colorError,
                    _ => _colorLog
                };

                // 从池取或新建，CopyFrom canonical 对象
                DebugLogEntry copy = _dataPool.Count > 0 ? _dataPool.Pop() : new DebugLogEntry(DateTime.MinValue, LogType.Log, null, null);
                copy.CopyFrom(src);
                item.poolEntry = copy;
                item.handler.Entry = copy;

                item.go.SetActive(true);
                item.rt.anchorMin = new Vector2(0, 1);
                item.rt.anchorMax = new Vector2(1, 1);
                item.rt.pivot = new Vector2(0.5f, 1);
                item.rt.sizeDelta = new Vector2(0, _entryHeight - 2);
                item.rt.anchoredPosition = new Vector2(0, -(logIdx * _entryHeight));
                BuildEntryText(item.text, logIdx + 1, copy);
                item.text.color = color;
            }
            else
            {
                _entries[i].go.SetActive(false);
            }
        }

        _countText.GetComponent<TextMeshProUGUI>().text = $"{logs.Count} entries";
    }

    private void EnsureCapacity(int need)
    {
        if (_entries.Length >= need) return;
        int oldLen = _entries.Length;
        Array.Resize(ref _entries, Mathf.Max(need, oldLen * 2, 16));
        for (int i = oldLen; i < _entries.Length; i++)
            _entries[i] = new EntryItem(_contentRoot);
    }

    // ── 优化③⑤：组件缓存类 + 数据副本 ──────────────
    private class EntryItem
    {
        public GameObject go;
        public RectTransform rt;
        public TextMeshProUGUI text;
        public LogEntryClickHandler handler;
        public DebugLogEntry poolEntry;   // 持有渲染副本（可能回池）

        public EntryItem(Transform parent)
        {
            go = new GameObject("e", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            rt = go.GetComponent<RectTransform>();
            text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = 12;
            text.fontStyle = FontStyles.Normal;
            text.alignment = TextAlignmentOptions.Left;
            text.margin = new Vector4(6, 2, 0, 2);
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = true;
            if (_sharedFont != null) text.font = _sharedFont;
            handler = go.AddComponent<LogEntryClickHandler>();
            go.SetActive(false);
        }
    }

    // ── 优化①：StringBuilder 构建条目文本 ────────────
    private void BuildEntryText(TextMeshProUGUI tmp, int index, DebugLogEntry entry)
    {
        _sb.Clear();
        _sb.Append('#');
        _sb.Append(index);
        _sb.Append(' ');
        _sb.Append(entry.ToString());
        tmp.SetText(_sb);
    }

    private class HeaderDragHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public System.Action onPointerDown;
        public System.Action onPointerUp;

        public void OnPointerDown(PointerEventData eventData)
        {
            onPointerDown?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            onPointerUp?.Invoke();
        }
    }

    private class LogEntryClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public DebugLogEntry Entry { private get; set; }

        private static GameObject _popup;
        private static TextMeshProUGUI _popupText;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Entry == null) return;
            ShowDetail(Entry);
        }

        private static void ShowDetail(DebugLogEntry entry)
        {
            if (_popup == null) CreatePopup();
            _popupText.text = entry.ToDetailedString();
            _popup.SetActive(true);
        }

        private static void CreatePopup()
        {
            if (_debugCanvas == null) return;
            _popup = BuildPopup(_debugCanvas.transform);
            _popupText = _popup.GetComponentInChildren<TextMeshProUGUI>();
        }

        private static GameObject BuildPopup(Transform parent)
        {
            var root = New("LogDetailPopup", parent);
            var rt = Rect(root);
            rt.anchorMin = new Vector2(0.25f, 0.2f);
            rt.anchorMax = new Vector2(0.75f, 0.8f);
            rt.sizeDelta = Vector2.zero;
            var bgImg = Img(root, new Color(0.08f, 0.08f, 0.08f, 0.98f));
            var bgBtn = root.AddComponent<Button>();
            bgBtn.targetGraphic = bgImg;
            bgBtn.onClick.AddListener(() => root.SetActive(false));

            var vlg = root.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(14, 14, 14, 10);
            vlg.spacing = 8;

            var txt = New("Text", root.transform);
            var txtTmp = txt.AddComponent<TextMeshProUGUI>();
            txtTmp.fontSize = 13;
            txtTmp.color = Color.white;
            txtTmp.alignment = TextAlignmentOptions.TopLeft;
            txtTmp.raycastTarget = false;
            if (_sharedFont != null)
                txtTmp.font = _sharedFont;
            txt.AddComponent<LayoutElement>().flexibleHeight = 56;

            var btnGo = New("Close", root.transform);
            Rect(btnGo).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 30);
            Img(btnGo, new Color(0.4f, 0.15f, 0.15f));
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnGo.GetComponent<Image>();
            btn.onClick.AddListener(() => root.SetActive(false));
            var lr = New("Label", btnGo.transform);
            Fill(Rect(lr));
            Text(lr, "Close", 13, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, false);

            root.SetActive(false);
            return root;
        }

        private static Image Img(GameObject go, Color c)
        {
            var img = go.AddComponent<Image>();
            img.color = c;
            img.type = Image.Type.Sliced;
            return img;
        }

        private static RectTransform Rect(GameObject go) => go.GetComponent<RectTransform>();
        private static GameObject New(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Fill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
        }

        private static void Text(GameObject go, string text, int size, FontStyles style, TextAlignmentOptions align, Color color, bool raycast)
        {
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = raycast;
        }
    }
}
