using UnityEngine;

// Expands a large psionic sphere obstacle centered on the boss.
// The sphere damages any player inside it — they must back away to the arena edge.
// After the field collapses the boss immediately fires a laser volley at the player
// to punish them for being cornered at max range.
public class Boss3ForceFieldAttack : EnemyBaseState
{
    private float _sphereRadius    = 9f;
    private float _warningDuration = 1.5f;
    private float _activeDuration  = 4f;
    private float _damage          = 18f;   // per second while inside
    private int   _followUpBullets = 10;
    private float _followUpSpeed   = 12f;

    private bool _spawned;
    private bool _done;
    private float _timer;

    public override void EnterState(EnemyStateManager state)
    {
        _spawned = false;
        _done    = false;
        _timer   = 0f;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (_done) return;

        _timer += Time.deltaTime;

        if (!_spawned)
        {
            _spawned = true;
            SpawnField(state);
        }

        // Wait for the field to expire then fire a follow-up volley
        if (_timer >= _warningDuration + _activeDuration)
        {
            _done = true;
            FireFollowUp(state);
            ((Boss3StateManager)state).TransitionToNextState();
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;

    private void SpawnField(EnemyStateManager state)
    {
        Obstacle o = new Obstacle
        {
            position        = state.transform.position,
            rotation        = Quaternion.identity,

            shapeType       = ObstacleShapeType.Sphere,
            sphereRadius    = _sphereRadius,

            warningDuration = _warningDuration,
            activeDuration  = _activeDuration,

            movementType    = ObstacleMovementType.Stationary,

            visualPrefab    = state.obstacleData.shockwavePrefab,
        };

        ObstacleManager.Instance.SpawnObstacle(o);
    }

    // Spread shotgun blast fired outward after the field drops — punishes the player
    // for being at max range while the field was active.
    private void FireFollowUp(EnemyStateManager state)
    {
        float stepAngle = 360f / _followUpBullets;

        for (int i = 0; i < _followUpBullets; i++)
        {
            float   angle = i * stepAngle * Mathf.Deg2Rad;
            Vector3 dir   = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));

            // Slight vertical spread
            dir.y = Random.Range(-0.2f, 0.2f);
            dir   = dir.normalized;

            Bullet b = new Bullet
            {
                position        = state.transform.position,
                direction       = dir,
                speed           = _followUpSpeed,
                damage          = 10f,
                maxLifetime     = 3f,
                collisionRadius = 0.2f,
                canBeParried    = true,
                destroyOnParry  = true,
                movementType    = BulletMovementType.Straight,
                visualPrefab    = state.bulletData.groundSlamBulletPrefab,
            };

            BulletManager.Instance.SpawnBullet(b);
        }
    }
}
