using UnityEngine;

public class Boss3StateManager : EnemyStateManager
{
    // ===============================
    // STATES
    // ===============================
    public Boss3IdleState          idleState          = new Boss3IdleState();
    public Boss3LaserBarrageAttack laserBarrageAttack = new Boss3LaserBarrageAttack();
    public Boss3RotatingLasers     rotatingLasers     = new Boss3RotatingLasers();
    public Boss3TelepathicThrow    telepathicThrow    = new Boss3TelepathicThrow();
    public Boss3DebrisStorm        debrisStorm        = new Boss3DebrisStorm();
    public Boss3ForceFieldAttack   forceFieldAttack   = new Boss3ForceFieldAttack();
    public Boss3PulseLaser         pulseLaser         = new Boss3PulseLaser();
    public Boss3WallSummon         wallSummon         = new Boss3WallSummon();

    private Boss3TiredState tiredState = new Boss3TiredState();

    // ===============================
    // HOVER
    // ===============================
    [Header("Hover")]
    [Tooltip("How high the boss floats above the floor beneath it")]
    public float hoverHeightAboveFloor = 4f;
    [Tooltip("How far up and down the boss bobs")]
    public float hoverBobAmplitude     = 0.4f;
    [Tooltip("Bobs per second")]
    public float hoverBobSpeed         = 1.2f;
    [Tooltip("How quickly the boss reaches the target hover height")]
    public float hoverSmoothSpeed      = 3f;
    [Tooltip("Minimum height above floor even if raycast fails")]
    public float minAbsoluteHeight     = 1.5f;

    // ===============================
    // WALL / FLOOR SAFETY
    // ===============================
    [Header("Wall / Floor Safety")]
    [Tooltip("Layer mask for walls and arena boundaries")]
    public LayerMask wallLayer;
    [Tooltip("Radius used for wall overlap checks")]
    public float bossRadius       = 1.2f;
    [Tooltip("How hard the boss is pushed away from walls each frame")]
    public float wallPushStrength = 5f;
    [Tooltip("Downward raycast distance to locate the floor")]
    public float floorRayDistance = 20f;

    // ===============================
    // PASSIVE FIRE
    // ===============================
    [Header("Passive Fire")]
    [Tooltip("Fires a single bullet at the player this often (seconds) regardless of attack state")]
    public float passiveFireRate   = 2.5f;
    [Tooltip("Damage of the constant passive shots")]
    public float passiveFireDamage = 6f;

    // ===============================
    // RANGE
    // ===============================
    [Header("Range Thresholds")]
    public float closeRange = 8f;
    public float farRange   = 18f;

    // ===============================
    // ATTACK WEIGHTS
    // ===============================
    [Header("Close Range Attack Weights")]
    [Range(0, 10)] public int closeWeight_LaserBarrage = 3;
    [Range(0, 10)] public int closeWeight_Rotating     = 2;
    [Range(0, 10)] public int closeWeight_Throw        = 4;
    [Range(0, 10)] public int closeWeight_Debris       = 2;
    [Range(0, 10)] public int closeWeight_ForceField   = 3;
    [Range(0, 10)] public int closeWeight_PulseLaser   = 2;
    [Range(0, 10)] public int closeWeight_WallSummon   = 1;

    [Header("Mid Range Attack Weights")]
    [Range(0, 10)] public int midWeight_LaserBarrage   = 4;
    [Range(0, 10)] public int midWeight_Rotating       = 3;
    [Range(0, 10)] public int midWeight_Throw          = 3;
    [Range(0, 10)] public int midWeight_Debris         = 3;
    [Range(0, 10)] public int midWeight_ForceField     = 2;
    [Range(0, 10)] public int midWeight_PulseLaser     = 3;
    [Range(0, 10)] public int midWeight_WallSummon     = 2;

    [Header("Far Range Attack Weights")]
    [Range(0, 10)] public int farWeight_LaserBarrage   = 4;
    [Range(0, 10)] public int farWeight_Rotating       = 4;
    [Range(0, 10)] public int farWeight_Throw          = 2;
    [Range(0, 10)] public int farWeight_Debris         = 4;
    [Range(0, 10)] public int farWeight_ForceField     = 2;
    [Range(0, 10)] public int farWeight_PulseLaser     = 4;
    [Range(0, 10)] public int farWeight_WallSummon     = 2;

    // ===============================
    // TIRED
    // ===============================
    [Header("Tired Settings")]
    public int   attacksBeforeTired = 4;
    public float tiredDuration      = 2f;

    // ===============================
    // LOOK AT
    // ===============================
    [Header("Look At Player")]
    public float lookAtSpeed = 2f;

    // ===============================
    // SWORD HIT DETECTION
    // ===============================
    [Header("Sword Hit Detection")]
    public float hitRadius     = 1.0f;
    public float minSwordSpeed = 0.8f;

    // ===============================
    // RUNTIME
    // ===============================
    [HideInInspector] public int   attackCounter = 0;
    [HideInInspector] public float health        = 150f;
    [HideInInspector] public float maxHealth     = 150f;
    public HealthBarUI bossHealthBar;

    private Sword  _sword;
    private float  _hitCooldown;
    private float  _bobTimer;
    private float  _passiveFireTimer;

    public override void Start()
    {
        health   = maxHealth;
        animator = GetComponent<Animator>();
        rb       = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.useGravity  = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        if (player == null)
            player = GameObject.FindWithTag("Player").transform;

        _sword = FindObjectOfType<Sword>();

        SwitchState(idleState);
    }

    public override void Update()
    {
        UpdateHover();
        PushAwayFromWalls();

        if (player != null)
        {
            Vector3 toPlayer = player.position - transform.position;
            if (toPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion target = Quaternion.LookRotation(toPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, lookAtSpeed * Time.deltaTime);
            }
        }

        PassiveFire();

        currentState.UpdateState(this);
    }

    void FixedUpdate()
    {
        if (_sword == null) return;
        if (_hitCooldown > 0f) { _hitCooldown -= Time.fixedDeltaTime; return; }

        Transform bladeBase = _sword.bladeBase;
        Transform bladeTip  = _sword.bladeTip;
        if (bladeBase == null || bladeTip == null) return;

        float dist = DistanceToSegment(transform.position, bladeBase.position, bladeTip.position);
        if (dist < hitRadius && _sword.Velocity.magnitude > minSwordSpeed)
        {
            float swingT     = Mathf.InverseLerp(_sword.minSwingDistance, _sword.maxSwingDistance, dist);
            float multiplier = Mathf.Lerp(_sword.minDamageMultiplier, _sword.maxDamageMultiplier, swingT);
            TakeDamage(_sword.damageAmount * multiplier);
            _hitCooldown = 0.3f;
        }
    }

    static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / ab.sqrMagnitude);
        return Vector3.Distance(point, a + t * ab);
    }

    // ===============================
    // HOVER WITH FLOOR DETECTION
    // ===============================
    void UpdateHover()
    {
        _bobTimer += Time.deltaTime * hoverBobSpeed;

        // Raycast downward to find the actual floor under the boss
        float floorY = 0f;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, floorRayDistance, ~0, QueryTriggerInteraction.Ignore))
            floorY = hit.point.y;

        float targetY = Mathf.Max(
            floorY + hoverHeightAboveFloor + Mathf.Sin(_bobTimer) * hoverBobAmplitude,
            floorY + minAbsoluteHeight);

        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * hoverSmoothSpeed);
        transform.position = pos;
    }

    // ===============================
    // WALL SAFETY
    // ===============================
    void PushAwayFromWalls()
    {
        if (wallLayer == 0) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, bossRadius, wallLayer);
        foreach (Collider col in hits)
        {
            // Push the boss away from the wall's closest surface point
            Vector3 closest  = col.ClosestPoint(transform.position);
            Vector3 pushDir  = transform.position - closest;
            if (pushDir.sqrMagnitude < 0.0001f) pushDir = Vector3.up;
            pushDir.Normalize();

            transform.position += pushDir * wallPushStrength * Time.deltaTime;
        }
    }

    // ===============================
    // PASSIVE FIRE
    // ===============================
    void PassiveFire()
    {
        if (player == null || bulletData == null) return;

        _passiveFireTimer += Time.deltaTime;
        if (_passiveFireTimer < passiveFireRate) return;
        _passiveFireTimer = 0f;

        Vector3 toPlayer = (player.position - transform.position).normalized;

        Bullet b = new Bullet
        {
            position        = transform.position,
            direction       = toPlayer,
            speed           = 10f,
            damage          = passiveFireDamage,
            maxLifetime     = 4f,
            collisionRadius = 0.18f,
            canBeParried    = true,
            destroyOnParry  = true,
            movementType    = BulletMovementType.Straight,
            visualPrefab    = bulletData.groundSlamBulletPrefab,
        };

        BulletManager.Instance.SpawnBullet(b);
    }

    // ===============================
    // DAMAGE
    // ===============================
    public void TakeDamage(float amount)
    {
        health = Mathf.Max(0f, health - amount);
        bossHealthBar?.UpdateHealthPercentage(health, maxHealth);

        if (health <= 0f)
        {
            HUDManager.Instance?.StopTimer();
            MenuController.Instance?.AdvanceToNextBoss(3);
        }
    }

    // ===============================
    // STATE TRANSITIONS
    // ===============================
    public void TransitionToNextState()
    {
        if (attackCounter >= attacksBeforeTired)
        {
            attackCounter = 0;
            SwitchState(tiredState);
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        attackCounter++;
        SwitchState(ChooseAttack(dist));
    }

    private EnemyBaseState ChooseAttack(float dist)
    {
        if (dist <= closeRange)
            return PickWeighted(new (EnemyBaseState, int)[]
            {
                (laserBarrageAttack, closeWeight_LaserBarrage),
                (rotatingLasers,     closeWeight_Rotating),
                (telepathicThrow,    closeWeight_Throw),
                (debrisStorm,        closeWeight_Debris),
                (forceFieldAttack,   closeWeight_ForceField),
                (pulseLaser,         closeWeight_PulseLaser),
                (wallSummon,         closeWeight_WallSummon),
            });

        if (dist >= farRange)
            return PickWeighted(new (EnemyBaseState, int)[]
            {
                (laserBarrageAttack, farWeight_LaserBarrage),
                (rotatingLasers,     farWeight_Rotating),
                (telepathicThrow,    farWeight_Throw),
                (debrisStorm,        farWeight_Debris),
                (forceFieldAttack,   farWeight_ForceField),
                (pulseLaser,         farWeight_PulseLaser),
                (wallSummon,         farWeight_WallSummon),
            });

        return PickWeighted(new (EnemyBaseState, int)[]
        {
            (laserBarrageAttack, midWeight_LaserBarrage),
            (rotatingLasers,     midWeight_Rotating),
            (telepathicThrow,    midWeight_Throw),
            (debrisStorm,        midWeight_Debris),
            (forceFieldAttack,   midWeight_ForceField),
            (pulseLaser,         midWeight_PulseLaser),
            (wallSummon,         midWeight_WallSummon),
        });
    }

    private EnemyBaseState PickWeighted((EnemyBaseState state, int weight)[] options)
    {
        int total = 0;
        foreach (var o in options) total += o.weight;
        if (total <= 0) return options[0].state;

        int roll = Random.Range(0, total);
        int cum  = 0;
        foreach (var o in options)
        {
            cum += o.weight;
            if (roll < cum) return o.state;
        }
        return options[0].state;
    }

    public void SwitchState(EnemyBaseState newState)
    {
        if (animator != null) DisableAnimationBools();
        currentState = newState;
        currentState.EnterState(this);
    }

    public void DisableAnimationBools()
    {
        if (animator == null) return;
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.type == AnimatorControllerParameterType.Bool)
                animator.SetBool(p.name, false);
    }
}
