using UnityEngine;

public class Boss1ChargeAttack : EnemyBaseState
{
    private float chargeSpeed = 10f;
    private float chargeStopDist = 2f;
    private float chargeDuration = 2f;
    private float steerSpeed = 50f;

    private int bulletWaves = 6;
    private int bulletsPerWave = 16;
    private float waveDuration = 0.8f;

    private float chargeTimer = 0f;
    private bool hasSlammed = false;
    private bool attackDone = false;
    private bool firingWaves = false;
    private int wavesFired = 0;
    private float waveTimer = 0f;
    private float waveInterval => waveDuration / bulletWaves;

    private Vector3 slamPosition;
    private Vector3 chargeDir;
    private Transform visualRoot;

    public override void EnterState(EnemyStateManager state)
    {
        chargeTimer = 0f;
        hasSlammed = false;
        attackDone = false;
        firingWaves = false;
        wavesFired = 0;
        waveTimer = 0f;

        visualRoot = state.animator.transform;

        Vector3 toPlayer = state.player.position - state.transform.position;
        toPlayer.y = 0f;
        chargeDir = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : state.transform.forward;

        state.transform.rotation = Quaternion.LookRotation(chargeDir);

        ((Boss1StateManager)state).smoothLookAtEnabled = false;
        state.animator.SetBool("Running", true);

        state.animator.applyRootMotion = false;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (attackDone)
            return;

        if (!hasSlammed)
        {
            Charge(state);
        }
        else
        {
            HandleWaves(state);
        }

        SnapToGround(state);

        if (visualRoot != null)
        {
            Vector3 local = visualRoot.localPosition;
            local.y = 0f;
            visualRoot.localPosition = local;
        }
    }

    private void HandleWaves(EnemyStateManager state)
    {
        if (!firingWaves || wavesFired >= bulletWaves) return;

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

    private void Charge(EnemyStateManager state)
    {
        chargeTimer += Time.deltaTime;

        Vector3 toPlayer = state.player.position - state.transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude > 0.001f)
        {
            float maxTurn = steerSpeed * Mathf.Deg2Rad * Time.deltaTime;
            chargeDir = Vector3.RotateTowards(chargeDir, toPlayer.normalized, maxTurn, 0f);
        }

        state.transform.rotation = Quaternion.LookRotation(chargeDir);

        float distToPlayer = toPlayer.magnitude;
        if (distToPlayer <= chargeStopDist || chargeTimer >= chargeDuration)
        {
            DoSlam(state);
            return;
        }

        Boss1StateManager boss = (Boss1StateManager)state;
        // Clamp step so the boss never overshoots into the player
        float step = Mathf.Min(chargeSpeed * Time.deltaTime, Mathf.Max(0f, distToPlayer - chargeStopDist));
        Vector3 nextPos = state.transform.position + chargeDir * step;

        if (boss.WouldHitWall(state.transform.position, chargeDir, step) ||
            !boss.IsPositionSafe(nextPos) ||
            !HasGroundBelow(nextPos, state.gameObject.layer))
        {
            DoSlam(state);
            return;
        }

        state.transform.position += chargeDir * step;
    }

    private void DoSlam(EnemyStateManager state)
    {
        hasSlammed = true;

        slamPosition = state.transform.position;
        slamPosition.y = 0f;

        state.animator.SetTrigger("GroundSlam");

        firingWaves = true;
        waveTimer = waveInterval;
    }

    private void SpawnBulletWave(EnemyStateManager state)
    {
        float angleStep = 360f / bulletsPerWave;

        for (int i = 0; i < bulletsPerWave; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            Bullet b = new Bullet
            {
                position = slamPosition,
                direction = dir,
                speed = 10f,
                damage = 12f,
                maxLifetime = 4f,
                collisionRadius = 0.3f,
                movementType = BulletMovementType.Straight,
                scale = 2f,
                visualPrefab = state.bulletData.groundSlamBulletPrefab,
            };

            BulletManager.Instance.SpawnBullet(b);
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 20f;

    private void SnapToGround(EnemyStateManager state)
    {
        int mask = ~(1 << state.gameObject.layer);
        Vector3 origin = state.transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 3f, mask, QueryTriggerInteraction.Ignore))
        {
            Vector3 pos = state.transform.position;
            pos.y = hit.point.y;
            state.transform.position = pos;
        }
    }

    private bool HasGroundBelow(Vector3 pos, int selfLayer)
    {
        int mask = ~(1 << selfLayer);
        Vector3 origin = pos + Vector3.up * 0.5f;
        return Physics.Raycast(origin, Vector3.down, 3f, mask, QueryTriggerInteraction.Ignore);
    }
}