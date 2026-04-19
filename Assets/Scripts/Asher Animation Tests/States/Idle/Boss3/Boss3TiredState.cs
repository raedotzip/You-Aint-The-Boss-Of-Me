using UnityEngine;

public class Boss3TiredState : EnemyBaseState
{
    private float _timer;

    public override void EnterState(EnemyStateManager state)
    {
        _timer = 0f;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        _timer += Time.deltaTime;
        Boss3StateManager b3 = (Boss3StateManager)state;
        if (_timer >= b3.tiredDuration)
            b3.TransitionToNextState();
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;
}
