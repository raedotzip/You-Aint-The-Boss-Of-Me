using UnityEngine;

// Fires several rapid bursts of bullets fanned around the player's position.
// Each burst fans 7 bullets across a 90-degree spread aimed at the player.
public class Boss3LaserBarrageAttack : EnemyBaseState
{
    private int   _burstCount    = 4;
    private int   _bulletsPerBurst = 7;
    private float _burstInterval = 0.45f;
    private float _fanAngle      = 90f;
    private float _bulletSpeed   = 14f;
    private float _bulletDamage  = 8f;
    private float _bulletLifetime = 3f;

    private int   _burstsLeft;
    private float _burstTimer;
    private bool  _done;

    public override void EnterState(EnemyStateManager state)
    {
        _burstsLeft = _burstCount;
        _burstTimer = 0f;
        _done       = false;
        FireBurst(state);
        _burstsLeft--;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (_done) return;

        _burstTimer += Time.deltaTime;
        if (_burstTimer >= _burstInterval)
        {
            _burstTimer = 0f;
            FireBurst(state);
            _burstsLeft--;

            if (_burstsLeft <= 0)
            {
                _done = true;
                ((Boss3StateManager)state).TransitionToNextState();
            }
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;

    private void FireBurst(EnemyStateManager state)
    {
        Vector3 toPlayer = state.player.position - state.transform.position;
        if (toPlayer.sqrMagnitude < 0.001f) toPlayer = state.transform.forward;
        toPlayer.y = 0f;
        toPlayer.Normalize();

        float halfFan  = _fanAngle * 0.5f;
        float stepAngle = _bulletsPerBurst > 1
            ? _fanAngle / (_bulletsPerBurst - 1)
            : 0f;

        for (int i = 0; i < _bulletsPerBurst; i++)
        {
            float   angle = -halfFan + stepAngle * i;
            Vector3 dir   = Quaternion.AngleAxis(angle, Vector3.up) * toPlayer;

            // Slight vertical aim toward player eye height
            Vector3 toPlayerFull = state.player.position - state.transform.position;
            float   yBias        = Mathf.Clamp(toPlayerFull.normalized.y, -0.5f, 0.5f);
            dir.y = yBias;
            dir   = dir.normalized;

            Bullet b = new Bullet
            {
                position        = state.transform.position,
                direction       = dir,
                speed           = _bulletSpeed + Random.Range(-1f, 1f),
                damage          = _bulletDamage,
                maxLifetime     = _bulletLifetime,
                collisionRadius = 0.18f,
                canBeParried    = true,
                destroyOnParry  = true,
                movementType    = BulletMovementType.Straight,
                visualPrefab    = state.bulletData.groundSlamBulletPrefab,
            };

            BulletManager.Instance.SpawnBullet(b);
        }
    }
}
