using UnityEngine;
using System.Collections.Generic;
using System;

public class SelectionManager
{
    private static SelectionManager _instance;
    public static SelectionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new SelectionManager();
            }
            return _instance;
        }
    }

    public List<Unit> SelectedUnits { get; private set; } = new List<Unit>();
    public int MaxSelectionCount { get; set; } = 10;

    public Action<Unit> OnUnitSelected;
    public Action<List<Unit>> OnMultiUnitsSelected;
    public Action OnSelectionCleared;

     public void SelectUnit(Unit unit)
    {
        if (unit == null) return;

        ClearAllHighlights();
        ClearSelection();
        
        SelectedUnits.Add(unit);
        
        HighlightUnit(unit, true);
        
        OnUnitSelected?.Invoke(unit);
        UIManager.Instance?.ShowUnitInfo(unit);
        
        EventManager.Instance?.OnUnitManuallySelected?.Invoke(unit);
    }
    public void SelectUnits(List<Unit> units)
    {
        if (units == null || units.Count == 0)
        {
            ClearAllHighlights();
            ClearSelection();
            return;
        }

        // 修复：先清除所有之前的选择高亮
        ClearAllHighlights();
        ClearSelection();
        
        int count = Mathf.Min(units.Count, MaxSelectionCount);
        for (int i = 0; i < count; i++)
        {
            SelectedUnits.Add(units[i]);
            // 显示每个选中单位的高亮
            HighlightUnit(units[i], true);
        }

        OnMultiUnitsSelected?.Invoke(SelectedUnits);
        UIManager.Instance?.UpdateCommandButtons(SelectedUnits);
    }

    public void AddToSelection(Unit unit)
    {
        if (unit == null || SelectedUnits.Contains(unit)) return;
        if (SelectedUnits.Count >= MaxSelectionCount) return;

        SelectedUnits.Add(unit);
        HighlightUnit(unit, true);
        OnMultiUnitsSelected?.Invoke(SelectedUnits);
    }

    public void ClearSelection()
    {
        SelectedUnits.Clear();
        OnSelectionCleared?.Invoke();
    }

    // 修复：新增方法，清除所有单位的高亮
    private void ClearAllHighlights()
    {
        var allUnits = UnitManager.Instance?.AllUnits;
        if (allUnits != null)
        {
            foreach (var unit in allUnits)
            {
                HighlightUnit(unit, false);
            }
        }
    }

    public void RemoveFromSelection(Unit unit)
    {
        if (SelectedUnits.Remove(unit))
        {
            HighlightUnit(unit, false);
            
            if (SelectedUnits.Count == 0)
            {
                OnSelectionCleared?.Invoke();
            }
            else
            {
                OnMultiUnitsSelected?.Invoke(SelectedUnits);
            }
        }
    }

    public Unit GetPrimarySelectedUnit()
    {
        return SelectedUnits.Count > 0 ? SelectedUnits[0] : null;
    }

    // 修复：高亮单位方法
    private void HighlightUnit(Unit unit, bool highlight)
    {
        if (unit == null) return;
        
        SelectionIndicator indicator = unit.GetComponent<SelectionIndicator>();
        if (indicator != null)
        {
            if (highlight)
            {
                indicator.Show(true);
            }
            else
            {
                indicator.Hide();
            }
        }
    }
}