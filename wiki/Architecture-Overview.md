# Architecture Overview

## Single-Scene Design

The entire game runs in one persistent Unity scene: `Assets/Scenes/AnimationTest.unity`. There are no scene loads during gameplay. All boss arenas, the menu area, spawn points, and managers coexist in the same scene. Objects are enabled and disabled as the player progresses.

This keeps transitions fast (no load screens) and makes it easy to share state between systems without any cross-scene communication.

---

## Game Flow

```
Game Start
    │
    ▼
Menu (menuSphere visible, HUD hidden)
    │
    │  Player slices a MenuBox
    ▼
Fade to black
    │
    ▼
Lab (player teleported to labSpawnPoint, HUD shown, timer starts)
    │
    │  Player walks into Boss 1 arena entrance
    │  BossArenaTrigger fires → BossManager.SetActiveBoss(1)
    ▼
Boss 1 Arena (Boss 1 activated)
    │
    │  Boss 1 health reaches 0
    ▼
Boss 2 Arena (player walks through arena trigger → Boss 2 activated)
    │
    │  Boss 2 health reaches 0
    ▼
Fade to black (3s delay)
    │
    ▼
Menu (menu shown, You Win box visible, boxes reset, HUD hidden)
    │
    │  Player dies at any point
    ▼
Fade to black → Menu (boxes reset, no You Win box)
```

The transition logic lives in `MenuController`. Boss arenas are entered by walking through a `BossArenaTrigger` volume — not directly from menu box slices. Boss defeat logic lives in each boss's `TakeDamage()` method, which calls `MenuController.Instance.AdvanceToNextBoss()`.

---

## Singleton Managers

All major systems are singletons — one instance per scene, accessible globally via `ClassName.Instance`. They set themselves in `Awake()`.

| Manager | Purpose |
|---------|---------|
| `BossManager` | Tracks which boss is active, routes damage |
| `MenuController` | Handles menu flow, teleportation, fade transitions |
| `HUDManager` | Controls all on-screen UI (health bars, boss name) |
| `BulletManager` | Spawns, moves, and pools all projectiles |
| `ObstacleManager` | Spawns, phases, and pools all arena obstacles |

Because they're singletons, any script can call e.g. `BossManager.Instance.TakeDamageOnActive(10f)` without needing a direct Inspector reference.

---

## Key GameObjects in the Scene

### Managers Object
A single persistent GameObject with all manager components attached (`BossManager`, `MenuController`, `HUDManager`, `BulletManager`, `ObstacleManager`).

### Player
Root GameObject with `CharacterController`, `PlayerMovement`, and `PlayerHealth`. The sword is a child of the player's hand transform and moves with it automatically via VR tracking.

### Boss1 / Boss2
Each boss is a skinned mesh driven by an `Animator`. The state manager script (`Boss1StateManager` / `Boss2StateManager`) is on the root GameObject. Bosses are disabled (`boss.enabled = false`) when not the active fight — `BossManager` enables them on demand.

### Menu Sphere
The spherical menu area. Contains child `MenuBox` GameObjects (the interactive option boxes). Hidden via `SetActive(false)` during boss fights.

### Spawn Points
Empty GameObjects marking where the player is placed:
- `menuSpawnPoint` — Inside the menu sphere
- `labSpawnPoint` — Where the player lands after slicing a menu box (the lab connecting area)
- `boss1SpawnPoint` — Boss 1 arena start position (used when `startingBoss = 1` in the Inspector, bypasses menu)
- `boss2SpawnPoint` — Boss 2 arena start position (used when `startingBoss = 2`)

All referenced by `MenuController`.

### Boss Arena Triggers
**File:** `Assets/Scripts/Enemies/BossArenaTrigger.cs`

Each boss arena entrance has a `BossArenaTrigger` component on a trigger `BoxCollider`. When the player walks (or dashes) through it, the trigger calls `BossManager.SetActiveBoss(bossIndex)` to start the fight. A polling fallback runs every frame so fast dashes that skip `OnTriggerEnter` are still caught. Triggers reset when `BossManager.ResetRun()` is called (on menu return).

---

## Folder Structure

```
Assets/Scripts/
├── Asher Animation Tests/   Boss state machines + all attack state scripts
│   └── States/
│       ├── Attacks/Boss1/   One .cs file per Boss 1 attack
│       ├── Attacks/Boss2/   One .cs file per Boss 2 attack
│       ├── Idle/
│       └── Movement/
├── Combat/
│   ├── Sword/               Sword.cs — melee detection, parrying
│   └── AttackTypes/
│       ├── BulletManager/   Bullet struct, BulletManager, movement
│       └── ObstacleManager/ Obstacle struct, ObstacleManager, movement
├── Enemies/
│   ├── EnemyStateManager/   Abstract base classes for all bosses
│   ├── BossManager.cs
│   ├── BossHitbox.cs
│   ├── BossHitFlash.cs      Visual flash on boss damage
│   ├── BossArenaTrigger.cs  Activates a boss when the player enters the arena
│   ├── Boss2MiniComputer.cs Mini computer entities for Boss 2's phase gate
│   └── Boss2Obstacle.cs     Boss 2 walkway obstacle behaviour
├── MapHighlights/           Mini-map highlight system (MapHighlightManager, MapShape, etc.)
├── Menu/
│   ├── MenuController.cs
│   ├── MenuBox.cs
│   └── MenuManager.cs
└── Player/
    ├── PlayerMovement.cs
    ├── PlayerHealth.cs
    ├── HUDManager.cs
    ├── BossPortraitHUD.cs   Shows boss portrait art during fights
    ├── LoreTyper.cs         Typewriter-style lore text on the HUD
    ├── LoreTrigger.cs       Trigger volume that fires lore messages
    ├── VRHudFollow.cs       Keeps HUD anchored to the VR camera
    └── Teleport.cs          Trigger-based player teleport volume
```

---

## ScriptableObjects

Attack behavior is data-driven. Configuration lives in ScriptableObject assets rather than hardcoded values, so you can tweak stats in the Inspector without touching code.

| Asset | Controls |
|-------|---------|
| `AttackData` | Bullet speed, damage, lifetime, parry rules, movement type |
| `BossBulletData` | Which bullet prefabs a boss uses |
| `BossObstacleData` | Which obstacle prefabs (walls, shockwaves) a boss uses |

Assets are stored under `Assets/Scripts/ScriptableObjects/Boss1/` and `Boss2/`.

---

## Object Pooling

Bullets and obstacles are pooled for performance. Neither system uses `Instantiate`/`Destroy` at runtime — visuals are rented from `BulletVisualPool` and returned when the bullet or obstacle expires. The bullet and obstacle data themselves are stored as structs in a `List<>`, avoiding garbage collection pressure.
