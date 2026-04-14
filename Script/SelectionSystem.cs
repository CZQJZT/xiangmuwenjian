using UnityEngine;
using System.Collections.Generic;

public class SelectionSystem : MonoBehaviour
{
    private static SelectionSystem _instance;
    
    // 修复：使用 FindObjectOfType
    public static SelectionSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SelectionSystem>();
            }
            return _instance;
        }
    }

    [Header("Camera Settings")]
    public Camera mainCamera;

    private InputHandler _inputHandler;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        _inputHandler = gameObject.AddComponent<InputHandler>();
        
        SelectionManager.Instance.OnUnitSelected += HandleUnitSelected;
        SelectionManager.Instance.OnMultiUnitsSelected += HandleMultiUnitsSelected;
        SelectionManager.Instance.OnSelectionCleared += HandleSelectionCleared;
    }

    private void OnDestroy()
    {
        SelectionManager.Instance.OnUnitSelected -= HandleUnitSelected;
        SelectionManager.Instance.OnMultiUnitsSelected -= HandleMultiUnitsSelected;
        SelectionManager.Instance.OnSelectionCleared -= HandleSelectionCleared;
    }

    private void HandleUnitSelected(Unit unit)
    {
        EventManager.Instance?.TriggerEvent("UnitSelected", unit);
        HighlightUnit(unit, true);
    }

    private void HandleMultiUnitsSelected(List<Unit> units)
    {
        EventManager.Instance?.TriggerEvent("MultiUnitsSelected", units.Count > 0 ? units[0] : null);
        foreach (var unit in units)
        {
            HighlightUnit(unit, true);
        }
    }

    private void HandleSelectionCleared() { }

    private void HighlightUnit(Unit unit, bool highlight)
    {
        SelectionIndicator indicator = unit.GetComponent<SelectionIndicator>();
        if (indicator != null)
        {
            indicator.Show(highlight);
        }
    }
}