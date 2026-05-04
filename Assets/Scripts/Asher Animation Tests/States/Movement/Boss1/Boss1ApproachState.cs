using UnityEngine;

public class Boss1ApproachState : EnemyBaseState
{
    private float approachSpeed  = 5f;
    private float stopDistance   = 6f;   // Stop when within close range
    private float maxDuration    = 3f;   // Safety timeout so boss always eventually attacks
    private float timer          = 0f;

    public override void EnterState(EnemyStateManager state)
    {
        timer = 0f;
        state.animator.SetBool("Running", true);
        ((Boss1StateManager)state).smoothLookAtEnabled = false;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        timer += Time.deltaTime;

        Vector3 toPlayer = state.player.position - state.transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        // Face the player
        if (toPlayer.sqrMagnitude > 0.001f)
            state.transform.rotation = Quaternion.LookRotation(toPlayer.normalized);

        if (dist <= stopDistance || timer >= maxDuration)
        {
            state.animator.SetBool("Running", false);
            Boss1StateManager boss = (Boss1StateManager)state;
            boss.smoothLookAtEnabled = true;
            boss.TransitionToNextState();
            return;
        }

        Vector3 step = toPlayer.normalized * approachSpeed * Time.deltaTime;

        // Don't walk into walls or the lava pit
        Boss1StateManager b = (Boss1StateManager)state;
        Vector3 nextPos = state.transform.position + step;
        if (!b.WouldHitWall(state.transform.position, toPlayer.normalized, step.magnitude)
            && b.IsPositionSafe(nextPos))
        {
            if (state.rb != null)
                state.rb.MovePosition(nextPos);
            else
                state.transform.position = nextPos;
        }
        else
        {
            // Can't walk there — just attack from here
            state.animator.SetBool("Running", false);
            b.smoothLookAtEnabled = true;
            b.TransitionToNextState();
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;
}
