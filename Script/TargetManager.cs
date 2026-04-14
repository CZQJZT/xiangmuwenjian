using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetManager
{
    private static TargetManager _instance;
    public static TargetManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new TargetManager();
            }
            return _instance;
        }
    }

    public Unit GetNearestEnemy(Unit source)
    {
        var units = UnitManager.Instance?.AllUnits;
        Unit nearestEnemy = null;
        float minDistance = float.MaxValue;

        if (units != null)
        {
            foreach (var unit in units)
            {
                if (unit.Team != source.Team && unit.CurrentState != UnitState.Dead)
                {
                    float distance = Vector3.Distance(source.Position, unit.Position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestEnemy = unit;
                    }
                }
            }
        }

        return nearestEnemy;
    }
}