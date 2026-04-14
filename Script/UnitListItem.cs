
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 单位列表项 - 显示单个单位的信息
/// </summary>
public class UnitListItem : MonoBehaviour
{
    [Header("Auto-Found Components")]
    private Image _iconImage;
    private TMP_Text _nameText;
    private TMP_Text _hpText;
    private TMP_Text _stateText;
    private Button _selectButton;
    private Slider _hpSlider;
    
    [Header("Settings")]
    public Color NormalColor = Color.white;
    public Color SelectedColor = new Color(1f, 0.8f, 0f);
    public Color DeadColor = Color.gray;
    
    private Unit _unit;
    private bool _isSelected = false;
    
    // 自动查找组件
    private void Awake()
    {
        AutoFindComponents();
    }
    
    /// <summary>
    /// 自动查找子对象中的组件
    /// </summary>
    private void AutoFindComponents()
    {
        // 查找 Image
        _iconImage = GetComponentInChildren<Image>(true);
        
        // 查找所有 TMP_Text
        TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);
        
        foreach (var text in allTexts)
        {
            string textName = text.name.ToLower();
            
            if (textName.Contains("name"))
            {
                _nameText = text;
            }
            else if (textName.Contains("hp") || textName.Contains("health"))
            {
                _hpText = text;
            }
            else if (textName.Contains("state") || textName.Contains("status"))
            {
                _stateText = text;
            }
        }
        
        // 查找 Button
        _selectButton = GetComponentInChildren<Button>(true);
        if (_selectButton == null)
        {
            _selectButton = GetComponent<Button>();
        }
        
        // 查找 Slider（可选）
        _hpSlider = GetComponentInChildren<Slider>(true);
        
        // 绑定点击事件
        if (_selectButton != null)
        {
            _selectButton.onClick.AddListener(OnItemSelected);
        }
    }
    
    /// <summary>
    /// 初始化列表项
    /// </summary>
    public void Initialize(Unit unit)
    {
        _unit = unit;
        UpdateDisplay();
    }
    
    /// <summary>
    /// 更新显示信息
    /// </summary>
    public void UpdateDisplay()
    {
        if (_unit == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        // 设置名称
        if (_nameText != null)
        {
            _nameText.text = _unit.Name ?? _unit.GetType().Name;
        }
        
        // 设置血量
        if (_hpText != null)
        {
            _hpText.text = $"{_unit.CurrentHealth}/{_unit.MaxHealth}";
        }
        
        // 设置血条
        if (_hpSlider != null)
        {
            _hpSlider.maxValue = _unit.MaxHealth;
            _hpSlider.value = _unit.CurrentHealth;
        }
        
        // 设置状态
        if (_stateText != null)
        {
            _stateText.text = GetStateDisplayName(_unit.CurrentState);
        }
        
        // 设置图标（如果有）
        if (_iconImage != null)
        {
            Sprite icon = GetUnitIcon();
            if (icon != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = true;
            }
            else
            {
                _iconImage.enabled = false;
            }
        }
        
        // 死亡单位显示灰色
        if (_unit.CurrentState == UnitState.Dead)
        {
            SetVisualState(DeadColor);
        }
        else if (_isSelected)
        {
            SetVisualState(SelectedColor);
        }
        else
        {
            SetVisualState(NormalColor);
        }
    }
    
    /// <summary>
    /// 获取单位图标
    /// </summary>
    private Sprite GetUnitIcon()
    {
        // 尝试从单位身上获取图标组件
        SpriteRenderer spriteRenderer = _unit.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            return spriteRenderer.sprite;
        }
        
        // 或者尝试从 UI Image 获取
        Image unitImage = _unit.GetComponent<Image>();
        if (unitImage != null && unitImage.sprite != null)
        {
            return unitImage.sprite;
        }
        
        return null;
    }
    
    /// <summary>
    /// 获取状态的显示名称
    /// </summary>
    private string GetStateDisplayName(UnitState state)
    {
        switch (state)
        {
            case UnitState.Idle: return "待机";
            case UnitState.Moving: return "移动中";
            case UnitState.Attacking: return "攻击中";
            case UnitState.Combat: return "战斗";
            case UnitState.Working: return "工作中";
            case UnitState.Dead: return "死亡";
            default: return state.ToString();
        }
    }
    
    /// <summary>
    /// 设置视觉状态
    /// </summary>
    private void SetVisualState(Color color)
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        if (_iconImage != null)
        {
            _iconImage.color = color;
        }
        
        canvasGroup.alpha = (_unit.CurrentState == UnitState.Dead) ? 0.5f : 1f;
    }
    
    /// <summary>
    /// 点击选中事件
    /// </summary>
     private void OnItemSelected()
    {
        if (_unit == null || _unit.CurrentState == UnitState.Dead)
        {
            return;
        }
        
        UnitListPanel parentPanel = GetComponentInParent<UnitListPanel>();
        
        if (parentPanel != null && parentPanel.IsTaskAssignmentMode())
        {
            TaskBuilding building = parentPanel.GetCurrentTaskBuilding();
            if (building != null)
            {
                building.AssignUnit(_unit);
                
                SelectionManager.Instance.ClearSelection();
                SelectionManager.Instance.SelectUnit(_unit);
                
                _isSelected = true;
                
                parentPanel.Hide();
                
                Debug.Log($"[UnitListItem] 分配单位 {_unit.Name} 到任务建筑 {building.Name}");
                return;
            }
        }
        
        SelectionManager.Instance.ClearSelection();
        SelectionManager.Instance.SelectUnit(_unit);
        
        _isSelected = true;
        
        Debug.Log($"[UnitListItem] 选中单位：{_unit.Name}");
    }
    
    /// <summary>
    /// 清理资源
    /// </summary>
    public void Cleanup()
    {
        if (_selectButton != null)
        {
            _selectButton.onClick.RemoveAllListeners();
        }
        _unit = null;
    }
    
    /// <summary>
    /// 设置选中状态
    /// </summary>
    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        UpdateDisplay();
    }
    
    private void OnDestroy()
    {
        Cleanup();
    }
}