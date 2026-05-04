using UnityEngine;

// Expanding rings of bullets with a rotating gap — player must dodge or stand in the gap
public class Boss2EMPWaveAttack : EnemyBaseState
{
    private int   ringCount        = 8;
    private float timeBetweenRings = 1.1f;
    private int   bulletsPerRing   = 48;
    private float gapSizeDegrees   = 50f;
    private float gapRotatePerRing = 90f;
    private float bulletSpeed      = 3f;
    private float bulletDamage     = 12f;
    private float bulletLifetime   = 16f;
    private float verticalSpread   = 10f;

    private int   _ringsFired;
    private float _timer;
    private float _gapAngle;
    private bool  _done;

    public override void EnterState(EnemyStateManager state)
    {
        _ringsFired = 0;
        _timer      = timeBetweenRings;
        _done       = false;

        Vector3 toPlayer = state.player.position - state.transform.position;
        toPlayer.y = 0f;
        _gapAngle = toPlayer.sqrMagnitude > 0.001f
            ? Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg
            : 0f;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (_done) return;

        _timer += Time.deltaTime;

        if (_timer >= timeBetweenRings)
        {
            _timer = 0f;
            FireRing(state);
            _gapAngle  += gapRotatePerRing;
            _ringsFired++;

            if (_ringsFired >= ringCount)
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

    private void FireRing(EnemyStateManager state)
    {
        float angleStep = 360f / bulletsPerRing;
        float halfGap   = gapSizeDegrees * 0.5f;

        for (int i = 0; i < bulletsPerRing; i++)
        {
            float angle = i * angleStep;
            if (Mathf.Abs(Mathf.DeltaAngle(angle, _gapAngle)) <= halfGap) continue;

            float rad   = angle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

            foreach (Transform sp in ((Boss2StateManager)state).GetAllSpawnPoints())
            {
                Vector3 spawnPos = sp.position;
                Vector3 spawnDir = TiltTowardPlayer(dir, spawnPos, state.player.position + Vector3.up * 1.0f);
                Vector3 right    = Vector3.Cross(Vector3.up, spawnDir).normalized;
                if (right.sqrMagnitude < 0.001f) right = Vector3.right;
                spawnDir = Quaternion.AngleAxis(Random.Range(-verticalSpread, verticalSpread), right) * spawnDir;

                Bullet b = new Bullet
                {
                    position        = spawnPos,
                    direction       = spawnDir,
                    speed           = ((Boss2StateManager)state).ScaleBulletSpeed(bulletSpeed),
                    damage          = bulletDamage,
                    maxLifetime     = bulletLifetime,
                    collisionRadius = 0.3f,
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
}
