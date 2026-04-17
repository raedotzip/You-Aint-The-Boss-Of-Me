using UnityEngine;

// Rapid aimed bursts of bullets — punishes players who stand still
// Mirrors Boss1TargetedBurstAttack tuned for the computer boss
public class Boss2DataStrikeAttack : EnemyBaseState
{
    public int   burstCount          = 4;
    public int   bulletsPerBurst     = 6;
    public float spreadAngle         = 10f;
    public float pauseBetweenBursts  = 0.6f;
    public float inBurstFireRate     = 0.05f;
    public float bulletSpeed         = 14f;
    public float damage              = 12f;
    public float lifetime            = 3f;

    private int   _burstsCompleted;
    private int   _bulletsInBurst;
    private float _burstTimer;
    private float _pauseTimer;
    private bool  _inBurst;
    private Vector3 _aimSnapshot;

    public override void EnterState(EnemyStateManager state)
    {
        _burstsCompleted = 0;
        _bulletsInBurst  = 0;
        _burstTimer      = 0f;
        _pauseTimer      = 0f;
        _inBurst         = true;
        SnapshotAim(state);
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (_burstsCompleted >= burstCount)
        {
            ((Boss2StateManager)state).TransitionToNextState();
            return;
        }

        if (_inBurst)
        {
            _burstTimer += Time.deltaTime;

            if (_burstTimer >= inBurstFireRate)
            {
                _burstTimer = 0f;
                FireBullet(state);
                _bulletsInBurst++;

                if (_bulletsInBurst >= bulletsPerBurst)
                {
                    _inBurst        = false;
                    _bulletsInBurst = 0;
                    _pauseTimer     = 0f;
                    _burstsCompleted++;
                }
            }
        }
        else
        {
            _pauseTimer += Time.deltaTime;
            if (_pauseTimer >= pauseBetweenBursts)
            {
                _inBurst = true;
                SnapshotAim(state);
            }
        }
    }

    private void SnapshotAim(EnemyStateManager state)
    {
        Vector3 toPlayer = state.player.position - state.transform.position;
        _aimSnapshot = toPlayer.normalized;
    }

    private void FireBullet(EnemyStateManager state)
    {
        float halfSpread = spreadAngle / 2f;
        float yaw        = Random.Range(-halfSpread, halfSpread);
        Vector3 dir      = Quaternion.Euler(0f, yaw, 0f) * _aimSnapshot;

        // TODO: replace with your AttackData — fill in bulletPrefab, collisionRadius, etc.
        // state.FireBullet(dir, yourAttackData);
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;
}
