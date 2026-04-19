using UnityEngine;

public class Boss1StateManager : EnemyStateManager
{
    // ===============================
    // STATES
    // ===============================
    public Boss1JumpBackMovement               jumpBackState           = new Boss1JumpBackMovement();
    public Boss1JumpSlamAttack                 jumpSlamState           = new Boss1JumpSlamAttack();
    public Boss1RepeatedGroundSlamBulletAttack repeatedBulletSlamState = new Boss1RepeatedGroundSlamBulletAttack();
    public Boss1ChargeAttack                   chargeAttack            = new Boss1ChargeAttack();
    public Boss1SpinAttack                     spinAttack              = new Boss1SpinAttack();
    public Boss1PunchAttack                    punchAttack             = new Boss1PunchAttack();
    public Boss1IdleState                      idleState               = new Boss1IdleState();
    public Boss1PipeAttack                     pipeAttack              = new Boss1PipeAttack();
    public Boss1JumpLeftMovement               jumpLeftState           = new Boss1JumpLeftMovement();
    public Boss1JumpRightMovement              jumpRightState          = new Boss1JumpRightMovement();
    public Boss1SpiralBurstAttack              spiralBurstAttack       = new Boss1SpiralBurstAttack();
    public Boss1TargetedBurstAttack            targetedBurstAttack     = new Boss1TargetedBurstAttack();
    public Boss1RingGapAttack                  ringGapAttack           = new Boss1RingGapAttack();

    private Boss1GroundSlamShockwaveAttack     shockwaveState          = new Boss1GroundSlamShockwaveAttack();
    private Boss1TiredState                    tiredState              = new Boss1TiredState();
    public  Boss1LavaFinisherState             lavaFinisherState       = new Boss1LavaFinisherState();

    // ===============================
    // RANGE SETTINGS
    // ===============================
    [Header("Range Thresholds")]
    public float closeRange   = 8f;
    public float farRange     = 18f;
    [Tooltip("Within this distance the boss will reactively punch or jump away")]
    public float tooCloseRange = 3f;

    // ===============================
    // RETREAT SETTINGS
    // ===============================
    [Header("Retreat Settings")]
    public float retreatRange                    = 5f;
    [Range(0f, 1f)] public float retreatChance   = 0.5f;
    [Range(0f, 1f)] public float sideJumpChance  = 0.7f;
    public float sideJumpDistance                = 6f;

    // ===============================
    // WALL / BOUNDS DETECTION
    // ===============================
    [Header("Wall Detection")]
    [Tooltip("Layer(s) that count as walls the boss cannot pass through")]
    public LayerMask wallLayer;
    [Tooltip("Radius of the sphere used for wall overlap and cast checks")]
    public float bossRadius          = 0.6f;
    [Tooltip("Height offset above the boss origin for wall casts (avoids floor hits)")]
    public float bossCheckHeight     = 1.0f;
    [Tooltip("Extra gap kept between the boss and a detected wall")]
    public float wallSafetyMargin    = 0.5f;
    [Tooltip("Y level below which the boss is considered to have fallen off the map")]
    public float fallThreshold       = -3f;

    // ===============================
    // LAVA PIT
    // ===============================
    [Header("Lava Pit")]
    [Tooltip("Empty GameObject with a BoxCollider sized to the pit — rotate/scale it to match")]
    public Transform lavaPitCenter;
    [Tooltip("Extra buffer inside the pit edge — boss lands this far short of the rim")]
    public float pitSafetyMargin = 1.5f;

    private BoxCollider _pitCollider;

    // ===============================
    // ATTACK WEIGHTS
    // ===============================
    // Close: boss is in your face — big melee, fast pressure
    [Header("Close Range Attack Weights")]
    [Range(0, 10)] public int closeWeight_Punch         = 6; // primary close attack
    [Range(0, 10)] public int closeWeight_JumpSlam      = 5; // jump into the player
    [Range(0, 10)] public int closeWeight_Spin          = 2;
    [Range(0, 10)] public int closeWeight_Charge        = 8; // charges through player
    [Range(0, 10)] public int closeWeight_TargetedBurst = 0;
    [Range(0, 10)] public int closeWeight_RingGap       = 1;

    // Mid: boss closes distance fast with charges and jumps
    [Header("Mid Range Attack Weights")]
    [Range(0, 10)] public int midWeight_Charge          = 8; // primary mid approach
    [Range(0, 10)] public int midWeight_Spin            = 1;
    [Range(0, 10)] public int midWeight_SpiralBurst     = 2;
    [Range(0, 10)] public int midWeight_TargetedBurst   = 2;
    [Range(0, 10)] public int midWeight_RingGap         = 2;

    // Far: rush the player, close in fast
    [Header("Far Range Attack Weights")]
    [Range(0, 10)] public int farWeight_Charge          = 8; // boss rushes in from far
    [Range(0, 10)] public int farWeight_SpiralBurst     = 2;
    [Range(0, 10)] public int farWeight_TargetedBurst   = 2;
    [Range(0, 10)] public int farWeight_RingGap         = 1;

    // ===============================
    // TIRED SETTINGS
    // ===============================
    [Header("Tired Settings")]
    public int   attacksBeforeTired        = 8;    // attacks before going tired (normal phase)
    public int   attacksBeforeTiredEnraged = 14;   // barely rests at low health
    public float tiredDuration             = 2f;   // vulnerable window (normal)
    public float tiredDurationEnraged      = 0.8f; // gets up much faster at ≤20% health

    // ===============================
    // SCALE
    // ===============================
    [Header("Boss Scale")]
    [Tooltip("Uniform scale applied at Start — set above 1 to make the boss larger than the player")]
    public float bossScale = 1.8f;

    // ===============================
    // LOOK-AT SETTINGS
    // ===============================
    [Header("Look At Player")]
    public float lookAtSpeed = 5f;  // Degrees/sec as a Slerp factor — higher = snappier

    // Set to false by states that control their own rotation (Spin, Charge, Punch)
    [HideInInspector] public bool smoothLookAtEnabled = true;

    // ===============================
    // RUNTIME
    // ===============================
    [HideInInspector] public int   attackCounter = 0;
    [HideInInspector] public float health        = 100f;
    [HideInInspector] public float maxHealth     = 100f;
    public HealthBarUI bossHealthBar;

    // True once health hits 0 — prevents re-triggering the finisher
    private bool _finisherTriggered = false;

    // Boss is enraged below 20% health — faster recovery, less frequent rest
    public bool IsEnraged => health / maxHealth <= 0.2f;

    private Vector3 _lastSafePosition;

    public override void Start()
    {
        health = maxHealth;
        animator = GetComponent<Animator>();
        transform.localScale = Vector3.one * bossScale;

        if (player == null)
            player = GameObject.FindWithTag("Player").transform;

        if (lavaPitCenter != null)
            _pitCollider = lavaPitCenter.GetComponent<BoxCollider>();

        _lastSafePosition = transform.position;

        ObstacleManager.Instance.PrewarmObstaclePools(obstacleData);

        SwitchState(idleState);
    }

    public override void Update()
    {
        if (smoothLookAtEnabled && player != null)
        {
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toPlayer);
                transform.rotation  = Quaternion.Slerp(transform.rotation, targetRot, lookAtSpeed * Time.deltaTime);
            }
        }

        currentState.UpdateState(this);

        // Hard safety net — teleport back if the boss somehow leaves the map
        EnforceBounds();
    }

    // Snaps boss back to last safe position if it falls off the map
    public void EnforceBounds()
    {
        if (_finisherTriggered) return;

        Vector3 pos = transform.position;

        if (pos.y < fallThreshold)
        {
            transform.position = _lastSafePosition;
            return;
        }

        // Track the last safe above-floor position for recovery
        if (pos.y >= 0f && !IsInPit(pos, 0f))
            _lastSafePosition = pos;
    }

    // Returns true if a sphere cast from pos in dir would hit a wall within distance
    public bool WouldHitWall(Vector3 pos, Vector3 dir, float distance)
    {
        Vector3 origin = pos + Vector3.up * bossCheckHeight;
        return Physics.SphereCast(origin, bossRadius, dir, out _, distance,
                                  wallLayer, QueryTriggerInteraction.Ignore);
    }

    // override protected void OnCollisionEnter(Collision collision)
    // {
    //     BossHurt();
    // }

    // public override void BossHurt()
    // {
    //     float damage = currentState.OnBossHurt(this);
    //     health = Mathf.Max(0f, health - damage);
    //     if (bossHealthBar != null) bossHealthBar.UpdateHealthPercentage(health, maxHealth);
    // }

    public void TakeDamage(float amount)
    {
        TriggerHitFlash(amount);

        // After the finisher triggers, hits push the boss into the lava instead
        if (_finisherTriggered)
        {
            lavaFinisherState.PushBoss(this);
            return;
        }

        health = Mathf.Max(0f, health - amount);
        if (bossHealthBar != null)
            bossHealthBar.UpdateHealthPercentage(health, maxHealth);

        if (health <= 0f)
        {
            _finisherTriggered = true;
            SwitchState(lavaFinisherState);
        }
    }

    // ===============================
    // STATE DECISION
    // ===============================
    public void TransitionToNextState()
    {
        int tiredThreshold = IsEnraged ? attacksBeforeTiredEnraged : attacksBeforeTired;
        if (attackCounter >= tiredThreshold)
        {
            attackCounter = 0;
            SwitchState(tiredState);
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        // Too close — reactive punch (60%) or jump away (40%)
        if (dist <= tooCloseRange)
        {
            if (Random.value < 0.6f)
            {
                attackCounter++;
                SwitchState(punchAttack);
            }
            else
            {
                SwitchState(ChooseRetreatState());
            }
            return;
        }

        // Retreat check — never retreat into idle
        if (dist <= retreatRange && Random.value <= retreatChance)
        {
            SwitchState(ChooseRetreatState());
            return;
        }

        // Always picks a real attack — idle and tired are never in the pool
        EnemyBaseState next = ChooseAttack(dist);
        attackCounter++;
        SwitchState(next);
    }

    // ===============================
    // RETREAT LOGIC
    // ===============================
    public EnemyBaseState ChooseRetreatState()
    {
        // Occasionally try jumping sideways instead of straight back
        if (Random.value <= sideJumpChance)
        {
            Vector3 toPlayer = (player.position - transform.position).normalized;
            Vector3 right    = Vector3.Cross(Vector3.up, toPlayer).normalized;

            bool canJumpRight = IsPositionSafe(transform.position + right * sideJumpDistance);
            bool canJumpLeft  = IsPositionSafe(transform.position - right * sideJumpDistance);

            if (canJumpRight && canJumpLeft)
            {
                // Both clear — pick randomly
                return Random.value > 0.5f ? (EnemyBaseState)jumpRightState : jumpLeftState;
            }
            else if (canJumpRight)
            {
                return jumpRightState;
            }
            else if (canJumpLeft)
            {
                return jumpLeftState;
            }

            // Neither side clear — fall through to jump back
        }

        // Verify jump back is also safe; if not, stay in place with a ranged attack
        Vector3 backDir  = -(player.position - transform.position).normalized;
        backDir.y = 0f;
        if (!IsPositionSafe(transform.position + backDir * sideJumpDistance))
            return spiralBurstAttack;

        return jumpBackState;
    }

    // Returns true if pos is not inside a wall and not inside the lava pit
    public bool IsPositionSafe(Vector3 pos)
    {
        if (_pitCollider != null && IsInPit(pos, 0f))
            return false;

        // CheckSphere detects overlap with wall geometry at the boss body height
        Vector3 checkPos = pos + Vector3.up * bossCheckHeight;
        if (Physics.CheckSphere(checkPos, bossRadius, wallLayer, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }

    // Returns true if world-pos is inside the BoxCollider expanded by extraMargin
    private bool IsInPit(Vector3 worldPos, float extraMargin)
    {
        if (_pitCollider == null) return false;
        Vector3 local    = lavaPitCenter.InverseTransformPoint(worldPos);
        Vector3 halfSize = _pitCollider.size * 0.5f;
        Vector3 center   = _pitCollider.center;
        return Mathf.Abs(local.x - center.x) < halfSize.x + extraMargin &&
               Mathf.Abs(local.z - center.z) < halfSize.z + extraMargin;
    }

    // Returns a safe landing position — stops at any wall along the jump path
    // and pulls back from the lava pit.
    public Vector3 ClampLandingPosition(Vector3 proposed, Vector3 jumpFrom)
    {
        proposed.y = 0f;
        jumpFrom.y = 0f;

        Vector3 dir  = proposed - jumpFrom;
        float   dist = dir.magnitude;

        if (dist > 0.01f)
        {
            Vector3 normDir = dir / dist;
            Vector3 origin  = jumpFrom + Vector3.up * bossCheckHeight;

            // Stop just before any wall between jump start and intended landing
            if (Physics.SphereCast(origin, bossRadius, normDir, out RaycastHit hit,
                                   dist, wallLayer, QueryTriggerInteraction.Ignore))
            {
                float safeDist = Mathf.Max(0f, hit.distance - bossRadius - wallSafetyMargin);
                proposed       = jumpFrom + normDir * safeDist;
                proposed.y     = 0f;
            }
        }

        // Pull back from rectangular lava pit
        if (_pitCollider != null && IsInPit(proposed, pitSafetyMargin))
        {
            Vector3 center   = _pitCollider.center;
            Vector3 halfSize = _pitCollider.size * 0.5f;
            float   hw       = halfSize.x + pitSafetyMargin;
            float   hd       = halfSize.z + pitSafetyMargin;

            Vector3 localFrom     = lavaPitCenter.InverseTransformPoint(jumpFrom);
            Vector3 localProposed = lavaPitCenter.InverseTransformPoint(proposed);

            float overlapX = hw - Mathf.Abs(localFrom.x - center.x);
            float overlapZ = hd - Mathf.Abs(localFrom.z - center.z);

            if (overlapX < overlapZ)
                localProposed.x = center.x + Mathf.Sign(localFrom.x - center.x) * hw;
            else
                localProposed.z = center.z + Mathf.Sign(localFrom.z - center.z) * hd;

            proposed   = lavaPitCenter.TransformPoint(localProposed);
            proposed.y = 0f;
        }

        return proposed;
    }

    // Legacy — kept so any remaining callers still compile
    public bool IsPositionInBounds(Vector3 pos) => IsPositionSafe(pos);

    // ===============================
    // ATTACK SELECTION
    // ===============================
    private EnemyBaseState ChooseAttack(float dist)
    {
        if (dist <= closeRange)
            return PickWeighted(new (EnemyBaseState, int)[]
            {
                (punchAttack,         closeWeight_Punch),
                (jumpSlamState,       closeWeight_JumpSlam),
                (spinAttack,          closeWeight_Spin),
                (chargeAttack,        closeWeight_Charge),
                (targetedBurstAttack, closeWeight_TargetedBurst),
                (ringGapAttack,       closeWeight_RingGap),
            });

        if (dist >= farRange)
            return PickWeighted(new (EnemyBaseState, int)[]
            {
                (chargeAttack,        farWeight_Charge),
                (spiralBurstAttack,   farWeight_SpiralBurst),
                (targetedBurstAttack, farWeight_TargetedBurst),
                (ringGapAttack,       farWeight_RingGap),
            });

        return PickWeighted(new (EnemyBaseState, int)[]
        {
            (chargeAttack,        midWeight_Charge),
            (spinAttack,          midWeight_Spin),
            (spiralBurstAttack,   midWeight_SpiralBurst),
            (targetedBurstAttack, midWeight_TargetedBurst),
            (ringGapAttack,       midWeight_RingGap),
        });
    }

    // ===============================
    // WEIGHTED RANDOM
    // ===============================
    private EnemyBaseState PickWeighted((EnemyBaseState state, int weight)[] options)
    {
        int total = 0;
        foreach (var o in options)
            total += o.weight;

        if (total <= 0)
            return options[0].state;

        int roll       = Random.Range(0, total);
        int cumulative = 0;

        foreach (var o in options)
        {
            cumulative += o.weight;
            if (roll < cumulative)
                return o.state;
        }

        return options[0].state;
    }

    public void SwitchState(EnemyBaseState newState)
    {
        DisableAnimationBools();
        currentState = newState;
        currentState.EnterState(this);
    }

    public void DisableAnimationBools()
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(param.name, false);
            }
        }
    }
}