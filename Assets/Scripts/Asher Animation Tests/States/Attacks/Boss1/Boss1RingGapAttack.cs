/**
 * Boss fires expanding rings of bullets, each ring having one gap the
 * player must position themselves inside to avoid damage.
 * The gap rotates between rings, forcing the player to keep moving.
 */

using UnityEngine;

public class Boss1RingGapAttack : EnemyBaseState
{
    // ===============================
    // RING SETTINGS
    // ===============================
    private int   ringCount          = 4;    // Total rings to fire
    private float timeBetweenRings   = 1.2f; // Gap between rings so player can reposition
    private int   bulletsPerRing     = 20;   // More bullets = smaller gap is more readable
    private float gapSizeDegrees     = 60f;  // Degrees of the opening (player must stand here)
    private float gapRotatePerRing   = 90f;  // How far the gap rotates each ring

    // ===============================
    // BULLET SETTINGS
    // ===============================
    private float bulletSpeed        = 8f;
    private float bulletDamage       = 15f;
    private float bulletLifetime     = 3f;

    // ===============================
    // RUNTIME
    // ===============================
    private int   ringsCompleted     = 0;
    private float ringTimer          = 0f;
    private float currentGapAngle    = 0f;  // Tracks where the gap currently is
    private bool  attackDone         = false;
    private bool animationDone = false;

    public override void EnterState(EnemyStateManager state)
    {
        ringsCompleted = 0;
        ringTimer      = timeBetweenRings; // Fire first ring immediately
        attackDone     = false;

        // Aim gap toward player on the first ring so it's always escapable
        Vector3 toPlayer  = state.player.position - state.transform.position;
        toPlayer.y        = 0f;
        currentGapAngle   = toPlayer.sqrMagnitude > 0.001f
            ? Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg
            : 0f;
        state.animator.SetTrigger("GroundSlam");
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (attackDone)
            return;

        ringTimer += Time.deltaTime;
        if (ringTimer >= timeBetweenRings / 2 && !animationDone)
        {
            state.animator.SetTrigger("GroundSlam");
            animationDone = true;
            Debug.Log("Animation Triggered");
        }
        if (ringTimer >= timeBetweenRings)
        {
            ringTimer = 0f;
            FireRing(state);
            ringsCompleted++;
            animationDone = false;

            // Rotate gap so the player can't just stand still
            currentGapAngle += gapRotatePerRing;

            if (ringsCompleted >= ringCount)
            {
                attackDone = true;
                Boss1StateManager boss = (Boss1StateManager)state;
                boss.TransitionToNextState();
            }
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;

    private void FireRing(EnemyStateManager state)
    {
        float angleStep   = 360f / bulletsPerRing;
        float halfGap     = gapSizeDegrees * 0.5f;
        Vector3 spawnBase = state.transform.position;

        for (int i = 0; i < bulletsPerRing; i++)
        {
            float angle = i * angleStep;

            // Skip bullets that fall inside the gap
            float deltaFromGap = Mathf.Abs(Mathf.DeltaAngle(angle, currentGapAngle));
            if (deltaFromGap <= halfGap)
                continue;

            float rad    = angle * Mathf.Deg2Rad;
            Vector3 dir  = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

            Vector3 spawnPos = spawnBase + dir * 0.8f;
            spawnPos.y       = Random.Range(0.3f, 0.9f);

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
