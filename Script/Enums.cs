using UnityEngine;

public enum Team
{
    Player,
    Enemy,
    Neutral
}

// 修复：添加 Combat 状态
public enum UnitState
{
    Idle,
    Moving,
    Attacking,
    Combat,      // 新增
    Working,
    Dead
}

public enum BuildingState
{
    Constructing,
    Active,
    Destroyed
}

public enum UnitType
{
    Worker,
    Soldier,
    Archer
}