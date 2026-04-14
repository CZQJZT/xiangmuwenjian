using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 任务建筑 - 点击后显示任务面板，可以分配单位执行任务
/// </summary>
public class TaskBuilding : Building
{
    [Header("Task Panel Settings")]
    public GameObject taskPanelPrefab;
    public Transform panelParent;
    
        [Header("Assignable Units")]
    [Tooltip("留空则允许所有 CombatUnit 类型，否则只允许列表中指定的类型")]
    public List<UnitType> assignableUnitTypes = new List<UnitType>();
    
    [Header("Task Processor Settings")]
    [Tooltip("任务处理器预制体，用于处理单位任务逻辑")]
    public GameObject taskProcessorPrefab;
    
    private TaskPanel _taskPanel;
    private List<Unit> _assignedUnits = new List<Unit>();
    
    [Header("Selection Mode")]
    private bool _workable = false;
    private string _currentTaskName;
    
    protected override void Start()
    {
        base.Start();
        ActivateBuilding();
        
        EventManager.Instance.OnUnitManuallySelected += HandleUnitSelected;
    }
    
    private void OnDestroy()
    {
        EventManager.Instance.OnUnitManuallySelected -= HandleUnitSelected;
    }
    
    public override void GameUpdate(float deltaTime)
    {
        if (CurrentState != BuildingState.Active) return;
    }
    
    private void OnMouseEnter()
    {
        if (CurrentState == BuildingState.Active)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
    
    private void OnMouseExit()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
    
    private void OnMouseDown()
    {
        if (CurrentState == BuildingState.Active)
        {
            OpenTaskPanel();
        }
    }
    
    /// <summary>
    /// 打开任务面板
    /// </summary>
    public void OpenTaskPanel()
    {
        if (_taskPanel == null)
        {
            if (taskPanelPrefab == null)
            {
                Debug.LogError("[TaskBuilding] taskPanelPrefab 未设置！");
                return;
            }
            
            if (panelParent == null)
            {
                Debug.LogError("[TaskBuilding] panelParent 未设置！");
                return;
            }
            
            GameObject panelObj = Instantiate(taskPanelPrefab, panelParent);
            _taskPanel = panelObj.GetComponent<TaskPanel>();
            
            if (_taskPanel == null)
            {
                Debug.LogWarning("[TaskBuilding] TaskPanel 组件不存在，尝试添加");
                _taskPanel = panelObj.AddComponent<TaskPanel>();
            }
        }
        
        _taskPanel.Initialize(this);
        _taskPanel.Show();
        
        Debug.Log($"[TaskBuilding] 打开任务面板：{Name}");
    }
    
    /// <summary>
    /// 关闭任务面板
    /// </summary>
    public void CloseTaskPanel()
    {
        if (_taskPanel != null)
        {
            _taskPanel.Hide();
        }
    }
    
    /// <summary>
    /// 启动单位选择模式
    /// </summary>
   public void StartSelectingUnits(string taskName)
    {
        _workable = true;
        _currentTaskName = taskName;
        
        Debug.Log($"[TaskBuilding] ===== 启动单位选择模式 =====");
        Debug.Log($"[TaskBuilding] 任务：{taskName}");
        Debug.Log($"[TaskBuilding] _workable 设置为: {_workable}");
        Debug.Log($"[TaskBuilding] 点击空白处可取消选择模式");
    }
    
    /// <summary>
    /// 禁用工作模式
    /// </summary>
     public void DisableWorkable()
    {
        if (!_workable) return;
        
        Debug.Log($"[TaskBuilding] ===== 关闭单位选择模式 =====");
        Debug.Log($"[TaskBuilding] _workable 从 true 设置为 false");
        
        _workable = false;
        _currentTaskName = null;
        
        // 通过 UIManager 隐藏单位列表面板
        if (UIManager.Instance != null && UIManager.Instance.UnitListPanel != null)
        {
            if (UIManager.Instance.UnitListPanel.gameObject.activeInHierarchy)
            {
                UIManager.Instance.UnitListPanel.Hide();
                Debug.Log("[TaskBuilding] 已关闭单位列表面板");
            }
        }
        
        Debug.Log($"[TaskBuilding] 单位选择模式已关闭");
    }
    /// <summary>
    /// 处理单位选中事件
    /// </summary>
    private void HandleUnitSelected(Unit unit)
    {
        Debug.Log($"[TaskBuilding] HandleUnitSelected 被调用, _workable={_workable}, unit={unit?.Name}");
        
        if (!_workable)
        {
            Debug.Log($"[TaskBuilding] _workable 为 false，跳过分配");
            return;
        }
        
        if (CanAssignUnit(unit))
        {
            AssignUnit(unit);
            
            Debug.Log($"[TaskBuilding] Workable模式下自动分配单位：{unit.Name}");
        }
        else
        {
            Debug.Log($"[TaskBuilding] 单位 {unit.Name} 无法分配");
        }
    }
    
    /// <summary>
    /// 分配单位到任务建筑
    /// </summary>
     public void AssignUnit(Unit unit)
    {
        
        
        // 记录分配时间
        unit.LastAssignedTime = Time.time;
        
        // 保存当前任务名称（从 TaskBuilding 的 _currentTaskName 获取）
        unit.CurrentTaskName = _currentTaskName;
        
        Debug.Log($"[TaskBuilding] ===== 记录单位分配 =====");
        Debug.Log($"[TaskBuilding] 单位名称: {unit.Name}");
        Debug.Log($"[TaskBuilding] 任务名称: {unit.CurrentTaskName}");
        Debug.Log($"[TaskBuilding] 分配时间: {unit.LastAssignedTime:F2}");
        Debug.Log($"[TaskBuilding] 冷却时长: {unit.AssignmentCooldown}秒");
        Debug.Log($"[TaskBuilding] 将在 {Time.time + unit.AssignmentCooldown:F2} 后重新显示");
        
        if (unit is CombatUnit combatUnit)
        {
            combatUnit.SetTargetBuilding(this);
        }
        
        MoveUnitToBuilding(unit);
        
        Debug.Log($"[TaskBuilding] 分配单位 {unit.Name} 到任务建筑 {Name}");
        
        EventManager.Instance?.OnUnitAssigned?.Invoke(unit, this);

        DisableWorkable();
    }
    
    /// <summary>
    /// 当单位到达建筑时，启动任务处理器
    /// </summary>
    public void OnUnitArrived(Unit unit, string taskName)
    {
        Debug.Log($"[TaskBuilding] 单位 {unit.Name} 已到达建筑 {Name}，启动任务处理器");
        
        if (taskProcessorPrefab == null)
        {
            Debug.LogError("[TaskBuilding] taskProcessorPrefab 未设置！请在 Inspector 中配置");
            return;
        }
        
        // 创建任务处理器
        GameObject processorObj = Instantiate(taskProcessorPrefab, transform);
        TaskProcessor processor = processorObj.GetComponent<TaskProcessor>();
        
        if (processor == null)
        {
            Debug.LogError("[TaskBuilding] TaskProcessor 组件不存在！");
            Destroy(processorObj);
            return;
        }
        
        // 订阅任务完成事件
        processor.OnTaskCompleted += (result) =>
        {
            HandleTaskCompletion(unit, result);
        };
        
        // 启动任务
        processor.StartTask(unit, this, taskName);
    }
    
    /// <summary>
    /// 处理任务完成
    /// </summary>
    private void HandleTaskCompletion(Unit unit, TaskProcessor.TaskResult result)
    {
        Debug.Log($"[TaskBuilding] 任务完成 - 单位: {unit.Name}, 结果: {result}");
        
        // 从已分配列表中移除（如果需要跟踪）
        if (_assignedUnits.Contains(unit))
        {
            _assignedUnits.Remove(unit);
        }
        
        // 这里可以添加任务完成后的奖励或其他逻辑
        if (result == TaskProcessor.TaskResult.Success)
        {
            Debug.Log($"[TaskBuilding] 任务成功！给予奖励...");
            // TODO: 添加成功奖励逻辑
        }
        else
        {
            Debug.LogWarning($"[TaskBuilding] 任务失败！");
            // TODO: 添加失败惩罚逻辑
        }
    }
    
    /// <summary>
    /// 移除分配的单位
    /// </summary>
    public void RemoveUnit(Unit unit)
    {
        if (_assignedUnits.Contains(unit))
        {
            _assignedUnits.Remove(unit);
            
            if (unit is CombatUnit combatUnit)
            {
                combatUnit.ClearTargetBuilding();
            }
            
            Debug.Log($"[TaskBuilding] 移除单位 {unit.Name}");
        }
    }
    
    /// <summary>
    /// 移动单位到建筑位置
    /// </summary>
    private void MoveUnitToBuilding(Unit unit)
    {
        if (unit != null)
        {
            unit.SetState(UnitState.Working);
            
            unit.MoveTo(Position);
        }
    }
    
    /// <summary>
    /// 检查单位是否可以被分配
    /// </summary>
    public bool CanAssignUnit(Unit unit)
    {
        Debug.Log($"[TaskBuilding.CanAssignUnit] 检查单位: {unit.Name}, 类型: {unit.GetType().Name}");
        
        
        float timeSinceAssigned = UnityEngine.Time.time - unit.LastAssignedTime;
        if (timeSinceAssigned < unit.AssignmentCooldown)
        {
            Debug.Log($"[TaskBuilding.CanAssignUnit] 单位 {unit.Name} 处于冷却期，剩余 {unit.AssignmentCooldown - timeSinceAssigned:F1}秒");
            return false;
        }
        
        if (unit is CombatUnit combatUnit)
        {
            Debug.Log($"[TaskBuilding.CanAssignUnit] 单位是 CombatUnit 类型");
            
            // 如果 assignableUnitTypes 为空，允许所有 CombatUnit
            if (assignableUnitTypes.Count > 0)
            {
                Debug.Log($"[TaskBuilding.CanAssignUnit] assignableUnitTypes 包含的类型: {string.Join(", ", assignableUnitTypes)}");
                
                if (!assignableUnitTypes.Contains(UnitType.Worker))
                {
                    Debug.Log($"[TaskBuilding.CanAssignUnit] assignableUnitTypes 不包含 UnitType.Worker，拒绝");
                    return false;
                }
            }
            else
            {
                Debug.Log($"[TaskBuilding.CanAssignUnit] assignableUnitTypes 为空，允许所有 CombatUnit");
            }
        }
        else
        {
            Debug.Log($"[TaskBuilding.CanAssignUnit] 单位不是 CombatUnit 类型，拒绝");
            return false;
        }
        
        Debug.Log($"[TaskBuilding.CanAssignUnit] 单位 {unit.Name} 符合分配条件");
        return true;
    }
    /// <summary>
    /// 获取已分配的单位列表
    /// </summary>
    public List<Unit> GetAssignedUnits()
    {
        return new List<Unit>(_assignedUnits);
    }
}