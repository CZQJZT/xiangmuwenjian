using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class MilitaryBuilding : Building
{
    [Header("Unit Training")]
    public List<UnitTrainingConfig> AvailableUnits = new List<UnitTrainingConfig>();
    
    [Header("Initial Settings")]
    public bool SpawnUnitOnStart = true;
    public int InitialUnitConfigId = 0;
    
    // 生产队列（存储配置而不是 Unit 实例）
    public Queue<ProductionItem> ProductionQueue { get; private set; } = new Queue<ProductionItem>();
    
    // 当前正在生产的单位
    private ProductionItem _currentProduction;
    private float _currentProductionTimer;
    
    // 最大队列长度
    public int MaxQueueSize = 5;
    
    // 事件
    public Action OnProductionStarted;
    public Action OnProductionCompleted;
    public Action OnQueueUpdated;
    public Action<Unit> OnUnitSpawned; // 单位生成事件
    protected override void Start()
    {
       
        ActivateBuilding();
       
        
    }

    /// <summary>
    /// 生成初始单位
    /// </summary>
    public void SpawnInitialUnit()
    {
        if (CurrentState != BuildingState.Active)
        {
            Debug.LogWarning("[MilitaryBuilding] 建筑未激活，无法生成初始单位");
            return;
        }
        
        UnitTrainingConfig config = null;
        
        // 如果指定了 ID，使用指定的配置
        if (InitialUnitConfigId > 0)
        {
            config = AvailableUnits.Find(u => u.ID == InitialUnitConfigId);
        }
        
        // 如果没有指定或找不到，使用第一个配置
        if (config == null)
        {
            config = AvailableUnits[0];
        }
        
        if (config != null)
        {
            Debug.Log($"[MilitaryBuilding] 游戏开始生成初始单位：{config.Name}");
            SpawnUnit(config);
        }
        else
        {
            Debug.LogError("[MilitaryBuilding] 没有可用的单位配置！");
        }
    }

    

    // 生产队列项
    [Serializable]
    public class ProductionItem
    {
        public UnitTrainingConfig Config;
        public float RemainingTime;
        
        public ProductionItem(UnitTrainingConfig config)
        {
            Config = config;
            RemainingTime = config.TrainingTime;
        }
    }

    public override void GameUpdate(float deltaTime)
    {
        if (CurrentState != BuildingState.Active) return;
        
        // 处理生产逻辑
        HandleProduction(deltaTime);
    }

    private void HandleProduction(float deltaTime)
    {
        // 如果当前没有生产单位，从队列中取出下一个
        if (_currentProduction == null)
        {
            if (ProductionQueue.Count > 0)
            {
                _currentProduction = ProductionQueue.Dequeue();
                _currentProductionTimer = _currentProduction.RemainingTime;
                OnProductionStarted?.Invoke();
                Debug.Log($"[MilitaryBuilding] 开始生产：{_currentProduction.Config.Name}");
            }
            return;
        }
        
        // 减少生产时间
        _currentProductionTimer -= deltaTime;
        
        // 检查是否完成生产
        if (_currentProductionTimer <= 0)
        {
            CompleteProduction();
        }
    }

    private void CompleteProduction()
    {
        if (_currentProduction == null) return;
        
        // 生成单位
        SpawnUnit(_currentProduction.Config);
        
        Debug.Log($"[MilitaryBuilding] 完成生产：{_currentProduction.Config.Name}");
        
        // 触发事件
        OnProductionCompleted?.Invoke();
        
        // 清空当前生产
        _currentProduction = null;
    }

    private void SpawnUnit(UnitTrainingConfig config)
    {
        if (config.Prefab == null)
        {
            Debug.LogError($"[MilitaryBuilding] 单位预制体为空：{config.Name}");
            return;
        }
        
        // 在建筑附近生成单位
        Vector3 spawnPosition = GetSpawnPosition();
        
        GameObject unitObj = Instantiate(config.Prefab, spawnPosition, Quaternion.identity);
        
        // 初始化单位属性
        Unit unit = unitObj.GetComponent<Unit>();
        if (unit != null)
        {
            unit.SetTeam(this.Team);
            
            // 使用命名池中的随机名称
            string randomName = config.NamePool.GetRandomName();
            if (!string.IsNullOrEmpty(randomName))
            {
                unitObj.name = randomName;
                unit.SetName(randomName); // 设置 Unit.Name 属性
                Debug.Log($"[MilitaryBuilding] 使用随机命名：{randomName} (原名称：{config.Name})");
            }
            else
            {
                // 如果命名池为空，使用配置中的默认名称
                unitObj.name = config.Name;
                unit.SetName(config.Name); // 设置 Unit.Name 属性
            }
        }
        
        Debug.Log($"[MilitaryBuilding] 单位已生成：{unitObj.name} at {spawnPosition}");
        
        // 触发单位生成事件
        OnUnitSpawned?.Invoke(unit);
    }

    private Vector3 GetSpawnPosition()
    {
        // 在建筑前方生成单位
        Vector3 spawnOffset = transform.forward * 3f + transform.right * 2f;
        return transform.position + spawnOffset;
    }

    // 训练单位（添加到队列）
    public bool TrainUnit(int unitConfigId)
    {
        // 查找配置
        UnitTrainingConfig config = AvailableUnits.Find(u => u.ID == unitConfigId);
        
        if (config == null)
        {
            Debug.LogError($"[MilitaryBuilding] 找不到单位配置 ID: {unitConfigId}");
            return false;
        }
        
        // 检查队列是否已满
        if (ProductionQueue.Count >= MaxQueueSize)
        {
            Debug.LogWarning($"[MilitaryBuilding] 生产队列已满！");
            return false;
        }
        
        // 检查资源是否足够
        if (!CanAffordUnit(config.Cost))
        {
            Debug.LogWarning($"[MilitaryBuilding] 资源不足！需要：{config.Cost}");
            return false;
        }
        
        // 添加到队列
        ProductionItem item = new ProductionItem(config);
        ProductionQueue.Enqueue(item);
        
        // 扣除资源
        DeductResource(config.Cost);
        
        Debug.Log($"[MilitaryBuilding] 单位已加入队列：{config.Name}, 队列长度：{ProductionQueue.Count}");
        
        // 触发事件
        OnQueueUpdated?.Invoke();
        
        return true;
    }

    // 通过类型训练单位
    public bool TrainUnit(UnitType type)
    {
        UnitTrainingConfig config = AvailableUnits.Find(u => u.UnitType == type);
        
        if (config == null)
        {
            Debug.LogError($"[MilitaryBuilding] 找不到单位类型：{type}");
            return false;
        }
        
        return TrainUnit(config.ID);
    }

    // 取消队列中的最后一个单位
    public bool CancelLastInQueue()
    {
        if (ProductionQueue.Count == 0) return false;
        
        // 将 Queue 转换为 List 以使用 Last()
        var queueList = ProductionQueue.ToList();
        var lastItem = queueList.Last();
        
        // 移除最后一个元素
        ProductionQueue = new Queue<ProductionItem>(queueList.Take(queueList.Count - 1));
        
        // 返还资源
        RefundResource(lastItem.Config.Cost);
        
        OnQueueUpdated?.Invoke();
        
        return true;
    }

    // 立即完成当前生产
    public void InstantFinish()
    {
        if (_currentProduction != null)
        {
            _currentProductionTimer = 0;
        }
    }

    private bool CanAffordUnit(int cost)
    {
        return ResourceManager.Instance != null && 
               ResourceManager.Instance.GetResource(Team) >= cost;
    }

    private void DeductResource(int cost)
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResource(Team, -cost);
        }
    }

    private void RefundResource(int amount)
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResource(Team, amount);
        }
    }

    // 获取当前生产状态
    public ProductionItem GetCurrentProduction()
    {
        return _currentProduction;
    }

    // 获取生产进度（0-1）
    public float GetProductionProgress()
    {
        if (_currentProduction == null || _currentProduction.Config.TrainingTime == 0)
        {
            return 0;
        }
        
        return 1f - (_currentProductionTimer / _currentProduction.Config.TrainingTime);
    }

    // 获取队列列表
    public List<ProductionItem> GetQueueList()
    {
        return new List<ProductionItem>(ProductionQueue);
    }
}