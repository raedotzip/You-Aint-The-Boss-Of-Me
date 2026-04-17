using UnityEngine;

public class Boss2IdleState : EnemyBaseState
{
    private float _idleDuration;
    private float _idleTimer;

    public override void EnterState(EnemyStateManager state)
    {
        _idleTimer    = 0f;
        _idleDuration = Random.Range(0.5f, 1.0f);
    }

    public override void UpdateState(EnemyStateManager state)
    {
        _idleTimer += Time.deltaTime;

        if (_idleTimer >= _idleDuration)
        {
            Boss2StateManager boss = (Boss2StateManager)state;
            boss.attackCounter = 0;
            boss.TransitionToNextState();
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 20f;
}
