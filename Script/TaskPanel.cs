using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 任务面板 - 显示预设的任务按钮，点击后进入选择单位模式
/// </summary>
public class TaskPanel : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject panelObject;
    public TMP_Text TitleText;
    
    [Header("Task Buttons")]
    [Tooltip("在 Inspector 中拖拽已创建好的任务按钮")]
    public Button[] taskButtons;
    
    [Header("Buttons")]
    public Button CloseButton;
    
    private TaskBuilding _currentBuilding;

    public bool Workable;
    
    private void Start()
    {
        // 绑定关闭按钮
        if (CloseButton != null)
        {
            CloseButton.onClick.AddListener(Hide);
        }
        Workable = false;
        // 初始隐藏
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 初始化面板
    /// </summary>
    public void Initialize(TaskBuilding building)
    {
        _currentBuilding = building;
        
        // 设置标题
        if (TitleText != null && building != null)
        {
            TitleText.text = $"{building.Name} - 任务分配";
        }
        
        // 绑定所有任务按钮的事件
        BindButtonEvents();
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
    /// 绑定所有任务按钮的点击事件
    /// </summary>
    private void BindButtonEvents()
    {
        if (taskButtons == null) return;
        
        foreach (Button button in taskButtons)
        {
            if (button != null)
            {
                // 获取按钮上的文本组件作为任务名称
                TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
                string taskName = buttonText != null ? buttonText.text : "未命名任务";
                
                // 移除旧的事件监听（防止重复绑定）
                button.onClick.RemoveAllListeners();
                
                // 绑定新的事件
                button.onClick.AddListener(() => OnTaskButtonClicked(taskName));
            }
        }
    }
    
    /// <summary>
    /// 任务按钮点击事件 - 进入选择单位模式
    /// </summary>
       private void OnTaskButtonClicked(string taskName)
    {
        Debug.Log($"[TaskPanel] 选择任务：{taskName}，进入选择单位模式");
        
        if (_currentBuilding != null)
        {
            _currentBuilding.StartSelectingUnits(taskName);
            
            Hide();
            
            // 通过 UIManager 显示单位列表面板
            UnitListPanel unitListPanel = null;
            
            if (UIManager.Instance != null)
            {
                unitListPanel = UIManager.Instance.UnitListPanel;
                Debug.Log($"[TaskPanel] UIManager.Instance 存在: {UIManager.Instance != null}");
                Debug.Log($"[TaskPanel] UIManager.Instance.UnitListPanel 存在: {unitListPanel != null}");
            }
            else
            {
                Debug.LogWarning("[TaskPanel] UIManager.Instance 为 null");
            }
            
            // 如果 UIManager 中没有设置，尝试直接查找
            if (unitListPanel == null)
            {
                unitListPanel = FindObjectOfType<UnitListPanel>();
                Debug.Log($"[TaskPanel] 尝试 FindObjectOfType: {unitListPanel != null}");
            }
            
            if (unitListPanel != null)
            {
                unitListPanel.ShowForTaskAssignment(_currentBuilding, taskName);
                Debug.Log($"[TaskPanel] 已打开单位列表面板");
            }
            else
            {
                Debug.LogError("[TaskPanel] 无法找到 UnitListPanel！请在 Unity Inspector 中设置 UIManager.UnitListPanel");
            }
        }
        else
        {
            Debug.LogError("[TaskPanel] _currentBuilding 为空！");
        }
    }
}

