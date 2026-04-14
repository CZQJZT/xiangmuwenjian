using UnityEngine;

public class CombatUnit : Unit
{
    [Header("Combat Settings")]
    public int AttackRange;
    public int AttackDamage;
    public float AttackCooldown;
    public float CurrentCooldown;
    
    [Header("Work Settings")]
    public int CarryCapacity;
    public Building CurrentBuilding;

    private Building _targetBuilding;

    public override void GameUpdate(float deltaTime)
    {
        if (CurrentCooldown > 0)
        {
            CurrentCooldown -= deltaTime;
        }

        if (CurrentState == UnitState.Combat || CurrentState == UnitState.Attacking)
        {
            Unit target = TargetManager.Instance?.GetNearestEnemy(this);
            if (target != null)
            {
                float distance = Vector3.Distance(Position, target.Position);
                if (distance <= AttackRange)
                {
                    Attack(target);
                }
            }
        }
        
        if (CurrentState == UnitState.Working && CurrentBuilding != null)
        {
            // 执行工作逻辑
        }
    }

    public void Attack(Unit target)
    {
        if (CurrentCooldown <= 0 && target != null)
        {
            CombatManager.Instance?.ProcessAttack(this, target);
            CurrentCooldown = AttackCooldown;
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