using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager
{
    private static CombatManager _instance;
    public static CombatManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new CombatManager();
            }
            return _instance;
        }
    }

    public int CalculateDamage(Unit attacker, Unit defender)
    {
        if (attacker is CombatUnit combatUnit)
        {
            // 伤害计算逻辑
            return combatUnit.AttackDamage;
        }
        return 0;
    }

    public void ProcessAttack(Unit attacker, Unit defender)
    {
        int damage = CalculateDamage(attacker, defender);
        defender.TakeDamage(damage);
    }
}