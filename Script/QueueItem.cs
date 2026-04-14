using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 生产队列项 - 显示正在排队的单位
/// </summary>
public class QueueItem : MonoBehaviour
{
    [Header("Auto-Found Components")]
    private Image _iconImage;
    private TMP_Text _nameText;
    private TMP_Text _timerText;
    private Button _cancelButton;
    private Slider _progressSlider;
    
    private int _queueIndex;
    
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
            else if (textName.Contains("timer") || textName.Contains("time"))
            {
                _timerText = text;
            }
        }
        
        // 查找 Button
        _cancelButton = GetComponentInChildren<Button>(true);
        if (_cancelButton == null)
        {
            _cancelButton = GetComponent<Button>();
        }
        
        // 查找 Slider
        _progressSlider = GetComponentInChildren<Slider>(true);
    }
    
    public void Initialize(string name, Sprite icon, float remainingTime, int index)
    {
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
        
        // 设置计时器
        if (_timerText != null)
        {
            _timerText.text = $"{remainingTime:F1}s";
        }
        
        _queueIndex = index;
    }
    
    public void UpdateTimer(float remainingTime)
    {
        if (_timerText != null)
        {
            _timerText.text = $"{remainingTime:F1}s";
        }
    }
    
    public void SetAsLastInQueue(bool isLast)
    {
        if (_cancelButton != null)
        {
            _cancelButton.gameObject.SetActive(isLast);
        }
    }
    
    public void AddCancelListener(UnityEngine.Events.UnityAction action)
    {
        if (_cancelButton != null)
        {
            _cancelButton.onClick.AddListener(action);
        }
    }
    
    public void RemoveAllListeners()
    {
        if (_cancelButton != null)
        {
            _cancelButton.onClick.RemoveAllListeners();
        }
    }
}