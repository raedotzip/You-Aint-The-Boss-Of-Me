using System.Collections;
using UnityEngine;

// Triggered when Boss 1 hits 0 HP.
// Boss leaps directly into the lava pit center and sinks.
public class Boss1LavaFinisherState : EnemyBaseState
{
    private Vector3 _startPos;
    private Vector3 _pitCenter;
    private float   _jumpTimer;
    private float   _jumpDuration = 1.4f;
    private float   _jumpHeight   = 5f;
    private bool    _landed;
    private bool    _dead;

    private float _sinkDuration = 2f;

    public override void EnterState(EnemyStateManager state)
    {
        Boss1StateManager boss = (Boss1StateManager)state;

        _startPos  = state.transform.position;
        _jumpTimer = 0f;
        _landed    = false;
        _dead      = false;

        // Target: the lava pit center (boss flies straight in)
        _pitCenter = boss.lavaPitCenter != null
            ? boss.lavaPitCenter.position
            : state.transform.position + state.transform.forward * 8f;
        _pitCenter.y = _startPos.y;

        boss.smoothLookAtEnabled = false;

        // Face the pit before launching
        Vector3 toPit = _pitCenter - _startPos;
        toPit.y = 0f;
        if (toPit.sqrMagnitude > 0.001f)
            state.transform.rotation = Quaternion.LookRotation(toPit);

        state.animator.SetTrigger("Jumping");
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (_dead) return;

        if (!_landed)
        {
            _jumpTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_jumpTimer / _jumpDuration);

            Vector3 pos = Vector3.Lerp(_startPos, _pitCenter, t);
            pos.y = Mathf.Lerp(_startPos.y, _pitCenter.y, t) + 4f * t * (1f - t) * _jumpHeight;
            state.transform.position = pos;

            if (t >= 1f)
            {
                state.transform.position = _pitCenter;
                _landed = true;
                _dead   = true;
                state.StartCoroutine(SinkIntoLava((Boss1StateManager)state));
            }
        }
    }

    // No longer used — kept so TakeDamage callers still compile
    public void PushBoss(Boss1StateManager boss) { }

    public override float OnBossHurt(EnemyStateManager state) => 0f;

    private IEnumerator SinkIntoLava(Boss1StateManager boss)
    {
        boss.smoothLookAtEnabled = false;
        boss.DisableAnimationBools();

        Vector3 sinkStart = boss.transform.position;
        Vector3 sinkEnd   = sinkStart + Vector3.down * 5f;
        float   elapsed   = 0f;

        while (elapsed < _sinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _sinkDuration);
            // Ease-in so it starts slow and accelerates like sinking
            boss.transform.position = Vector3.Lerp(sinkStart, sinkEnd, t * t);
            yield return null;
        }

        MenuController.Instance?.AdvanceToNextBoss(1);
    }
}
