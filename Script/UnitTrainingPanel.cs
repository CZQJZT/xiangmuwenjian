
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UnitTrainingPanel : MonoBehaviour
{
    [Header("UI References")]
    public Transform AvailableUnitsContent;
    public Transform ProductionQueueContent;
    public TMP_Text CurrentProductionText;
    public Slider ProductionProgressBar;
    public Button TrainButtonPrefab;
    public Button QueueItemPrefab;
    
    [Header("Data")]
    private MilitaryBuilding _currentBuilding;
    private List<Button> _availableButtons = new List<Button>();
    private List<Button> _queueButtons = new List<Button>();
    
    private void Start()
    {
        gameObject.SetActive(false);
    }
    
    public void Show(MilitaryBuilding building)
    {
        _currentBuilding = building;
        
        // 订阅事件
        building.OnProductionStarted += UpdateUI;
        building.OnProductionCompleted += UpdateUI;
        building.OnQueueUpdated += UpdateUI;
        
        UpdateUI();
        gameObject.SetActive(true);
    }
    
    public void Hide()
    {
        if (_currentBuilding != null)
        {
            _currentBuilding.OnProductionStarted -= UpdateUI;
            _currentBuilding.OnProductionCompleted -= UpdateUI;
            _currentBuilding.OnQueueUpdated -= UpdateUI;
        }
        
        gameObject.SetActive(false);
    }
    
    private void UpdateUI()
    {
        if (_currentBuilding == null) return;
        
        UpdateAvailableUnits();
        UpdateProductionQueue();
        UpdateCurrentProduction();
    }
    
    private void UpdateAvailableUnits()
    {
        ClearButtons(AvailableUnitsContent, _availableButtons);
        
        foreach (var config in _currentBuilding.AvailableUnits)
        {
            CreateUnitButton(config, AvailableUnitsContent, _availableButtons, true);
        }
    }
    
    private void UpdateProductionQueue()
    {
        ClearButtons(ProductionQueueContent, _queueButtons);
        
        var queue = _currentBuilding.GetQueueList();
        
        foreach (var item in queue)
        {
            CreateQueueItem(item, ProductionQueueContent, _queueButtons);
        }
    }
    
    private void UpdateCurrentProduction()
    {
        var current = _currentBuilding.GetCurrentProduction();
        
        if (current != null)
        {
            CurrentProductionText.text = $"<b>正在生产:</b> {current.Config.Name}\n{current.RemainingTime:F1}s";
            ProductionProgressBar.value = _currentBuilding.GetProductionProgress();
            ProductionProgressBar.gameObject.SetActive(true);
        }
        else
        {
            CurrentProductionText.text = "<i>空闲</i>";
            ProductionProgressBar.gameObject.SetActive(false);
        }
    }
    
    private void CreateUnitButton(UnitTrainingConfig config, Transform parent, List<Button> buttonList, bool isAvailable)
    {
        GameObject buttonObj = Instantiate(TrainButtonPrefab.gameObject, parent);
        Button button = buttonObj.GetComponent<Button>();
        
        // 设置图标和文本
        Image icon = buttonObj.GetComponentInChildren<Image>();
        TMP_Text text = buttonObj.GetComponentInChildren<TMP_Text>();
        
        if (icon != null && config.Icon != null)
        {
            icon.sprite = config.Icon;
        }
        
        if (text != null)
        {
            text.text = $"{config.Name}\n<size=12>{config.Cost}金 | {config.TrainingTime}s</size>";
        }
        
        // 绑定点击事件
        button.onClick.AddListener(() => OnTrainUnitClicked(config.ID));
        
        // 检查资源
        CheckResource(button, config.Cost);
        
        buttonList.Add(button);
    }
    
    private void CreateQueueItem(MilitaryBuilding.ProductionItem item, Transform parent, List<Button> buttonList)
    {
        GameObject buttonObj = Instantiate(QueueItemPrefab.gameObject, parent);
        Button button = buttonObj.GetComponent<Button>();
        
        TMP_Text text = buttonObj.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = $"{item.Config.Name}\n{item.RemainingTime:F1}s";
        }
        
        // 点击取消（如果是最后一个）
        button.onClick.AddListener(() => OnQueueItemClicked(item));
        
        buttonList.Add(button);
    }
    
    private void OnTrainUnitClicked(int configId)
    {
        if (_currentBuilding != null)
        {
            _currentBuilding.TrainUnit(configId);
        }
    }
    
    private void OnQueueItemClicked(MilitaryBuilding.ProductionItem item)
    {
        // 只允许取消最后一个
        var queue = _currentBuilding.GetQueueList();
        if (queue.Count > 0 && queue[queue.Count - 1] == item)
        {
            _currentBuilding.CancelLastInQueue();
        }
    }
    
    private void CheckResource(Button button, int cost)
    {
        // 可以在这里添加实时资源检查逻辑
        // 如果资源不足，禁用按钮
    }
    
    private void ClearButtons(Transform parent, List<Button> buttonList)
    {
        foreach (var button in buttonList)
        {
            Destroy(button.gameObject);
        }
        buttonList.Clear();
        
        // 清除所有子对象（以防万一）
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
    
    private void OnDestroy()
    {
        Hide();
    }
}