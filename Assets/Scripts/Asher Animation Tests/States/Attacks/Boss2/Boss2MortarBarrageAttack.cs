using UnityEngine;

// Fires arcing shots one at a time that fall toward the player's position with random scatter.
// Each mortar is angled upward so it travels an arc before hitting player height.
// The stagger forces the player to keep moving rather than dodge once and stop.
public class Boss2MortarBarrageAttack : EnemyBaseState
{
    private int   mortarCount    = 8;
    private float mortarInterval = 0.22f;  // seconds between each mortar
    private float aimScatter     = 2.8f;   // random scatter radius around player
    private float arcLift        = 4f;     // how much to angle upward for the arc effect
    private float bulletSpeed    = 7.5f;
    private float bulletDamage   = 13f;
    private float bulletLifetime = 4f;

    private int   _fired;
    private float _fireTimer;
    private bool  _done;

    public override void EnterState(EnemyStateManager state)
    {
        _fired     = 0;
        _fireTimer = mortarInterval; // trigger first shot immediately
        _done      = false;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (_done) return;

        _fireTimer += Time.deltaTime;

        while (_fireTimer >= mortarInterval && _fired < mortarCount)
        {
            _fireTimer -= mortarInterval;
            FireMortar(state);
            _fired++;
        }

        if (_fired >= mortarCount && _fireTimer >= mortarInterval)
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

        Bullet b = new Bullet
        {
            position        = spawnPos,
            direction       = dir.normalized,
            speed           = bulletSpeed,
            damage          = bulletDamage,
            maxLifetime     = bulletLifetime,
            collisionRadius = 0.3f,
            canBeParried    = true,
            destroyOnParry  = false,
            movementType    = BulletMovementType.Straight,
            visualPrefab    = state.bulletData.groundSlamBulletPrefab,
            scale           = 0.9f,
        };
        BulletManager.Instance.SpawnBullet(b);
    }
}
