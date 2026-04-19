using System.Collections;
using UnityEngine;

// Triggered when Boss 1 hits 0 HP.
// Boss jumps to the lava pit edge, staggers there, and the player beats it into the lava.
public class Boss1LavaFinisherState : EnemyBaseState
{
    // Jump arc to the lava edge
    private Vector3 _startPos;
    private Vector3 _edgePos;
    private Vector3 _pitCenter;
    private float   _jumpTimer;
    private float   _jumpDuration = 1.2f;
    private float   _jumpHeight   = 4f;
    private bool    _landed;

    // Push-into-lava mechanic
    private float _pushPerHit    = 1.1f;  // metres per player hit
    private float _pushToKill    = 3.5f;  // total push needed to fall in
    private float _pushSoFar;
    private bool  _dead;

    // Sink animation after final push
    private float _sinkDuration  = 1.8f;

    public override void EnterState(EnemyStateManager state)
    {
        Boss1StateManager boss = (Boss1StateManager)state;

        _startPos = state.transform.position;
        _pushSoFar = 0f;
        _jumpTimer = 0f;
        _landed    = false;
        _dead      = false;

        // Find a position at the lava pit rim closest to the boss
        _pitCenter = boss.lavaPitCenter != null
            ? boss.lavaPitCenter.position
            : state.transform.position + state.transform.forward * 6f;
        _pitCenter.y = _startPos.y;

        BoxCollider pit = boss.lavaPitCenter != null
            ? boss.lavaPitCenter.GetComponent<BoxCollider>()
            : null;

        Vector3 dirToBoss = (_startPos - _pitCenter);
        dirToBoss.y = 0f;
        if (dirToBoss.sqrMagnitude < 0.001f) dirToBoss = state.transform.forward;
        dirToBoss.Normalize();

        // Step back from pit center to just inside the rim so the boss teeters at the edge
        float rimDist = pit != null
            ? Mathf.Min(pit.size.x, pit.size.z) * 0.5f - 0.3f
            : 2f;
        _edgePos   = _pitCenter + dirToBoss * rimDist;
        _edgePos.y = _startPos.y;

        boss.smoothLookAtEnabled = false;
        state.animator.SetTrigger("Jumping");
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (_dead) return;

        if (!_landed)
        {
            // Arc jump to the lava rim
            _jumpTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_jumpTimer / _jumpDuration);

            Vector3 pos = Vector3.Lerp(_startPos, _edgePos, t);
            pos.y = Mathf.Lerp(_startPos.y, _edgePos.y, t) + 4f * t * (1f - t) * _jumpHeight;
            state.transform.position = pos;

            if (t >= 1f)
            {
                state.transform.position = _edgePos;
                _landed = true;

                // Face the pit so the push looks correct
                Vector3 toPit = _pitCenter - _edgePos;
                toPit.y = 0f;
                if (toPit.sqrMagnitude > 0.001f)
                    state.transform.rotation = Quaternion.LookRotation(toPit);

                state.animator.SetBool("Tired", true);
            }
        }
        // Phase 2 is handled entirely by PushBoss() calls from Boss1StateManager.TakeDamage
    }

    // Called by Boss1StateManager instead of applying HP damage after health = 0
    public void PushBoss(Boss1StateManager boss)
    {
        if (!_landed || _dead) return;

        Vector3 toPit = _pitCenter - boss.transform.position;
        toPit.y = 0f;
        if (toPit.sqrMagnitude > 0.001f)
            boss.transform.position += toPit.normalized * _pushPerHit;

        _pushSoFar += _pushPerHit;

        if (_pushSoFar >= _pushToKill)
        {
            _dead = true;
            boss.StartCoroutine(SinkIntoLava(boss));
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;

    private IEnumerator SinkIntoLava(Boss1StateManager boss)
    {
        boss.smoothLookAtEnabled = false;
        boss.animator.SetBool("Tired", false);

        Vector3 sinkStart = boss.transform.position;
        Vector3 sinkEnd   = sinkStart + Vector3.down * 4f;
        float   elapsed   = 0f;

        while (elapsed < _sinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _sinkDuration);
            // Ease-in so it starts slow and accelerates like a real fall
            boss.transform.position = Vector3.Lerp(sinkStart, sinkEnd, t * t);
            yield return null;
        }

        MenuController.Instance?.AdvanceToNextBoss(1);
    }
}
