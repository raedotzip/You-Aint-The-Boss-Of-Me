/**
 * Boss lunges forward and punches the player
 */

using UnityEngine;

public class Boss1PunchAttack : EnemyBaseState
{
    // ===============================
    // LUNGE SETTINGS
    // ===============================
    private float lungeSpeed    = 20f;   // Fast lunge toward player
    private float lungeDistance = 4f;    // How far the boss lunges
    private float stopDistance  = 1.5f;  // How close before punch triggers

    // ===============================
    // PUNCH SETTINGS
    // ===============================
    private float punchDamage   = 25f;
    private float punchRange    = 2.5f;  // Hitbox range of the punch
    private float punchDuration = 0.3f;  // How long the punch hitbox is active

    // ===============================
    // RECOVERY SETTINGS
    // ===============================
    private float recoveryTime  = 0.5f;  // How long boss is frozen after punching

    // ===============================
    // RUNTIME
    // ===============================
    private Vector3 startPos;
    private Vector3 targetPos;

    private float   lungeTimer    = 0f;
    private float   punchTimer    = 0f;
    private float   recoveryTimer = 0f;

    private bool    hasLunged     = false;
    private bool    hasPunched    = false;
    private bool    isRecovering  = false;
    private bool    attackDone    = false;

    private bool    punchActive   = false;

    public override void EnterState(EnemyStateManager state)
    {
        startPos      = state.transform.position;
        lungeTimer    = 0f;
        punchTimer    = 0f;
        recoveryTimer = 0f;
        hasLunged     = false;
        hasPunched    = false;
        isRecovering  = false;
        attackDone    = false;
        punchActive   = false;

        ((Boss1StateManager)state).smoothLookAtEnabled = false;

        // Face the player immediately
        FacePlayer(state);

        // Pre-calculate lunge target — stop just in front of player
        Vector3 toPlayer = (state.player.position - state.transform.position).normalized;
        targetPos        = state.player.position - toPlayer * stopDistance;
        targetPos.y      = 0f;

        //state.animator.SetTrigger("Punch");
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (attackDone)
            return;

        if (!hasLunged)
        {
            Lunge(state);
            return;
        }

        if (punchActive)
        {
            PunchUpdate(state);
            return;
        }

        if (isRecovering)
        {
            recoveryTimer += Time.deltaTime;
            if (recoveryTimer >= recoveryTime)
            {
                attackDone = true;
                Boss1StateManager boss = (Boss1StateManager)state;
                boss.smoothLookAtEnabled = true;
                boss.TransitionToNextState();
            }
        }
    }

    public override float OnBossHurt(EnemyStateManager state)
    {
        // Boss can be hit during recovery — takes extra damage as punishment
        if (isRecovering)
            return 15f;

        return 0f;
    }

    // ===============================
    // LUNGE
    // ===============================
    private void Lunge(EnemyStateManager state)
    {
        FacePlayer(state);

        Vector3 toTarget  = targetPos - state.transform.position;
        toTarget.y        = 0f;
        float distToTarget = toTarget.magnitude;

        // Move toward target
        float step = lungeSpeed * Time.deltaTime;
        Vector3 move = toTarget.normalized * Mathf.Min(step, distToTarget);
        if (state.rb != null)
            state.rb.MovePosition(state.transform.position + move);
        else
            state.transform.position += move;

        lungeTimer += Time.deltaTime;

        // Trigger punch when close enough
        if (distToTarget <= 0.1f || lungeTimer > 1f)
        {
            hasLunged   = true;
            punchActive = true;
            punchTimer  = 0f;
        }
    }

    // ===============================
    // PUNCH HITBOX
    // ===============================
    private void PunchUpdate(EnemyStateManager state)
    {
        punchTimer += Time.deltaTime;

        if (!hasPunched)
        {
            // Check if player is within punch range
            float distToPlayer = Vector3.Distance(
                state.transform.position,
                state.player.position
            );

            if (distToPlayer <= punchRange)
            {
                HitPlayer(state);
                hasPunched = true;
            }
        }

        // Punch window closes after punchDuration
        if (punchTimer >= punchDuration)
        {
            punchActive   = false;
            isRecovering  = true;
            recoveryTimer = 0f;
        }
    }

    // ===============================
    // HELPERS
    // ===============================
    private void HitPlayer(EnemyStateManager state)
    {
        // Find the player and apply damage
        // Hook this into your player health system
        PlayerHealth playerHealth = state.player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.TakeDamage(punchDamage);

        Debug.Log("Player punched for " + punchDamage + " damage");
    }

    private void FacePlayer(EnemyStateManager state)
    {
        Vector3 lookDir = state.player.position - state.transform.position;
        lookDir.y       = 0f;
        if (lookDir != Vector3.zero)
            state.transform.rotation = Quaternion.LookRotation(lookDir);
    }
}