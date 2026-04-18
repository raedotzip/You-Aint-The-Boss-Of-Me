using System.Collections.Generic;
using UnityEngine;

public class Boss2StateManager : EnemyStateManager
{
    // ===============================
    // STATES
    // ===============================
    public Boss2IdleState         idleState         = new Boss2IdleState();
    public Boss2LaserBeamAttack   laserBeamAttack   = new Boss2LaserBeamAttack();
    public Boss2VirusSwarmAttack  virusSwarmAttack  = new Boss2VirusSwarmAttack();
    public Boss2EMPWaveAttack     empWaveAttack     = new Boss2EMPWaveAttack();
    public Boss2DataStrikeAttack  dataStrikeAttack  = new Boss2DataStrikeAttack();
    public Boss2SpiralAttack           spiralAttack           = new Boss2SpiralAttack();
    public Boss2ObstacleBarrageAttack  obstacleBarrageAttack  = new Boss2ObstacleBarrageAttack();

    private Boss2TiredState       tiredState        = new Boss2TiredState();

    // ===============================
    // PHASE GATE
    // ===============================
    [Header("Phase Gate")]
    public int miniComputersTotal = 5;

    [Tooltip("The force field GameObject sitting around the main computer")]
    public GameObject forceField;

    [Tooltip("Prefab spawned by the Obstacle Barrage attack — needs a Boss2Obstacle component and Box Collider")]
    public GameObject obstaclePrefab;

    [Header("Bullet Spawn Points")]
    [Tooltip("Empty GameObjects bullets can fire from — falls back to boss position if empty")]
    public Transform[] bulletSpawnPoints;

    // ===============================
    // STAGE SYSTEM
    // ===============================
    [Header("Stage System")]
    [Tooltip("Radius around the boss used to push the player out when the force field re-expands")]
    public float forceFieldPushRadius = 8f;

    // Stage 0 → vulnerability window 1 (health 100→66.7)
    // Stage 1 → vulnerability window 2 (health 66.7→33.3)
    // Stage 2 → vulnerability window 3 (health 33.3→0) → boss dies
    private int  _stage                 = 0;
    private int  _miniComputersRemaining;
    private bool _isActive              = false;
    private bool _forceFieldUp          = true;

    private readonly List<Boss2MiniComputer> _miniComputerRefs = new List<Boss2MiniComputer>();

    // ===============================
    // RANGE SETTINGS
    // ===============================
    [Header("Range Thresholds")]
    public float closeRange = 8f;
    public float farRange   = 18f;

    // ===============================
    // ATTACK WEIGHTS — Close
    // ===============================
    [Header("Close Range Attack Weights")]
    [Range(0, 10)] public int closeWeight_LaserBeam  = 2;
    [Range(0, 10)] public int closeWeight_VirusSwarm = 2;
    [Range(0, 10)] public int closeWeight_EMPWave    = 3;
    [Range(0, 10)] public int closeWeight_DataStrike = 4;
    [Range(0, 10)] public int closeWeight_Spiral          = 3;
    [Range(0, 10)] public int closeWeight_ObstacleBarrage = 2;

    // ===============================
    // ATTACK WEIGHTS — Mid
    // ===============================
    [Header("Mid Range Attack Weights")]
    [Range(0, 10)] public int midWeight_LaserBeam         = 3;
    [Range(0, 10)] public int midWeight_VirusSwarm        = 3;
    [Range(0, 10)] public int midWeight_EMPWave           = 3;
    [Range(0, 10)] public int midWeight_DataStrike        = 2;
    [Range(0, 10)] public int midWeight_Spiral            = 3;
    [Range(0, 10)] public int midWeight_ObstacleBarrage   = 3;

    // ===============================
    // ATTACK WEIGHTS — Far
    // ===============================
    [Header("Far Range Attack Weights")]
    [Range(0, 10)] public int farWeight_LaserBeam         = 3;
    [Range(0, 10)] public int farWeight_VirusSwarm        = 3;
    [Range(0, 10)] public int farWeight_EMPWave           = 2;
    [Range(0, 10)] public int farWeight_DataStrike        = 3;
    [Range(0, 10)] public int farWeight_Spiral            = 2;
    [Range(0, 10)] public int farWeight_ObstacleBarrage   = 4;

    // ===============================
    // TIRED SETTINGS
    // ===============================
    [Header("Tired Settings")]
    public int   attacksBeforeTired = 4;
    public float tiredDuration      = 2.5f;

    // ===============================
    // RUNTIME
    // ===============================
    [HideInInspector] public int   attackCounter = 0;
    [HideInInspector] public float health        = 100f;
    [HideInInspector] public float maxHealth     = 100f;
    public HealthBarUI bossHealthBar;

    [Header("Sword Hit Detection")]
    public float hitRadius     = 0.6f;
    public float minSwordSpeed = 0.8f;

    private Sword _sword;
    private float _hitCooldown;

    public override void Start()
    {
        _miniComputersRemaining = miniComputersTotal;
        health   = maxHealth;
        animator = GetComponent<Animator>();
        rb       = GetComponent<Rigidbody>();

        if (rb != null)
            rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (player == null)
            player = GameObject.FindWithTag("Player").transform;

        if (forceField != null)
            forceField.SetActive(true);

        _sword = FindObjectOfType<Sword>();
    }

    public override void Update()
    {
        if (!_isActive) return;
        currentState.UpdateState(this);
    }

    void FixedUpdate()
    {
        if (_forceFieldUp || _sword == null) return;
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
    // MINI COMPUTER REGISTRATION
    // ===============================
    public void RegisterMiniComputer(Boss2MiniComputer mini)
    {
        if (!_miniComputerRefs.Contains(mini))
            _miniComputerRefs.Add(mini);
    }

    // ===============================
    // PHASE GATE
    // ===============================
    public void OnMiniComputerDestroyed(Boss2MiniComputer mini)
    {
        _miniComputersRemaining--;
        int destroyed = miniComputersTotal - _miniComputersRemaining;
        Debug.Log($"[Boss2] Mini computer destroyed ({destroyed}/{miniComputersTotal}). Stage {_stage}.");

        if (!_isActive)
        {
            _isActive = true;
            SwitchState(idleState);
        }

        if (_forceFieldUp && _miniComputersRemaining <= 0)
            StartVulnerabilityPhase();
    }

    // Force field drops — boss is hittable but does NOT attack
    private void StartVulnerabilityPhase()
    {
        _isActive     = false;   // no attacks during vulnerability window
        _forceFieldUp = false;

        if (forceField != null)
            forceField.SetActive(false);

        if (bossHealthBar != null)
            bossHealthBar.UpdateHealthPercentage(health, maxHealth);

        if (HUDManager.Instance != null)
            HUDManager.Instance.ShowBossBar(true);

        Debug.Log($"[Boss2] Vulnerability window {_stage + 1} open — take {maxHealth / 3f:F0} damage to trigger repair.");
    }

    // Player dealt a stage's worth of damage — mini PCs revive, force field re-expands
    private void TriggerRepairPhase()
    {
        _stage++;
        _miniComputersRemaining = miniComputersTotal;
        _forceFieldUp           = true;
        _isActive               = false;

        if (forceField != null)
            forceField.SetActive(true);

        PushPlayerOutOfForceField();

        foreach (var mini in _miniComputerRefs)
            if (mini != null) mini.Revive();

        _isActive = true;
        SwitchState(idleState);

        Debug.Log($"[Boss2] Repair phase — stage now {_stage}. Destroy the mini computers again.");
    }

    private void PushPlayerOutOfForceField()
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float dist     = toPlayer.magnitude;
        float pushTo   = forceFieldPushRadius + 2f;

        if (dist < pushTo)
        {
            Vector3 dir    = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector3.forward;
            Vector3 newPos = transform.position + dir * pushTo;
            newPos.y       = player.position.y;

            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.position = newPos;
            if (cc != null) cc.enabled = true;
        }
    }

    // ===============================
    // DAMAGE
    // ===============================
    public void TakeDamage(float amount)
    {
        if (_forceFieldUp) return;

        health = Mathf.Max(0f, health - amount);

        // Each stage allows exactly 1/3 of max health to be dealt.
        // Clamp so the player can't skip a stage in a single hit.
        float stageFloor = maxHealth * (2f - _stage) / 3f;
        if (health < stageFloor)
            health = stageFloor;

        if (bossHealthBar != null)
            bossHealthBar.UpdateHealthPercentage(health, maxHealth);

        if (health <= stageFloor)
        {
            if (_stage < 2)
            {
                TriggerRepairPhase();
            }
            else
            {
                // Final vulnerability window — boss dies
                health = 0f;
                if (bossHealthBar != null)
                    bossHealthBar.UpdateHealthPercentage(health, maxHealth);
                Debug.Log("[Boss2] Main computer destroyed!");
                HUDManager.Instance?.StopTimer();
                MenuController.Instance?.AdvanceToNextBoss(2);
            }
        }
    }

    // ===============================
    // SPAWN POINTS
    // ===============================
    public Vector3 GetRandomSpawnPoint()
    {
        if (bulletSpawnPoints != null && bulletSpawnPoints.Length > 0)
            return bulletSpawnPoints[Random.Range(0, bulletSpawnPoints.Length)].position;
        return transform.position;
    }

    public Transform[] GetAllSpawnPoints()
    {
        if (bulletSpawnPoints != null && bulletSpawnPoints.Length > 0)
            return bulletSpawnPoints;
        return new Transform[] { transform };
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
        EnemyBaseState next = ChooseAttack(dist);
        attackCounter++;
        SwitchState(next);
    }

    // ===============================
    // ATTACK SELECTION
    // ===============================
    private EnemyBaseState ChooseAttack(float dist)
    {
        if (dist <= closeRange)
            return PickWeighted(new (EnemyBaseState, int)[]
            {
                (laserBeamAttack,       closeWeight_LaserBeam),
                (virusSwarmAttack,      closeWeight_VirusSwarm),
                (empWaveAttack,         closeWeight_EMPWave),
                (dataStrikeAttack,      closeWeight_DataStrike),
                (spiralAttack,          closeWeight_Spiral),
                (obstacleBarrageAttack, closeWeight_ObstacleBarrage),
            });

        if (dist >= farRange)
            return PickWeighted(new (EnemyBaseState, int)[]
            {
                (laserBeamAttack,       farWeight_LaserBeam),
                (virusSwarmAttack,      farWeight_VirusSwarm),
                (empWaveAttack,         farWeight_EMPWave),
                (dataStrikeAttack,      farWeight_DataStrike),
                (spiralAttack,          farWeight_Spiral),
                (obstacleBarrageAttack, farWeight_ObstacleBarrage),
            });

        return PickWeighted(new (EnemyBaseState, int)[]
        {
            (laserBeamAttack,       midWeight_LaserBeam),
            (virusSwarmAttack,      midWeight_VirusSwarm),
            (empWaveAttack,         midWeight_EMPWave),
            (dataStrikeAttack,      midWeight_DataStrike),
            (spiralAttack,          midWeight_Spiral),
            (obstacleBarrageAttack, midWeight_ObstacleBarrage),
        });
    }

    // ===============================
    // WEIGHTED RANDOM
    // ===============================
    private EnemyBaseState PickWeighted((EnemyBaseState state, int weight)[] options)
    {
        int total = 0;
        foreach (var o in options) total += o.weight;

        if (total <= 0) return options[0].state;

        int roll       = Random.Range(0, total);
        int cumulative = 0;

        foreach (var o in options)
        {
            cumulative += o.weight;
            if (roll < cumulative) return o.state;
        }

        return options[0].state;
    }

    // ===============================
    // STATE SWITCHING
    // ===============================
    public void SwitchState(EnemyBaseState newState)
    {
        if (animator != null) DisableAnimationBools();
        currentState = newState;
        currentState.EnterState(this);
    }

    public void DisableAnimationBools()
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Bool)
                animator.SetBool(param.name, false);
        }
    }
}
