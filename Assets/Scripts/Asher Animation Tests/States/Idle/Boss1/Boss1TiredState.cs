using UnityEngine;

public class Boss1TiredState : EnemyBaseState
{
    private float tiredDuration = 3f;
    private float timer         = 0f;
    private bool  hasBeenHit    = false;

    public override void EnterState(EnemyStateManager state)
    {
        timer     = 0f;
        Boss1StateManager boss = (Boss1StateManager)state;
        tiredDuration = boss.tiredDuration;
        hasBeenHit = false;

        // Play tired animation — boss hunches over, vulnerable
        //state.animator.SetTrigger("Tired");

        Debug.Log("Boss is tired — player can attack!");
        state.animator.SetBool("Tired", true);
    }

    public override void UpdateState(EnemyStateManager state)
    {
        timer += Time.deltaTime;

        if (timer >= tiredDuration)
        {
            // Go straight to next attack — skip idle so boss doesn't freeze
            Boss1StateManager boss = (Boss1StateManager)state;
            boss.attackCounter = 0;
            boss.TransitionToNextState();
        }
        Debug.Log(timer);
    }

    public override float OnBossHurt(EnemyStateManager state)
    {
        hasBeenHit = true;

        // Boss takes full damage during tired state
        return 20f;
    }
}