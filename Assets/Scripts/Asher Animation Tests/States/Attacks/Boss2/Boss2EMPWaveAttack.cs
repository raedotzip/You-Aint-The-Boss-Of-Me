using UnityEngine;

// Expanding rings of bullets with a rotating gap — player must dodge or stand in the gap
public class Boss2EMPWaveAttack : EnemyBaseState
{
    private int   ringCount        = 3;
    private float timeBetweenRings = 1.5f;
    private int   bulletsPerRing   = 18;
    private float gapSizeDegrees   = 60f;
    private float gapRotatePerRing = 120f;
    private float bulletSpeed      = 7f;
    private float bulletDamage     = 12f;
    private float bulletLifetime   = 3f;

    private int   _ringsFired;
    private float _timer;
    private float _gapAngle;
    private bool  _done;

    public override void EnterState(EnemyStateManager state)
    {
        _ringsFired = 0;
        _timer      = timeBetweenRings; // fire first ring immediately
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

    private void FireRing(EnemyStateManager state)
    {
        float angleStep  = 360f / bulletsPerRing;
        float halfGap    = gapSizeDegrees * 0.5f;
        Vector3 spawnBase = state.transform.position;

        for (int i = 0; i < bulletsPerRing; i++)
        {
            float angle = i * angleStep;
            if (Mathf.Abs(Mathf.DeltaAngle(angle, _gapAngle)) <= halfGap) continue;

            float rad = angle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

            Vector3 spawnPos = spawnBase + dir * 0.8f;
            spawnPos.y = Random.Range(0.3f, 0.9f);

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
}
