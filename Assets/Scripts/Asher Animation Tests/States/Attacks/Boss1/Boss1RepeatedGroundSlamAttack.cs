/**
 * Boss slams the ground, causing bullets to erupt from the boss position
 * and arc through the air landing randomly around the player
 */

using UnityEngine;

public class Boss1RepeatedGroundSlamBulletAttack : EnemyBaseState
{
    private int   slamCount        = 4;
    private float timeBetweenSlams = 1.0f;

    private int   bulletsPerSlam   = 100;
    private float bulletDamage     = 10f;
    private float landingRadius    = 5f;   // How far from player bullets can land
    private float landingRadiusMin = 1f;   // Minimum distance from player so they aren't instant hits
    private float arcDuration      = 1.2f; // How long each bullet takes to travel

    private int   slamsCompleted   = 0;
    private float slamTimer        = 0f;
    private bool  waitingForNext   = false;

    public override void EnterState(EnemyStateManager state)
    {
        slamsCompleted = 0;
        slamTimer      = 0f;
        waitingForNext = false;

        DoSlam(state);
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (slamsCompleted >= slamCount)
        {
            // All slams done — jump at the player
                Boss1StateManager boss = (Boss1StateManager)state;
                boss.TransitionToNextState();
            return;
        }

        if (waitingForNext)
        {
            slamTimer += Time.deltaTime;

            if (slamTimer >= timeBetweenSlams)
            {
                slamTimer      = 0f;
                waitingForNext = false;
                DoSlam(state);
            }
        }
    }

    public override float OnBossHurt(EnemyStateManager state)
    {
        return 0;
    }

    private void DoSlam(EnemyStateManager state)
    {
        slamsCompleted++;
        waitingForNext = true;

        //state.animator.SetTrigger("GroundSlam");

        SpawnBulletBurst(state);
    }

    private void SpawnBulletBurst(EnemyStateManager state)
    {
        Vector3 bossPos   = state.transform.position;
        Vector3 playerPos = state.player.position;

        for (int i = 0; i < bulletsPerSlam; i++)
        {
            // Pick a random landing spot around the player
            float randomAngle  = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float randomRadius = Random.Range(landingRadiusMin, landingRadius);

            Vector3 landingPos = new Vector3(
                playerPos.x + Mathf.Cos(randomAngle) * randomRadius,
                0f,
                playerPos.z + Mathf.Sin(randomAngle) * randomRadius
            );

            SpawnArcBullet(bossPos, landingPos, state);
        }
    }

    private void SpawnArcBullet(Vector3 from, Vector3 to, EnemyStateManager state)
    {
        // Calculate the velocity needed to arc from boss to landing spot
        // using projectile motion: v = displacement / time
        Vector3 displacement = to - from;

        // Horizontal velocity covers the XZ distance in arcDuration seconds
        Vector3 horizontalVel = new Vector3(
            displacement.x / arcDuration,
            0f,
            displacement.z / arcDuration
        );

        // Vertical velocity must overcome gravity and reach the landing spot Y in arcDuration
        // using: y = v0*t - 0.5*g*t^2 → v0 = (y + 0.5*g*t^2) / t
        float gravity     = 18f;
        float verticalVel = (displacement.y + 0.5f * gravity * arcDuration * arcDuration) / arcDuration;

        Vector3 initialVelocity = new Vector3(horizontalVel.x, verticalVel, horizontalVel.z);

        Bullet b = new Bullet
        {
            position        = from,
            direction       = initialVelocity.normalized,
            speed           = initialVelocity.magnitude,
            damage          = bulletDamage,
            maxLifetime     = arcDuration + 0.5f, // small buffer so it doesn't vanish mid-air
            collisionRadius = 0.3f,
            canBeParried    = true,
            destroyOnParry  = false,
            movementType    = BulletMovementType.Arc,
            visualPrefab    = state.bulletData.groundSlamBulletPrefab,
        };

        BulletManager.Instance.SpawnBullet(b);
    }
}