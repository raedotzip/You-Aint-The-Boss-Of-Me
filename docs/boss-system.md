# Boss System

## Overview

Every boss is a state machine. The boss is always in exactly one state (idle, a specific attack, movement, tired) and transitions between states based on player distance, attack history, and randomized weights.

---

## Base Classes

### EnemyStateManager

**File:** `Assets/Scripts/Enemies/EnemyStateManager/EnemyStateManager.cs`

Abstract base class that all boss state managers inherit from. Handles the core state machine loop.

```
Awake/Start
  └─▶ EnterState(initialState)

Every Update frame
  └─▶ currentState.UpdateState(this)

When damage is taken
  └─▶ TakeDamage(amount)  ← defined in each boss subclass
```

**Key fields available to all bosses:**
- `currentState` — The currently executing state
- `animator` — The boss's Animator component
- `rb` — Rigidbody
- `player` — Transform of the player (auto-found by "Player" tag)
- `obstacleData` — ScriptableObject with obstacle prefabs
- `bulletData` — ScriptableObject with bullet prefabs

**`FireBullet(Vector3 direction, AttackData data)`** — Spawns a bullet via `BulletManager`. Any attack state can call this on its `EnemyStateManager` reference.

### EnemyBaseState

**File:** `Assets/Scripts/Enemies/EnemyStateManager/EnemyBaseState.cs`

Abstract base class for every individual state (attacks, idle, movement).

```csharp
public abstract class EnemyBaseState
{
    public abstract void EnterState(EnemyStateManager boss);
    public abstract void UpdateState(EnemyStateManager boss);
    public abstract float OnBossHurt(EnemyStateManager boss);
}
```

- `EnterState` — Called once when the state becomes active. Set up timers, start animations, store initial positions here.
- `UpdateState` — Called every frame. Drive the attack logic here using timers.
- `OnBossHurt` — Called if the boss takes damage during this state. Return a damage float (can modify incoming damage).

---

## Boss 1 — Roe Jogan

**File:** `Assets/Scripts/Asher Animation Tests/Boss1StateManager.cs`

### Stats

| Stat | Default |
|------|---------|
| Health | 100 |
| Arena radius | 28m |
| Attacks before tired | 5 |
| Tired duration | 2s |
| Retreat distance | 5m (25% chance) |

### Attack States

Boss 1 has over 20 states. Attack selection is range-based with weighted randomization.

#### Close Range (≤ 8m)
| Attack | Weight | Description |
|--------|--------|-------------|
| Punch | 4 | Lunges forward, deals 25 damage if within 2.5m |
| Jump Slam | 3 | Leaps at player, area slam on landing |
| Spin Attack | 3 | Spinning melee with wide hitbox |
| Charge | 2 | Long-distance bull rush |

#### Mid Range (8–18m)
| Attack | Weight | Description |
|--------|--------|-------------|
| Charge | 4 | Closes distance fast |
| Bullet Slam | 2 | Fires projectile spread on slam |
| Spiral Burst | 3 | Rotating ring of bullets |
| Shockwave | 2 | Radial obstacle ring |

#### Far Range (≥ 18m)
| Attack | Weight | Description |
|--------|--------|-------------|
| Charge | 4 | Closes gap from across arena |
| Targeted Burst | 4 | Accurate multi-projectile burst at player |
| Shockwave | 2 | Radial ring obstacle |
| Spiral Burst | 2 | Wide spiral spread |

### AI Behavior

**Rotation:** The boss smoothly rotates toward the player at 5°/frame (slerp). Look-at is disabled during attacks that have their own movement.

**Fatigue system:** After 5 attacks (`attacksBeforeTired`), the boss enters the Tired state for 2 seconds before selecting the next attack. This creates a predictable recovery window.

**Retreat logic:** If the player is within 5m at the start of a new state selection, there's a 25% chance the boss jumps away. It prefers jumping sideways (left or right) over jumping straight back.

**Boundary check:** The boss checks if it's within 28m of the arena center before selecting movement states. If out of bounds, it picks an attack rather than moving further away.

---

## Boss 2 — The Mainframe

**File:** `Assets/Scripts/Asher Animation Tests/Boss2StateManager.cs`

### Phase Gate: Mini Computers

Boss 2 starts with a force field up. The force field makes it immune to all damage (`if (_forceFieldUp) return;` in `TakeDamage`). To drop the force field, the player must destroy the 5 mini computers positioned around the arena.

- Each `Boss2MiniComputer` can be damaged by the sword
- When a mini computer is destroyed, it calls `Boss2StateManager.OnMiniComputerDestroyed()`
- When the first mini computer is destroyed, `ActivateMainComputer()` is called — this disables the force field and shows the boss health bar

### Stats

| Stat | Default |
|------|---------|
| Health | 100 |
| Attacks before tired | 4 |

### Attack States

| Attack | Description |
|--------|-------------|
| Laser Beam | Slow-tracking laser sweep |
| Virus Swarm | Multiple fast projectiles in a spread |
| EMP Wave | Expanding ring obstacle |
| Data Strike | Targeted high-damage shot |
| Spiral | Rotating projectile spiral |
| Obstacle Barrage | Multiple overlapping obstacle spawns |

---

## Attack State Pattern

Every attack follows the same structure. Here's the pattern using Boss1PunchAttack as the example:

```
EnterState()
  ├─ Store start position
  ├─ Calculate lunge target (toward player)
  ├─ Disable look-at rotation
  └─ Start animation trigger

UpdateState()  (called each frame)
  ├─ Phase 1 — Startup (timer < startupDuration)
  │     Wait / play wind-up animation
  ├─ Phase 2 — Active (timer < startupDuration + activeDuration)
  │     Move boss, check player distance, apply damage
  └─ Phase 3 — Recovery (timer >= activeDuration)
        Freeze boss, re-enable look-at
        After recovery delay → call boss.TransitionToNextState()
```

### Transition

At the end of every attack, the state calls `boss.TransitionToNextState()`. This is defined in the `StateManager` and handles:
1. Incrementing `attackCounter`
2. Checking if boss should enter Tired state
3. Measuring player distance
4. Running weighted random selection for next attack

### Creating a New Attack State

1. Create a new `.cs` file in `Assets/Scripts/Asher Animation Tests/States/Attacks/Boss1/` (or Boss2)
2. Inherit from `EnemyBaseState`
3. Implement `EnterState`, `UpdateState`, `OnBossHurt`
4. Store a reference to an `AttackData` ScriptableObject for bullet/obstacle stats
5. At the end of the attack, call `boss.TransitionToNextState()`
6. Register the state in the boss's StateManager (see [Adding a Boss](adding-a-boss.md))

---

## Limb Hitboxes

**File:** `Assets/Scripts/Enemies/BossHitbox.cs`

Add `BossHitbox` components to child GameObjects on individual bone transforms to enable per-limb damage multipliers. The sword checks for this component on hit and multiplies damage accordingly.

Suggested multipliers:

| Limb | Multiplier |
|------|-----------|
| Head | 2.0 |
| Torso | 1.0 |
| Arms | 0.75 |
| Legs | 0.5 |

To set up limb hitboxes:
1. Expand the boss rig in the Hierarchy to find bone GameObjects
2. Add a Collider (CapsuleCollider or BoxCollider) sized to the limb
3. Set the collider's layer to match the boss's existing collider layer
4. Add `BossHitbox` component — set `boss` to the StateManager, set `damageMultiplier`
5. Remove the single root CapsuleCollider (the limb colliders replace it)
