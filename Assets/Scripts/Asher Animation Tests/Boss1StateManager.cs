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
    private Boss1MapSeparatorAttack            mapSeparatorState       = new Boss1MapSeparatorAttack();
    private Boss1TiredState                    tiredState              = new Boss1TiredState();

    // ===============================
    // RANGE SETTINGS
    // ===============================
    [Header("Range Thresholds")]
    public float closeRange = 8f;
    public float farRange   = 18f;

    // ===============================
    // RETREAT SETTINGS
    // ===============================
    [Header("Retreat Settings")]
    public float retreatRange                    = 5f;   // tighter — boss commits more
    [Range(0f, 1f)] public float retreatChance   = 0.25f; // less frequent retreating
    [Range(0f, 1f)] public float sideJumpChance  = 0.55f; // mostly jumps sideways to reposition
    public float mapBoundsRadius                 = 28f;
    public float sideJumpDistance                = 6f;

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
    [Range(0, 10)] public int closeWeight_Punch         = 4; // most common — fast and aggressive
    [Range(0, 10)] public int closeWeight_JumpSlam      = 3; // launches into player
    [Range(0, 10)] public int closeWeight_Spin          = 3; // sweeping close attack
    [Range(0, 10)] public int closeWeight_BulletSlam    = 1; // rare at close range
    [Range(0, 10)] public int closeWeight_Charge        = 3; // charges through player
    [Range(0, 10)] public int closeWeight_TargetedBurst = 1; // occasional ranged surprise
    [Range(0, 10)] public int closeWeight_RingGap       = 1;

    // Mid: boss closes distance or uses area attacks
    [Header("Mid Range Attack Weights")]
    [Range(0, 10)] public int midWeight_BulletSlam      = 2;
    [Range(0, 10)] public int midWeight_Charge          = 4; // aggressively closes in
    [Range(0, 10)] public int midWeight_Spin            = 2;
    [Range(0, 10)] public int midWeight_Shockwave       = 2;
    [Range(0, 10)] public int midWeight_MapSeparator    = 0;
    [Range(0, 10)] public int midWeight_SpiralBurst     = 3; // good mid-range pressure
    [Range(0, 10)] public int midWeight_TargetedBurst   = 3; // tracks player well at mid
    [Range(0, 10)] public int midWeight_RingGap         = 2;

    // Far: forces player to move, boss closes in
    [Header("Far Range Attack Weights")]
    [Range(0, 10)] public int farWeight_Shockwave       = 2;
    [Range(0, 10)] public int farWeight_MapSeparator    = 0;
    [Range(0, 10)] public int farWeight_BulletSlam      = 2;
    [Range(0, 10)] public int farWeight_Charge          = 4; // boss rushes in from far
    [Range(0, 10)] public int farWeight_SpiralBurst     = 3;
    [Range(0, 10)] public int farWeight_TargetedBurst   = 4; // punishes staying far away
    [Range(0, 10)] public int farWeight_RingGap         = 2;

    // ===============================
    // TIRED SETTINGS
    // ===============================
    [Header("Tired Settings")]
    public int   attacksBeforeTired = 5;  // more attacks before resting
    public float tiredDuration      = 2f; // shorter rest window

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

    public override void Start()
    {
        health = maxHealth;
        animator = GetComponent<Animator>();

        if (player == null)
            player = GameObject.FindWithTag("Player").transform;

        if (lavaPitCenter != null)
            _pitCollider = lavaPitCenter.GetComponent<BoxCollider>();

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
        health = Mathf.Max(0f, health - amount);
        if (bossHealthBar != null)
            bossHealthBar.UpdateHealthPercentage(health, maxHealth);

        if (health <= 0f)
        {
            MenuController.Instance?.AdvanceToNextBoss(1);
        }
    }

    // ===============================
    // STATE DECISION
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

        return jumpBackState;
    }

    // Check if a position is within the map boundary and not in the lava pit
    public bool IsPositionSafe(Vector3 pos)
    {
        Vector2 flat = new Vector2(pos.x, pos.z);
        if (flat.magnitude > mapBoundsRadius) return false;

        if (_pitCollider != null && IsInPit(pos, 0f))
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

    // Returns a safe landing position — clamps to map bounds and pulls back from the
    // rectangular lava pit so the boss lands just short of the near rim.
    public Vector3 ClampLandingPosition(Vector3 proposed, Vector3 jumpFrom)
    {
        proposed.y = 0f;

        // Clamp to map boundary
        Vector2 flat = new Vector2(proposed.x, proposed.z);
        if (flat.magnitude > mapBoundsRadius)
        {
            flat       = flat.normalized * mapBoundsRadius;
            proposed.x = flat.x;
            proposed.z = flat.y;
        }

        // Pull back from rectangular lava pit
        if (_pitCollider != null && IsInPit(proposed, pitSafetyMargin))
        {
            Vector3 center   = _pitCollider.center;
            Vector3 halfSize = _pitCollider.size * 0.5f;
            float   hw       = halfSize.x + pitSafetyMargin;
            float   hd       = halfSize.z + pitSafetyMargin;

            // Work in pit-local space; snap to the face nearest to jumpFrom
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

    // Legacy name kept so existing callers still compile
    public bool IsPositionInBounds(Vector3 pos)
    {
        Vector2 flat = new Vector2(pos.x, pos.z);
        return flat.magnitude <= mapBoundsRadius;
    }

    // ===============================
    // ATTACK SELECTION
    // ===============================
    private EnemyBaseState ChooseAttack(float dist)
    {
        if (dist <= closeRange)
            return PickWeighted(new (EnemyBaseState, int)[]
            {
                (punchAttack,             closeWeight_Punch),
                (jumpSlamState,           closeWeight_JumpSlam),
                (spinAttack,              closeWeight_Spin),
                (repeatedBulletSlamState, closeWeight_BulletSlam),
                (chargeAttack,            closeWeight_Charge),
                (targetedBurstAttack,     closeWeight_TargetedBurst),
                (ringGapAttack,           closeWeight_RingGap),
            });

        if (dist >= farRange)
            return PickWeighted(new (EnemyBaseState, int)[]
            {
                (shockwaveState,          farWeight_Shockwave),
                (mapSeparatorState,       farWeight_MapSeparator),
                (repeatedBulletSlamState, farWeight_BulletSlam),
                (chargeAttack,            farWeight_Charge),
                (spiralBurstAttack,       farWeight_SpiralBurst),
                (targetedBurstAttack,     farWeight_TargetedBurst),
                (ringGapAttack,           farWeight_RingGap),
            });

        return PickWeighted(new (EnemyBaseState, int)[]
        {
            (repeatedBulletSlamState, midWeight_BulletSlam),
            (chargeAttack,            midWeight_Charge),
            (spinAttack,              midWeight_Spin),
            (shockwaveState,          midWeight_Shockwave),
            (mapSeparatorState,       midWeight_MapSeparator),
            (spiralBurstAttack,       midWeight_SpiralBurst),
            (targetedBurstAttack,     midWeight_TargetedBurst),
            (ringGapAttack,           midWeight_RingGap),
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