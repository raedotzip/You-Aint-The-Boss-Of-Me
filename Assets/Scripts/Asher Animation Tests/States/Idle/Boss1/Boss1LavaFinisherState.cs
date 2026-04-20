using System.Collections;
using UnityEngine;

// Triggered when Boss 1 hits 0 HP.
// Phase 1 — Boss leaps to the lava edge and wobbles.
// Phase 2 — Player lands one more hit → boss is knocked into the lava and sinks.
public class Boss1LavaFinisherState : EnemyBaseState
{
    private enum Phase { JumpingToEdge, WobbleAtEdge, FallingIntoLava, Done }

    private Phase   _phase;
    private Vector3 _startPos;
    private Vector3 _edgePos;
    private Vector3 _lavaCenter;
    private float   _jumpTimer;
    private float   _lavaFallDepth;
    private Vector3 _sinkStart;
    private Vector3 _sinkTarget;   // XZ target in the lava center
    private float   _sinkSpeed;    // units per second

    private const float JumpDuration = 1.4f;
    private const float JumpHeight   = 5f;
    private const float DefaultFallSpeed = 4f; // units/sec downward

    public override void EnterState(EnemyStateManager state)
    {
        Boss1StateManager boss = (Boss1StateManager)state;

        _phase         = Phase.JumpingToEdge;
        _startPos      = state.transform.position;
        _jumpTimer     = 0f;
        _lavaFallDepth = boss.lavaFallDepth;

        _lavaCenter = boss.lavaPitCenter != null
            ? boss.lavaPitCenter.position
            : state.transform.position + state.transform.forward * 8f;
        _lavaCenter.y = _startPos.y;

        // Edge position — use designer-placed point if assigned,
        // otherwise land 70% of the way from boss to pit center.
        if (boss.lavaEdgePosition != null)
        {
            _edgePos   = boss.lavaEdgePosition.position;
            _edgePos.y = _startPos.y;
        }
        else
        {
            Vector3 toCenter = _lavaCenter - _startPos;
            toCenter.y = 0f;
            _edgePos   = _startPos + toCenter * 0.7f;
            _edgePos.y = _startPos.y;
        }

        boss.smoothLookAtEnabled = false;

        // Face the edge before launching
        Vector3 dir = _edgePos - _startPos;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            state.transform.rotation = Quaternion.LookRotation(dir);

        boss.isAirborne = true;
        boss.DisableAnimationBools();
        state.animator?.SetTrigger("Jumping");
        Debug.Log($"[Boss1] Finisher started. Edge={_edgePos} LavaCenter={_lavaCenter} FallDepth={_lavaFallDepth}");
    }

    public override void UpdateState(EnemyStateManager state)
    {
        Boss1StateManager boss = (Boss1StateManager)state;

        switch (_phase)
        {
            case Phase.JumpingToEdge:
                _jumpTimer += Time.deltaTime;
                float t = Mathf.Clamp01(_jumpTimer / JumpDuration);

                Vector3 pos = Vector3.Lerp(_startPos, _edgePos, t);
                pos.y += 4f * t * (1f - t) * JumpHeight;
                state.transform.position = pos;

                if (t >= 1f)
                {
                    state.transform.position = _edgePos;
                    boss.isAirborne = false;
                    _phase = Phase.WobbleAtEdge;
                    boss.DisableAnimationBools();
                    state.animator?.SetBool("Tired", true);
                    Debug.Log("[Boss1] Staggered at lava edge — hit the boss!");
                }
                break;

            case Phase.WobbleAtEdge:
                // Unstable wobble at the edge
                state.transform.position = _edgePos + new Vector3(
                    Mathf.Sin(Time.time * 9f) * 0.06f, 0f,
                    Mathf.Sin(Time.time * 7f) * 0.03f);

                // Keep facing the lava
                Vector3 toLava = _lavaCenter - state.transform.position;
                toLava.y = 0f;
                if (toLava.sqrMagnitude > 0.001f)
                    state.transform.rotation = Quaternion.LookRotation(toLava);
                break;

            case Phase.FallingIntoLava:
            {
                Vector3 cur = state.transform.position;

                // Drift toward lava center XZ while falling
                float xzStep = _sinkSpeed * 0.5f * Time.deltaTime;
                cur.x = Mathf.MoveTowards(cur.x, _sinkTarget.x, xzStep);
                cur.z = Mathf.MoveTowards(cur.z, _sinkTarget.z, xzStep);

                // Accelerate downward (gravity-like)
                cur.y -= _sinkSpeed * Time.deltaTime;
                _sinkSpeed += 6f * Time.deltaTime; // accelerate as he sinks

                state.transform.position = cur;

                if (cur.y <= _lavaFallDepth && _phase != Phase.Done)
                {
                    _phase = Phase.Done;
                    Debug.Log($"[Boss1] Sunk to y={cur.y} — calling AdvanceToNextBoss(1). MenuController={(MenuController.Instance != null ? "OK" : "NULL")}");
                    if (MenuController.Instance != null)
                        MenuController.Instance.AdvanceToNextBoss(1);
                    else
                        Debug.LogError("[Boss1] MenuController.Instance is null — cannot advance to next boss!");
                }
                break;
            }
        }
    }

    // Called by Boss1StateManager.TakeDamage() when the player hits during the finisher.
    // Blocked during JumpingToEdge so the boss must fully land before the player can push him.
    public void PushBoss(Boss1StateManager boss)
    {
        if (_phase != Phase.WobbleAtEdge)
        {
            Debug.Log($"[Boss1] PushBoss ignored — phase is {_phase}, must be WobbleAtEdge");
            return;
        }

        _phase      = Phase.FallingIntoLava;
        _sinkStart  = boss.transform.position;
        _sinkTarget = new Vector3(_lavaCenter.x, 0f, _lavaCenter.z);
        _sinkSpeed  = DefaultFallSpeed;

        boss.smoothLookAtEnabled = false;
        boss.DisableAnimationBools();
        Debug.Log($"[Boss1] Knocked into lava! Sinking from {_sinkStart} to depth {_lavaFallDepth}");
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;
}
