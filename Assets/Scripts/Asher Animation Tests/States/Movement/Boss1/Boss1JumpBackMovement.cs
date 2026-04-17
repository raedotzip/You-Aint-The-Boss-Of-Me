using UnityEngine;

public class Boss1JumpBackMovement : EnemyBaseState
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

        // Calculate away direction using the player's position relative to boss
        // Since boss just landed near the player, use a fallback if they're too close
        Vector3 toPlayer = state.player.position - state.transform.position;
        toPlayer.y       = 0f;

        Vector3 awayDir;
        if (toPlayer.sqrMagnitude > 0.01f)
        {
            awayDir = -toPlayer.normalized;
        }
        else
        {
            // Absolute fallback — jump in the direction the boss is facing
            awayDir = -state.transform.forward;
            Debug.LogWarning("JumpBack fallback direction used — boss and player too close");
        }

        targetPosition  = startPosition + awayDir * jumpDistance;
        targetPosition.y = 0f;
        elapsedTime     = 0f;
        hasTransitioned = false;
        state.animator.SetTrigger("Jumping");

        Debug.Log($"JumpBack — start: {startPosition}, target: {targetPosition}, awayDir: {awayDir}");

        // Face the player while jumping back
        if (toPlayer != Vector3.zero)
        {
            toPlayer.y = 0f;
            state.transform.rotation = Quaternion.LookRotation(toPlayer);
        }
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
            boss.SwitchState(boss.punchAttack);
        }
    }

    public override float OnBossHurt(EnemyStateManager state)
    {
        return 0;
    }
}