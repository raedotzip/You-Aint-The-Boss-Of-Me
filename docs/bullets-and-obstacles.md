# Bullets & Obstacles

Both bullets and obstacles are managed by dedicated singleton managers that share a single visual pool. Neither system uses `Instantiate` or `Destroy` at runtime — everything is pooled.

---

## The Bullet System

### How a Bullet Is Born

Every projectile in the game starts life as a `Bullet` struct. The flow from attack state to live bullet looks like this:

```
Boss attack state
  └─▶ boss.FireBullet(direction, attackData)        ← EnemyStateManager helper
        └─▶ BulletManager.SpawnBullet(bullet)        ← hands the struct to the manager
              ├─▶ BulletVisualPool.Instance.Spawn()  ← rents a visual GameObject from the pool
              └─▶ bullets.Add(b)                     ← struct goes into the live list
```

`FireBullet` is a convenience method on `EnemyStateManager`. It builds the struct from an `AttackData` ScriptableObject and the direction you pass in:

```csharp
// Inside any attack state — fire straight at the player
Vector3 dir = (boss.player.position - boss.transform.position).normalized;
boss.FireBullet(dir, boss.bulletData.myAttackData);
```

---

### The Bullet Struct

**File:** `Assets/Scripts/Combat/AttackTypes/BulletManager/Bullet.cs`

Bullets are value types (`struct`) not reference types (`class`). This means they live on the stack / in the list without heap allocation, so spawning many bullets per second doesn't cause garbage collection pauses.

```
Bullet
├── position          Current world position (updated every frame)
├── direction         Normalized travel direction
├── velocity          Used only by Arc movement (accumulates gravity)
├── speed             Units per second
├── damage            HP removed from player on hit
├── maxLifetime       Seconds before auto-despawn
├── collisionRadius   Hit detection radius (auto-scaled to match visual size)
├── scale             Visual size multiplier (0 = default 1.0)
├── canBeParried      Whether the sword can deflect it
├── destroyOnParry    Destroy immediately on parry instead of deflecting
├── isParried         Set true after sword deflects it — now damages boss instead of player
├── pendingDestroy    Set true on hit/parry — cleaned up at end of frame
├── movementType      Which BulletMovementType pattern to use
├── lifeTime          Elapsed time since spawn (internal counter)
├── spawnTime         Time.time at spawn (used by movement patterns)
├── attackData        Reference to the source AttackData asset
├── visual            The rented visual GameObject (position synced every frame)
└── visualPrefab      The original prefab asset (needed to return to correct pool queue)
```

---

### Every-Frame Loop (BulletManager.Update)

Each frame, `BulletManager` iterates the entire bullet list backwards (so removals don't break indices) and does four things per bullet:

1. **Move** — calls `BulletMovement.UpdateMovement(ref b, dt, globalTime)`
2. **Sync visual** — sets `b.visual.transform.position = b.position`
3. **Age** — increments `b.lifeTime`; removes bullet if `>= b.maxLifetime`
4. **Collision check**:
   - Non-parried bullets check distance to player center (eye height offset): if `dist <= collisionRadius + playerHitRadius` → `PlayerHealth.TakeDamage(b.damage)`
   - Parried bullets check distance to the active boss: if close enough → `BossManager.TakeDamageOnActive(parriedBossDamage)`

At the end of the loop there's a second pass to clean up any bullets marked `pendingDestroy` (set during parry detection, which runs in `FixedUpdate` and can't safely remove from the list mid-sweep).

After both passes, the spatial grid is rebuilt for the next parry detection call.

---

### Movement Types

**File:** `Assets/Scripts/Combat/AttackTypes/BulletManager/BulletMovement.cs`

Each bullet has a `BulletMovementType` that controls how its position updates. The math runs in `BulletMovement.UpdateMovement()`, a static class so there's no per-bullet overhead.

| Type | Behavior | Notes |
|------|---------|-------|
| `Straight` | `position += direction * speed * dt` | Default. Fast, predictable. |
| `Sine` | Moves forward while weaving side-to-side | Uses `Mathf.Sin(t * 5f)` on a perpendicular axis. Frequency hardcoded at 5. |
| `Spiral` | Curves in a loose spiral as it travels | Adds a rotating offset to the forward direction using `Sin(t * 6f)`. |
| `Arc` | Falls under gravity (18 m/s²) | Initializes `velocity` from `direction * speed` on first frame, then accumulates gravity. Visual rotates to match velocity direction. |

The `t` value used in Sine and Spiral is `globalTime - bullet.spawnTime` — per-bullet time, not global time. This means bullets fired at different moments weave out of sync with each other, which looks much more natural than having all bullets pulse together.

#### Adding a New Movement Type

1. Add a new entry to the `BulletMovementType` enum in `BulletMovementType.cs`
2. Add a `case` block in `BulletMovement.UpdateMovement()` that modifies `bullet.position` (and optionally `bullet.direction`)
3. Set the new type on your `AttackData` ScriptableObject

---

### The Visual Pool

**File:** `Assets/Scripts/Combat/AttackTypes/BulletManager/BulletVisualPool.cs`

The pool is a `Dictionary<GameObject, Queue<GameObject>>` — one `Queue` per prefab. This means you can have five different bullet prefabs all pooling independently without any cross-contamination.

#### Spawn

```
BulletVisualPool.Spawn(prefab, position, direction, scale)
  ├─ Look up the Queue for this prefab
  ├─ If Queue is empty → Prewarm(prefab, pool, 1)  ← grow dynamically
  ├─ Dequeue one GameObject
  ├─ Set position, rotation (forward = direction), scale
  └─ SetActive(true) → return it
```

#### Despawn

```
BulletVisualPool.Despawn(obj, prefab)
  ├─ SetActive(false)        ← hides it immediately
  └─ Enqueue back into the correct prefab's Queue
```

The `visualPrefab` field on the `Bullet` struct exists specifically so `DespawnBulletVisual` can pass the original prefab asset to `Despawn()` and return the object to the right queue. Without this, a bullet spawned from PrefabA could accidentally go into PrefabB's pool.

#### Prewarming

When `BulletManager` first sees a new prefab, it calls `Prewarm(prefab, pool, defaultPrewarmCount)` (default: **32 instances**). This happens lazily on the first `SpawnBullet` call for that prefab, not at scene load.

You can also prewarm explicitly at scene start for better performance:

```csharp
// Call this from a boss's Start() or from BossManager.SetActiveBoss()
BulletVisualPool.Instance.PrewarmPool(myBulletPrefab, 20);
```

`ObstacleManager.PrewarmObstaclePools(data)` does exactly this for obstacle visuals — 3 walls and 5 shockwaves are pre-allocated when a boss activates.

#### What "Pooling" Means in Practice

Without pooling, spawning a bullet calls `Instantiate` (slow, allocates memory) and destroying it calls `Destroy` (triggers garbage collection). Sixty bullets per second would cause visible frame stutter in VR.

With the pool, `Spawn` just dequeues an existing disabled object and enables it. `Despawn` disables it and enqueues it. No allocation, no GC. The pool grows automatically if demand exceeds supply (one extra object created on demand), so you never get a null bullet but you also don't pre-allocate memory you don't need.

---

### Adding a New Bullet Type

**Step 1 — Create an AttackData asset**

Right-click in the Project window → `Create → ScriptableObjects → AttackData`

| Field | What to set |
|-------|------------|
| `damage` | HP removed from player per hit |
| `bulletSpeed` | Units/second |
| `lifetime` | Seconds before auto-despawn |
| `collisionRadius` | Starting hit radius (auto-scales with `scale`) |
| `canBeParried` | True if the sword can deflect it |
| `destroyOnParry` | True to destroy on parry, false to deflect back |
| `movementType` | Pick from Straight / Sine / Spiral / Arc |
| `bulletPrefab` | A prefab with a Renderer — this is what the player sees |

**Step 2 — Reference it from the boss**

Assign the asset to the boss's `BossBulletData` ScriptableObject, or directly as a serialized field on your attack state.

**Step 3 — Fire it from an attack state**

```csharp
// Fire at player
Vector3 dir = (boss.player.position - boss.transform.position).normalized;
boss.FireBullet(dir, myAttackData);

// Fire in a spread (example: 5 bullets, 15° apart)
for (int i = -2; i <= 2; i++)
{
    Vector3 spreadDir = Quaternion.Euler(0, i * 15f, 0) * dir;
    boss.FireBullet(spreadDir, myAttackData);
}
```

**Step 4 — Make a visual prefab**

The prefab just needs a `Renderer` (MeshRenderer or SpriteRenderer). The pool handles position and scale — no scripts needed on the prefab itself. Keep it simple: a sphere primitive with a glowing material works fine.

---

## The Obstacle System

Obstacles work on the same pooling pattern as bullets. Both systems share `BulletVisualPool` — the name is a bit misleading, but it's a general-purpose visual pool.

### The Obstacle Struct

**File:** `Assets/Scripts/Combat/AttackTypes/ObstacleManager/Obstacle.cs`

```
Obstacle
├── position / rotation   World transform
├── phase                 Warning → Active → Dying
├── lifeTime / maxLifetime
├── pendingDestroy
│
├── shapeType             Box / Sphere / Cylinder
├── boxHalfExtents        (Box only)
├── sphereRadius          (Sphere only)
├── cylinderRadius        (Cylinder only)
├── cylinderHeight        (Cylinder only)
├── isHollow              True for ring/shockwave (damages if INSIDE ring)
├── innerRadius           Inner edge of hollow ring
│
├── warningDuration       Seconds in Warning phase before becoming dangerous
├── activeDuration        Seconds of active damage
│
├── movementType          ObstacleMovementType (Static, Linear, etc.)
├── velocity              Movement direction + speed
│
├── scalesOverTime        True for expanding shockwave rings
├── initialScale / finalScale
│
├── visual                Rented visual GameObject
└── visualPrefab          Original prefab (for pool return)
```

### The Warning Phase

The warning phase is what gives the player time to react. When an obstacle spawns with `warningDuration > 0`, it enters `ObstaclePhase.Warning` first. During this phase:

- The obstacle is **visible but does zero damage**
- The visual pulses in opacity: `alpha = 0.2 + Abs(Sin(t * π * 6)) * 0.6`
  - This creates an urgent flickering effect that builds as the warning time runs out
  - The frequency (6 oscillations per warning period) is tuned to feel like "get out now"
- The collision shape is **not yet active** — the player can stand inside it safely

When `lifeTime >= warningDuration`, the phase flips to `Active`:
- `OnBecomeActive()` is called — scales the visual to match the collision box, calls `WallObstacleVisual.SetActive()`
- Collision damage starts (`wallDamagePerSecond * deltaTime` per frame of overlap)

If `warningDuration = 0`, the obstacle skips straight to Active immediately on spawn — use this for instant traps or attacks where the telegraph happens through animation rather than the warning flicker.

### The Active Phase

During `ObstaclePhase.Active`, every frame the manager calls `OverlapsShape(obstacle, playerPosition, playerCollisionRadius)`. If it returns true, `PlayerHealth.TakeDamage(wallDamagePerSecond * dt)` is called.

Damage is per-second (default 10 HP/s), not per-hit, so moving through an obstacle quickly does less damage than standing in it.

The visual also gets a subtle sine bounce during Active (`Sin(activeElapsed * 1.2f) * 0.05m`) to make it feel alive.

### Shape Collision

| Shape | How overlap is tested |
|-------|-----------------------|
| `Box` | Rotates the player position into the obstacle's local space, then checks if it's within `halfExtents + playerRadius` on all three axes |
| `Sphere` | Simple distance check: `dist < sphereRadius + playerRadius` |
| `Cylinder` | Checks height (flat slab), then flat 2D distance. If `isHollow`, also checks inner radius — player is "inside" the ring if between `innerRadius` and `cylinderRadius`. Expanding shockwave rings use this. |

### Expanding Rings

Shockwave obstacles use `scalesOverTime = true`. During the Active phase, the manager linearly interpolates `cylinderRadius` from `initialScale.x / 2` to `finalScale.x / 2` over `activeDuration`. It also drives the `ShockwaveVisual` component with the elapsed time so the visual wave animation stays in sync with the collision.

### Adding a New Obstacle Type

**Step 1 — Build the Obstacle struct in your attack state:**

```csharp
Obstacle wall = new Obstacle
{
    position        = spawnPosition,
    rotation        = Quaternion.identity,
    shapeType       = ObstacleShapeType.Box,
    boxHalfExtents  = new Vector3(4f, 2f, 0.3f),  // wide, tall, thin wall
    warningDuration = 0.8f,   // 0.8s of flickering warning
    activeDuration  = 2.0f,   // 2s of active damage
    movementType    = ObstacleMovementType.Static,
    visualPrefab    = obstacleData.wallPrefab,
};

ObstacleManager.Instance.SpawnObstacle(wall);
```

**Step 2 — Visual prefab**

The prefab needs a `Renderer`. If it has a `WallObstacleVisual` component, `SetWarning(alpha)`, `SetActive()`, and `SetDying()` will be called automatically on phase transitions for visual feedback. If it has a `ShockwaveVisual` component, the wave animation is driven by the manager.

You can use a plain cube or sphere prefab too — the manager will scale it to match the collision box on activation.

**Step 3 — Prewarm (optional but recommended)**

If your boss uses this obstacle frequently, prewarm the pool before the fight:

```csharp
BulletVisualPool.Instance.PrewarmPool(obstacleData.wallPrefab, 4);
```

Call this from `BossManager.SetActiveBoss()` or from the boss's `Start()`.
