# Player System

---

## Movement

**File:** `Assets/Scripts/Player/PlayerMovement.cs`

VR locomotion using the thumbstick. Movement direction is relative to the headset's facing direction, so the player moves where they're looking regardless of which way their body is pointing.

### Basic Movement

The left thumbstick (`SteamVR_Actions.default_Move`) produces a `Vector2`. The script projects this onto the horizontal plane relative to the camera's forward/right directions and applies it via `CharacterController.Move()` at `moveSpeed` (default 2 m/s).

Movement uses **2 sub-steps per frame** for stability — each step moves half the distance. This reduces tunneling through thin geometry at high speeds.

### Gravity

A vertical velocity accumulates each frame using standard gravity (-9.81 m/s²). When grounded, a small constant downward force (-2 m/s²) keeps the character pressed to the floor instead of bouncing. Ground detection uses a sphere cast at 90% of the capsule's radius.

### Dash

**Input:** Right trigger (`SteamVR_Actions.default_Dash`)  
**Distance:** 4m maximum  
**Duration:** 0.15s  
**Cooldown:** 1s between dashes

The dash moves the player in the direction of their current thumbstick input (or forward if no input). It uses **4 sub-steps** for accuracy.

Three safety checks run every dash step:

| Check | What it does |
|-------|-------------|
| **Wall detection** | Sphere cast ahead — if a wall is within the remaining dash distance, stops early |
| **Ledge detection** | Raycast down from the projected position — if no ground is found, cancels the step to prevent falling off edges |
| **Slope handling** | Projects the dash vector onto the ground normal so you slide along slopes rather than bumping into them |

### Teleport Reset

When `MenuController` teleports the player between arenas, it calls `PlayerMovement.SyncTeleport()` to reset vertical velocity and cancel any in-progress dash. This prevents the player from arriving at the new spawn point while still moving from the previous arena.

### Inspector Fields

| Field | Default | Description |
|-------|---------|-------------|
| `moveSpeed` | 2 m/s | Walking speed |
| `dashDistance` | 4m | Maximum dash travel distance |
| `dashDuration` | 0.15s | Time to complete a full dash |
| `dashCooldown` | 1s | Time before next dash is allowed |

---

## Health

**File:** `Assets/Scripts/Player/PlayerHealth.cs`

Tracks the player's HP and updates the health bar UI.

### Taking Damage

Damage comes from two sources:
- **Bullets** — `BulletManager` calls `PlayerHealth.TakeDamage(bullet.damage)` when a non-parried bullet reaches the player's hit radius (0.5m around the player's eye height)
- **Obstacles** — `ObstacleManager` calls `PlayerHealth.TakeDamage(wallDamagePerSecond * deltaTime)` every frame while the player overlaps an active obstacle

A hit cooldown (default 0.5s) prevents rapid successive hits from stacking — useful for obstacles that would otherwise drain health extremely fast.

### Healing

`PlayerHealth.Heal(amount)` is called by `Sword.cs` when the player lands a qualifying wide swing hit (life steal). It clamps health to `maxHealth`.

### Death

When health reaches 0, `OnDeath()` is called. Currently a placeholder — add respawn, game over screen, or return-to-menu logic here.

### Inspector Fields

| Field | Default | Description |
|-------|---------|-------------|
| `maxHealth` | 100 | Maximum player health |
| `hitCooldown` | 0.5s | Min time between damage events |
| `playerHealthBar` | — | `HealthBarUI` reference for the health bar |

---

## HUD

**File:** `Assets/Scripts/Player/HUDManager.cs`  
**Health Bar:** `Assets/Scripts/Player/HealthBarUI.cs`

### HUDManager

Singleton that controls all UI visibility. During the menu, the entire HUD is hidden. During boss fights, it's shown with the appropriate boss name and health bar.

See the [Managers](Managers#hudmanager) page for full details.

### HealthBarUI

Handles the animated health bar fill and color feedback. Used for both the player and boss health bars.

**Visual feedback:**
- **Color** changes based on health percentage:
  - Green — 100% to 60%
  - Yellow — 60% to 30%
  - Red — below 30%
- **Smooth drain** — The bar doesn't jump instantly; it depletes at a configurable speed so you can see how much health was lost
- **Damage flash** — Brief white flash for 0.12s on damage hit

**Usage:**
```csharp
// Called by PlayerHealth and Boss StateManagers
healthBarUI.UpdateHealthPercentage(currentHealth, maxHealth);
```

---

## VR Input Summary

| Action | Binding | Script |
|--------|---------|--------|
| Move | Left thumbstick | `PlayerMovement` |
| Dash | Right trigger | `PlayerMovement` |
| Sword | Right hand position/rotation (tracked automatically) | `Sword` |

All inputs use SteamVR Actions defined in `SteamVR_Actions.default_*`. The action set is configured in the SteamVR Input window (`Window → SteamVR Input`).

---

## Teleportation

**File:** `Assets/Scripts/Player/Teleport.cs` (trigger-based)  
Also handled by `MenuController.TeleportPlayer()` (programmatic)

The `MenuController` teleport sequence:
1. Disable `CharacterController` (required to manually set position)
2. Call `PlayerMovement.SyncTeleport()` to clear velocity
3. Set `player.transform.SetPositionAndRotation(target.position, target.rotation)`
4. Call `Physics.SyncTransforms()` — forces the physics engine to update before re-enabling the CharacterController
5. Re-enable `CharacterController`

---

## Lore System

**LoreTyper:** `Assets/Scripts/Player/LoreTyper.cs`  
**LoreTrigger:** `Assets/Scripts/Player/LoreTrigger.cs`

Displays typewriter-style text on the HUD. Used for ambient world-building as the player moves through the lab and arenas.

### LoreTyper

Attached to a HUD GameObject with a `TMP_Text` element. Types a message in character by character, holds it, then deletes it. Can play a chain of messages in sequence.

| Inspector field | Default | Description |
|----------------|---------|-------------|
| `typeSpeed` | 0.05s | Seconds per character typed |
| `deleteSpeed` | 0.025s | Seconds per character deleted (faster than type) |
| `holdDuration` | 3s | How long the full text shows before deleting |
| `gapBetween` | 0.4s | Silence between messages in a sequence |
| `cursor` | `_` | Blinking cursor appended while typing |

**API** (called via `HUDManager`):
- `HUDManager.Instance.ShowLore("message")` — single message
- `HUDManager.Instance.ShowLoreSequence(new string[]{"a","b"})` — sequence
- `HUDManager.Instance.CancelLore()` — clears immediately

### LoreTrigger

Place a `LoreTrigger` on any trigger collider in the scene to fire lore automatically when the player walks through.

| Inspector field | Description |
|----------------|-------------|
| `loreTyper` | Reference to the HUD's `LoreTyper` |
| `messages[]` | One entry = single message. Multiple = sequence. |
| `playerTag` | Tag on the player root (default: `"Player"`) |

Triggers fire once and reset when `ResetTrigger()` is called.

Skipping step 4 causes the CharacterController to snap back to its last known physics position on the first frame, which looks like a teleport failure.
