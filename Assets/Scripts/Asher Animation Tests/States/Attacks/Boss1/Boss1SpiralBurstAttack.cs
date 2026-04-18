/**
 * Boss fires multiple spiral arms of bullets that rotate outward.
 * Each arm is offset by an equal angle. The arms rotate as they fire,
 * creating a pinwheel pattern that the player must dodge between.
 */

using UnityEngine;

public class Boss1SpiralBurstAttack : EnemyBaseState
{
    // ===============================
    // SPIRAL SETTINGS
    // ===============================
    private int   armCount       = 4;      // Number of spiral arms
    private float rotateSpeed    = 120f;   // Degrees per second the pattern rotates
    private float duration       = 4f;     // How long the spiral lasts

    // ===============================
    // BULLET SETTINGS
    // ===============================
    private float fireRate       = 0.06f;  // Seconds between bullet volleys
    private float bulletSpeed    = 9f;
    private float bulletDamage   = 10f;
    private float bulletLifetime = 2.5f;
    private float downwardAngle  = -2f;    // Slight downward tilt so bullets stay near ground

    // ===============================
    // RUNTIME
    // ===============================
    private float currentAngle   = 0f;
    private float durationTimer  = 0f;
    private float fireTimer      = 0f;
    private bool  attackDone     = false;

    public override void EnterState(EnemyStateManager state)
    {
        currentAngle  = state.transform.eulerAngles.y;
        durationTimer = 0f;
        fireTimer     = 0f;
        attackDone    = false;

        ((Boss1StateManager)state).smoothLookAtEnabled = false;
        state.animator.SetBool("Spin", true);
        Debug.Log("SpiralBurstAttack");
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (attackDone)
            return;

        durationTimer += Time.deltaTime;
        fireTimer     += Time.deltaTime;
        currentAngle  += rotateSpeed * Time.deltaTime;

        if (fireTimer >= fireRate)
        {
            fireTimer = 0f;
            FireArms(state);
        }

        if (durationTimer >= duration)
        {
            attackDone = true;
            Boss1StateManager boss = (Boss1StateManager)state;
            boss.smoothLookAtEnabled = true;
            boss.TransitionToNextState();
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;

    private void FireArms(EnemyStateManager state)
    {
        float angleStep = 360f / armCount;

        for (int i = 0; i < armCount; i++)
        {
            float armAngle = (currentAngle + i * angleStep) * Mathf.Deg2Rad;

            Vector3 outwardDir = new Vector3(Mathf.Sin(armAngle), 0f, Mathf.Cos(armAngle));

            // Tilt slightly downward
            Vector3 tiltAxis = Vector3.Cross(outwardDir, Vector3.up);
            Vector3 finalDir = Quaternion.AngleAxis(downwardAngle, tiltAxis) * outwardDir;

            Vector3 spawnPos   = state.transform.position + outwardDir * 0.8f;
            spawnPos.y         = Random.Range(0.3f, 0.8f);

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
                movementType    = BulletMovementType.Straight,
                visualPrefab    = state.bulletData.groundSlamBulletPrefab,
            };

            BulletManager.Instance.SpawnBullet(b);
        }
    }
}
