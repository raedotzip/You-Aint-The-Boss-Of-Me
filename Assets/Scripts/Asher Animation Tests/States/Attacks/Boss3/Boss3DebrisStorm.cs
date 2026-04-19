using UnityEngine;

// Telekinetically hurls debris at the player — slow arc bullets that fall from
// above and to the sides, giving the player a moment to dash out of the way.
public class Boss3DebrisStorm : EnemyBaseState
{
    private int   _debrisCount    = 8;
    private float _spawnRadius    = 4f;   // horizontal spread around player
    private float _spawnHeight    = 6f;   // how high above player the debris spawns
    private float _speed          = 5f;
    private float _damage         = 12f;
    private float _lifetime       = 3f;
    private float _collisionRadius = 0.4f;

    private float _warningDuration = 0.6f;
    private bool  _fired;
    private bool  _done;
    private float _timer;

    public override void EnterState(EnemyStateManager state)
    {
        _fired = false;
        _done  = false;
        _timer = 0f;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (_done) return;

        _timer += Time.deltaTime;

        if (!_fired && _timer >= _warningDuration)
        {
            _fired = true;
            SpawnDebris(state);
        }

        // Wait for debris to have time to land, then move on
        if (_fired && _timer >= _warningDuration + _lifetime * 0.5f)
        {
            _done = true;
            ((Boss3StateManager)state).TransitionToNextState();
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;

    private void SpawnDebris(EnemyStateManager state)
    {
        Vector3 playerPos = state.player.position;

        for (int i = 0; i < _debrisCount; i++)
        {
            // Random spawn position scattered around the player at height
            Vector2 radial   = Random.insideUnitCircle * _spawnRadius;
            Vector3 spawnPos = new Vector3(
                playerPos.x + radial.x,
                playerPos.y + _spawnHeight + Random.Range(0f, 2f),
                playerPos.z + radial.y);

            // Aim toward the player's feet with slight spread
            Vector3 target = playerPos + new Vector3(
                Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f));
            Vector3 dir = (target - spawnPos).normalized;

            Bullet b = new Bullet
            {
                position        = spawnPos,
                direction       = dir,
                speed           = _speed + Random.Range(-1f, 1f),
                damage          = _damage,
                maxLifetime     = _lifetime,
                collisionRadius = _collisionRadius,
                canBeParried    = false,
                destroyOnParry  = false,
                movementType    = BulletMovementType.Arc,
                visualPrefab    = state.bulletData.groundSlamBulletPrefab,
                scale           = 2f,
            };

            BulletManager.Instance.SpawnBullet(b);
        }
    }
}
