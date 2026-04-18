/**
 * Boss fires rapid bursts of aimed bullets directly at the player,
 * with a brief pause between each burst so the player can dodge.
 * Each bullet in a burst spreads slightly around the aim direction,
 * forcing the player to keep moving.
 */

using UnityEngine;

public class Boss1TargetedBurstAttack : EnemyBaseState
{
    // ===============================
    // BURST SETTINGS
    // ===============================
    private int   burstCount       = 5;    // How many burst volleys to fire
    private int   bulletsPerBurst  = 5;    // Bullets per volley
    private float spreadAngle      = 12f;  // Degrees of spread within a burst
    private float pauseBetweenBursts = 0.7f; // Gap between volleys (player can dodge)

    // ===============================
    // BULLET SETTINGS
    // ===============================
    private float bulletSpeed      = 14f;
    private float bulletDamage     = 12f;
    private float bulletLifetime   = 3f;
    private float inBurstFireRate  = 0.06f; // Time between bullets within one burst

    // ===============================
    // RUNTIME
    // ===============================
    private int   burstsCompleted  = 0;
    private int   bulletsThisBurst = 0;
    private float pauseTimer       = 0f;
    private float inBurstTimer     = 0f;
    private bool  inBurst          = false;
    private bool  attackDone       = false;
    private Vector3 lockedAimDir;  // Direction locked when burst starts so mid-burst corrections don't help

    public override void EnterState(EnemyStateManager state)
    {
        burstsCompleted  = 0;
        bulletsThisBurst = 0;
        pauseTimer       = 0f;
        inBurstTimer     = inBurstFireRate; // Fire immediately on enter
        attackDone       = false;
        inBurst          = false;

        StartBurst(state);
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (attackDone)
            return;

        if (inBurst)
        {
            inBurstTimer += Time.deltaTime;

            if (inBurstTimer >= inBurstFireRate)
            {
                if (bulletsThisBurst == 0) state.animator.SetTrigger("Clap");
                inBurstTimer = 0f;
                FireBullet(state);
                bulletsThisBurst++;
                if (bulletsThisBurst >= bulletsPerBurst)
                {
                    inBurst       = false;
                    pauseTimer    = 0f;
                    burstsCompleted++;

                    if (burstsCompleted >= burstCount)
                    {
                        attackDone = true;
                        Boss1StateManager boss = (Boss1StateManager)state;
                        boss.TransitionToNextState();
                    }
                }
            }
        }
        else
        {
            pauseTimer += Time.deltaTime;

            if (pauseTimer >= pauseBetweenBursts)
                StartBurst(state);
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;

    private void StartBurst(EnemyStateManager state)
    {
        inBurst          = true;
        bulletsThisBurst = 0;
        inBurstTimer     = inBurstFireRate; // Fire first bullet this frame

        // Lock aim direction at burst start — snapshot of where the player is NOW
        Vector3 toPlayer = state.player.position - state.transform.position;
        toPlayer.y       = 0f;
        lockedAimDir     = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : state.transform.forward;
    }

    private void FireBullet(EnemyStateManager state)
    {
        // Spread bullets evenly across spreadAngle, centered on lockedAimDir
        float halfSpread  = spreadAngle * 0.5f;
        float step        = bulletsPerBurst > 1 ? spreadAngle / (bulletsPerBurst - 1) : 0f;
        float angleOffset = -halfSpread + step * bulletsThisBurst;

        Vector3 dir = Quaternion.AngleAxis(angleOffset, Vector3.up) * lockedAimDir;
        dir.y       = -0.05f; // Very slight downward tilt so bullets reach player height
        dir         = dir.normalized;

        Vector3 spawnPos = state.transform.position;
        spawnPos.y       = Random.Range(0.5f, 1.2f);

        Bullet b = new Bullet
        {
            position        = spawnPos,
            direction       = dir,
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
