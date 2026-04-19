using UnityEngine;

public class Boss1TiredState : EnemyBaseState
{
    private float tiredDuration  = 3f;
    private float getUpDelay     = 1.0f; // stand-up animation window before next attack
    private float timer          = 0f;
    private float getUpTimer     = 0f;
    private bool  hasBeenHit     = false;
    private bool  gettingUp      = false;

    public override void EnterState(EnemyStateManager state)
    {
        timer      = 0f;
        getUpTimer = 0f;
        hasBeenHit = false;
        gettingUp  = false;

        Boss1StateManager boss = (Boss1StateManager)state;
        tiredDuration = boss.IsEnraged ? boss.tiredDurationEnraged : boss.tiredDuration;

        state.animator.SetBool("Tired", true);
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (!gettingUp)
        {
            timer += Time.deltaTime;
            SnapToGround(state);

            if (timer >= tiredDuration)
            {
                gettingUp = true;
                getUpTimer = 0f;
                // Clear tired so the stand-up animation plays
                state.animator.SetBool("Tired", false);
            }
        }
        else
        {
            getUpTimer += Time.deltaTime;
            SnapToGround(state);
            if (getUpTimer >= getUpDelay)
            {
                Boss1StateManager boss = (Boss1StateManager)state;
                boss.attackCounter = 0;
                boss.TransitionToNextState();
            }
        }
    }

    private void SnapToGround(EnemyStateManager state)
    {
        int mask = ~(1 << state.gameObject.layer);
        Vector3 origin = state.transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 3f,
                            mask, QueryTriggerInteraction.Ignore))
        {
            Vector3 pos = state.transform.position;
            pos.y = hit.point.y;
            state.transform.position = pos;
        }
    }

    public override float OnBossHurt(EnemyStateManager state)
    {
        hasBeenHit = true;
        return 20f;
    }
}