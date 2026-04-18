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
Boss 1 Arena (player teleported, Boss 1 activated, HUD shown)
    │
    │  Boss 1 health reaches 0
    ▼
Fade to black
    │
    ▼
Boss 2 Arena (player teleported, Boss 2 activated)
    │
    │  Boss 2 health reaches 0
    ▼
Fade to black
    │
    ▼
Menu (menu shown, boxes reset, HUD hidden)
```

The transition logic lives in `MenuController`. Boss defeat logic lives in each boss's `TakeDamage()` method, which calls `MenuController.Instance.AdvanceToNextBoss()`.

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
- `boss1SpawnPoint` — Boss 1 arena start position
- `boss2SpawnPoint` — Boss 2 arena start position

All referenced by `MenuController`.

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
│   └── BossHitbox.cs
├── Menu/
│   ├── MenuController.cs
│   └── MenuBox.cs
└── Player/
    ├── PlayerMovement.cs
    ├── PlayerHealth.cs
    └── HUDManager.cs
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
