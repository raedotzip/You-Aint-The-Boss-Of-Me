# Managers

All managers are singletons. Each sets `Instance = this` in `Awake()` and is accessible globally via `ClassName.Instance`. They all live on a single persistent GameObject in the scene.

---

## BossManager

**File:** `Assets/Scripts/Enemies/BossManager.cs`

The central hub for boss fights. It knows which boss is currently active, enables/disables bosses when the fight changes, and routes all incoming damage to the right target.

### Inspector Fields

| Field | Description |
|-------|-------------|
| `boss1` | Reference to the Boss1StateManager in the scene |
| `boss2` | Reference to the Boss2StateManager in the scene |
| `startingBoss` | Which boss is active on scene load (0 = menu, 1 = Boss 1, 2 = Boss 2). Useful for testing a specific boss without going through the menu. |
| `boss1Name` | Display name shown in the HUD ("Roe Jogan") |
| `boss2Name` | Display name shown in the HUD ("The Mainframe") |
| `boss1ShowsBarImmediately` | If true, the health bar appears as soon as Boss 1 starts. Default: true. |
| `boss2ShowsBarImmediately` | If true, Boss 2's health bar appears immediately. Default: false (bar only shows after mini computers are destroyed). |

### Key Methods

**`SetActiveBoss(int bossIndex)`**  
The main transition method. Deactivates the current boss, activates the new one, and updates the HUD.
- `0` — No boss (between fights / menu)
- `1` — Activates Boss 1, wires its health bar, shows boss name
- `2` — Activates Boss 2

Call this whenever you want to switch which boss is fighting. `MenuController` calls this automatically during transitions.

**`TakeDamageOnActive(float amount)`**  
Routes damage to whichever boss is currently active. Called by `BulletManager` when a parried bullet hits a boss, and by `Sword` on melee hits.

**`GetActiveBoss()`**  
Returns the active `EnemyStateManager`, or `null` if no boss is active. Used by `BulletManager` to check if a parried bullet should deal damage.

### How It Connects

```
MenuController.StartBoss(1)
    └─▶ BossManager.SetActiveBoss(1)
            ├─▶ boss1.enabled = true
            └─▶ HUDManager.SetBossName("Roe Jogan")
                HUDManager.ShowBossBar(true)

Sword hits boss
    └─▶ BossManager.TakeDamageOnActive(damage)
            └─▶ boss1.TakeDamage(damage)

Parried bullet hits boss
    └─▶ BulletManager detects collision
            └─▶ BossManager.TakeDamageOnActive(parriedBossDamage)
```

---

## MenuController

**File:** `Assets/Scripts/Menu/MenuController.cs`

Manages the entire single-scene flow: showing/hiding the menu, teleporting the player, starting boss fights, and handling screen fades. This is the main game flow controller.

### Inspector Fields

| Field | Description |
|-------|-------------|
| `menuSphere` | The menu area GameObject — shown in menu, hidden during fights |
| `menuBoxes[]` | All MenuBox GameObjects — shown in menu, hidden during fights |
| `menuSpawnPoint` | Where the player stands in the menu |
| `boss1SpawnPoint` | Where the player appears for Boss 1 |
| `boss2SpawnPoint` | Where the player appears for Boss 2 |
| `player` | The player's root GameObject (needs `CharacterController`) |
| `fadeDuration` | How long the black screen fade takes in seconds (default: 0.4) |

### Key Methods

**`StartBoss(int bossIndex)`**  
Starts a boss fight with a screen fade. Hides the menu, teleports the player, shows the HUD, and activates the boss. Wire this to a `MenuBox.onSliced` UnityEvent in the Inspector. Use `StartBoss1()` or `StartBoss2()` as the parameterless versions for UnityEvent wiring.

**`AdvanceToNextBoss(int completedBossIndex)`**  
Called by a boss when it dies. If there's a next boss (`completedBossIndex + 1 <= 2`), transitions to it. Otherwise returns to the menu.

**`ReturnToMenu()`**  
Fades to black, deactivates all bosses, shows the menu, teleports the player back, and resets all menu boxes so they can be sliced again.

**`QuitGame()`**  
Exits the application (uses `EditorApplication.isPlaying = false` in the editor).

### Fade System

Fades use `SteamVR_Fade.View()` which fades the actual VR compositor — both eyes go black simultaneously, which feels clean in headset.

Sequence for every transition:
1. `SteamVR_Fade.View(Color.black, fadeDuration)` — fade to black
2. `yield return new WaitForSeconds(fadeDuration)` — wait for black
3. Move player, change game state
4. `SteamVR_Fade.View(Color.clear, fadeDuration)` — fade back in

### Wiring Menu Boxes

Each `MenuBox` in the scene has an `onSliced` UnityEvent. Wire it in the Inspector:

- "Play Boss 1" box → `MenuController.StartBoss1()`
- "Play Boss 2" box → `MenuController.StartBoss2()`

---

## HUDManager

**File:** `Assets/Scripts/Player/HUDManager.cs`

Controls all player-visible UI. Hides everything during the menu and shows it during fights.

### Inspector Fields

| Field | Description |
|-------|-------------|
| `playerBar` | `HealthBarUI` component for the player's health bar |
| `bossBar` | `HealthBarUI` component for the boss's health bar |
| `bossBarContainer` | Parent GameObject of the boss bar — toggled for visibility |
| `bossNameText` | TextMeshPro text showing the boss name |
| `hudRoot` | Root of the entire HUD — hidden while in the menu |

### Key Methods

**`ShowHUD(bool show)`** — Toggles the entire HUD on/off.  
**`ShowBossBar(bool show)`** — Toggles just the boss health bar.  
**`SetBossName(string name)`** — Updates the boss name text.

`BossManager` calls `SetBossName` and `ShowBossBar` whenever a boss is activated.

---

## BulletManager

**File:** `Assets/Scripts/Combat/AttackTypes/BulletManager/BulletManager.cs`

Handles the entire lifecycle of all projectiles in the game. Boss attack states create bullets by calling `EnemyStateManager.FireBullet()`, which passes a `Bullet` struct to `BulletManager.SpawnBullet()`.

### How Bullets Work

Bullets are stored as structs in a `List<Bullet>` — no MonoBehaviours, no GameObjects with scripts. Each frame, the manager loops over the list and updates every bullet's position, checks collisions, handles lifetime, and syncs the visual prefab.

**Bullet lifecycle:**
1. Boss calls `FireBullet(direction, attackData)`
2. Manager creates a `Bullet` struct, rents a visual from `BulletVisualPool`
3. Every `Update`: position moves, phase synced, collisions checked
4. On player hit: `PlayerHealth.TakeDamage(bullet.damage)`
5. On parried bullet hitting boss: `BossManager.TakeDamageOnActive(parriedBossDamage)`
6. On expiry/hit: visual returned to pool, bullet removed from list

### Parry Detection

`Sword.cs` calls `TryParryBullets()` every `FixedUpdate`. The manager uses a **spatial grid** (2m cells) to quickly find bullets near the sword, then does an OBB (oriented bounding box) narrowphase test. Parried bullets turn cyan and have `isParried = true` — they then damage the boss instead of the player.

### Key Inspector Fields

| Field | Description |
|-------|-------------|
| `playerHitRadius` | How close a bullet needs to be to the player's center to deal damage (0.5m) |
| `playerEyeHeight` | Vertical offset for the player hit center (1.0m above player root) |
| `bossHitRadius` | How close a parried bullet needs to be to the boss (1.5m) |
| `parriedBossDamage` | How much damage each parried bullet deals to the boss |

---

## ObstacleManager

**File:** `Assets/Scripts/Combat/AttackTypes/ObstacleManager/ObstacleManager.cs`

Same pooling pattern as `BulletManager` but for arena obstacles (walls, shockwave rings, falling blocks). Boss attack states call `SpawnObstacle()`.

### Obstacle Phases

Every obstacle moves through three phases:

| Phase | Description |
|-------|-------------|
| **Warning** | Obstacle is visible but not yet dangerous. Gives the player time to react. |
| **Active** | Deals `wallDamagePerSecond` to the player if they're overlapping it. |
| **Dying** | Fading out, despawning soon. No damage. |

### Obstacle Shapes

The manager supports three collision shapes:
- **Box** — Rotated bounding box
- **Sphere** — Distance check
- **Cylinder / Hollow Ring** — Flat distance + height; hollow rings only damage if you're inside the ring radius (used for shockwaves you need to jump over or find the gap in)
