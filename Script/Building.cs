using UnityEngine;

public abstract class Building : MonoBehaviour
{
    public int ID { get; protected set; }
    public string Name { get; protected set; }
    public int MaxHealth { get; protected set; }
    public int CurrentHealth { get; protected set; }
    public Vector3 Position { get; set; }
    public Team Team { get; protected set; }
    public BuildingState CurrentState { get; protected set; }
    public float ConstructionProgress { get; protected set; }
    public float RequiredConstructionTime { get; protected set; }

    protected virtual void Start()
    {
        Position = transform.position;
    }

    // Unity 的 Update 不能有参数
    protected virtual void Update()
    {
        Position = transform.position;
        // 调用自定义的更新方法
        GameUpdate(Time.deltaTime);
    }

    // 自定义更新方法（可带参数）
    public abstract void GameUpdate(float deltaTime);

    public virtual void OnDamage(int damage)
    {
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            CurrentState = BuildingState.Destroyed;
        }
    }
    
    /// <summary>
    /// 激活建筑
    /// </summary>
    public void ActivateBuilding()
    {
        CurrentState = BuildingState.Active;
        ConstructionProgress = 1f;
        Debug.Log($"[Building] {gameObject.name} 已激活，状态：{CurrentState}");
    }
    
    /// <summary>
    /// 开始建造建筑（如果有建造过程）
    /// </summary>
    public void StartConstruction(float constructionTime)
    {
        CurrentState = BuildingState.Constructing;
        RequiredConstructionTime = constructionTime;
        ConstructionProgress = 0f;
    }
}