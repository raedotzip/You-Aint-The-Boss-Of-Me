/**
 * Boss spins in place firing bullets in a spiral pattern
 * that arc downward as they travel outward
 */

using UnityEngine;

public class Boss1SpinAttack : EnemyBaseState
{
    // ===============================
    // SPIN SETTINGS
    // ===============================
    private float spinDuration   = 3f;
    private float spinSpeed      = 360f;
    private float spinAccelerate = 180f;

    // ===============================
    // BULLET SETTINGS
    // ===============================
    private float bulletFireRate = 0.01f;
    private float bulletSpeed    = 12f;
    private float bulletDamage   = 10f;
    private float bulletLifetime = 1.5f;
    private float downwardAngle  = -3.0f;  // Degrees below horizontal — gentle downward slope

    // ===============================
    // RUNTIME
    // ===============================
    private float spinTimer      = 0f;
    private float bulletTimer    = 0f;
    private float currentAngle   = 0f;
    private float currentSpinSpeed = 0f;
    private bool  attackDone     = false;

    public override void EnterState(EnemyStateManager state)
    {
        spinTimer        = 0f;
        bulletTimer      = 0f;
        attackDone       = false;
        currentSpinSpeed = spinSpeed;
        currentAngle     = state.transform.eulerAngles.y;

        ((Boss1StateManager)state).smoothLookAtEnabled = false;
        state.animator.SetBool("Spin", true);
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (attackDone)
        {
            Boss1StateManager boss = (Boss1StateManager)state;
            boss.TransitionToNextState();
            return;
        }

        spinTimer        += Time.deltaTime;
        bulletTimer      += Time.deltaTime;
        currentSpinSpeed += spinAccelerate * Time.deltaTime;

        // Rotate boss
        currentAngle += currentSpinSpeed * Time.deltaTime;
        state.transform.rotation = Quaternion.Euler(0f, currentAngle, 0f);

        if (bulletTimer >= bulletFireRate)
        {
            bulletTimer = 0f;
            SpawnSpinBullet(state);
        }

        if (spinTimer >= spinDuration)
        {
            attackDone = true;
            ((Boss1StateManager)state).smoothLookAtEnabled = true;
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0;

    private void SpawnSpinBullet(EnemyStateManager state)
    {
        Vector3 bossPos = state.transform.position;

        // Outward direction based on current spin angle — purely horizontal
        float   rad        = currentAngle * Mathf.Deg2Rad;
        Vector3 outwardDir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

        // Tilt direction downward by downwardAngle degrees so bullets
        // travel outward AND slightly downward — no gravity needed
        Vector3 finalDir = Quaternion.AngleAxis(downwardAngle, Vector3.Cross(outwardDir, Vector3.up))
                           * outwardDir;

        // Spawn at boss position, offset outward so bullet starts at his edge
        Vector3 spawnPos = bossPos + outwardDir * 0.8f;
        spawnPos.y       = Random.Range(0.3f, 0.7f);

        Bullet b = new Bullet
        {
            position        = spawnPos,
            direction       = finalDir.normalized,
            speed           = bulletSpeed,
            damage          = bulletDamage,
            maxLifetime     = bulletLifetime,
            collisionRadius = 0.3f,
            canBeParried    = true,
            destroyOnParry  = true,
            movementType    = BulletMovementType.Straight, // Straight so they travel far
            visualPrefab    = state.bulletData.groundSlamBulletPrefab,
        };

        BulletManager.Instance.SpawnBullet(b);
    }
}