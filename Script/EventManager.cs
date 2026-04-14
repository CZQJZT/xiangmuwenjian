using System;
using UnityEngine;
public class EventManager
{
    private static EventManager _instance;
    public static EventManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new EventManager();
            }
            return _instance;
        }
    }

    public Action<Unit> OnUnitKilled;
    public Action<Unit, Building> OnUnitAssigned;
    public Action<Unit> OnUnitManuallySelected;

    public void TriggerEvent(string eventName, Unit unit)
    {
        if (eventName == "UnitKilled")
        {
            OnUnitKilled?.Invoke(unit);
        }
    }
}