using UnityEngine;

// Fires several bullets aimed directly at the player, staggered over time
public class Boss2VirusSwarmAttack : EnemyBaseState
{
    private int   virusCount   = 6;
    private float spawnDelay   = 0.25f; // seconds between each bullet
    private float bulletSpeed  = 6f;
    private float bulletDamage = 10f;
    private float lifetime     = 5f;

    private float _timer;
    private int   _spawned;
    private bool  _done;

    public override void EnterState(EnemyStateManager state)
    {
        _timer   = spawnDelay; // fire first one immediately
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
            FireVirus(state);
            _spawned++;
        }

        if (_spawned >= virusCount && _timer >= 1f)
        {
            _done = true;
            ((Boss2StateManager)state).TransitionToNextState();
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;

    private void FireVirus(EnemyStateManager state)
    {
        Vector3 toPlayer = state.player.position - state.transform.position;
        toPlayer.y = 0f;
        Vector3 dir = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : state.transform.forward;

        // Slight random spread so not all bullets are perfectly overlapping
        dir = Quaternion.Euler(
            Random.Range(-5f, 5f),
            Random.Range(-10f, 10f),
            0f) * dir;

        Vector3 spawnPos = state.transform.position;
        spawnPos.y = Random.Range(0.5f, 1.2f);

        Bullet b = new Bullet
        {
            position        = spawnPos,
            direction       = dir.normalized,
            speed           = bulletSpeed,
            damage          = bulletDamage,
            maxLifetime     = lifetime,
            collisionRadius = 0.3f,
            canBeParried    = true,
            destroyOnParry  = true,
            movementType    = BulletMovementType.Straight,
            visualPrefab    = state.bulletData.groundSlamBulletPrefab,
        };

        BulletManager.Instance.SpawnBullet(b);
    }
}
