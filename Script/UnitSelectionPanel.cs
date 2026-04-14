using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
public class UnitSelectionPanel : MonoBehaviour
{
    public Image UnitIcon { get; set; }
    public TMP_Text UnitStats { get; set; }

    public void UpdateInfo(Unit unit)
    {
        if (unit != null && UnitStats != null)
        {
            UnitStats.text = $"<b>{unit.Name}</b>\nHP: {unit.CurrentHealth}/{unit.MaxHealth}";
        }
    }
}