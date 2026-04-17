using UnityEngine;

// Fires bullets aimed at the player — alternates direct shots with wavy sine bullets
public class Boss2VirusSwarmAttack : EnemyBaseState
{
    private int   virusCount   = 12;
    private float spawnDelay   = 0.15f;
    private float bulletSpeed  = 3.5f;
    private float bulletDamage = 10f;
    private float lifetime     = 5f;

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

    private void FireVirus(EnemyStateManager state, int index)
    {
        Vector3 toPlayer = state.player.position - state.transform.position;
        Vector3 dir = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : state.transform.forward;

        // Every other bullet fans out with spread
        if (index % 2 == 1)
        {
            dir = Quaternion.Euler(
                Random.Range(-8f, 8f),
                Random.Range(-18f, 18f),
                0f) * dir;
        }

        Vector3 spawnPos = ((Boss2StateManager)state).GetRandomSpawnPoint();
        spawnPos.y = state.player.position.y + 1.0f + Random.Range(-0.15f, 0.15f);

        BulletMovementType movement = (index % 3 == 0) ? BulletMovementType.Sine : BulletMovementType.Straight;

        Bullet b = new Bullet
        {
            position        = spawnPos,
            direction       = dir.normalized,
            speed           = bulletSpeed + Random.Range(-1f, 1.5f),
            damage          = bulletDamage,
            maxLifetime     = lifetime,
            collisionRadius = 0.3f,
            canBeParried    = true,
            destroyOnParry  = true,
            movementType    = movement,
            visualPrefab    = state.bulletData.groundSlamBulletPrefab,
        };

        BulletManager.Instance.SpawnBullet(b);
    }
}
