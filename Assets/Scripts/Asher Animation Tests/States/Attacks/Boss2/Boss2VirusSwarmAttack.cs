using UnityEngine;

// Fires bullets aimed at the player — alternates direct shots with wavy sine bullets
public class Boss2VirusSwarmAttack : EnemyBaseState
{
    private int   virusCount   = 12;
    private float spawnDelay   = 0.15f;
    private float bulletSpeed  = 2.8f;
    private float bulletDamage = 10f;
    private float lifetime     = 8f;

    private float _timer;
    private int   _spawned;
    private bool  _done;

    public override void EnterState(EnemyStateManager state)
    {
        _timer   = spawnDelay;
        _spawned = 0;
        _done    = false;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (_done) return;

        _timer += Time.deltaTime;

        while (_spawned < virusCount && _timer >= spawnDelay)
        {
            _timer -= spawnDelay;
            FireVirus(state, _spawned);
            _spawned++;
        }

        if (_spawned >= virusCount && _timer >= 0.8f)
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
                dir = Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(-18f, 18f), 0f) * dir;

            Bullet b = new Bullet
            {
                position        = spawnPos,
                direction       = dir,
                speed           = bulletSpeed + Random.Range(-1f, 1.5f),
                damage          = bulletDamage,
                maxLifetime     = lifetime,
                collisionRadius = 0.3f,
                canBeParried    = true,
                destroyOnParry  = true,
                movementType    = movement,
                visualPrefab    = state.bulletData.groundSlamBulletPrefab,
                scale           = 0.4f,
            };

            BulletManager.Instance.SpawnBullet(b);
        }
    }
}
