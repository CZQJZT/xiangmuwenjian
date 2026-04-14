using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitManager
{
    private static UnitManager _instance;
    public static UnitManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new UnitManager();
            }
            return _instance;
        }
    }

    public List<Unit> AllUnits { get; set; } = new List<Unit>();

    public List<Unit> GetUnitsInRange(Vector3 position, float range)
    {
        var result = new List<Unit>();
        foreach (var unit in AllUnits)
        {
            if (Vector3.Distance(position, unit.Position) <= range)
            {
                result.Add(unit);
            }
        }
        return result;
    }

    public void AddUnit(Unit unit)
    {
        AllUnits.Add(unit);
    }

    public void RemoveUnit(Unit unit)
    {
        AllUnits.Remove(unit);
    }
}