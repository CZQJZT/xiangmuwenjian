using UnityEngine;

public abstract class Unit : MonoBehaviour
{
    public int ID { get; protected set; }
    public string Name { get; protected set; }
    public int MaxHealth { get; protected set; }
    public int CurrentHealth { get; protected set; }
    public float MoveSpeed { get; protected set; }
    public Vector3 Position { get; set; }
    public Team Team { get; protected set; }
    public UnitState CurrentState { get; protected set; }
    
    // 单位分配冷却相关
    public float LastAssignedTime { get; set; } = -999f; // 上次被分配的时间，初始值为负数表示从未被分配
    public float AssignmentCooldown { get; set; } = 5f; // 分配后冷却时间（秒）

// 任务相关
    public string CurrentTaskName { get; set; } // 当前执行的任务名称

    protected MovementComponent MovementComponent { get; set; }


    protected virtual void Start()
    {
        Position = transform.position;
        MovementComponent = GetComponent<MovementComponent>();
        UnitManager.Instance?.AddUnit(this);
    }

    protected virtual void OnDestroy()
    {
        UnitManager.Instance?.RemoveUnit(this);
    }

    protected virtual void Update()
    {
        Position = transform.position;
        GameUpdate(Time.deltaTime);
    }

    public abstract void GameUpdate(float deltaTime);

    public virtual void OnDeath()
    {
        CurrentState = UnitState.Dead;
        EventManager.Instance?.TriggerEvent("UnitKilled", this);
    }

    public void TakeDamage(int damage)
    {
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            OnDeath();
        }
    }

    // 便捷方法：通过 MovementComponent 移动
    public void MoveTo(Vector3 target)
    {
        MovementComponent?.MoveTo(target);
        CurrentState = UnitState.Moving;
    }

    public void StopMoving()
    {
        MovementComponent?.Stop();
        CurrentState = UnitState.Idle;
    }
    
    // 新增：公共方法用于设置单位状态
    public void SetState(UnitState state)
    {
        CurrentState = state;
    }
    
    // 新增：公共方法用于设置阵营
    public void SetTeam(Team team)
    {
        Team = team;
    }

  

    public void SetName(string name)
    {
        Name = name;
    }
}