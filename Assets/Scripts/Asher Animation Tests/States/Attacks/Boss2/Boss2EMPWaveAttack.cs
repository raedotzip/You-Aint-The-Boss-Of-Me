using UnityEngine;

// Expanding rings of bullets with a rotating gap — player must dodge or stand in the gap
// Mirrors Boss1RingGapAttack but tuned for the computer boss
public class Boss2EMPWaveAttack : EnemyBaseState
{
    public int   ringCount        = 3;
    public float timeBetweenRings = 1.5f;
    public int   bulletsPerRing   = 18;
    public float gapSizeDegrees   = 60f;
    public float gapRotatePerRing = 120f;  // gap shifts each ring so player has to move
    public float bulletSpeed      = 7f;
    public float damage           = 12f;
    public float lifetime         = 3f;

    private int   _ringsFired;
    private float _timer;
    private float _gapAngle;

    public override void EnterState(EnemyStateManager state)
    {
        _ringsFired = 0;
        _timer      = timeBetweenRings; // fire first ring immediately
        // Gap starts pointing at the player so the first ring is always dodgeable
        Vector3 toPlayer = state.player.position - state.transform.position;
        _gapAngle = Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        _timer += Time.deltaTime;

        if (_ringsFired < ringCount && _timer >= timeBetweenRings)
        {
            _timer = 0f;
            FireRing(state);
            _gapAngle  += gapRotatePerRing;
            _ringsFired++;
        }

        if (_ringsFired >= ringCount && _timer >= lifetime)
            ((Boss2StateManager)state).TransitionToNextState();
    }

    private void FireRing(EnemyStateManager state)
    {
        float angleStep  = 360f / bulletsPerRing;
        float halfGap    = gapSizeDegrees / 2f;

        for (int i = 0; i < bulletsPerRing; i++)
        {
            float angle = i * angleStep;
            float diff  = Mathf.DeltaAngle(angle, _gapAngle);
            if (Mathf.Abs(diff) <= halfGap) continue;

            float rad = angle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

            // TODO: replace with your AttackData — fill in bulletPrefab, collisionRadius, etc.
            // state.FireBullet(dir, yourAttackData);
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;
}
