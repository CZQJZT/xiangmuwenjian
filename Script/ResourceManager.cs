using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager
{
    private static ResourceManager _instance;
    public static ResourceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new ResourceManager();
            }
            return _instance;
        }
    }

    public Dictionary<Team, int> TeamResources { get; set; } = new Dictionary<Team, int>();

    public void AddResource(Team team, int amount)
    {
        if (TeamResources.ContainsKey(team))
        {
            TeamResources[team] += amount;
        }
        else
        {
            TeamResources[team] = amount;
        }
    }
    
    public int GetResource(Team team)
    {
        if (TeamResources.ContainsKey(team))
        {
            return TeamResources[team];
        }
        return 0;
    }
    
    public bool HasEnough(Team team, int amount)
    {
        return GetResource(team) >= amount;
    }
}