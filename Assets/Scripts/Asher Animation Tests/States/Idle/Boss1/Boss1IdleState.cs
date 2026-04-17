using UnityEngine;

public class Boss1IdleState : EnemyBaseState
{
    private float idleDuration = 0f;
    private float idleTimer    = 0f;
    private float timer         = 0f;

    public override void EnterState(EnemyStateManager state)
    {
        idleTimer = 0f;

        // Pick a random idle duration between 3 and 5 seconds
        idleDuration = Random.Range(0.5f, 1.0f);

        // state.animator.SetTrigger("Idle");
    }

    public override void UpdateState(EnemyStateManager state)
    {
        idleTimer += Time.deltaTime;

        if (idleTimer >= idleDuration)
        {
            Boss1StateManager boss = (Boss1StateManager)state;

            // Reset attack counter so idle never leads directly into tired
            boss.attackCounter = 0;
            boss.TransitionToNextState();
        }
    }

    public override float OnBossHurt(EnemyStateManager state)
    {
        // Boss takes full damage while idle — reward the player for aggression
        return 20f;
    }
}