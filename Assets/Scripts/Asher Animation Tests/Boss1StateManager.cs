using UnityEngine;
using System.Collections.Generic;
using System.Collections;

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

    [Header("Arena Bounds")]
    [Tooltip("Parent GameObject. Every BoxCollider child with Is Trigger checked defines a valid zone. Boss is clamped to the nearest zone if it leaves all of them.")]
    public Transform arenaBoundsRoot;

    private BoxCollider[] _arenaBoundsColliders;

    // ===============================
    // LAVA PIT
    // ===============================
    [Header("Lava Pit")]
    [Tooltip("Empty GameObject with a BoxCollider sized to the pit — rotate/scale it to match")]
    public Transform lavaPitCenter;
    [Tooltip("Optional: exact spot where boss stands at the lava edge while wobbling. If unassigned, auto-computed as 70% between boss and pit center.")]
    public Transform lavaEdgePosition;
    [Tooltip("Extra buffer inside the pit edge — boss lands this far short of the rim")]
    public float pitSafetyMargin = 1.5f;
    [Tooltip("Y level at which the boss is considered fully sunk — triggers AdvanceToNextBoss")]
    public float lavaFallDepth = -15f;

    private BoxCollider _pitCollider;

    // ===============================
    // ATTACK WEIGHTS
    // ===============================
    // Close: boss is in your face — big melee, fast pressure
    [Header("Close Range Attack Weights")]
    [Range(0, 10)] public int closeWeight_Punch         = 5; // primary close attack
    [Range(0, 10)] public int closeWeight_JumpSlam      = 8; // jump into the player
    [Range(0, 10)] public int closeWeight_Spin          = 0;
    [Range(0, 10)] public int closeWeight_Charge        = 9; // charges through player
    [Range(0, 10)] public int closeWeight_TargetedBurst = 0;
    [Range(0, 10)] public int closeWeight_RingGap       = 0;

    // Mid: boss closes distance fast with charges and jumps
    [Header("Mid Range Attack Weights")]
    [Range(0, 10)] public int midWeight_Charge          = 9; // primary mid approach
    [Range(0, 10)] public int midWeight_JumpSlam        = 6; // jump slam from mid range
    [Range(0, 10)] public int midWeight_Spin            = 0;
    [Range(0, 10)] public int midWeight_SpiralBurst     = 1;
    [Range(0, 10)] public int midWeight_TargetedBurst   = 1;
    [Range(0, 10)] public int midWeight_RingGap         = 1;

    // Far: rush the player, close in fast
    [Header("Far Range Attack Weights")]
    [Range(0, 10)] public int farWeight_Charge          = 9; // boss rushes in from far
    [Range(0, 10)] public int farWeight_JumpSlam        = 4; // jump slam from far
    [Range(0, 10)] public int farWeight_SpiralBurst     = 1;
    [Range(0, 10)] public int farWeight_TargetedBurst   = 1;
    [Range(0, 10)] public int farWeight_RingGap         = 0;

    // ===============================
    // TIRED SETTINGS
    // ===============================
    [Header("Tired Settings")]
    public int   attacksBeforeTired        = 12;   // attacks before going tired (normal phase)
    public float tiredDuration             = 0.6f; // vulnerable window (normal)
    public int   attacksBeforeTiredEnraged = 20;   // barely rests at low health
    public float tiredDurationEnraged      = 0.3f; // gets up much faster at ≤20% health


    // ===============================
    // BOSS MUSIC
    // ===============================
    [Header("Boss Music")]
    public AudioSource musicSource;

    public AudioClip bossMusicNormal;

    [Range(0f, 1f)]
    public float musicVolume = 0.6f;

    public bool musicStarted = false;

    public void StartBossMusic()
    {
        if (musicStarted) return;

        if (musicSource == null || bossMusicNormal == null)
        {
            Debug.LogWarning("[Boss1 Music] Missing AudioSource or clip!");
            return;
        }

        musicStarted = true;

        musicSource.clip = bossMusicNormal;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.Play();

        Debug.Log("[Boss1 Music] Started");
    }

    public override void StopBossMusic()
    {
        if (musicSource != null) musicSource.Stop();
        musicStarted = false;
    }

    // ===============================
    // ATTACK AUDIO
    // ===============================
    [Header("Boss Attack SFX")]
    public AudioSource audioSource;
    [Range(0f, 2f)]
    public float sfxVolume = 1f;

    [Header("SFX")]
    public AudioClip spinStartClip;
    public AudioClip slamClip;
  

    // ===============================
    // STOPWATCH
    // ===============================
    [Header("Boss Timer")]
    public float bossTimer = 0f;
    private bool _timerRunning = false;
    private bool _timerStarted = false;

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

    [Header("Health")]
    public float maxHealth = 100f;
    public HealthBarUI bossHealthBar;
    public BossTimerUI bossTimerUI;


    void OnValidate()
    {
        health = maxHealth;
    }

    // True once health hits 0 — prevents re-triggering the finisher
    public bool finisherTriggered = false;

    // Set by jump states so EnforceBounds skips the wall-overlap check mid-air
    [HideInInspector] public bool isAirborne = false;

    // Skips the wall-overlap check for one frame after a state switch so the
    // localPosition reset in SwitchState doesn't cause a false wall detection.
    private bool _skipWallCheckNextFrame = false;

    // Boss is enraged below 20% health — faster recovery, less frequent rest
    public bool IsEnraged => health / maxHealth <= 0.2f;

    private Vector3 _lastSafePosition;
     
    public override void Start()
    {
        health = maxHealth;

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowBossBar(true);
            HUDManager.Instance.UpdateBossHealth(health, maxHealth);
        }

        animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        // The Rigidbody lives on a child FBX node, not on this root transform.
        // MovePosition on the child rb moves it independently of the root, so the
        // boss runs in place. Making it kinematic prevents physics fighting parent
        // movement; nulling the reference forces all states to use transform.position.
        if (rb != null)
        {
            rb.isKinematic = true;
            rb = null;
        }
        // Also kinematize any child Rigidbodies (the FBX node) that aren't caught above.
        foreach (var childRb in GetComponentsInChildren<Rigidbody>())
        {
            childRb.isKinematic = true;
            Debug.Log($"[Boss1] Kinematized child Rigidbody on '{childRb.gameObject.name}'");
        }

        if (player == null)
            player = GameObject.FindWithTag("Player").transform;

        if (lavaPitCenter != null)
            _pitCollider = lavaPitCenter.GetComponent<BoxCollider>();

        if (arenaBoundsRoot != null)
        {
            var all = arenaBoundsRoot.GetComponentsInChildren<BoxCollider>();
            var list = new List<BoxCollider>();
            foreach (var b in all) if (b.isTrigger) list.Add(b);
            _arenaBoundsColliders = list.ToArray();
        }

        _lastSafePosition = transform.position;

        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>())
            smr.updateWhenOffscreen = true;

        ObstacleManager.Instance?.PrewarmObstaclePools(obstacleData);

        SwitchState(idleState);
        StartBossMusic();
    }

    public override void Update()
    {
        if (_timerRunning)
            bossTimer += Time.deltaTime;

        if (currentState == null) return;
        //Debug.Log($"Pos:{transform.position}, Current State: {currentState}");
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

        // Animation clips incrementally drift the FBX child node's local Y.
        // Clamping it every frame keeps the SkinnedMeshRenderer bounds accurate
        // so Unity's frustum culling never incorrectly hides the boss.
        if (animator != null && animator.transform != transform)
        {
            Vector3 local = animator.transform.localPosition;
            if (local.y != 0f)
            {
                local.y = 0f;
                animator.transform.localPosition = local;
            }
        }

        // Hard safety net — teleport back if the boss somehow leaves the map
        EnforceBounds();
    }

    // Snaps boss back to last safe position if it falls off the map or clips into a wall
    public void EnforceBounds()
    {
        if (finisherTriggered) return;

        Vector3 pos = transform.position;

        // Fell off the map
        if (pos.y < fallThreshold)
        {
            Debug.LogWarning($"[Boss1] EnforceBounds: fell below fallThreshold ({pos.y:F2} < {fallThreshold}) — teleporting to {_lastSafePosition}. State={currentState?.GetType().Name}");
            transform.position = _lastSafePosition;
            return;
        }

        // Clipped into wall geometry — skip this check while airborne or for one
        // frame after a state switch (localPosition reset can cause a false hit).
        if (!isAirborne && wallLayer != 0 && !_skipWallCheckNextFrame)
        {
            Vector3 checkPos = pos + Vector3.up * bossCheckHeight;
            if (Physics.CheckSphere(checkPos, bossRadius, wallLayer, QueryTriggerInteraction.Ignore))
            {
                Debug.LogWarning($"[Boss1] EnforceBounds: wall overlap at {pos} — teleporting to {_lastSafePosition}. State={currentState?.GetType().Name}");
                transform.position = _lastSafePosition;
                return;
            }
        }
        _skipWallCheckNextFrame = false;

        // Clamp to arena bounds — boss must be inside at least one trigger zone
        if (_arenaBoundsColliders != null && _arenaBoundsColliders.Length > 0)
        {
            bool insideAny = false;
            foreach (var box in _arenaBoundsColliders)
            {
                if (box.bounds.Contains(pos)) { insideAny = true; break; }
            }

            if (!insideAny)
            {
                // Find the closest point across all zones and snap XZ to it
                Vector3 best = pos;
                float minDist = float.MaxValue;
                foreach (var box in _arenaBoundsColliders)
                {
                    Vector3 cp = box.ClosestPoint(pos);
                    float d = (cp.x - pos.x) * (cp.x - pos.x) + (cp.z - pos.z) * (cp.z - pos.z);
                    if (d < minDist) { minDist = d; best = cp; }
                }
                transform.position = new Vector3(best.x, pos.y, best.z);
                pos = transform.position;
            }
        }

        // Only record a safe position when the boss is on solid ground —
        // prevents an out-of-bounds spot from overwriting a good recovery point
        if (pos.y >= 0f && !IsInPit(pos, 0f))
        {
            int groundMask = ~(1 << gameObject.layer);
            Vector3 groundOrigin = pos + Vector3.up * 0.5f;
            if (Physics.Raycast(groundOrigin, Vector3.down, 3f, groundMask, QueryTriggerInteraction.Ignore))
                _lastSafePosition = pos;
        }
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
        if (finisherTriggered)
        {
            lavaFinisherState.PushBoss(this);
            return;
        }

        health = Mathf.Max(0f, health - amount);
        if (bossHealthBar != null)
            bossHealthBar.UpdateHealthPercentage(health, maxHealth);
        HUDManager.Instance?.UpdateBossHealth(health, maxHealth);

        if (health <= 0f)
        {
            finisherTriggered = true;
            SwitchState(lavaFinisherState);
        }
    }

    // ===============================
    // STATE DECISION
    // ===============================
    public void TransitionToNextState()
    {
        if (!_timerStarted)
        {
            _timerStarted = true;
            _timerRunning = true;
            bossTimer = 0f;

            bossTimerUI?.StartTimer();
            Debug.Log("[Boss1] Stopwatch started on first attack.");
        }

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
        return new Vector3(Mathf.Clamp(proposed.x, -10, 11), 0f, Mathf.Clamp(proposed.z, -7, 11));
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
                (jumpSlamState,       farWeight_JumpSlam),
                (spiralBurstAttack,   farWeight_SpiralBurst),
                (targetedBurstAttack, farWeight_TargetedBurst),
                (ringGapAttack,       farWeight_RingGap),
            });

        return PickWeighted(new (EnemyBaseState, int)[]
        {
            (chargeAttack,        midWeight_Charge),
            (jumpSlamState,       midWeight_JumpSlam),
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
        _skipWallCheckNextFrame = true;
        DisableAnimationBools();

        // If the Animator is on a child FBX node, animation keyframes can drift its
        // local Y over time, making the visible mesh sink underground or float.
        // Reset local position each state transition to prevent accumulation.
        if (animator != null && animator.transform != transform)
            animator.transform.localPosition = Vector3.zero;

        currentState = newState;
        currentState.EnterState(this);
    }

    public void DisableAnimationBools()
    {
        if (animator == null) return;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Bool)
                animator.SetBool(param.name, false);
        }
    }

    public void LerpDownTired()
    {
        StartCoroutine(LerpDownCoroutine());
    }

    IEnumerator LerpDownCoroutine()
    {
        float amountMovedDown = 0f;
        Vector3 currentPos = transform.position;
        while (amountMovedDown < 0.5f)
        {
            amountMovedDown += Time.deltaTime;
            transform.position = new Vector3(currentPos.x, currentPos.y - amountMovedDown, currentPos.z);
            yield return null;
        }
        transform.position = new Vector3(currentPos.x, currentPos.y - 0.5f, currentPos.z);
    }

    public void LerpUpTired()
    {
        StartCoroutine(LerpUpCoroutine());
    }

    IEnumerator LerpUpCoroutine()
    {
        float amountMovedUp = 0f;
        Vector3 currentPos = transform.position;
        while (amountMovedUp < 0.5f)
        {
            amountMovedUp += Time.deltaTime;
            transform.position = new Vector3(currentPos.x, currentPos.y + amountMovedUp, currentPos.z);
            yield return null;
        }
        transform.position = new Vector3(currentPos.x, currentPos.y + 0.5f, currentPos.z);
    }
}