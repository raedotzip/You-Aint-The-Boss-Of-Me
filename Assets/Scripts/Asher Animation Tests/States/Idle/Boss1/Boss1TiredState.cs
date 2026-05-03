using UnityEngine;

public class Boss1TiredState : EnemyBaseState
{
    private float tiredDuration  = 3f;
    private float getUpDelay     = 1.0f; // stand-up animation window before next attack
    private float timer          = 0f;
    private float getUpTimer     = 0f;
    private Vector3 tiredStartPos;
    private Vector3 tiredDownPos;
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
        tiredStartPos = boss.transform.position;
        tiredDownPos = new Vector3(tiredStartPos.x, tiredStartPos.y - 0.5f, tiredStartPos.z);
        boss.smoothLookAtEnabled = false;

    state.animator.SetBool("Tired", true);
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (!gettingUp)
        {
            timer += Time.deltaTime;
            //SnapToGround(state);
            if (timer <= 0.5)
            {

                Vector3 tiredTransitionPos = Vector3.Lerp(tiredStartPos, tiredDownPos, timer / 0.5f);
                state.transform.position = tiredTransitionPos;
            }

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
            //SnapToGround(state);
            state.transform.position = Vector3.Lerp(tiredDownPos, tiredStartPos, getUpTimer / getUpDelay);
            
            if (getUpTimer >= getUpDelay)
            {
                Boss1StateManager boss = (Boss1StateManager)state;
                boss.smoothLookAtEnabled = true;
                boss.attackCounter = 0;
                boss.transform.position = tiredStartPos;
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
            // Added a -0.5 here just as a bit of a janky way to make him look like he rests on the ground.
            pos.y = hit.point.y - 0.5f;
            state.transform.position = pos;
        }
    }

    public override float OnBossHurt(EnemyStateManager state)
    {
        hasBeenHit = true;
        return 20f;
    }
}