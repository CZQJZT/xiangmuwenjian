using UnityEngine;
using System;

[Serializable]
public class UnitTrainingConfig
{
    [Header("Basic Info")]
    public int ID;
    public string Name;
    public Sprite Icon;
    
    [Header("Prefab")]
    public GameObject Prefab;
    
    [Header("Cost & Time")]
    public int Cost;
    public float TrainingTime;
    
    [Header("Description")]
    [TextArea(3, 5)]
    public string Description;
    
    [Header("Type")]
    public UnitType UnitType;
    
    [Header("Name Pool")]
    public UnitNamePool NamePool = new UnitNamePool();
}