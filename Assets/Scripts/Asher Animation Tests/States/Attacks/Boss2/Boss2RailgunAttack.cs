using UnityEngine;

// Pauses for a dramatic charge, then dumps a tight dense burst straight at the player's position.
// The charge window is telegraphed — savvy players can move, but the burst is fast enough to punish late reactions.
public class Boss2RailgunAttack : EnemyBaseState
{
    private float chargeTime      = 0.9f;   // pause before firing
    private int   bulletCount     = 22;     // bullets in the burst
    private float spreadAngle     = 10f;    // cone width in degrees
    private float bulletInterval  = 0.035f; // seconds between each bullet
    private float bulletSpeed     = 10f;
    private float bulletDamage    = 16f;
    private float bulletLifetime  = 3.5f;

    private float   _elapsed;
    private bool    _fired;
    private int     _bulletsLeft;
    private float   _fireTimer;
    private bool    _done;
    private Vector3 _targetDir;

    public override void EnterState(EnemyStateManager state)
    {
        _elapsed     = 0f;
        _fired       = false;
        _bulletsLeft = bulletCount;
        _fireTimer   = 0f;
        _done        = false;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (_done) return;

        float dt  = Time.deltaTime;
        _elapsed += dt;

        if (!_fired)
        {
            if (_elapsed >= chargeTime)
            {
                _fired     = true;
                _fireTimer = 0f;
                // Snapshot player position at the moment of firing
                Vector3 toPlayer = state.player.position - state.transform.position;
                _targetDir = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : state.transform.forward;
            }
            return;
        }

        _fireTimer += dt;
        while (_fireTimer >= bulletInterval && _bulletsLeft > 0)
        {
            _fireTimer  -= bulletInterval;
            _bulletsLeft--;
            FireBullet(state);
        }

        if (_bulletsLeft <= 0)
        {
            _done = true;
            ((Boss2StateManager)state).TransitionToNextState();
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;

    private void FireBullet(EnemyStateManager state)
    {
        float rY  = Random.Range(-spreadAngle * 0.5f, spreadAngle * 0.5f);
        float rX  = Random.Range(-spreadAngle * 0.25f, spreadAngle * 0.25f);
        Vector3 dir = Quaternion.Euler(rX, rY, 0f) * _targetDir;

        foreach (Transform sp in ((Boss2StateManager)state).GetAllSpawnPoints())
        {
            Bullet b = new Bullet
            {
                position        = sp.position,
                direction       = dir.normalized,
                speed           = bulletSpeed,
                damage          = bulletDamage,
                maxLifetime     = bulletLifetime,
                collisionRadius = 0.22f,
                canBeParried    = true,
                destroyOnParry  = true,
                movementType    = BulletMovementType.Straight,
                visualPrefab    = state.bulletData.groundSlamBulletPrefab,
                scale           = 0.7f,
            };
            BulletManager.Instance.SpawnBullet(b);
        }
    }
}
