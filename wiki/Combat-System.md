# Combat System

---

## Sword

**File:** `Assets/Scripts/Combat/Sword/Sword.cs`

The player's only weapon. Attached to the player's hand transform and moves with it via VR tracking. Every frame it sweeps along the blade to detect hits, scans for menu boxes and mini computers, and checks for bullets to parry.

### Blade Points

Two transforms define the blade:
- `bladeBase` — At the guard/handle
- `bladeTip` — At the point

The sweep casts spheres at 5 equally spaced points along the blade (base, 25%, 50%, 75%, tip) and checks each one for motion-based hits.

### Melee Hit Detection

Every `FixedUpdate`, the sword runs a `SphereCastAll` from each blade point's previous position to its current position. If it hits something on `meleeParryLayer` or `menuLayer`, it checks what it hit:

1. **Mini computer** → `Boss2MiniComputer.TakeDamage()`
2. **Menu box** → `MenuBox.OnSliced()`
3. **Boss** → `BossManager.TakeDamageOnActive(damage * swingMultiplier * limbMultiplier)`

There's a per-Rigidbody hit cooldown (default 0.2s) to prevent rapid-fire hits from a single swing.

### Swing Damage Scaling

The sword tracks the arc distance the blade tip travels during a swing. This is used to scale damage:

| Swing type | Tip distance | Multiplier |
|------------|-------------|-----------|
| Flick / wrist snap | ≤ 0.06m | 0.1× |
| Medium swing | (interpolated) | 0.1–2.0× |
| Full arm swing | ≥ 0.50m | 2.0× |

`Mathf.InverseLerp(minSwingDistance, maxSwingDistance, swingTipDistance)` gives a 0–1 value, then `Mathf.Lerp(minMultiplier, maxMultiplier, t)` converts it to the multiplier.

### Limb Damage Multipliers

If the hit collider has a `BossHitbox` component, its `damageMultiplier` is also applied:

```
final damage = damageAmount × swingMultiplier × limbMultiplier
```

See the [Boss System](Boss-System#limb-hitboxes) page for setup instructions.

### Bullet Parrying

Every `FixedUpdate`, `ParryBullets()` is called. It passes the sword's OBB (oriented bounding box from the `MeshRenderer.bounds`) and current velocity to `BulletManager.TryParryBullets()`.

A bullet is parried if it overlaps the sword's OBB. Parried bullets:
- Turn cyan
- Stop damaging the player
- Travel in the direction of the sword's velocity (or reflect 180° if the sword is still)
- Deal `parriedBossDamage` to the boss on impact

**Parry angle:** The sword's facing direction vs. the bullet's incoming direction must be within 70° for a standard parry. During the first `perfectWindow` seconds of a swing (default 0.15s), the window is the full 70°. Outside that window it's 49°.

### Life Steal

If `swingTipDistance >= healSwingThreshold` (0.35m — a proper arm swing) and the sword hits a boss, `PlayerHealth.Heal(healAmount)` is called. Flicks don't heal; you have to commit to a full swing.

### Hit Stop

On every boss hit, the game briefly freezes at 85% time scale for 0.05 seconds. This gives a satisfying impact feel in VR.

### Inspector Fields

| Field | Default | Description |
|-------|---------|-------------|
| `sphereRadius` | 0.025m | Hit detection sphere size |
| `hitCooldown` | 0.2s | Min time between hits on same object |
| `damageAmount` | 10 | Base damage per hit |
| `minSwingDistance` | 0.06m | Tip distance for min damage |
| `maxSwingDistance` | 0.50m | Tip distance for max damage |
| `minDamageMultiplier` | 0.1 | Multiplier at min swing |
| `maxDamageMultiplier` | 2.0 | Multiplier at max swing |
| `healSwingThreshold` | 0.35m | Min swing distance to trigger life steal |
| `healAmount` | 5 | HP restored on qualifying hit |
| `parryAngle` | 70° | Sword angle for successful parry |
| `speedMultiplier` | 1.5 | Bullet speed boost after parry |
| `hitStopDuration` | 0.05s | Length of hit stop freeze |
| `hitStopScale` | 0.85 | Time scale during hit stop |

---

## Bullets

**File:** `Assets/Scripts/Combat/AttackTypes/BulletManager/BulletManager.cs`  
**Struct:** `Assets/Scripts/Combat/AttackTypes/BulletManager/Bullet.cs`

### Firing a Bullet

Boss attack states call `boss.FireBullet(direction, attackData)` on their `EnemyStateManager` reference. This builds a `Bullet` struct and passes it to `BulletManager.SpawnBullet()`.

```csharp
// Example from an attack state
Vector3 dir = (boss.player.position - boss.transform.position).normalized;
boss.FireBullet(dir, boss.bulletData.myAttackData);
```

### Bullet Movement Types

Set in `AttackData.movementType`:

| Type | Behavior |
|------|---------|
| `Straight` | Straight line at constant speed |
| `Sine` | Weaves side-to-side |
| `Spiral` | Curves in a spiral |
| `Arc` | Falls under gravity |

### AttackData ScriptableObject

**File:** `Assets/Scripts/Combat/AttackTypes/AttackData.cs`

Create one per attack type. Right-click in Project → `Create → ScriptableObjects → AttackData`.

| Field | Description |
|-------|-------------|
| `damage` | Damage dealt to the player |
| `bulletSpeed` | How fast the bullet travels |
| `lifetime` | Seconds before the bullet despawns |
| `collisionRadius` | Hit detection radius |
| `canBeParried` | Whether the sword can deflect it |
| `destroyOnParry` | Destroy on parry vs. deflect and return |
| `movementType` | Movement pattern enum |
| `bulletPrefab` | Visual prefab (pooled) |

### Parried Bullet Damage

When a parried bullet reaches the boss, `BossManager.TakeDamageOnActive(parriedBossDamage)` is called. The `parriedBossDamage` value is set on `BulletManager` in the Inspector (default: 5).

---

## Obstacles

**File:** `Assets/Scripts/Combat/AttackTypes/ObstacleManager/ObstacleManager.cs`

Obstacles are arena hazards — walls, rings, falling blocks — that boss attacks spawn. Like bullets, they're pooled structs.

### Spawning an Obstacle

Boss attack states call `ObstacleManager.Instance.SpawnObstacle(obstacleData)`.

### Obstacle Shapes

| Shape | Fields used | Collision |
|-------|------------|-----------|
| Box | `boxHalfExtents` | Rotated AABB |
| Sphere | `sphereRadius` | Distance check |
| Cylinder | `cylinderRadius`, `cylinderHeight` | Flat distance + height |
| Hollow Ring | `cylinderRadius`, `cylinderHeight`, `isHollow = true` | Damages if *inside* the ring — player must be outside or find the gap |

### Phases

| Phase | Duration | Effect |
|-------|----------|--------|
| Warning | `warningDuration` | Visible but harmless. Player has time to dodge. |
| Active | `activeDuration` | Deals `wallDamagePerSecond` to overlapping player |
| Dying | (short) | Fading out, no damage |

### Scale Animation

Obstacles can grow over their lifetime (used for expanding shockwave rings). Set `scalesOverTime = true` and configure `initialScale` / `finalScale` on the obstacle data.

---

## Menu Boxes

**File:** `Assets/Scripts/Menu/MenuBox.cs`

Interactive menu objects that the player slices to start a fight. The sword detects them the same way it detects boss hits — via the `ScanMenuBoxesAlongBlade()` overlap scan.

### How a Slice Works

1. Sword blade overlaps the box's collider
2. `MenuBox.OnSliced()` is called
3. Box flashes white and scales up to 1.25× briefly
4. `onSliced` UnityEvent is invoked
5. `MenuController.StartBoss1()` or `StartBoss2()` fires (wired in Inspector)
6. Screen fades, player teleports, fight begins

### Hover Feedback

While the blade tip is within `hoverRadius` (0.45m) of the box center, the box glows cyan. This gives the player visual feedback about where to slice.

### Inspector Fields

| Field | Description |
|-------|-------------|
| `label` | Text displayed on the box face |
| `hoverRadius` | Distance at which the box starts glowing |
| `onSliced` | UnityEvent — wire to `MenuController.StartBoss1()` or `StartBoss2()` |
| `normalColor` | Default color (dark blue) |
| `hoverColor` | Hover color (cyan) |
| `sliceColor` | Slice flash color (white) |
