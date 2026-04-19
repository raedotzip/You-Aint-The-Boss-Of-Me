using UnityEngine;

public class Boss1TiredState : EnemyBaseState
{
    private float tiredDuration = 3f;
    private float timer         = 0f;
    private bool  hasBeenHit    = false;

    public override void EnterState(EnemyStateManager state)
    {
        timer      = 0f;
        hasBeenHit = false;

        Boss1StateManager boss = (Boss1StateManager)state;
        tiredDuration = boss.IsEnraged ? boss.tiredDurationEnraged : boss.tiredDuration;

        state.animator.SetBool("Tired", true);
    }

    public override void UpdateState(EnemyStateManager state)
    {
        timer += Time.deltaTime;

        SnapToGround(state);

        if (timer >= tiredDuration)
        {
            Boss1StateManager boss = (Boss1StateManager)state;
            boss.attackCounter = 0;
            boss.TransitionToNextState();
        }
    }

    // Raycasts down from above the boss and pins its Y to the floor so the
    // animation never floats above the ground.
    private void SnapToGround(EnemyStateManager state)
    {
        Vector3 origin = state.transform.position + Vector3.up * 2f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 4f,
                            ((Boss1StateManager)state).wallLayer, QueryTriggerInteraction.Ignore))
        {
            Vector3 pos = state.transform.position;
            pos.y = hit.point.y;
            state.transform.position = pos;
        }
    }

    public override float OnBossHurt(EnemyStateManager state)
    {
        hasBeenHit = true;

        // Boss takes full damage during tired state
        return 20f;
    }
}