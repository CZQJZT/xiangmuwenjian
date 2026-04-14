using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackComponent
{
    public void Execute(CombatUnit attacker, Unit target)
    {
        attacker.Attack(target);
    }
}