
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 单位列表面板 - 显示所有单位的列表
/// </summary>
public class UnitListPanel : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject panelObject;
    public TMP_Text TitleText;
    
    [Header("Unit List Content")]
    public Transform UnitListContent;
    public UnitListItem UnitListItemPrefab;
    
    [Header("Filter Options")]
    public Toggle ShowAliveToggle;
    public Toggle ShowDeadToggle;
    public Dropdown TeamFilterDropdown;
    
    [Header("Buttons")]
    public Button CloseButton;
    public Button RefreshButton;
    
    [Header("Info Display")]
    public TMP_Text TotalCountText;
    public TMP_Text AliveCountText;
    public TMP_Text DeadCountText;
    
    [Header("Settings")]
    public bool AutoUpdate = false; // 禁用自动更新
    public float UpdateInterval = 2f;
    public float RefreshCooldown = 0.5f; // 刷新冷却时间（秒）
    
    private List<UnitListItem> _listItems = new List<UnitListItem>();
    private float _lastUpdateTime;
    private Team _selectedTeamFilter = Team.Player;
    
    // 保存对所有军事建筑的引用
    private List<MilitaryBuilding> _militaryBuildings = new List<MilitaryBuilding>();
    
    // 标记是否正在刷新中，防止重复刷新
    private bool _isRefreshing = false;
    
    private TaskBuilding _currentTaskBuilding;
    private string _currentTaskName;
    private bool _isTaskAssignmentMode = false;
    
    private void Start()
    {
        // 绑定按钮事件
        if (CloseButton != null)
        {
            CloseButton.onClick.AddListener(Hide);
        }
        
        if (RefreshButton != null)
        {
            RefreshButton.onClick.AddListener(RefreshList);
        }
        
        // 绑定筛选选项事件
        if (ShowAliveToggle != null)
        {
            ShowAliveToggle.onValueChanged.AddListener(OnFilterChanged);
        }
        
        if (ShowDeadToggle != null)
        {
            ShowDeadToggle.onValueChanged.AddListener(OnFilterChanged);
        }
        
        if (TeamFilterDropdown != null)
        {
            TeamFilterDropdown.onValueChanged.AddListener(OnTeamFilterChanged);
        }
        
        // 订阅单位事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnUnitKilled += OnUnitKilled;
        }
        
        // 订阅军事建筑的单位生成事件
        SubscribeToBuildingEvents();
        
        // 初始隐藏
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 订阅所有军事建筑的单位生成事件
    /// </summary>
    private void SubscribeToBuildingEvents()
    {
        // 查找场景中所有的 MilitaryBuilding
        MilitaryBuilding[] buildings = FindObjectsOfType<MilitaryBuilding>();
        
        foreach (var building in buildings)
        {
            building.OnUnitSpawned += OnUnitSpawned;
            _militaryBuildings.Add(building);
            Debug.Log($"[UnitListPanel] 已订阅建筑事件：{building.gameObject.name}");
        }
    }
    
    /// <summary>
    /// 单位生成事件处理
    /// </summary>
    private void OnUnitSpawned(Unit unit)
    {
        Debug.Log($"[UnitListPanel] 新单位生成：{unit.gameObject.name}，将在 0.2 秒后刷新列表");
        // 延迟刷新，确保单位完全初始化
        CancelInvoke(nameof(RefreshList));
        Invoke(nameof(RefreshList), 0.2f);
    }
    
    /// <summary>
    /// 显示面板
    /// </summary>
    public void Show()
    {
        if (panelObject != null)
        {
            panelObject.SetActive(true);
        }
        gameObject.SetActive(true);
        
        RefreshList();
    }
    
    /// <summary>
    /// 显示面板（用于任务分配模式）
    /// </summary>
    public void ShowForTaskAssignment(TaskBuilding building, string taskName)
    {
        _currentTaskBuilding = building;
        _currentTaskName = taskName;
        _isTaskAssignmentMode = true;
        
        if (TitleText != null && building != null)
        {
            TitleText.text = $"分配单位到：{building.Name} - 任务：{taskName}";
        }
        
        Show();
    }
    
    /// <summary>
    /// 隐藏面板
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        
        if (panelObject != null)
        {
            panelObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 刷新列表
    /// </summary>
    public void RefreshList()
    {
        // 防止重复刷新
        if (_isRefreshing)
        {
            Debug.LogWarning("[UnitListPanel] 正在刷新中，跳过本次请求");
            return;
        }
        
        _isRefreshing = true;
        
        try
        {
            Debug.Log($"[UnitListPanel] 开始刷新列表...");
            
            ClearList();
            BuildList();
            UpdateStatistics();
            
            Debug.Log($"[UnitListPanel] 刷新完成，共 {_listItems.Count} 个单位");
        }
        finally
        {
            _isRefreshing = false;
        }
    }
    
    /// <summary>
    /// 清空列表（立即销毁）
    /// </summary>
    private void ClearList()
    {
        foreach (var item in _listItems)
        {
            if (item != null && item.gameObject != null)
            {
                // 立即销毁，不等待帧结束
                DestroyImmediate(item.gameObject);
            }
        }
        _listItems.Clear();
    }
    
    /// <summary>
    /// 构建列表
    /// </summary>
    private void BuildList()
    {
        if (UnitManager.Instance == null || UnitListItemPrefab == null || UnitListContent == null)
        {
            return;
        }
        
        foreach (var unit in UnitManager.Instance.AllUnits)
        {
            // 应用筛选
            if (!ShouldShowUnit(unit))
            {
                continue;
            }
            
            CreateUnitListItem(unit);
        }
    }
    
    /// <summary>
    /// 创建单位列表项（立即生成）
    /// </summary>
    private void CreateUnitListItem(Unit unit)
    {
        // 直接实例化并初始化，不再延迟
        InstantiateAndInitialize(unit);
    }
    
    /// <summary>
    /// 实例化并初始化列表项
    /// </summary>
    private void InstantiateAndInitialize(Unit unit)
    {
        UnitListItem item = Instantiate(UnitListItemPrefab, UnitListContent);
        item.Initialize(unit);
        _listItems.Add(item);
        
        Debug.Log($"[UnitListPanel] 生成列表项：{unit.gameObject.name}，当前总数：{_listItems.Count}");
    }
    
    /// <summary>
    /// 判断是否应该显示该单位
    /// </summary>
    private bool ShouldShowUnit(Unit unit)
    {
        if (unit.CurrentState == UnitState.Dead)
        {
            Debug.Log($"[UnitListPanel] 单位 {unit.Name} 已死亡，不显示");
            return false;
        }
        
        // 检查分配冷却：如果单位最近被分配过，且在冷却期内，则不显示
        float timeSinceAssigned = Time.time - unit.LastAssignedTime;
        Debug.Log($"[UnitListPanel] 单位 {unit.Name} - 上次分配时间: {unit.LastAssignedTime:F2}, 当前时间: {Time.time:F2}, 经过时间: {timeSinceAssigned:F2}秒, 冷却时间: {unit.AssignmentCooldown}秒");
        
        if (timeSinceAssigned < unit.AssignmentCooldown)
        {
            Debug.Log($"[UnitListPanel] 单位 {unit.Name} 处于分配冷却期，剩余时间：{unit.AssignmentCooldown - timeSinceAssigned:F1}秒，不显示");
            return false;
        }
        
        if (_isTaskAssignmentMode && _currentTaskBuilding != null)
        {
            if (!_currentTaskBuilding.CanAssignUnit(unit))
            {
                Debug.Log($"[UnitListPanel] 单位 {unit.Name} 不符合 TaskBuilding 分配条件，不显示");
                return false;
            }
        }
        
        // 阵营筛选
        if (TeamFilterDropdown != null && TeamFilterDropdown.isActiveAndEnabled)
        {
            if (unit.Team != _selectedTeamFilter)
            {
                Debug.Log($"[UnitListPanel] 单位 {unit.Name} 阵营不匹配，不显示");
                return false;
            }
        }
        
        // 生死状态筛选
        bool isAlive = unit.CurrentState != UnitState.Dead;
        bool showAlive = ShowAliveToggle == null || ShowAliveToggle.isOn;
        bool showDead = ShowDeadToggle == null || ShowDeadToggle.isOn;
        
        if (isAlive && !showAlive)
        {
            return false;
        }
        
        if (!isAlive && !showDead)
        {
            return false;
        }
        
        Debug.Log($"[UnitListPanel] 单位 {unit.Name} 符合显示条件");
        return true;
    }
    
    /// <summary>
    /// 更新统计信息
    /// </summary>
    private void UpdateStatistics()
    {
        if (UnitManager.Instance == null)
        {
            return;
        }
        
        int totalCount = UnitManager.Instance.AllUnits.Count;
        int aliveCount = 0;
        int deadCount = 0;
        
        foreach (var unit in UnitManager.Instance.AllUnits)
        {
            if (unit.CurrentState == UnitState.Dead)
            {
                deadCount++;
            }
            else
            {
                aliveCount++;
            }
        }
        
        if (TotalCountText != null)
        {
            TotalCountText.text = $"总数：{totalCount}";
        }
        
        if (AliveCountText != null)
        {
            AliveCountText.text = $"存活：{aliveCount}";
        }
        
        if (DeadCountText != null)
        {
            DeadCountText.text = $"死亡：{deadCount}";
        }
    }
    
    /// <summary>
    /// 筛选条件改变
    /// </summary>
    private void OnFilterChanged(bool value)
    {
        RefreshList();
    }
    
    /// <summary>
    /// 阵营筛选改变
    /// </summary>
    private void OnTeamFilterChanged(int index)
    {
        _selectedTeamFilter = (Team)index;
        RefreshList();
    }
    
    /// <summary>
    /// 单位被击杀事件
    /// </summary>
    private void OnUnitKilled(Unit unit)
    {
        Debug.Log($"[UnitListPanel] 单位死亡：{unit.gameObject.name}，将在 0.2 秒后刷新列表");
        // 延迟刷新，避免与其他事件冲突
        CancelInvoke(nameof(RefreshList));
        Invoke(nameof(RefreshList), 0.2f);
    }
    
    private void Update()
    {
        // 不再自动刷新，只更新选中状态
        UpdateSelectionHighlight();
    }
    
    /// <summary>
    /// 更新所有列表项显示
    /// </summary>
    private void UpdateAllItems()
    {
        foreach (var item in _listItems)
        {
            if (item != null)
            {
                item.UpdateDisplay();
            }
        }
    }
    
    /// <summary>
    /// 更新选中高亮
    /// </summary>
    private void UpdateSelectionHighlight()
    {
        var selectedUnits = SelectionManager.Instance.SelectedUnits;
        
        foreach (var item in _listItems)
        {
            if (item != null)
            {
                // 检查这个单位是否在选中列表中
                bool isSelected = selectedUnits.Contains(GetUnitFromItem(item));
                item.SetSelected(isSelected);
            }
        }
    }
    
    /// <summary>
    /// 从列表项获取单位（需要 UnitListItem 提供访问器）
    /// </summary>
    private Unit GetUnitFromItem(UnitListItem item)
    {
        // 这里需要通过反射或其他方式获取，暂时返回 null
        // 更好的方法是在 UnitListItem 中添加公共属性
        return null;
    }
    
    /// <summary>
    /// 获取当前任务建筑
    /// </summary>
    public TaskBuilding GetCurrentTaskBuilding()
    {
        return _currentTaskBuilding;
    }
    
    /// <summary>
    /// 检查是否在任务分配模式
    /// </summary>
    public bool IsTaskAssignmentMode()
    {
        return _isTaskAssignmentMode;
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnUnitKilled -= OnUnitKilled;
        }
        
        // 取消订阅军事建筑的事件
        foreach (var building in _militaryBuildings)
        {
            if (building != null)
            {
                building.OnUnitSpawned -= OnUnitSpawned;
            }
        }
        _militaryBuildings.Clear();
        
        ClearList();
    }
}