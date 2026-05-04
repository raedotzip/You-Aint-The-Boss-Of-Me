using System.Collections;
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
    public Boss2RailgunAttack          railgunAttack          = new Boss2RailgunAttack();
    public Boss2MortarBarrageAttack    mortarBarrageAttack    = new Boss2MortarBarrageAttack();

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
    // CEILING CURTAIN (bullet walls across walkways)
    // ===============================
    [Header("Ceiling Curtain — Path Blocking")]
    [Tooltip("Empty GameObjects on the ceiling/force-field above each walkway. Forward = fire direction (toward floor). Right = across the walkway.")]
    public Transform[] ceilingFirePoints;

    [Tooltip("Total width of the bullet curtain in meters — match your walkway width")]
    public float curtainWidth = 6f;

    [Tooltip("Seconds between curtain volleys per phase (phase 0, 1, 2)")]
    public float[] pathBlockIntervals = { 1.5f, 1f, 0.5f };

    [Tooltip("How many ceiling fire points shoot simultaneously per phase")]
    public int[] pathBlockFirePointsPerSpawn = { 2, 3, 4 };

    private float _pathBlockTimer;

    // Bullet columns, gap width, speed, and damage scale up per phase
    private static readonly int[]   CurtainBulletCols   = { 12,  16,  22   };
    private static readonly int[]   CurtainGapCols      = { 3,   2,   2    };
    private static readonly float[] CurtainBulletSpeed  = { 4.5f, 6f, 8f   };
    private static readonly float[] CurtainBulletDamage = { 8f,  11f, 15f  };

    // ===============================
    // PHASE ESCALATION
    // ===============================
    // Phase 0 → 1 → 2: boss rests less, attacks more, curtain pressure increases
    private static readonly float[] PhasesTiredDuration      = { 2.5f, 1.5f, 0.4f };
    private static readonly int[]   PhasesAttacksBeforeTired = { 3,    4,    6    };

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
    [Range(0, 10)] public int farWeight_Railgun           = 2;
    [Range(0, 10)] public int farWeight_MortarBarrage     = 3;

    // ===============================
    // CLOSE + MID RAILGUN / MORTAR WEIGHTS
    // ===============================
    [Header("Close Range — Railgun / Mortar")]
    [Range(0, 10)] public int closeWeight_Railgun         = 1;
    [Range(0, 10)] public int closeWeight_MortarBarrage   = 1;

    [Header("Mid Range — Railgun / Mortar")]
    [Range(0, 10)] public int midWeight_Railgun           = 2;
    [Range(0, 10)] public int midWeight_MortarBarrage     = 2;

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
    [HideInInspector] public float health        = 300f;

    [Header("Health")]
    public float maxHealth = 300f;
    public HealthBarUI bossHealthBar;


    [Header("Sword Hit Detection")]
    public float hitRadius     = 0.6f;
    public float minSwordSpeed = 0.8f;

    private Sword _sword;
    private float _hitCooldown;

    void OnValidate()
    {
        health = maxHealth;
    }

    void OnDisable()
    {
        _isActive = false;
    }

    // ===============================
    // WEIGHT SNAPSHOT (captured at Start so ResetBoss can restore phase-0 values)
    // ===============================
    private struct WeightSnapshot
    {
        public int cLaser, cVirus, cEMP, cData, cSpiral, cObstacle, cRailgun, cMortar;
        public int mLaser, mVirus, mEMP, mData, mSpiral, mObstacle, mRailgun, mMortar;
        public int fLaser, fVirus, fEMP, fData, fSpiral, fObstacle, fRailgun, fMortar;
    }
    private WeightSnapshot _weights0;

    private WeightSnapshot CaptureWeights() => new WeightSnapshot
    {
        cLaser = closeWeight_LaserBeam,  cVirus = closeWeight_VirusSwarm, cEMP = closeWeight_EMPWave,
        cData  = closeWeight_DataStrike, cSpiral = closeWeight_Spiral,    cObstacle = closeWeight_ObstacleBarrage,
        cRailgun = closeWeight_Railgun,  cMortar = closeWeight_MortarBarrage,
        mLaser = midWeight_LaserBeam,    mVirus = midWeight_VirusSwarm,   mEMP = midWeight_EMPWave,
        mData  = midWeight_DataStrike,   mSpiral = midWeight_Spiral,      mObstacle = midWeight_ObstacleBarrage,
        mRailgun = midWeight_Railgun,    mMortar = midWeight_MortarBarrage,
        fLaser = farWeight_LaserBeam,    fVirus = farWeight_VirusSwarm,   fEMP = farWeight_EMPWave,
        fData  = farWeight_DataStrike,   fSpiral = farWeight_Spiral,      fObstacle = farWeight_ObstacleBarrage,
        fRailgun = farWeight_Railgun,    fMortar = farWeight_MortarBarrage,
    };

    private void RestoreWeights(WeightSnapshot w)
    {
        closeWeight_LaserBeam = w.cLaser;   closeWeight_VirusSwarm = w.cVirus;   closeWeight_EMPWave = w.cEMP;
        closeWeight_DataStrike = w.cData;   closeWeight_Spiral = w.cSpiral;      closeWeight_ObstacleBarrage = w.cObstacle;
        closeWeight_Railgun = w.cRailgun;   closeWeight_MortarBarrage = w.cMortar;
        midWeight_LaserBeam = w.mLaser;     midWeight_VirusSwarm = w.mVirus;     midWeight_EMPWave = w.mEMP;
        midWeight_DataStrike = w.mData;     midWeight_Spiral = w.mSpiral;        midWeight_ObstacleBarrage = w.mObstacle;
        midWeight_Railgun = w.mRailgun;     midWeight_MortarBarrage = w.mMortar;
        farWeight_LaserBeam = w.fLaser;     farWeight_VirusSwarm = w.fVirus;     farWeight_EMPWave = w.fEMP;
        farWeight_DataStrike = w.fData;     farWeight_Spiral = w.fSpiral;        farWeight_ObstacleBarrage = w.fObstacle;
        farWeight_Railgun = w.fRailgun;     farWeight_MortarBarrage = w.fMortar;
    }

    public override void Start()
    {
        _miniComputersRemaining = miniComputersTotal;
        health   = maxHealth;
        animator = GetComponent<Animator>();
        // Apply Phase 0 settings so inspector defaults align with the escalation table
        tiredDuration      = PhasesTiredDuration[0];
        attacksBeforeTired = PhasesAttacksBeforeTired[0];
        _pathBlockTimer    = pathBlockIntervals != null && pathBlockIntervals.Length > 0 ? pathBlockIntervals[0] : 5f;
        _weights0          = CaptureWeights();
        rb       = GetComponent<Rigidbody>();

        if (rb != null)
            rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (player == null)
            player = GameObject.FindWithTag("Player").transform;

        if (forceField != null)
            forceField.SetActive(true);

        _sword = FindObjectOfType<Sword>();
        if (_sword == null)
            Debug.LogWarning("[Boss2] Sword not found at Start — will retry in FixedUpdate.");
    }

    public override void Update()
    {
        if (_isActive && _forceFieldUp)
            UpdatePathBlocking();

        if (!_isActive) return;
        this.currentState.UpdateState(this);
    }

    void FixedUpdate()
    {
        if (_sword == null) _sword = FindObjectOfType<Sword>();
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
        {
            _miniComputerRefs.Add(mini);
            // Keep count in sync with actual registered computers so the
            // force field drops correctly even if miniComputersTotal is wrong.
            _miniComputersRemaining = _miniComputerRefs.Count;
        }
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
            _isActive       = true;
            _pathBlockTimer = pathBlockIntervals != null && pathBlockIntervals.Length > _stage ? pathBlockIntervals[_stage] : 5f;
            SwitchState(idleState);
            StartBossMusic();
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
        {
            HUDManager.Instance.ShowBossBar(true);
            HUDManager.Instance.UpdateBossHealth(health, maxHealth);
        }

        Debug.Log($"[Boss2] Vulnerability window {_stage + 1} open — take {maxHealth / 3f:F0} damage to trigger repair.");
    }

    // Player dealt a stage's worth of damage — mini PCs revive, force field re-expands
    private void TriggerRepairPhase()
    {
        _stage++;
        _miniComputersRemaining = miniComputersTotal;
        _forceFieldUp           = true;
        _isActive               = false;

        // Escalate difficulty for the new phase
        int idx            = Mathf.Min(_stage, PhasesTiredDuration.Length - 1);
        tiredDuration      = PhasesTiredDuration[idx];
        attacksBeforeTired = PhasesAttacksBeforeTired[idx];
        ApplyPhaseAttackWeights(_stage);
        _pathBlockTimer    = pathBlockIntervals != null && pathBlockIntervals.Length > _stage ? pathBlockIntervals[_stage] : 2.5f;

        if (forceField != null)
            forceField.SetActive(true);

        PushPlayerOutOfForceField();

        foreach (var mini in _miniComputerRefs)
            if (mini != null) mini.Revive();

        _isActive = true;
        SwitchState(idleState);

        Debug.Log($"[Boss2] Phase {_stage + 1} — tiredDuration={tiredDuration:F1}s, attacksBeforeTired={attacksBeforeTired}.");
    }

    private void PushPlayerOutOfForceField()
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float dist   = toPlayer.magnitude;
        float pushTo = forceFieldPushRadius + 2f;

        if (dist >= pushTo) return;

        Vector3 dir    = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector3.forward;
        Vector3 endPos = transform.position + dir * pushTo;
        endPos.y       = player.position.y;

        StartCoroutine(PushPlayerCoroutine(endPos, 0.6f));
    }

    private IEnumerator PushPlayerCoroutine(Vector3 endPos, float duration)
    {
        var     cc       = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        float   elapsed  = 0f;
        Vector3 startPos = player.position;

        while (elapsed < duration)
        {
            elapsed        += Time.deltaTime;
            float t         = Mathf.Clamp01(elapsed / duration);
            player.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        player.position = endPos;
        if (cc != null) cc.enabled = true;
    }

    // ===============================
    // DAMAGE
    // ===============================
    public void TakeDamage(float amount)
    {
        if (_forceFieldUp) return;

        TriggerHitFlash(amount);
        health = Mathf.Max(0f, health - amount);

        // Each stage allows exactly 1/3 of max health to be dealt.
        // Clamp so the player can't skip a stage in a single hit.
        float stageFloor = maxHealth * (2f - _stage) / 3f;
        if (health < stageFloor)
            health = stageFloor;

        if (bossHealthBar != null)
            bossHealthBar.UpdateHealthPercentage(health, maxHealth);
        HUDManager.Instance?.UpdateBossHealth(health, maxHealth);

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
                HUDManager.Instance?.UpdateBossHealth(health, maxHealth);
                Debug.Log("[Boss2] Main computer destroyed!");
                HUDManager.Instance?.StopTimer();
                animator.SetBool("Destroyed", true);
                MenuController.Instance?.AdvanceToNextBoss(2);
                if (musicSource != null)
                {
                    musicSource.Stop();
                }
                _musicStarted = false;
            }
        }
    }

    // ===============================
    // BULLET SPEED SCALING
    // ===============================
    [Header("Bullet Speed Scaling")]
    [Tooltip("Multiplier at distance 0 (right next to boss) — must be above 0")]
    [Range(0.1f, 2f)] public float nearBulletSpeedMultiplier = 0.5f;
    [Tooltip("Multiplier at farRange distance and beyond")]
    [Range(1f, 8f)]   public float farBulletSpeedMultiplier  = 5f;

    public float ScaleBulletSpeed(float baseSpeed)
    {
        float dist = Vector3.Distance(transform.position, player.position);
        float t    = Mathf.Clamp01(dist / farRange);
        return baseSpeed * Mathf.Lerp(nearBulletSpeedMultiplier, farBulletSpeedMultiplier, t);
    }

    [Header("Far Range Bullet Density")]
    [Tooltip("How many times more targeted bullets are fired when at far range")]
    [Range(1f, 5f)] public float farBulletCountMultiplier = 2.5f;

    [HideInInspector] public float phaseCountMultiplier = 1f;

    public int ScaleBulletCount(int baseCount)
    {
        float dist = Vector3.Distance(transform.position, player.position);
        float t    = Mathf.InverseLerp(closeRange, farRange, dist);
        return Mathf.RoundToInt(baseCount * Mathf.Lerp(1f, farBulletCountMultiplier, t) * phaseCountMultiplier);
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
    // BOSS MUSIC
    // ===============================
    [Header("Boss Music")]
    public AudioSource musicSource;

    public AudioClip bossMusicNormal;

    [Range(0f, 1f)]
    public float musicVolume = 0.6f;

    private bool _musicStarted = false;

    public void StartBossMusic()
    {
        if (_musicStarted) return;

        if (musicSource == null || bossMusicNormal == null)
        {
            Debug.LogWarning("[Boss2 Music] Missing AudioSource or clip!");
            return;
        }

        _musicStarted = true;

        musicSource.clip = bossMusicNormal;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.Play();

        Debug.Log("[Boss2 Music] Started");
    }

    public override void StopBossMusic()
    {
        if (musicSource != null) musicSource.Stop();
        _musicStarted = false;
    }

    public override void ResetBoss()
    {
        health            = maxHealth;
        _stage            = 0;
        _isActive         = false;
        _forceFieldUp     = true;
        attackCounter     = 0;
        phaseCountMultiplier = 1f;

        tiredDuration      = PhasesTiredDuration[0];
        attacksBeforeTired = PhasesAttacksBeforeTired[0];
        _pathBlockTimer    = pathBlockIntervals != null && pathBlockIntervals.Length > 0 ? pathBlockIntervals[0] : 5f;
        _musicStarted      = false;

        _miniComputersRemaining = _miniComputerRefs.Count > 0 ? _miniComputerRefs.Count : miniComputersTotal;
        foreach (var mini in _miniComputerRefs)
            if (mini != null) mini.Revive();

        if (forceField != null) forceField.SetActive(true);
        if (animator != null) animator.SetBool("Destroyed", false);

        if (bossHealthBar != null) bossHealthBar.UpdateHealthPercentage(health, maxHealth);
        HUDManager.Instance?.UpdateBossHealth(health, maxHealth);
        HUDManager.Instance?.ShowBossBar(false);

        RestoreWeights(_weights0);
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
                (railgunAttack,         closeWeight_Railgun),
                (mortarBarrageAttack,   closeWeight_MortarBarrage),
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
                (railgunAttack,         farWeight_Railgun),
                (mortarBarrageAttack,   farWeight_MortarBarrage),
            });

        return PickWeighted(new (EnemyBaseState, int)[]
        {
            (laserBeamAttack,       midWeight_LaserBeam),
            (virusSwarmAttack,      midWeight_VirusSwarm),
            (empWaveAttack,         midWeight_EMPWave),
            (dataStrikeAttack,      midWeight_DataStrike),
            (spiralAttack,          midWeight_Spiral),
            (obstacleBarrageAttack, midWeight_ObstacleBarrage),
            (railgunAttack,         midWeight_Railgun),
            (mortarBarrageAttack,   midWeight_MortarBarrage),
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
    // CEILING CURTAIN PATH BLOCKING
    // ===============================
    private void UpdatePathBlocking()
    {
        if (ceilingFirePoints == null || ceilingFirePoints.Length == 0) return;

        _pathBlockTimer -= Time.deltaTime;
        if (_pathBlockTimer > 0f) return;

        float interval  = pathBlockIntervals != null && pathBlockIntervals.Length > _stage ? pathBlockIntervals[_stage] : 5f;
        _pathBlockTimer = interval;

        int count = pathBlockFirePointsPerSpawn != null && pathBlockFirePointsPerSpawn.Length > _stage
            ? pathBlockFirePointsPerSpawn[_stage] : 1;
        count = Mathf.Min(count, ceilingFirePoints.Length);

        int[] shuffled = ShuffledIndices(ceilingFirePoints.Length);
        for (int i = 0; i < count; i++)
            FireCeilingCurtain(ceilingFirePoints[shuffled[i]]);
    }

    private void FireCeilingCurtain(Transform firePoint)
    {
        if (firePoint == null) return;
        if (bulletData == null) { Debug.LogWarning("[Boss2] bulletData is null — assign it in the Inspector on Boss2StateManager."); return; }

        int   idx    = Mathf.Min(_stage, CurtainBulletCols.Length - 1);
        int   cols   = CurtainBulletCols[idx];
        int   gap    = CurtainGapCols[idx];
        float speed  = CurtainBulletSpeed[idx];
        float damage = CurtainBulletDamage[idx];

        // Gap position: never flush against the edges so there is always wall on both sides
        int gapStart = Random.Range(1, cols - gap);

        for (int i = 0; i < cols; i++)
        {
            if (i >= gapStart && i < gapStart + gap) continue; // leave the gap open

            float   t        = cols > 1 ? (float)i / (cols - 1) : 0.5f;
            Vector3 offset   = firePoint.right * Mathf.Lerp(-curtainWidth * 0.5f, curtainWidth * 0.5f, t);
            Vector3 spawnPos = firePoint.position + offset;

            Bullet b = new Bullet
            {
                position        = spawnPos,
                direction       = firePoint.forward,
                speed           = speed,
                damage          = damage,
                maxLifetime     = 3f,
                collisionRadius = 0.25f,
                canBeParried    = false,
                movementType    = BulletMovementType.Straight,
                visualPrefab    = bulletData.groundSlamBulletPrefab,
                scale           = 0.5f,
            };
            BulletManager.Instance.SpawnBullet(b);
        }
    }

    private static int[] ShuffledIndices(int count)
    {
        int[] arr = new int[count];
        for (int i = 0; i < count; i++) arr[i] = i;
        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
        return arr;
    }

    // ===============================
    // PHASE ATTACK WEIGHTS
    // ===============================
    private void ApplyPhaseAttackWeights(int phase)
    {
        if (phase == 1)
        {
            // Phase 2 — area denial and sustained pressure
            closeWeight_Spiral = 5; closeWeight_EMPWave = 5; closeWeight_ObstacleBarrage = 4;
            closeWeight_MortarBarrage = 3;
            midWeight_Spiral   = 5; midWeight_LaserBeam = 5; midWeight_ObstacleBarrage   = 5;
            midWeight_Railgun  = 3; midWeight_MortarBarrage = 3;
            farWeight_LaserBeam = 6; farWeight_ObstacleBarrage = 6; farWeight_Spiral     = 4;
            farWeight_Railgun   = 3; farWeight_MortarBarrage  = 4;
            phaseCountMultiplier = 1.5f;
        }
        else if (phase == 2)
        {
            // Phase 3 — maximum aggression: fast high-damage attacks, railgun and mortar rain
            closeWeight_DataStrike = 7; closeWeight_LaserBeam = 6; closeWeight_Spiral = 6;
            closeWeight_ObstacleBarrage = 5; closeWeight_Railgun = 3; closeWeight_MortarBarrage = 4;
            midWeight_DataStrike   = 6; midWeight_LaserBeam   = 7; midWeight_Spiral    = 6;
            midWeight_ObstacleBarrage = 5; midWeight_Railgun = 5; midWeight_MortarBarrage = 5;
            farWeight_LaserBeam    = 7; farWeight_DataStrike  = 6; farWeight_VirusSwarm = 5;
            farWeight_ObstacleBarrage = 5; farWeight_Railgun = 6; farWeight_MortarBarrage = 6;
            phaseCountMultiplier = 2.5f;
        }
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
