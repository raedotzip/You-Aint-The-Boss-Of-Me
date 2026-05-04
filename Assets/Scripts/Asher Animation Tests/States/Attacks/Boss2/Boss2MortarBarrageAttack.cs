using UnityEngine;

// Fires arcing shots one at a time that fall toward the player's position with random scatter.
// Each mortar is angled upward so it travels an arc before hitting player height.
// The stagger forces the player to keep moving rather than dodge once and stop.
public class Boss2MortarBarrageAttack : EnemyBaseState
{
    private int   mortarCount    = 14;
    private float mortarInterval = 0.22f;  // seconds between each mortar
    private float aimScatter     = 3f;     // random scatter radius around player
    private float arcLift        = 0f;     // 0 = aim directly at player; positive values tilt upward
    private float bulletSpeed    = 7.5f;
    private float bulletDamage   = 13f;
    private float bulletLifetime = 10f;
    private float verticalSpread = 8f;

    private int   _fired;
    private int   _scaledCount;
    private float _fireTimer;
    private bool  _done;

    public override void EnterState(EnemyStateManager state)
    {
        _fired        = 0;
        _scaledCount  = ((Boss2StateManager)state).ScaleBulletCount(mortarCount);
        _fireTimer    = mortarInterval; // trigger first shot immediately
        _done         = false;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (_done) return;

        _fireTimer += Time.deltaTime;

        while (_fireTimer >= mortarInterval && _fired < _scaledCount)
        {
            _fireTimer -= mortarInterval;
            FireMortar(state);
            _fired++;
        }

        if (_fired >= _scaledCount && _fireTimer >= mortarInterval)
        {
            _done = true;
            ((Boss2StateManager)state).TransitionToNextState();
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;

    private void FireMortar(EnemyStateManager state)
    {
        Vector3 scatter = new Vector3(
            Random.Range(-aimScatter, aimScatter),
            0f,
            Random.Range(-aimScatter, aimScatter));

        Vector3 target   = state.player.position + Vector3.up * 1.0f + scatter;
        Vector3 spawnPos = ((Boss2StateManager)state).GetRandomSpawnPoint();
        Vector3 dir      = (target - spawnPos);

        // Arc upward so the bullet visually drops in from above
        dir.y += arcLift;
        Vector3 finalDir = dir.normalized;
        Vector3 right    = Vector3.Cross(Vector3.up, finalDir).normalized;
        if (right.sqrMagnitude < 0.001f) right = Vector3.right;
        finalDir = Quaternion.AngleAxis(Random.Range(-verticalSpread, verticalSpread), right) * finalDir;

        Bullet b = new Bullet
        {
            position        = spawnPos,
            direction       = finalDir,
            speed           = ((Boss2StateManager)state).ScaleBulletSpeed(bulletSpeed),
            damage          = bulletDamage,
            maxLifetime     = bulletLifetime,
            collisionRadius = 0.3f,
            canBeParried    = true,
            destroyOnParry  = false,
            movementType    = BulletMovementType.Straight,
            visualPrefab    = state.bulletData.groundSlamBulletPrefab,
            scale           = 1.1f,
        };
        BulletManager.Instance.SpawnBullet(b);
    }
}
