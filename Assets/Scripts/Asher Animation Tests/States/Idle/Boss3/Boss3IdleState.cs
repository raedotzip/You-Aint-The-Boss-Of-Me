using UnityEngine;

public class Boss3IdleState : EnemyBaseState
{
    private float _timer;
    private float _delay = 1.2f;

    public override void EnterState(EnemyStateManager state)
    {
        _timer = 0f;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        _timer += Time.deltaTime;
        if (_timer >= _delay)
            ((Boss3StateManager)state).TransitionToNextState();
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;
}
