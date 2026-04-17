/**
 * Throw at the ceiling to cause things to fall
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss1ThrowAtCeilingAttack : EnemyBaseState
{
    public override void EnterState(EnemyStateManager state)
    {

    }

    public override void UpdateState(EnemyStateManager state)
    {
        
    }

    public override float OnBossHurt(EnemyStateManager state)
    {
        return 0;
    }
}