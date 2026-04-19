using UnityEngine;

// Three laser arms at 120-degree intervals spin around the boss.
// Player must time their dodges to step between the rotating beams.
public class Boss3RotatingLasers : EnemyBaseState
{
    private int   _armCount      = 3;
    private float _rotateSpeed   = 100f;   // degrees per second
    private float _duration      = 4f;
    private float _fireRate      = 0.12f;  // seconds between bullet salvos per arm
    private float _bulletSpeed   = 9f;
    private float _bulletDamage  = 10f;
    private float _bulletLifetime = 2.5f;
    private float _bulletScale   = 2.2f;   // large beams for visual clarity

    private float _angle;
    private float _elapsed;
    private float _fireTimer;
    private bool  _done;

    public override void EnterState(EnemyStateManager state)
    {
        // Start arms pointing toward the player
        Vector3 toPlayer = state.player.position - state.transform.position;
        toPlayer.y = 0f;
        _angle     = toPlayer.sqrMagnitude > 0.001f
            ? Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg
            : 0f;

        _elapsed   = 0f;
        _fireTimer = 0f;
        _done      = false;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (_done) return;

        float dt = Time.deltaTime;
        _elapsed   += dt;
        _angle     += _rotateSpeed * dt;
        _fireTimer += dt;

        if (_fireTimer >= _fireRate)
        {
            _fireTimer = 0f;
            FireArms(state);
        }

        if (_elapsed >= _duration)
        {
            _done = true;
            ((Boss3StateManager)state).TransitionToNextState();
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;

    private void FireArms(EnemyStateManager state)
    {
        float armSpacing = 360f / _armCount;

        for (int i = 0; i < _armCount; i++)
        {
            float   armAngle = (_angle + armSpacing * i) * Mathf.Deg2Rad;
            Vector3 dir      = new Vector3(Mathf.Sin(armAngle), 0f, Mathf.Cos(armAngle));

            // Slight downward angle so beams hit player standing on the floor
            dir.y = -0.15f;
            dir   = dir.normalized;

            Bullet b = new Bullet
            {
                position        = state.transform.position,
                direction       = dir,
                speed           = _bulletSpeed,
                damage          = _bulletDamage,
                maxLifetime     = _bulletLifetime,
                collisionRadius = 0.2f,
                canBeParried    = true,
                destroyOnParry  = true,
                movementType    = BulletMovementType.Straight,
                visualPrefab    = state.bulletData.groundSlamBulletPrefab,
                scale           = _bulletScale,
            };

            BulletManager.Instance.SpawnBullet(b);
        }
    }
}
