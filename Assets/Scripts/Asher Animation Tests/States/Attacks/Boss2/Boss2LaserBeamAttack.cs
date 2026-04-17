using UnityEngine;

// Simulates a sweeping laser by firing dense salvos of fast bullets that rotate across the arena
public class Boss2LaserBeamAttack : EnemyBaseState
{
    private float sweepSpeed     = 130f; // degrees per second
    private float sweepRange     = 200f; // total degrees swept per pass
    private int   sweepCount     = 2;    // passes left-right
    private float fireRate       = 0.04f;
    private int   bulletsPerSalvo = 3;
    private float bulletSpeed    = 8f;
    private float bulletDamage   = 9f;
    private float bulletLifetime = 3.5f;

    private float _angle;
    private float _swept;
    private float _sweepDirection;
    private int   _sweepsCompleted;
    private float _fireTimer;
    private bool  _done;

    public override void EnterState(EnemyStateManager state)
    {
        _swept            = 0f;
        _sweepDirection   = 1f;
        _sweepsCompleted  = 0;
        _fireTimer        = fireRate;
        _done             = false;

        // Start sweep from one side of the player
        Vector3 toPlayer = state.player.position - state.transform.position;
        toPlayer.y = 0f;
        float baseAngle = toPlayer.sqrMagnitude > 0.001f
            ? Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg
            : 0f;
        _angle = baseAngle - sweepRange * 0.5f;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (_done) return;

        float dt     = Time.deltaTime;
        float dAngle = sweepSpeed * dt;
        _angle += dAngle * _sweepDirection;
        _swept += dAngle;

        _fireTimer += dt;
        while (_fireTimer >= fireRate)
        {
            _fireTimer -= fireRate;
            FireSalvo(state);
        }

        if (_swept >= sweepRange)
        {
            _swept           = 0f;
            _sweepDirection *= -1f;
            _sweepsCompleted++;

            if (_sweepsCompleted >= sweepCount)
            {
                _done = true;
                ((Boss2StateManager)state).TransitionToNextState();
            }
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;

    private void FireSalvo(EnemyStateManager state)
    {
        float rad     = _angle * Mathf.Deg2Rad;
        Vector3 hDir  = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        float targetY = state.player.position.y;
        Vector3 spawnBase = state.transform.position;

        for (int i = 0; i < bulletsPerSalvo; i++)
        {
            float yOffset = Mathf.Lerp(-0.25f, 0.25f, bulletsPerSalvo > 1 ? i / (float)(bulletsPerSalvo - 1) : 0.5f);
            Vector3 spawnPos = spawnBase;
            spawnPos.y = targetY + 1.0f + yOffset;

            Vector3 dir = hDir;
            dir.y = yOffset * 0.4f;
            dir   = dir.normalized;

            Bullet b = new Bullet
            {
                position        = spawnPos,
                direction       = dir,
                speed           = bulletSpeed,
                damage          = bulletDamage,
                maxLifetime     = bulletLifetime,
                collisionRadius = 0.22f,
                canBeParried    = true,
                destroyOnParry  = true,
                movementType    = BulletMovementType.Straight,
                visualPrefab    = state.bulletData.groundSlamBulletPrefab,
                scale           = 1.5f,
            };

            BulletManager.Instance.SpawnBullet(b);
        }
    }
}
