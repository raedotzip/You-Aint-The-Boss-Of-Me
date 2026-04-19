/**
 * Boss charges at the player, slams the ground sending out a shockwave
 * and spawning bullets in every direction
 */

using UnityEngine;

public class Boss1ChargeAttack : EnemyBaseState
{
    // ===============================
    // CHARGE SETTINGS
    // ===============================
    private float chargeSpeed    = 60f;
    private float chargeStopDist = 2f;
    private float chargeDuration = 3f;

    // How many degrees per second the boss can steer mid-charge.
    private float steerSpeed     = 50f;

    // ===============================
    // BULLET SETTINGS
    // ===============================
    private int   bulletWaves    = 6;
    private int   bulletsPerWave = 16;
    private float waveDuration   = 0.8f;
    private float bulletSpeed    = 10f;
    private float bulletDamage   = 15f;
    private float bulletLifetime = 4f;
    private float playerHeight   = 1.8f;

    // ===============================
    // RUNTIME
    // ===============================
    private float   chargeTimer   = 0f;
    private bool    hasSlammed    = false;
    private bool    attackDone    = false;
    private bool    firingWaves   = false;
    private int     wavesFired    = 0;
    private float   waveTimer     = 0f;
    private float   waveInterval  => waveDuration / bulletWaves;
    private Vector3 slamPosition;

    // Direction locked at charge start — only steers slowly toward player
    private Vector3 chargeDir;

    private float trailFireRate   = 0.08f;
    private float trailTimer      = 0f;
    private int   trailBulletsPerShot = 3;
    private float trailSpeed      = 3f;
    private float trailLifetime   = 0.8f;
    private float trailArcHeight  = 2f;

    // Player knockback on direct hit
    private float knockbackSpeed    = 10f;
    private float knockbackDuration = 0.35f;
    private float knockbackRadius   = 2.2f; // how close the boss must be to the player to land a hit
    private bool  _hasKnockedPlayer = false;

    public override void EnterState(EnemyStateManager state)
    {
        chargeTimer      = 0f;
        hasSlammed       = false;
        attackDone       = false;
        firingWaves      = false;
        wavesFired       = 0;
        waveTimer        = 0f;
        trailTimer       = 0f;
        _hasKnockedPlayer = false;

        // Lock charge direction toward player's current position at attack start
        Vector3 toPlayer = state.player.position - state.transform.position;
        toPlayer.y       = 0f;
        chargeDir        = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : state.transform.forward;

        state.transform.rotation = Quaternion.LookRotation(chargeDir);

        ((Boss1StateManager)state).smoothLookAtEnabled = false;
        state.animator.SetBool("Running", true);
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (attackDone)
            return;

        if (!hasSlammed)
        {
            Charge(state);
            return;
        }

        // ---- Staggered bullet waves after slam ----
        if (firingWaves && wavesFired < bulletWaves)
        {
            waveTimer += Time.deltaTime;

            if (waveTimer >= waveInterval)
            {
                waveTimer = 0f;
                SpawnBulletWave(state);
                wavesFired++;

                if (wavesFired >= bulletWaves)
                {
                    attackDone = true;
                    Boss1StateManager boss = (Boss1StateManager)state;
                    boss.smoothLookAtEnabled = true;
                    boss.TransitionToNextState();
                }
            }
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0;

    // ===============================
    // CHARGE
    // ===============================
    private void Charge(EnemyStateManager state)
    {
        chargeTimer += Time.deltaTime;
        trailTimer  += Time.deltaTime;

        // Slowly steer chargeDir toward the player — limited by steerSpeed so
        // the player must dash sideways to escape, not just walk out of the way.
        Vector3 toPlayer = state.player.position - state.transform.position;
        toPlayer.y       = 0f;

        if (toPlayer.sqrMagnitude > 0.001f)
        {
            float maxTurn = steerSpeed * Time.deltaTime;
            chargeDir = Vector3.RotateTowards(chargeDir, toPlayer.normalized, maxTurn * Mathf.Deg2Rad, 0f);
        }

        // Face the current charge direction
        state.transform.rotation = Quaternion.LookRotation(chargeDir);

        // Slam when close to player OR time runs out
        float distToPlayer = toPlayer.magnitude;
        if (distToPlayer <= chargeStopDist || chargeTimer >= chargeDuration)
        {
            DoSlam(state);
            return;
        }

        // Slam early rather than charging through a wall or into the lava pit
        Boss1StateManager boss1 = (Boss1StateManager)state;
        float step = chargeSpeed * Time.deltaTime;
        if (boss1.WouldHitWall(state.transform.position, chargeDir, step) ||
            !boss1.IsPositionSafe(state.transform.position + chargeDir * step))
        {
            DoSlam(state);
            return;
        }

        if (state.rb != null)
            state.rb.MovePosition(state.transform.position + chargeDir * step);
        else
            state.transform.position += chargeDir * step;

        // Knock the player back if the boss body overlaps them during the charge
        if (!_hasKnockedPlayer &&
            Vector3.Distance(state.transform.position, state.player.position) <= knockbackRadius)
        {
            PlayerMovement pm = state.player.GetComponentInParent<PlayerMovement>();
            if (pm == null) pm = state.player.GetComponentInChildren<PlayerMovement>();
            if (pm != null)
            {
                Vector3 knockDir = (state.player.position - state.transform.position);
                knockDir.y = 0f;
                if (knockDir.sqrMagnitude < 0.001f) knockDir = chargeDir;
                pm.TakeKnockback(knockDir.normalized, knockbackSpeed, knockbackDuration);
                _hasKnockedPlayer = true;
            }
        }

        // Fire trail bullets while charging
        if (trailTimer >= trailFireRate)
        {
            trailTimer = 0f;
            SpawnTrailBullets(state, chargeDir);
        }
    }

    // ===============================
    // SLAM
    // ===============================
    private void DoSlam(EnemyStateManager state)
    {
        hasSlammed     = true;
        slamPosition   = state.transform.position;
        slamPosition.y = 0f;

        state.animator.SetTrigger("GroundSlam");

        firingWaves = true;
        waveTimer   = waveInterval; // Fire first wave immediately
    }

    // ===============================
    // BULLET WAVE
    // ===============================
    private void SpawnBulletWave(EnemyStateManager state)
    {
        float waveAngleOffset = (wavesFired / (float)bulletWaves) * 360f / bulletsPerWave;
        float angleStep       = 360f / bulletsPerWave;

        for (int i = 0; i < bulletsPerWave; i++)
        {
            float angle = (i * angleStep + waveAngleOffset) * Mathf.Deg2Rad;

            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            float spawnY      = Random.Range(0.5f, playerHeight);
            float spawnJitter = Random.Range(0f, 0.8f);

            Vector3 spawnPos  = slamPosition + dir * spawnJitter;
            spawnPos.y        = spawnY;

            float upwardBias  = spawnY / playerHeight * 0.4f;
            Vector3 finalDir  = (dir + Vector3.up * upwardBias).normalized;

            Bullet b = new Bullet
            {
                position        = spawnPos,
                direction       = finalDir,
                speed           = bulletSpeed + Random.Range(-1f, 2f),
                damage          = bulletDamage,
                maxLifetime     = bulletLifetime,
                collisionRadius = 0.3f,
                canBeParried    = true,
                destroyOnParry  = true,
                movementType    = BulletMovementType.Straight,
                scale           = 2f,
                visualPrefab    = state.bulletData.groundSlamBulletPrefab,
            };

            BulletManager.Instance.SpawnBullet(b);
        }
    }

    private void SpawnTrailBullets(EnemyStateManager state, Vector3 chargeDir)
    {
        Vector3 bossPos = state.transform.position;
        Vector3 right   = Vector3.Cross(Vector3.up, chargeDir).normalized;

        // Three directions — behind left, directly behind, behind right
        Vector3[] trailDirs = new Vector3[]
        {
            (-chargeDir + right  * 0.6f).normalized,  // behind-left
            -chargeDir,                                 // directly behind
            (-chargeDir - right  * 0.6f).normalized,  // behind-right
        };

        foreach (Vector3 dir in trailDirs)
        {
            // Randomize spawn point slightly so they don't all come from one spot
            Vector3 spawnPos = bossPos + new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(0f, 0.8f),     // Vary height slightly on the boss body
                Random.Range(-0.3f, 0.3f)
            );

            // Angle upward slightly so they arc before dropping
            float upward     = Random.Range(0.3f, 0.6f);
            Vector3 finalDir = (dir + Vector3.up * upward).normalized;

            Bullet b = new Bullet
            {
                position        = spawnPos,
                direction       = finalDir,
                speed           = trailSpeed + Random.Range(-0.5f, 1f),
                damage          = 0f,
                maxLifetime     = trailLifetime,
                collisionRadius = 0.1f,
                canBeParried    = false,
                destroyOnParry  = false,
                movementType    = BulletMovementType.Arc,
                scale           = 2f,
                visualPrefab    = state.bulletData.groundSlamBulletPrefab,
            };

            BulletManager.Instance.SpawnBullet(b);
        }
    }
}