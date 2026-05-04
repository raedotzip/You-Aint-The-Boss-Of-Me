using UnityEngine;

// Rapid aimed bursts of bullets — punishes players who stand still
public class Boss2DataStrikeAttack : EnemyBaseState
{
    private int   burstCount         = 5;
    private int   bulletsPerBurst    = 12;
    private float spreadAngle        = 25f;
    private float pauseBetweenBursts = 0.5f;
    private float inBurstFireRate    = 0.04f;
    private float bulletSpeed        = 4.5f;
    private float bulletDamage       = 12f;
    private float bulletLifetime     = 12f;
    private float verticalSpread     = 10f;

    private int   _burstsCompleted;
    private int   _bulletsThisBurst;
    private int   _scaledBulletsPerBurst;
    private float _burstTimer;
    private float _pauseTimer;
    private bool  _inBurst;
    private bool  _done;
    private Vector3 _aimSnapshot;

    public override void EnterState(EnemyStateManager state)
    {
        _burstsCompleted       = 0;
        _bulletsThisBurst      = 0;
        _scaledBulletsPerBurst = ((Boss2StateManager)state).ScaleBulletCount(bulletsPerBurst);
        _burstTimer            = inBurstFireRate;
        _pauseTimer            = 0f;
        _done                  = false;
        _inBurst               = true;
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

                if (_bulletsThisBurst >= _scaledBulletsPerBurst)
                {
                    _inBurst          = false;
                    _bulletsThisBurst = 0;
                    _pauseTimer       = 0f;
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

    static Vector3 TiltTowardPlayer(Vector3 dir, Vector3 spawnPos, Vector3 playerPos)
    {
        float hDist = new Vector3(playerPos.x - spawnPos.x, 0f, playerPos.z - spawnPos.z).magnitude;
        if (hDist > 0.01f)
            dir.y = (playerPos.y - spawnPos.y) / hDist;
        return dir.normalized;
    }

    private void SnapshotAim(EnemyStateManager state)
    {
        // Aim directly at player in 3D so bullets travel at player height
        Vector3 toPlayer = state.player.position - state.transform.position;
        _aimSnapshot = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : state.transform.forward;
    }

    private void FireBullet(EnemyStateManager state)
    {
        float step        = _scaledBulletsPerBurst > 1 ? spreadAngle / (_scaledBulletsPerBurst - 1) : 0f;
        float angleOffset = -spreadAngle * 0.5f + step * _bulletsThisBurst;
        Vector3 dir       = Quaternion.AngleAxis(angleOffset, Vector3.up) * _aimSnapshot;
        dir               = dir.normalized;

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
                scale           = 0.6f,
            };

            BulletManager.Instance.SpawnBullet(b);
        }
    }
}
