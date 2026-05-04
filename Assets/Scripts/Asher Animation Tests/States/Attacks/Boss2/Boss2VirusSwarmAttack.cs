using UnityEngine;

// Fires bullets aimed at the player — alternates direct shots with wavy sine bullets
public class Boss2VirusSwarmAttack : EnemyBaseState
{
    private int   virusCount     = 20;
    private float spawnDelay     = 0.15f;
    private float bulletSpeed    = 2.8f;
    private float bulletDamage   = 10f;
    private float lifetime       = 16f;
    private float verticalSpread = 12f;

    private float _timer;
    private int   _spawned;
    private int   _scaledCount;
    private bool  _done;

    public override void EnterState(EnemyStateManager state)
    {
        _timer       = spawnDelay;
        _spawned     = 0;
        _scaledCount = ((Boss2StateManager)state).ScaleBulletCount(virusCount);
        _done        = false;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (_done) return;

        _timer += Time.deltaTime;

        while (_spawned < _scaledCount && _timer >= spawnDelay)
        {
            _timer -= spawnDelay;
            FireVirus(state, _spawned);
            _spawned++;
        }

        if (_spawned >= _scaledCount && _timer >= 0.8f)
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

    private void FireVirus(EnemyStateManager state, int index)
    {
        bool               scattered = (index % 2 == 1);
        BulletMovementType movement  = (index % 3 == 0) ? BulletMovementType.Sine : BulletMovementType.Straight;
        Vector3            targetPos = state.player.position + Vector3.up * 1.0f;

        foreach (Transform sp in ((Boss2StateManager)state).GetAllSpawnPoints())
        {
            Vector3 spawnPos = sp.position;
            Vector3 toPlayer = targetPos - spawnPos;
            Vector3 dir      = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : state.transform.forward;

            if (scattered)
                dir = Quaternion.Euler(Random.Range(-6f, 6f), Random.Range(-20f, 20f), 0f) * dir;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            if (right.sqrMagnitude < 0.001f) right = Vector3.right;
            dir = Quaternion.AngleAxis(Random.Range(-verticalSpread, verticalSpread), right) * dir;

            Bullet b = new Bullet
            {
                position        = spawnPos,
                direction       = dir,
                speed           = ((Boss2StateManager)state).ScaleBulletSpeed(bulletSpeed) + Random.Range(-1f, 1.5f),
                damage          = bulletDamage,
                maxLifetime     = lifetime,
                collisionRadius = 0.3f,
                canBeParried    = true,
                destroyOnParry  = true,
                movementType    = movement,
                visualPrefab    = state.bulletData.groundSlamBulletPrefab,
                scale           = 0.6f,
            };

            BulletManager.Instance.SpawnBullet(b);
        }
    }
}
