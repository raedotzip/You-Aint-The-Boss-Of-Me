using UnityEngine;

// Telepathically grabs the player, lifts them, then flings them across the arena.
// Phase 1 — Warning:  telegraph for 0.8s
// Phase 2 — Lift:     disable CC and float player upward over 0.4s
// Phase 3 — Throw:    fling the player 10m in a random horizontal direction, re-enable CC
public class Boss3TelepathicThrow : EnemyBaseState
{
    private float _warningDuration = 0.8f;
    private float _liftDuration    = 0.4f;
    private float _throwDistance   = 10f;
    private float _throwHeight     = 2.5f;  // how high above current floor the player lands
    private float _liftAmount      = 2f;    // meters the player rises during lift

    private enum Phase { Warning, Lift, Done }
    private Phase _phase;
    private float _timer;

    private CharacterController _cc;
    private Vector3 _liftStart;
    private Vector3 _throwDir;

    public override void EnterState(EnemyStateManager state)
    {
        _phase = Phase.Warning;
        _timer = 0f;
        _cc    = null;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        _timer += Time.deltaTime;

        switch (_phase)
        {
            case Phase.Warning:
                if (_timer >= _warningDuration)
                    StartLift(state);
                break;

            case Phase.Lift:
                UpdateLift(state);
                break;
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;

    // -----------------------------------------------
    private void StartLift(EnemyStateManager state)
    {
        _phase = Phase.Lift;
        _timer = 0f;

        _cc = state.player.GetComponent<CharacterController>();
        if (_cc != null) _cc.enabled = false;

        _liftStart = state.player.position;

        // Pick a random horizontal throw direction away from the boss
        float   angle    = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        _throwDir        = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
    }

    private void UpdateLift(EnemyStateManager state)
    {
        float t = Mathf.Clamp01(_timer / _liftDuration);

        // Float the player upward during the lift phase
        Vector3 pos = _liftStart;
        pos.y       = _liftStart.y + _liftAmount * t;
        state.player.position = pos;

        if (_timer >= _liftDuration)
            DoThrow(state);
    }

    private void DoThrow(EnemyStateManager state)
    {
        _phase = Phase.Done;

        // Fling the player to the thrown position
        Vector3 dest = state.player.position + _throwDir * _throwDistance;
        dest.y       = _throwHeight;
        state.player.position = dest;

        // Re-enable the CharacterController — gravity in PlayerMovement handles landing
        if (_cc != null) _cc.enabled = true;

        // Fire a burst of bullets at the thrown-to position as a combo
        FireFollowUpBullets(state, dest);

        ((Boss3StateManager)state).TransitionToNextState();
    }

    private void FireFollowUpBullets(EnemyStateManager state, Vector3 target)
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 spawnOffset = new Vector3(
                Random.Range(-2f, 2f), Random.Range(3f, 6f), Random.Range(-2f, 2f));
            Vector3 spawnPos = target + spawnOffset;
            Vector3 dir      = (target - spawnPos).normalized;

            Bullet b = new Bullet
            {
                position        = spawnPos,
                direction       = dir,
                speed           = 7f,
                damage          = 6f,
                maxLifetime     = 2f,
                collisionRadius = 0.2f,
                canBeParried    = true,
                destroyOnParry  = true,
                movementType    = BulletMovementType.Straight,
                visualPrefab    = state.bulletData.groundSlamBulletPrefab,
            };

            BulletManager.Instance.SpawnBullet(b);
        }
    }
}
