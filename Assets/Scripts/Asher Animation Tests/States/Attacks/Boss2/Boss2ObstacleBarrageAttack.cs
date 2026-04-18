using UnityEngine;

// Slides physical wall obstacles across the arena while firing tracking bullets.
// Obstacles spawn at the arena edge perpendicular to the player and slide through.
public class Boss2ObstacleBarrageAttack : EnemyBaseState
{
    // Obstacle settings
    private int   obstacleCount       = 4;
    private float obstacleSpeed       = 4.5f;
    private float obstacleLifetime    = 5f;
    private float arenaRadius         = 12f;  // how far from boss to spawn edge
    private float staggerDelay        = 0.6f; // seconds between each obstacle spawn

    // Covering bullet fire between obstacles
    private float bulletFireRate   = 0.35f;
    private float bulletSpeed      = 4f;
    private float bulletDamage     = 10f;
    private float bulletLifetime   = 4f;

    private int   _obstaclesSpawned;
    private float _spawnTimer;
    private float _bulletTimer;
    private bool  _done;
    private float _attackDuration;  // set once all obstacles are out

    public override void EnterState(EnemyStateManager state)
    {
        _obstaclesSpawned = 0;
        _spawnTimer       = staggerDelay; // spawn first one immediately
        _bulletTimer      = 0f;
        _done             = false;
        _attackDuration   = obstacleCount * staggerDelay + obstacleLifetime;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (_done) return;

        float dt = Time.deltaTime;

        // Staggered obstacle spawns
        if (_obstaclesSpawned < obstacleCount)
        {
            _spawnTimer += dt;
            if (_spawnTimer >= staggerDelay)
            {
                _spawnTimer = 0f;
                SpawnObstacle(state, _obstaclesSpawned);
                _obstaclesSpawned++;
            }
        }

        // Bullet harassment while obstacles are live
        _bulletTimer += dt;
        if (_bulletTimer >= bulletFireRate)
        {
            _bulletTimer = 0f;
            FireTrackingBullet(state);
        }

        // End after all obstacles have had time to cross
        _attackDuration -= dt;
        if (_attackDuration <= 0f)
        {
            _done = true;
            ((Boss2StateManager)state).TransitionToNextState();
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;

    private void SpawnObstacle(EnemyStateManager state, int index)
    {
        Boss2StateManager boss = (Boss2StateManager)state;
        if (boss.obstaclePrefab == null) return;

        // Compute a direction perpendicular to the player-boss axis,
        // then alternate which side each obstacle comes from
        Vector3 toPlayer = state.player.position - state.transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.001f) toPlayer = state.transform.forward;
        toPlayer.Normalize();

        // Perpendicular axis in the horizontal plane
        Vector3 perp = new Vector3(-toPlayer.z, 0f, toPlayer.x);

        // Alternate left/right and offset along the forward axis so lanes are staggered
        float side       = (index % 2 == 0) ? 1f : -1f;
        float forwardOff = Mathf.Lerp(-arenaRadius * 0.4f, arenaRadius * 0.4f,
                                      index / Mathf.Max(1f, obstacleCount - 1f));

        Vector3 spawnPos = state.transform.position
                         + perp * (side * arenaRadius)
                         + toPlayer * forwardOff;
        spawnPos.y = state.player.position.y - 1.2f; // ground the obstacle at foot level

        // Rotate so the flat face points in the travel direction
        Quaternion rot = Quaternion.LookRotation(-perp * side);

        GameObject go = Object.Instantiate(boss.obstaclePrefab, spawnPos, rot);
        Boss2Obstacle obs = go.GetComponent<Boss2Obstacle>();
        if (obs != null)
        {
            obs.moveDirection = perp * (-side); // crosses toward the other side
            obs.speed         = obstacleSpeed;
            obs.lifetime      = obstacleLifetime;
        }
    }

    private void FireTrackingBullet(EnemyStateManager state)
    {
        Vector3 toPlayer = state.player.position - state.transform.position;
        Vector3 dir      = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : state.transform.forward;

        // Light spread so the player can't just stand still
        dir = Quaternion.Euler(
            Random.Range(-6f, 6f),
            Random.Range(-12f, 12f),
            0f) * dir;

        Vector3 spawnPos = ((Boss2StateManager)state).GetRandomSpawnPoint();
        spawnPos.y = state.player.position.y + 1.0f;

        Bullet b = new Bullet
        {
            position        = spawnPos,
            direction       = dir.normalized,
            speed           = bulletSpeed,
            damage          = bulletDamage,
            maxLifetime     = bulletLifetime,
            collisionRadius = 0.3f,
            canBeParried    = true,
            destroyOnParry  = true,
            movementType    = BulletMovementType.Straight,
            visualPrefab    = state.bulletData.groundSlamBulletPrefab,
        };

        BulletManager.Instance.SpawnBullet(b);
    }
}
