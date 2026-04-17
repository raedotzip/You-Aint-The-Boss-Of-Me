using UnityEngine;

public class Boss1JumpSlamAttack : EnemyBaseState
{
    private Vector3 startPosition;
    private Vector3 targetPosition;

    private float journeyTime = 1.2f;
    private float jumpHeight  = 5f;
    private float elapsedTime = 0f;
    private bool  hasLanded   = false;

    public override void EnterState(EnemyStateManager state)
    {
        startPosition = state.transform.position;

        // Land slightly short of the player so boss and player don't overlap
        Vector3 toPlayer = (state.player.position - state.transform.position).normalized;
        targetPosition   = state.player.position - toPlayer * 2f;
        targetPosition.y = 0f;

        elapsedTime = 0f;
        hasLanded   = false;
        state.animator.SetTrigger("JumpAttack");
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (hasLanded)
            return;

        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / journeyTime);

        Vector3 flatPosition  = Vector3.Lerp(startPosition, targetPosition, t);
        flatPosition.y       += jumpHeight * 4 * t * (1 - t);
        state.transform.position = flatPosition;

        if (t >= 1f)
        {
            hasLanded = true;
            state.transform.position = targetPosition;
            state.animator.SetTrigger("GroundSlam");
            Boss1StateManager boss = (Boss1StateManager)state;
            boss.TransitionToNextState();
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0;
}