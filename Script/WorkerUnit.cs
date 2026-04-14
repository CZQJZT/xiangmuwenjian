using UnityEngine;

public class WorkerUnit : Unit
{
    public int CarryCapacity { get; set; }
    public Building CurrentBuilding { get; set; }

    private Building _targetBuilding;

    // 重写 GameUpdate 而不是 Update
    public override void GameUpdate(float deltaTime)
    {
        if (CurrentState == UnitState.Working && CurrentBuilding != null)
        {
            // 执行工作逻辑
        }
    }

    public void SetTargetBuilding(Building building)
    {
        _targetBuilding = building;
    }

    public Building GetTargetBuilding()
    {
        return _targetBuilding;
    }

    public void ClearTargetBuilding()
    {
        _targetBuilding = null;
    }
}