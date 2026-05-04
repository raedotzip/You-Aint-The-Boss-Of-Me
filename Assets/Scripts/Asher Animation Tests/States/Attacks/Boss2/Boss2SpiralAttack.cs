using UnityEngine;

// Two interleaved spiral arms rotating outward from the boss — classic bullet-hell pattern
public class Boss2SpiralAttack : EnemyBaseState
{
    private float rotateSpeed    = 200f; // degrees per second
    private float fireRate       = 0.06f;
    private int   arms           = 5;
    private float bulletSpeed    = 3.5f;
    private float bulletDamage   = 11f;
    private float bulletLifetime = 16f;
    private float duration       = 4.5f;
    private float verticalSpread = 12f;

    private float _angle;
    private float _fireTimer;
    private float _elapsed;
    private bool  _done;

    public override void EnterState(EnemyStateManager state)
    {
        _elapsed   = 0f;
        _fireTimer = fireRate;
        _done      = false;

        Vector3 toPlayer = state.player.position - state.transform.position;
        toPlayer.y = 0f;
        _angle = toPlayer.sqrMagnitude > 0.001f
            ? Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg
            : 0f;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (_done) return;

        float dt   = Time.deltaTime;
        _elapsed  += dt;
        _angle    += rotateSpeed * dt;
        _fireTimer += dt;

        while (_fireTimer >= fireRate)
        {
            _fireTimer -= fireRate;
            FireArms(state);
        }

        if (_elapsed >= duration)
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

    private void FireArms(EnemyStateManager state)
    {
        for (int arm = 0; arm < arms; arm++)
        {
            float angle   = _angle + arm * (360f / arms);
            float rad     = angle * Mathf.Deg2Rad;
            Vector3 hDir  = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

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
                    collisionRadius = 0.3f,
                    canBeParried    = true,
                    destroyOnParry  = false,
                    movementType    = BulletMovementType.Straight,
                    visualPrefab    = state.bulletData.groundSlamBulletPrefab,
                    scale           = 0.7f,
                };

                BulletManager.Instance.SpawnBullet(b);
            }
        }
    }
}
