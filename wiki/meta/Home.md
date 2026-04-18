# Boss Rush VR — Wiki

Welcome to the developer wiki for Boss Rush VR. This covers how the game is built, how each system works, and how to extend the game with new content.

**Course:** CSCE352 / EMAR440 — Exploring Virtual Reality  
**Authors:** Raegan Scheet, Asher Lahm, Kenny Kouete

---

## What Is This Game?

A speed-running VR boss rush. The player stands in a menu sphere, slices a box with their sword to start a fight, and battles two bosses back to back. After both are defeated, the game returns to the menu. The goal is to beat both bosses as fast as possible.

The player has one weapon (a motion-tracked sword), one movement ability (a dash), and two defensive tools (parrying bullets back and life steal on wide swings).

---

## Quick Start (For Developers)

1. Open `Assets/Scenes/AnimationTest.unity`
2. Press **Play** in Unity
3. Slice a menu box with the sword to start Boss 1
4. To skip to a specific boss, set `BossManager.startingBoss` to `1` or `2` in the Inspector before pressing Play

---

## How the Game Is Structured

The entire game runs in **one scene**. There are no scene loads. All bosses, the menu area, spawn points, and manager systems coexist in `AnimationTest.unity`. Objects are enabled and disabled as needed.

The flow is:

```
Menu  →  Boss 1  →  Boss 2  →  Menu
```

`MenuController` owns this flow. Boss defeat calls `MenuController.AdvanceToNextBoss()`. Transitions fade to black using SteamVR's compositor fade.

---

## Documentation Pages

### Getting Oriented
- [Architecture Overview](Architecture-Overview) — Scene structure, game flow diagram, singleton managers, folder layout, ScriptableObjects, pooling

### Systems
- [Managers](Managers) — BossManager, MenuController, HUDManager, BulletManager, ObstacleManager — what each one does and how to call it
- [Boss System](Boss-System) — State machine pattern, Boss 1 AI and attack table, Boss 2 phase gate, limb hitboxes
- [Bullets & Obstacles](Bullets-and-Obstacles) — Full deep-dive: the Bullet struct, BulletManager update loop, movement types, the visual pool, warning phase, obstacle shapes, how to add new bullets and obstacles
- [Combat System](Combat-System) — Sword hit detection, swing damage scaling, parrying, life steal, hit stop, menu box interaction
- [Player System](Player-System) — VR locomotion, dash mechanic (wall detection, ledge detection, slope handling), health, HUD, teleport sequence

### Guides
- [Adding a Boss](Adding-a-Boss) — Complete step-by-step: StateManager, attack states, ScriptableObjects, scene setup, BossManager wiring, MenuController wiring, checklist

---

## Key Scripts at a Glance

| Script | Location | What It Does |
|--------|----------|-------------|
| `BossManager` | `Scripts/Enemies/` | Tracks active boss, routes damage from sword and parried bullets |
| `MenuController` | `Scripts/Menu/` | Menu flow, fade transitions, player teleportation, boss progression |
| `HUDManager` | `Scripts/Player/` | Shows/hides health bars and boss name |
| `BulletManager` | `Scripts/Combat/AttackTypes/BulletManager/` | Moves all bullets, handles collision, manages parry |
| `BulletVisualPool` | `Scripts/Combat/AttackTypes/BulletManager/` | Shared object pool for bullet and obstacle visuals |
| `ObstacleManager` | `Scripts/Combat/AttackTypes/ObstacleManager/` | Phases obstacles through Warning → Active → Dying, checks player overlap |
| `EnemyStateManager` | `Scripts/Enemies/EnemyStateManager/` | Abstract base for all boss state machines |
| `EnemyBaseState` | `Scripts/Enemies/EnemyStateManager/` | Abstract base for every individual attack / idle / movement state |
| `Boss1StateManager` | `Scripts/Asher Animation Tests/` | "Roe Jogan" — melee/projectile boss AI |
| `Boss2StateManager` | `Scripts/Asher Animation Tests/` | "The Mainframe" — tech boss with force-field phase gate |
| `Sword` | `Scripts/Combat/Sword/` | Blade sweep hit detection, parrying, swing scaling, life steal |
| `PlayerMovement` | `Scripts/Player/` | Thumbstick movement, dash with wall/ledge/slope checks |
| `PlayerHealth` | `Scripts/Player/` | Player HP, hit cooldown, death callback |
| `MenuBox` | `Scripts/Menu/` | Interactive sword-sliceable menu option |
| `BossHitbox` | `Scripts/Enemies/` | Per-limb damage multiplier (head/torso/arms/legs) |
| `AttackData` | ScriptableObject | Per-attack bullet config: damage, speed, lifetime, movement type |

---

## Common Tasks

**I want to test a specific boss without going through the menu**  
Set `BossManager.startingBoss` to `1` or `2` in the Inspector.

**I want to add a new attack to Boss 1**  
Create a new class in `Scripts/Asher Animation Tests/States/Attacks/Boss1/` that inherits `EnemyBaseState`. Register it in `Boss1StateManager` and add it to the weighted random selection in `TransitionToNextState()`. See [Boss System](Boss-System) for the full pattern.

**I want to add a new boss**  
Follow the [Adding a Boss](Adding-a-Boss) guide. It covers every file you need to create or touch, in order, with a checklist at the end.

**I want to add a new bullet type**  
Create an `AttackData` ScriptableObject, set the fields, make a visual prefab, and call `boss.FireBullet(dir, attackData)` from your attack state. See [Bullets & Obstacles](Bullets-and-Obstacles) for details on movement types and the visual pool.

**I want to add a new obstacle**  
Build an `Obstacle` struct with your shape, timing, and prefab, then call `ObstacleManager.Instance.SpawnObstacle(obstacle)`. See [Bullets & Obstacles](Bullets-and-Obstacles) for the full field reference and warning-phase behavior.

**I want to change the screen fade duration**  
Adjust `fadeDuration` on `MenuController` in the Inspector. Default is 0.4 seconds.

**I want to change how much damage parried bullets deal**  
Adjust `parriedBossDamage` on `BulletManager` in the Inspector. Default is 5.

---

## Architecture in One Diagram

```
Player Input (SteamVR)
  ├── Thumbstick → PlayerMovement (walk, dash)
  └── Hand tracking → Sword (melee, parry)

Sword
  ├── Blade sweep → BossManager.TakeDamageOnActive()
  │                   └── Boss1/2.TakeDamage()
  │                         └── health <= 0 → MenuController.AdvanceToNextBoss()
  └── Parry → BulletManager marks bullet isParried = true
                └── Parried bullet reaches boss → BossManager.TakeDamageOnActive()

Boss Attack States
  ├── boss.FireBullet() → BulletManager.SpawnBullet()
  │                         └── BulletVisualPool.Spawn(prefab)
  └── ObstacleManager.SpawnObstacle()
        └── BulletVisualPool.Spawn(prefab)

BulletManager (every frame)
  ├── Move all bullets
  ├── Check player collision → PlayerHealth.TakeDamage()
  └── Check boss collision (parried only) → BossManager.TakeDamageOnActive()

ObstacleManager (every frame)
  ├── Phase bullets: Warning → Active → Dying
  ├── Warning: pulse visual opacity
  └── Active: check player overlap → PlayerHealth.TakeDamage()
```
