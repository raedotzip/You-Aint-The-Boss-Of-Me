using System.Collections.Generic;
using UnityEngine;

public class BulletManager : MonoBehaviour
{
    public static BulletManager Instance;
    private Transform player;
    private PlayerHealth playerHealth;
    [SerializeField] private float playerHitRadius  = 0.5f;
    [SerializeField] private float playerEyeHeight  = 1.0f; // offset from player root to chest/eye
    [SerializeField] private float bossHitRadius   = 1.5f;
    [SerializeField] private float parriedBossDamage = 5f;
    [SerializeField] private LayerMask mapWallLayer;

    // ===============================
    // BULLET STORAGE
    // ===============================
    private readonly List<Bullet> bullets = new List<Bullet>();

    // ===============================
    // SPATIAL GRID
    // ===============================
    private readonly Dictionary<Vector3Int, List<int>> grid = new Dictionary<Vector3Int, List<int>>(1024);
    [SerializeField] private float cellSize = 2f;

    // ===============================
    // UNITY
    // ===============================

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            player       = p.transform;
            playerHealth = p.GetComponent<PlayerHealth>();
        }

        // Boss damage is now routed through BossManager so any boss can receive parry damage
    }

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        float globalTime = Time.time;

        for (int i = bullets.Count - 1; i >= 0; i--)
        {
            Bullet b = bullets[i];

            // Movement
            BulletMovement.UpdateMovement(ref b, dt, globalTime);

            // Sync visual
            if (b.visual != null)
                b.visual.transform.position = b.position;

            // Lifetime
            b.lifeTime += dt;

            if (b.lifeTime >= b.maxLifetime)
            {
                DespawnBulletVisual(b);
                bullets.RemoveAt(i);
                continue;
            }

            // Destroy bullets that hit a wall/floor geometry
            if (mapWallLayer != 0 && Physics.CheckSphere(b.position, b.collisionRadius, mapWallLayer, QueryTriggerInteraction.Ignore))
            {
                DespawnBulletVisual(b);
                bullets.RemoveAt(i);
                continue;
            }

            bullets[i] = b;

            if (!b.pendingDestroy)
            {
                // Player collision — only for non-parried bullets
                if (!b.isParried && player != null)
                {
                    Vector3 playerCenter = player.position + Vector3.up * playerEyeHeight;
                    float dist = Vector3.Distance(b.position, playerCenter);
                    if (dist <= b.collisionRadius + playerHitRadius)
                    {
                        playerHealth?.TakeDamage(b.damage);
                        b.pendingDestroy = true;
                        bullets[i] = b;
                        continue;
                    }
                }

                // Boss collision — only for parried bullets
                if (b.isParried && BossManager.Instance != null)
                {
                    EnemyStateManager activeBoss = BossManager.Instance.GetActiveBoss();
                    if (activeBoss != null)
                    {
                        float dist = Vector3.Distance(b.position, activeBoss.transform.position);
                        if (dist <= b.collisionRadius + bossHitRadius)
                        {
                            BossManager.Instance.TakeDamageOnActive(parriedBossDamage);
                            b.pendingDestroy = true;
                            bullets[i] = b;
                            continue;
                        }
                    }
                }
            }
        }

        // Safe removal pass — handles bullets marked by parry
        for (int i = bullets.Count - 1; i >= 0; i--)
        {
            if (bullets[i].pendingDestroy)
            {
                DespawnBulletVisual(bullets[i]);
                bullets.RemoveAt(i);
            }
        }

        RebuildGrid();
    }

    // ===============================
    // BULLET SPAWN (from full Bullet struct)
    // ===============================
    public void SpawnBullet(Bullet b)
    {
        if (b.visualPrefab == null)
        {
            Debug.LogWarning("Bullet visualPrefab is null. Cannot spawn.");
            return;
        }

        // Apply default scale if the spawner didn't set one
        if (b.scale <= 0f) b.scale = 1f;

        // Scale collision radius to match visual size — existing explicit values are preserved
        // only if they were already scaled for a non-default bullet size.
        // Scale the collision to match: base radius 0.3 * (scale/1) so bigger bullets are easier to hit
        b.collisionRadius = Mathf.Max(b.collisionRadius, 0.3f * b.scale);

        // Spawn pooled visual from the prefab — apply scale
        b.visual = BulletVisualPool.Instance.Spawn(b.visualPrefab, b.position, b.direction, b.scale);

        // Initialize timing
        b.spawnTime = Time.time;
        b.lifeTime = 0f;
        b.pendingDestroy = false;

        bullets.Add(b);
    }

    private void DespawnBulletVisual(Bullet b)
    {
        if (b.visual != null && b.visualPrefab != null)
        {
            // Pass the instance and the original prefab so the pool
            // returns it to the correct queue
            BulletVisualPool.Instance.Despawn(b.visual, b.visualPrefab);
        }
    }

    // ===============================
    // SPATIAL HASH
    // ===============================
    Vector3Int GetCell(Vector3 pos)
    {
        return new Vector3Int(
            Mathf.FloorToInt(pos.x / cellSize),
            Mathf.FloorToInt(pos.y / cellSize),
            Mathf.FloorToInt(pos.z / cellSize)
        );
    }

    void RebuildGrid()
    {
        grid.Clear();

        for (int i = 0; i < bullets.Count; i++)
        {
            Vector3Int cell = GetCell(bullets[i].position);

            if (!grid.TryGetValue(cell, out var list))
            {
                list = new List<int>(8);
                grid[cell] = list;
            }

            list.Add(i);
        }
    }

    // ===============================
    // OPTIMIZED PARRY
    // ===============================
    // Called every FixedUpdate by Sword. swordVelocity may be zero (sword held still).
    // Any bullet touching the sword OBB is parried — no swing required.
    // If the sword is moving, parried bullets reflect along sword velocity.
    // If the sword is stationary, parried bullets reflect back toward the boss.
    public void TryParryBullets(
        Vector3 swordCenter,
        Transform swordTransform,
        Vector3 halfExtents,
        float boundingSphereRadius,
        Vector3 swordVelocity,
        float parryAngle,
        float speedMultiplier)
    {
        bool swordMoving     = swordVelocity.sqrMagnitude > 0.01f;
        Vector3 swordVelNorm = swordMoving ? swordVelocity.normalized : Vector3.zero;

        int cellRadius     = Mathf.CeilToInt((boundingSphereRadius + 1f) / cellSize);
        Vector3Int centerCell = GetCell(swordCenter);

        for (int x = -cellRadius; x <= cellRadius; x++)
            for (int y = -cellRadius; y <= cellRadius; y++)
                for (int z = -cellRadius; z <= cellRadius; z++)
                {
                    Vector3Int cell = centerCell + new Vector3Int(x, y, z);

                    if (!grid.TryGetValue(cell, out var bulletIndices))
                        continue;

                    for (int k = 0; k < bulletIndices.Count; k++)
                    {
                        int i    = bulletIndices[k];
                        Bullet b = bullets[i];

                        if (!b.canBeParried || b.pendingDestroy || b.isParried)
                            continue;

                        // Broadphase sphere check
                        Vector3 toBullet = b.position - swordCenter;
                        float combined   = boundingSphereRadius + b.collisionRadius;
                        if (toBullet.sqrMagnitude > combined * combined)
                            continue;

                        // OBB narrowphase
                        Vector3 local = swordTransform.InverseTransformPoint(b.position);
                        if (Mathf.Abs(local.x) > halfExtents.x + b.collisionRadius) continue;
                        if (Mathf.Abs(local.y) > halfExtents.y + b.collisionRadius) continue;
                        if (Mathf.Abs(local.z) > halfExtents.z + b.collisionRadius) continue;

                        // ---- Bullet is touching the sword — parry it ----
                        if (b.destroyOnParry)
                        {
                            b.pendingDestroy = true;
                            bullets[i] = b;
                            continue;
                        }

                        // Reflect direction:
                        // Sword moving  → deflect along sword velocity (player-aimed)
                        // Sword still   → reflect straight back along incoming direction
                        if (swordMoving)
                            b.direction = swordVelNorm;
                        else
                            b.direction = -b.direction; // straight back the way it came

                        b.speed    *= speedMultiplier;
                        b.isParried = true;
                        bullets[i]  = b;

                        if (b.visual != null)
                            b.visual.GetComponent<Renderer>().material.color = Color.cyan;
                    }
                }
    }
}