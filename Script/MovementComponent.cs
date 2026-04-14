using UnityEngine;
using UnityEngine.AI;

public class MovementComponent : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Unit _unit;
    private bool _isInitialized = false;

    // 组件初始化时获取 Unit 引用并配置 NavMeshAgent（使用 Awake 确保最早执行）
    private void Awake()
    {
        _unit = GetComponent<Unit>();
        
        if (_unit == null)
        {
            Debug.LogError($"[MovementComponent] {gameObject.name} 没有 Unit 组件！");
            return;
        }
        
        // 检查 GameObject 是否有效
        if (gameObject == null)
        {
            Debug.LogError($"[MovementComponent] GameObject 为空，无法初始化！");
            return;
        }
        
        InitializeNavMeshAgent();
    }

    /// <summary>
    /// 初始化 NavMeshAgent
    /// </summary>
    private void InitializeNavMeshAgent()
    {
        // 检查是否已经初始化
        if (_isInitialized)
        {
            return;
        }
        
        // 检查 GameObject 是否被标记为销毁
        if (this == null || gameObject == null)
        {
            Debug.LogError($"[MovementComponent] 组件或 GameObject 已被销毁！");
            return;
        }
        
        try
        {
            // 【关键修改】先尝试获取已有的 NavMeshAgent
            _agent = GetComponent<NavMeshAgent>();
            
            if (_agent == null)
            {
                Debug.Log($"[MovementComponent] {gameObject.name} 没有找到 NavMeshAgent，尝试添加...");
                
                // 如果没有，才添加
                _agent = gameObject.AddComponent<NavMeshAgent>();
                
                if (_agent == null)
                {
                    Debug.LogError($"[MovementComponent] {gameObject.name} 无法添加 NavMeshAgent! 请检查 Prefab 是否正确。");
                    Debug.LogError($"[MovementComponent] 可能原因：1.Prefab 损坏 2.缺少 Collider 3.GameObject 已销毁");
                    return;
                }
                
                Debug.Log($"[MovementComponent] {gameObject.name} 成功添加 NavMeshAgent");
            }
            else
            {
                Debug.Log($"[MovementComponent] {gameObject.name} 已有 NavMeshAgent，使用现有组件");
            }
            
            _agent.enabled = true;
            _agent.updateRotation = true;
            _agent.updateUpAxis = true;
            
            // 配置移动参数
            _agent.speed = _unit.MoveSpeed > 0 ? _unit.MoveSpeed : 5f;
            _agent.acceleration = 50;
            _agent.angularSpeed = 120;
            _agent.stoppingDistance = 0.1f;
            
            // 确保单位在 NavMesh 上
            EnsureOnNavMesh();
            
            _isInitialized = true;
            
            Debug.Log($"[MovementComponent] {gameObject.name} 初始化完成，速度：{_agent.speed}, isOnNavMesh: {_agent.isOnNavMesh}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MovementComponent] 初始化 NavMeshAgent 时发生异常：{e.Message}");
            Debug.LogError($"[MovementComponent] StackTrace: {e.StackTrace}");
        }
    }

    /// <summary>
    /// 确保单位在 NavMesh 上
    /// </summary>
    private void EnsureOnNavMesh()
    {
        if (_agent != null && !_agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                Debug.Log($"[MovementComponent] {gameObject.name} 已定位到 NavMesh: {hit.position}");
            }
            else
            {
                Debug.LogWarning($"[MovementComponent] {gameObject.name} 附近找不到 NavMesh，当前位置：{transform.position}");
            }
        }
    }

    public void MoveTo(Vector3 target)
    {
        // 确保已初始化
        if (!_isInitialized)
        {
            Debug.LogWarning($"[MovementComponent] {gameObject.name} 未初始化，正在初始化...");
            InitializeNavMeshAgent();
            
            if (!_isInitialized)
            {
                Debug.LogError($"[MovementComponent] {gameObject.name} 初始化失败！");
                return;
            }
        }
        
        if (_agent == null)
        {
            Debug.LogError($"[MovementComponent] {gameObject.name} NavMeshAgent 为空!");
            return;
        }
        
        if (!_agent.isOnNavMesh)
        {
            Debug.LogWarning($"[MovementComponent] {gameObject.name} 不在 NavMesh 上！位置：{transform.position}");
            
            // 尝试重新定位到 NavMesh
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                Debug.Log($"[MovementComponent] {gameObject.name} 已重新定位到 NavMesh: {hit.position}");
                
                // 重新检查是否在 NavMesh 上
                if (!_agent.isOnNavMesh)
                {
                    Debug.LogError($"[MovementComponent] {gameObject.name} 重新定位后仍然不在 NavMesh 上！");
                    return;
                }
            }
            else
            {
                Debug.LogError($"[MovementComponent] {gameObject.name} 附近找不到 NavMesh!");
                return;
            }
        }
        
        _agent.SetDestination(target);
        Debug.Log($"[MovementComponent] {gameObject.name} 开始移动到：{target}, 距离：{Vector3.Distance(transform.position, target)}");
    }

    public void Stop()
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }
    }

    public void Resume()
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
        }
    }

    public bool IsMoving()
    {
        return _agent != null && _agent.isOnNavMesh && _agent.remainingDistance > _agent.stoppingDistance;
    }

    public Vector3 GetTargetPosition()
    {
        if (_agent != null && _agent.isOnNavMesh && _agent.hasPath)
        {
            return _agent.destination;
        }
        return transform.position;
    }

    // Unity 生命周期方法（无参数）
    private void Update()
    {
        if (_isInitialized && _agent != null && _unit != null)
        {
            if (!_agent.isOnNavMesh)
            {
                return;
            }
            
            // 同步 Unit 的位置到 Transform
            _unit.Position = transform.position;
            
            // 如果到达目的地，更新状态
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                if (_unit.CurrentState == UnitState.Moving)
                {
                    // 检查是否到达了 TaskBuilding
                    CheckIfArrivedAtTaskBuilding();
                    
                    // 如果没有特殊处理，才设置为 Idle
                    if (_unit.CurrentState == UnitState.Moving)
                    {
                        _unit.SetState(UnitState.Idle);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 检查是否到达了 TaskBuilding
    /// </summary>
    private void CheckIfArrivedAtTaskBuilding()
    {
        if (_unit is CombatUnit combatUnit)
        {
            Building targetBuilding = combatUnit.GetTargetBuilding();
            
            if (targetBuilding is TaskBuilding taskBuilding)
            {
                Debug.Log($"[MovementComponent] 单位 {_unit.Name} 已到达 TaskBuilding: {taskBuilding.Name}");
                
                // 获取单位当前的任务名称
                string taskName = _unit.CurrentTaskName ?? "默认任务";
                
                // 通知 TaskBuilding 启动任务处理器
                taskBuilding.OnUnitArrived(_unit, taskName);
                
                // 设置单位为 Working 状态
                _unit.SetState(UnitState.Working);
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (_agent != null && _agent.hasPath)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _agent.destination);
            Gizmos.DrawWireSphere(_agent.destination, 0.5f);
        }
    }
}