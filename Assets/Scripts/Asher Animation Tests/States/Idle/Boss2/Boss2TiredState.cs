using UnityEngine;

public class Boss2TiredState : EnemyBaseState
{
    private float _timer;
    private bool  _hasBeenHit;

    public override void EnterState(EnemyStateManager state)
    {
        _timer      = 0f;
        _hasBeenHit = false;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        Boss2StateManager boss = (Boss2StateManager)state;
        _timer += Time.deltaTime;

        if (_timer >= boss.tiredDuration || _hasBeenHit)
        {
            boss.attackCounter = 0;
            boss.TransitionToNextState();
        }
    }

    public override float OnBossHurt(EnemyStateManager state)
    {
        _hasBeenHit = true;
        return 20f;
    }
}
