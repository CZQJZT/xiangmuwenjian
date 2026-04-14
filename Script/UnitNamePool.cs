using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单位命名池 - 为生成的单位提供随机命名
/// </summary>
[Serializable]
public class UnitNamePool
{
    [Header("Name Pool")]
    public List<string> Names = new List<string>();
    
    /// <summary>
    /// 获取一个随机名称
    /// </summary>
    public string GetRandomName()
    {
        if (Names == null || Names.Count == 0)
        {
            return null;
        }
        
        int index = UnityEngine.Random.Range(0, Names.Count);
        return Names[index];
    }
    
    /// <summary>
    /// 添加名称到池中
    /// </summary>
    public void AddName(string name)
    {
        if (!string.IsNullOrEmpty(name) && !Names.Contains(name))
        {
            Names.Add(name);
        }
    }
}