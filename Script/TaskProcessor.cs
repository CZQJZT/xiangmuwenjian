
using UnityEngine;
using System;

/// <summary>
/// 任务处理器 - 处理单位执行任务的逻辑
/// </summary>
public class TaskProcessor : MonoBehaviour
{
    [Header("Task Settings")]
    public float CheckInterval = 5f; // 检测间隔（秒）
    public float SuccessRate = 0.5f; // 成功率（50%）
    public int MaxAttempts = 6; // 最大尝试次数
    
    [Header("Visual Settings")]
    [Tooltip("需要隐藏的单位 MeshRenderer 组件，可在 Inspector 中拖拽指定")]
    public MeshRenderer unitMeshRenderer;
    
    private Unit _assignedUnit;
    private TaskBuilding _targetBuilding;
    private string _taskName;
    
    private int _currentAttempt = 0;
    private float _nextCheckTime;
    private bool _isProcessing = false;
    
    // 事件
    public Action<TaskResult> OnTaskCompleted;
    
    /// <summary>
    /// 任务结果枚举
    /// </summary>
    public enum TaskResult
    {
        Success,    // 任务成功
        Failed      // 任务失败
    }
    
    /// <summary>
    /// 开始处理任务
    /// </summary>
    public void StartTask(Unit unit, TaskBuilding building, string taskName)
    {
        _assignedUnit = unit;
        _targetBuilding = building;
        _taskName = taskName;
        _currentAttempt = 0;
        _isProcessing = true;
        _nextCheckTime = Time.time + CheckInterval;
        
        // 隐藏单位
        HideUnit();
        
        Debug.Log($"[TaskProcessor] ===== 开始任务 =====");
        Debug.Log($"[TaskProcessor] 单位: {_assignedUnit.Name}");
        Debug.Log($"[TaskProcessor] 建筑: {_targetBuilding.Name}");
        Debug.Log($"[TaskProcessor] 任务: {_taskName}");
        Debug.Log($"[TaskProcessor] 检测间隔: {CheckInterval}秒");
        Debug.Log($"[TaskProcessor] 成功率: {SuccessRate * 100}%");
        Debug.Log($"[TaskProcessor] 最大尝试次数: {MaxAttempts}");
    }
    
    private void Update()
    {
        if (!_isProcessing) return;
        
        // 检查是否到达检测时间
        if (Time.time >= _nextCheckTime)
        {
            PerformCheck();
        }
    }
    
    /// <summary>
    /// 执行一次检测
    /// </summary>
    private void PerformCheck()
    {
        _currentAttempt++;
        
        Debug.Log($"[TaskProcessor] ===== 第 {_currentAttempt}/{MaxAttempts} 次检测 =====");
        
        // 生成随机数判断是否成功
        float randomValue = UnityEngine.Random.value; // 0-1之间的随机数
        bool isSuccess = randomValue < SuccessRate;
        
        Debug.Log($"[TaskProcessor] 随机值: {randomValue:F2}, 阈值: {SuccessRate:F2}, 结果: {(isSuccess ? "成功" : "失败")}");
        
        if (isSuccess)
        {
            // 任务成功
            CompleteTask(TaskResult.Success);
        }
        else if (_currentAttempt >= MaxAttempts)
        {
            // 达到最大尝试次数，任务失败
            Debug.LogWarning($"[TaskProcessor] 已达到最大尝试次数 ({MaxAttempts})，任务失败！");
            CompleteTask(TaskResult.Failed);
        }
        else
        {
            // 继续下一次检测
            _nextCheckTime = Time.time + CheckInterval;
            Debug.Log($"[TaskProcessor] 将在 {_nextCheckTime - Time.time:F1} 秒后进行下一次检测");
        }
    }
    
    /// <summary>
    /// 完成任务
    /// </summary>
    private void CompleteTask(TaskResult result)
    {
        _isProcessing = false;
        
        Debug.Log($"[TaskProcessor] ===== 任务结束 =====");
        Debug.Log($"[TaskProcessor] 结果: {(result == TaskResult.Success ? "成功" : "失败")}");
        Debug.Log($"[TaskProcessor] 总尝试次数: {_currentAttempt}");
        
        // 触发完成事件
        OnTaskCompleted?.Invoke(result);
        
        // 恢复单位状态
        RestoreUnit();
        
        // 向-Y轴方向移动10个单位
        MoveUnitDownward();
        
        // 销毁此处理器
        Destroy(gameObject, 0.1f);
    }
    
    /// <summary>
    /// 隐藏单位（关闭 MeshRenderer）
    /// </summary>
    private void HideUnit()
    {
        if (_assignedUnit != null && _assignedUnit.gameObject != null)
        {
            // 隐藏 MeshRenderer
            if (unitMeshRenderer != null)
            {
                unitMeshRenderer.enabled = false;
                Debug.Log($"[TaskProcessor] 单位 {_assignedUnit.Name} 的 MeshRenderer 已关闭");
            }
            else
            {
                // 如果没有手动指定，则自动查找所有子对象的 MeshRenderer
                MeshRenderer[] meshRenderers = _assignedUnit.GetComponentsInChildren<MeshRenderer>();
                foreach (var renderer in meshRenderers)
                {
                    renderer.enabled = false;
                }
                Debug.Log($"[TaskProcessor] 单位 {_assignedUnit.Name} 的所有 MeshRenderer 已关闭");
            }
            
            // 隐藏 SelectionIndicator
            SelectionIndicator indicator = _assignedUnit.GetComponent<SelectionIndicator>();
            if (indicator != null)
            {
                indicator.Hide();
                Debug.Log($"[TaskProcessor] 单位 {_assignedUnit.Name} 的 SelectionIndicator 已隐藏");
            }
            
            Debug.Log($"[TaskProcessor] 单位 {_assignedUnit.Name} 已隐藏，开始执行任务");
        }
    }
    
    /// <summary>
    /// 恢复单位（使其重新可分配）
    /// </summary>
    private void RestoreUnit()
    {
        if (_assignedUnit != null && _assignedUnit.gameObject != null)
        {
            // 恢复 MeshRenderer
            if (unitMeshRenderer != null)
            {
                unitMeshRenderer.enabled = true;
            }
            else
            {
                // 如果没有手动指定，则自动查找所有子对象的 MeshRenderer
                MeshRenderer[] meshRenderers = _assignedUnit.GetComponentsInChildren<MeshRenderer>();
                foreach (var renderer in meshRenderers)
                {
                    renderer.enabled = true;
                }
            }
            
            // 恢复 SelectionIndicator（如果需要显示选中状态）
            SelectionIndicator indicator = _assignedUnit.GetComponent<SelectionIndicator>();
            if (indicator != null)
            {
                // 检查单位是否被选中，如果被选中则显示指示器
                bool isSelected = SelectionManager.Instance.SelectedUnits.Contains(_assignedUnit);
                indicator.Show(isSelected);
                Debug.Log($"[TaskProcessor] 单位 {_assignedUnit.Name} 的 SelectionIndicator 已恢复，选中状态: {isSelected}");
            }
            
            // 重置分配时间，使单位立即可被再次分配
            _assignedUnit.LastAssignedTime = Time.time - _assignedUnit.AssignmentCooldown - 1f;
            
            // 重置单位状态
            _assignedUnit.SetState(UnitState.Idle);
            
            // 清除目标建筑
            if (_assignedUnit is CombatUnit combatUnit)
            {
                combatUnit.ClearTargetBuilding();
            }
            
            Debug.Log($"[TaskProcessor] 单位 {_assignedUnit.Name} 已恢复，现在是可分配状态");
        }
    }
    
    /// <summary>
    /// 向-Y轴方向移动单位10个单位
    /// </summary>
    private void MoveUnitDownward()
    {
        if (_assignedUnit != null && _assignedUnit.gameObject != null)
        {
            // 计算新位置：当前位置向-Y轴移动10个单位
            Vector3 newPosition = _assignedUnit.transform.position + new Vector3(0, -50f, 0);
            
            // 直接设置位置
            _assignedUnit.transform.position = newPosition;
            
            // 更新 Unit 的位置属性
            _assignedUnit.Position = newPosition;
            
            Debug.Log($"[TaskProcessor] 单位 {_assignedUnit.Name} 向-Y轴移动10个单位，新位置: {newPosition}");
        }
    }
    
    /// <summary>
    /// 强制取消任务
    /// </summary>
    public void CancelTask()
    {
        if (_isProcessing)
        {
            Debug.Log($"[TaskProcessor] 任务被强制取消");
            _isProcessing = false;
            RestoreUnit();
            Destroy(gameObject, 0.1f);
        }
    }
    
    private void OnDestroy()
    {
        Debug.Log($"[TaskProcessor] 任务处理器已销毁");
    }
}