using UnityEngine;

public class Boss1JumpLeftMovement : EnemyBaseState
{
    private Vector3 startPosition;
    private Vector3 targetPosition;

    private float journeyTime = 0.8f;
    private float elapsedTime = 0f;
    private float jumpHeight  = 3f;
    public AnimationCurve jumpCurve;
    public float jumpDistance = 6f;

    private bool hasTransitioned = false;

    public override void EnterState(EnemyStateManager state)
    {
        startPosition = state.transform.position;

        Vector3 toPlayer = (state.player.position - state.transform.position).normalized;

        // Left = negative right relative to boss→player direction
        Vector3 left     = -Vector3.Cross(Vector3.up, toPlayer).normalized;
        targetPosition   = startPosition + left * jumpDistance;
        targetPosition.y = 0f;

        elapsedTime     = 0f;
        hasTransitioned = false;

        // Face player while jumping sideways
        Vector3 lookDir = toPlayer;
        lookDir.y = 0f;
        if (lookDir != Vector3.zero)
            state.transform.rotation = Quaternion.LookRotation(lookDir);
        state.animator.SetTrigger("Jumping");
        Debug.Log("Jumping Right");
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (hasTransitioned)
            return;

        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / journeyTime);

        Vector3 position = Vector3.Lerp(startPosition, targetPosition, t);
        float curveValue = (jumpCurve != null) ? jumpCurve.Evaluate(t) : 4 * t * (1 - t);
        position.y       = Mathf.Lerp(startPosition.y, targetPosition.y, t) + curveValue * jumpHeight;

        state.transform.position = position;

        if (t >= 1f)
        {
            state.transform.position = targetPosition;
            hasTransitioned          = true;

            Boss1StateManager boss = (Boss1StateManager)state;
            float distToPlayer     = Vector3.Distance(state.transform.position, state.player.position);

            if (distToPlayer >= boss.closeRange)
                boss.SwitchState(boss.chargeAttack);
            else
                boss.SwitchState(boss.repeatedBulletSlamState);
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0;
}