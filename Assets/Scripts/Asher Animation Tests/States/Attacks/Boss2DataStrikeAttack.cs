using UnityEngine;

// Rapid aimed bursts of bullets — punishes players who stand still
public class Boss2DataStrikeAttack : EnemyBaseState
{
    private int   burstCount         = 4;
    private int   bulletsPerBurst    = 6;
    private float spreadAngle        = 10f;
    private float pauseBetweenBursts = 0.6f;
    private float inBurstFireRate    = 0.05f;
    private float bulletSpeed        = 14f;
    private float bulletDamage       = 12f;
    private float bulletLifetime     = 3f;

    private int   _burstsCompleted;
    private int   _bulletsThisBurst;
    private float _burstTimer;
    private float _pauseTimer;
    private bool  _inBurst;
    private bool  _done;
    private Vector3 _aimSnapshot;

    public override void EnterState(EnemyStateManager state)
    {
        _burstsCompleted = 0;
        _bulletsThisBurst = 0;
        _burstTimer = inBurstFireRate;
        _pauseTimer = 0f;
        _done = false;
        _inBurst = true;
        SnapshotAim(state);
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (_done) return;

        if (_inBurst)
        {
            _burstTimer += Time.deltaTime;
            if (_burstTimer >= inBurstFireRate)
            {
                _burstTimer = 0f;
                FireBullet(state);
                _bulletsThisBurst++;

                if (_bulletsThisBurst >= bulletsPerBurst)
                {
                    _inBurst = false;
                    _bulletsThisBurst = 0;
                    _pauseTimer = 0f;
                    _burstsCompleted++;

                    if (_burstsCompleted >= burstCount)
                    {
                        _done = true;
                        ((Boss2StateManager)state).TransitionToNextState();
                    }
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

    public override float OnBossHurt(EnemyStateManager state) => 0f;

    private void SnapshotAim(EnemyStateManager state)
    {
        Vector3 toPlayer = state.player.position - state.transform.position;
        toPlayer.y = 0f;
        _aimSnapshot = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : state.transform.forward;
    }

    private void FireBullet(EnemyStateManager state)
    {
        float step        = bulletsPerBurst > 1 ? spreadAngle / (bulletsPerBurst - 1) : 0f;
        float angleOffset = -spreadAngle * 0.5f + step * _bulletsThisBurst;
        Vector3 dir       = Quaternion.AngleAxis(angleOffset, Vector3.up) * _aimSnapshot;
        dir.y             = -0.05f;
        dir               = dir.normalized;

        Vector3 spawnPos = state.transform.position;
        spawnPos.y = Random.Range(0.5f, 1.2f);

        Bullet b = new Bullet
        {
            position        = spawnPos,
            direction       = dir,
            speed           = bulletSpeed,
            damage          = bulletDamage,
            maxLifetime     = bulletLifetime,
            collisionRadius = 0.3f,
            canBeParried    = true,
            destroyOnParry  = true,
            movementType    = BulletMovementType.Straight,
            visualPrefab    = state.bulletData.groundSlamBulletPrefab,
        };

        BulletManager.Instance.SpawnBullet(b);
    }
}
