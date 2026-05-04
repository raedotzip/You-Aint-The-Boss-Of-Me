using UnityEngine;

// Pauses for a dramatic charge, then dumps a tight dense burst straight at the player's position.
// The charge window is telegraphed — savvy players can move, but the burst is fast enough to punish late reactions.
public class Boss2RailgunAttack : EnemyBaseState
{
    private float chargeTime      = 0.9f;   // pause before firing
    private int   bulletCount     = 35;     // bullets in the burst
    private float spreadAngle     = 15f;    // cone width in degrees
    private float bulletInterval  = 0.035f; // seconds between each bullet
    private float bulletSpeed     = 10f;
    private float bulletDamage    = 16f;
    private float bulletLifetime  = 8f;
    private float verticalSpread  = 10f;

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
        _bulletsLeft = ((Boss2StateManager)state).ScaleBulletCount(bulletCount);
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
                // Snapshot flat horizontal direction — TiltTowardPlayer corrects height per spawn point
                Vector3 toPlayer = state.player.position - state.transform.position;
                toPlayer.y = 0f;
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

    static Vector3 TiltTowardPlayer(Vector3 dir, Vector3 spawnPos, Vector3 playerPos)
    {
        float hDist = new Vector3(playerPos.x - spawnPos.x, 0f, playerPos.z - spawnPos.z).magnitude;
        if (hDist > 0.01f)
            dir.y = (playerPos.y - spawnPos.y) / hDist;
        return dir.normalized;
    }

    private void FireBullet(EnemyStateManager state)
    {
        float rY  = Random.Range(-spreadAngle * 0.5f, spreadAngle * 0.5f);
        float rX  = Random.Range(-spreadAngle * 0.25f, spreadAngle * 0.25f);
        Vector3 dir = Quaternion.Euler(rX, rY, 0f) * _targetDir;

        foreach (Transform sp in ((Boss2StateManager)state).GetAllSpawnPoints())
        {
            Vector3 spawnDir = TiltTowardPlayer(dir, sp.position, state.player.position + Vector3.up * 1.0f);
            Vector3 right    = Vector3.Cross(Vector3.up, spawnDir).normalized;
            if (right.sqrMagnitude < 0.001f) right = Vector3.right;
            spawnDir = Quaternion.AngleAxis(Random.Range(-verticalSpread, verticalSpread), right) * spawnDir;
            Bullet b = new Bullet
            {
                position        = sp.position,
                direction       = spawnDir,
                speed           = ((Boss2StateManager)state).ScaleBulletSpeed(bulletSpeed),
                damage          = bulletDamage,
                maxLifetime     = bulletLifetime,
                collisionRadius = 0.22f,
                canBeParried    = true,
                destroyOnParry  = true,
                movementType    = BulletMovementType.Straight,
                visualPrefab    = state.bulletData.groundSlamBulletPrefab,
                scale           = 0.6f,
            };
            BulletManager.Instance.SpawnBullet(b);
        }
    }
}
