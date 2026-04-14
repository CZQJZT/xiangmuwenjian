using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class UIManager : MonoBehaviour
{
    private static UIManager _instance;
    
    // 修复：使用 FindObjectOfType 而不是 new
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManager>();
                
                // 如果场景中不存在，创建一个新的 GameObject
                if (_instance == null)
                {
                    GameObject managerObj = new GameObject("UIManager");
                    _instance = managerObj.AddComponent<UIManager>();
                }
            }
            return _instance;
        }
    }

    [Header("UI References")]
    public HUDPanel HUD;
    public UnitSelectionPanel SelectionPanel;
    public BuildMenuPanel BuildMenu;
    public UnitTrainingPanel TrainingPanel;
    public UnitListPanel UnitListPanel;

    private void Awake()
    {
        // 确保单例唯一性
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void OpenBuildMenu(Team team)
    {
        BuildMenu?.Show(GetAvailableBuildings(team));
    }
    
    public void OpenTaskPanel(TaskBuilding building)
    {
        if (building != null)
        {
            building.OpenTaskPanel();
        }
    }
    
    public void OpenTrainingPanel(MilitaryBuilding building)
    {
        if (building != null && TrainingPanel != null)
        {
            TrainingPanel.Show(building);
        }
    }

    public void ShowUnitInfo(Unit unit)
    {
        SelectionPanel?.UpdateInfo(unit);
    }

    public void CloseTrainingPanel()
    {
        TrainingPanel?.Hide();
    }
    
    public void OpenUnitListPanel()
    {
        UnitListPanel?.Show();
    }
    
    public void CloseUnitListPanel()
    {
        UnitListPanel?.Hide();
    }

    public void UpdateCommandButtons(List<Unit> selectedUnits)
    {
        if (selectedUnits == null || selectedUnits.Count == 0)
        {
            HideCommandButtons();
            return;
        }

        ShowCommandButtons();
        
        bool hasCombatUnit = selectedUnits.Any(u => u is CombatUnit);

        UpdateAttackButton(hasCombatUnit);
        UpdateMoveButton(true);
        UpdateWorkButton(hasCombatUnit);
    }

    private List<Building> GetAvailableBuildings(Team team)
    {
        return new List<Building>();
    }

    private void ShowCommandButtons() { }
    private void HideCommandButtons() { }
    private void UpdateAttackButton(bool visible) { }
    private void UpdateMoveButton(bool visible) { }
    private void UpdateWorkButton(bool visible) { }
}