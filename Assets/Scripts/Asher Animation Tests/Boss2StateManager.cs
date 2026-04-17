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

    [Tooltip("Destroy this many mini computers to drop the force field and start the main computer")]
    public int miniComputersToActivate = 1;

    [Tooltip("The force field GameObject sitting around the main computer — disabled on activation")]
    public GameObject forceField;

    [Tooltip("Prefab spawned by the Obstacle Barrage attack — needs a Boss2Obstacle component and Box Collider")]
    public GameObject obstaclePrefab;

    [Header("Bullet Spawn Points")]
    [Tooltip("Empty GameObjects bullets can fire from — falls back to boss position if empty")]
    public Transform[] bulletSpawnPoints;

    private int  _miniComputersRemaining;
    private bool _isActive       = false;
    private bool _forceFieldUp   = true;

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
    public float hitRadius    = 0.6f;
    public float minSwordSpeed = 0.8f;

    private Sword  _sword;
    private float  _hitCooldown;

    public override void Start()
    {
        _miniComputersRemaining = miniComputersTotal;
        health    = maxHealth;
        animator  = GetComponent<Animator>();
        rb        = GetComponent<Rigidbody>();

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
    // PHASE GATE
    // ===============================
    public void OnMiniComputerDestroyed(Boss2MiniComputer mini)
    {
        _miniComputersRemaining--;
        int destroyed = miniComputersTotal - _miniComputersRemaining;
        Debug.Log($"[Boss2] Mini computer destroyed ({destroyed}/{miniComputersTotal}).");

        if (_forceFieldUp && _miniComputersRemaining <= 0)
            ActivateMainComputer();
    }

    private void ActivateMainComputer()
    {
        _isActive     = true;
        _forceFieldUp = false;

        if (forceField != null)
            forceField.SetActive(false);

        if (bossHealthBar != null)
            bossHealthBar.UpdateHealthPercentage(health, maxHealth);

        if (HUDManager.Instance != null)
            HUDManager.Instance.ShowBossBar(true);

        SwitchState(idleState);
    }

    // ===============================
    // DAMAGE
    // ===============================
    public void TakeDamage(float amount)
    {
        if (_forceFieldUp) return;

        health = Mathf.Max(0f, health - amount);

        if (bossHealthBar != null)
            bossHealthBar.UpdateHealthPercentage(health, maxHealth);

        if (health <= 0f)
        {
            MenuController.Instance?.AdvanceToNextBoss(2);
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
