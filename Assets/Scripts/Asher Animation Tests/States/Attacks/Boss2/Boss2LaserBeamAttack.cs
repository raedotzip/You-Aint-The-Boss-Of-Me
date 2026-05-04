using UnityEngine;

// Simulates a sweeping laser by firing dense salvos of fast bullets that rotate across the arena
public class Boss2LaserBeamAttack : EnemyBaseState
{
    private float sweepSpeed      = 130f; // degrees per second
    private float sweepRange      = 200f; // total degrees swept per pass
    private int   sweepCount      = 2;    // passes left-right
    private float fireRate        = 0.04f;
    private int   bulletsPerSalvo = 12;
    private float bulletSpeed     = 6f;
    private float bulletDamage    = 9f;
    private float bulletLifetime  = 10f;
    private float verticalSpread  = 12f;

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

    static Vector3 TiltTowardPlayer(Vector3 dir, Vector3 spawnPos, Vector3 playerPos)
    {
        float hDist = new Vector3(playerPos.x - spawnPos.x, 0f, playerPos.z - spawnPos.z).magnitude;
        if (hDist > 0.01f)
            dir.y = (playerPos.y - spawnPos.y) / hDist;
        return dir.normalized;
    }

    private void FireSalvo(EnemyStateManager state)
    {
        float rad    = _angle * Mathf.Deg2Rad;
        Vector3 hDir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

        foreach (Transform sp in ((Boss2StateManager)state).GetAllSpawnPoints())
        {
            Vector3 spawnPos = sp.position;
            Vector3 dir      = TiltTowardPlayer(hDir, spawnPos, state.player.position + Vector3.up * 1.0f);
            Vector3 right    = Vector3.Cross(Vector3.up, dir).normalized;
            if (right.sqrMagnitude < 0.001f) right = Vector3.right;
            dir = Quaternion.AngleAxis(Random.Range(-verticalSpread, verticalSpread), right) * dir;

            Bullet b = new Bullet
            {
                position        = spawnPos,
                direction       = dir,
                speed           = ((Boss2StateManager)state).ScaleBulletSpeed(bulletSpeed),
                damage          = bulletDamage,
                maxLifetime     = bulletLifetime,
                collisionRadius = 0.22f,
                canBeParried    = true,
                destroyOnParry  = true,
                movementType    = BulletMovementType.Straight,
                visualPrefab    = state.bulletData.groundSlamBulletPrefab,
                scale           = 0.65f,
            };

            BulletManager.Instance.SpawnBullet(b);
        }
    }
}
