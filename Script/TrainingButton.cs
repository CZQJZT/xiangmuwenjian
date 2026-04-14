using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 单位训练按钮 - 用于显示可训练的单位
/// </summary>
public class TrainingButton : MonoBehaviour
{
    [Header("Auto-Found Components")]
    private Image _iconImage;
    private TMP_Text _nameText;
    private TMP_Text _costText;
    private TMP_Text _timeText;
    private Button _trainButton;
    
    [Header("States")]
    public Color NormalColor = Color.white;
    public Color DisabledColor = Color.gray;
    
    private int _unitId;
    private bool _canAfford = true;
    
    // 在 Start 或 Awake 中自动查找组件
    private void Awake()
    {
        AutoFindComponents();
    }
    
    /// <summary>
    /// 自动查找子对象中的组件
    /// </summary>
    private void AutoFindComponents()
    {
        // 查找 Image（通常是 Icon）
        _iconImage = GetComponentInChildren<Image>(true);
        
        // 查找所有 TMP_Text
        TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);
        
        foreach (var text in allTexts)
        {
            // 根据名称或标签分配文本组件
            string textName = text.name.ToLower();
            
            if (textName.Contains("name"))
            {
                _nameText = text;
            }
            else if (textName.Contains("cost"))
            {
                _costText = text;
            }
            else if (textName.Contains("time"))
            {
                _timeText = text;
            }
        }
        
        // 查找 Button
        _trainButton = GetComponentInChildren<Button>(true);
        
        // 如果没有找到，尝试从自身获取
        if (_trainButton == null)
        {
            _trainButton = GetComponent<Button>();
        }
    }
    
    public void Initialize(int unitId, string name, Sprite icon, int cost, float time, bool canAfford)
    {
        _unitId = unitId;
        
        // 设置图标
        if (_iconImage != null && icon != null)
        {
            _iconImage.sprite = icon;
            _iconImage.enabled = true;
        }
        else if (_iconImage != null)
        {
            _iconImage.enabled = false;
        }
        
        // 设置名称
        if (_nameText != null)
        {
            _nameText.text = name;
        }
        
        // 设置造价
        if (_costText != null)
        {
            _costText.text = $"{cost}金";
        }
        
        // 设置时间
        if (_timeText != null)
        {
            _timeText.text = $"{time:F1}s";
        }
        
        SetInteractable(canAfford);
    }
    
    public void SetInteractable(bool interactable)
    {
        if (_trainButton != null)
        {
            _trainButton.interactable = interactable;
        }
        
        _canAfford = interactable;
        
        // 更新颜色
        if (_iconImage != null)
        {
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = interactable ? 1f : 0.5f;
        }
    }
    
    public void AddListener(UnityEngine.Events.UnityAction action)
    {
        if (_trainButton != null)
        {
            _trainButton.onClick.AddListener(action);
        }
    }
    
    public void RemoveAllListeners()
    {
        if (_trainButton != null)
        {
            _trainButton.onClick.RemoveAllListeners();
        }
    }
}