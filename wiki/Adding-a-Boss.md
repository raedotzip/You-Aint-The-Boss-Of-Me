# Adding a New Boss

This guide walks through everything needed to add a third (or fourth, etc.) boss to the game. The existing Boss 1 and Boss 2 are good references throughout.

---

## Step 1 — Create the StateManager Script

Create a new script at `Assets/Scripts/Asher Animation Tests/Boss3StateManager.cs`.

Inherit from `EnemyStateManager`:

```csharp
using UnityEngine;

public class Boss3StateManager : EnemyStateManager
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float health;

    [Header("States")]
    private Boss3IdleState    idleState    = new Boss3IdleState();
    private Boss3AttackState  attackState  = new Boss3AttackState();
    // ... add more states here

    [Header("AI")]
    public int attacksBeforeTired = 4;
    public int attackCounter = 0;

    public override void Start()
    {
        health = maxHealth;
        currentState = idleState;
        base.Start();
    }

    public void TakeDamage(float amount)
    {
        health = Mathf.Max(0f, health - amount);
        HUDManager.Instance?.UpdateBossHealth(health, maxHealth);

        if (health <= 0f)
        {
            MenuController.Instance?.AdvanceToNextBoss(3);
        }
    }

    public void SwitchState(EnemyBaseState newState)
    {
        currentState = newState;
        currentState.EnterState(this);
    }

    public void TransitionToNextState()
    {
        attackCounter++;
        if (attackCounter >= attacksBeforeTired)
        {
            attackCounter = 0;
            // SwitchState(tiredState);
            return;
        }
        // Add your attack selection logic here (see Boss1StateManager for examples)
        SwitchState(attackState);
    }
}
```

---

## Step 2 — Create Attack States

Create a folder: `Assets/Scripts/Asher Animation Tests/States/Attacks/Boss3/`

Each attack is its own class inheriting from `EnemyBaseState`:

```csharp
public class Boss3AttackState : EnemyBaseState
{
    private float _timer;
    private float _duration = 2f;

    public override void EnterState(EnemyStateManager boss)
    {
        _timer = 0f;
        boss.animator.SetTrigger("Attack"); // match your animator parameter name
    }

    public override void UpdateState(EnemyStateManager boss)
    {
        _timer += Time.deltaTime;

        // Example: fire a bullet halfway through
        if (_timer >= _duration * 0.5f && _timer < _duration * 0.5f + Time.deltaTime)
        {
            Vector3 dir = (boss.player.position - boss.transform.position).normalized;
            boss.FireBullet(dir, boss.bulletData.someAttackData);
        }

        if (_timer >= _duration)
        {
            Boss3StateManager b3 = boss as Boss3StateManager;
            b3?.TransitionToNextState();
        }
    }

    public override float OnBossHurt(EnemyStateManager boss)
    {
        return 0f; // return extra damage if you want to punish this state
    }
}
```

Copy and modify existing Boss 1 or Boss 2 states as starting points — they already have working patterns for lunging, bullet firing, obstacle spawning, and timers.

---

## Step 3 — Create ScriptableObject Data Assets

**AttackData** — One asset per attack type. Right-click in the Project window:  
`Create → ScriptableObjects → AttackData`

Set damage, bullet speed, lifetime, prefab, movement type, etc.

**BossBulletData** — Groups bullet prefab references.  
Create at `Assets/Scripts/ScriptableObjects/Boss3/Boss3BulletData.asset`

**BossObstacleData** — Groups obstacle prefab references.  
Create at `Assets/Scripts/ScriptableObjects/Boss3/Boss3ObstacleData.asset`

Assign these assets to your Boss3StateManager in the Inspector.

---

## Step 4 — Set Up the Boss in the Scene

1. Add your boss's FBX / prefab to the scene
2. Add `Boss3StateManager` to the root GameObject
3. Assign `Animator`, `Rigidbody`, `bulletData`, `obstacleData` in the Inspector
4. Add and position a `boss3SpawnPoint` empty GameObject (used only when `startingBoss = 3` for direct testing)

---

## Step 4b — Add a Boss Arena Trigger

Boss fights start when the player **walks into the arena**, not when a menu box is sliced. You need a `BossArenaTrigger` volume at the arena entrance.

1. Create an empty GameObject at the arena entrance, sized to cover the doorway
2. Add a `BoxCollider` (mark it as trigger — the script does this automatically)
3. Add the `BossArenaTrigger` component
4. Set `bossIndex = 3`
5. The trigger fires `BossManager.SetActiveBoss(3)` when the player enters

The trigger resets automatically when the player returns to the menu.

---

## Step 5 — Register with BossManager

Open `Assets/Scripts/Enemies/BossManager.cs` and add Boss 3:

```csharp
[Header("Boss References")]
public Boss1StateManager boss1;
public Boss2StateManager boss2;
public Boss3StateManager boss3;   // ← add this

[Header("Boss Names")]
public string boss1Name = "Roe Jogan";
public string boss2Name = "The Mainframe";
public string boss3Name = "Your Boss Name";  // ← add this
```

In `SetActiveBoss()`, add a block for index 3 following the same pattern as Boss 1 and Boss 2:

```csharp
if (bossIndex == 3 && boss3 != null)
{
    HUDManager.Instance?.SetBossName(boss3Name);
    HUDManager.Instance?.SetBossPortrait(3);
    HUDManager.Instance?.UpdateBossHealth(boss3.health, boss3.maxHealth);
    SetBossActive(boss3, true);
    HUDManager.Instance?.ShowBossBar(true);
}
```

Also update `GetActiveBoss()`:
```csharp
if (_activeBossIndex == 3) return boss3;
```

And `TakeDamageOnActive()`:
```csharp
if (_activeBossIndex == 3 && boss3 != null) boss3.TakeDamage(amount);
```

Then wire the Inspector reference: drag your Boss3 GameObject into `BossManager.boss3`.

---

## Step 6 — Register with MenuController

Open `Assets/Scripts/Menu/MenuController.cs`.

Add a spawn point field:
```csharp
[Tooltip("Where the player spawns for Boss 3")]
public Transform boss3SpawnPoint;
```

Update `BossSpawnPoint()`:
```csharp
Transform BossSpawnPoint(int index)
{
    if (index == 1) return boss1SpawnPoint;
    if (index == 2) return boss2SpawnPoint;
    if (index == 3) return boss3SpawnPoint;  // ← add this
    return menuSpawnPoint;
}
```

Update `AdvanceToNextBoss()` — the existing code already handles this automatically since it just increments the index. As long as `BossManager` knows about boss 3 and `BossSpawnPoint` returns a valid point for index 3, the progression works.

Assign `boss3SpawnPoint` in the Inspector.

---

## Step 7 — Add a Menu Box (Optional)

If you want a direct "Play Boss 3" menu option:

1. Add a new `MenuBox` GameObject inside the menu sphere
2. Add a `Rigidbody` (kinematic) and a collider
3. Set the layer to match other menu boxes
4. Set the `label` field to "Boss 3" (or whatever)
5. Wire `onSliced` → `MenuController.StartBoss3()` (you'll need to add this helper):
   ```csharp
   public void StartBoss3() => StartBoss(3);
   ```
6. Add the new box to `MenuController.menuBoxes[]` in the Inspector

---

## Step 8 — Test It

1. Set `BossManager.startingBoss = 3` in the Inspector to skip the menu and jump straight into Boss 3 for faster iteration
2. Play the scene and verify:
   - Boss activates and health bar shows
   - Attacks fire correctly
   - Taking damage updates the health bar
   - Reaching 0 HP transitions to the next boss or menu
3. Set `startingBoss` back to `0` when done testing

---

## Checklist

- [ ] `Boss3StateManager.cs` created and inherits `EnemyStateManager`
- [ ] At least one attack state created
- [ ] `TakeDamage()` calls `HUDManager.Instance.UpdateBossHealth()` and `MenuController.Instance.AdvanceToNextBoss(3)`
- [ ] ScriptableObject data assets created and assigned
- [ ] Boss GameObject in scene with all components wired
- [ ] `boss3SpawnPoint` placed in scene (for direct-test bypass)
- [ ] `BossArenaTrigger` volume placed at arena entrance with `bossIndex = 3`
- [ ] `BossManager` updated: field, `SetActiveBoss`, `GetActiveBoss`, `TakeDamageOnActive`
- [ ] `MenuController` updated: spawn point field and `BossSpawnPoint()`
- [ ] Inspector references all assigned
- [ ] Tested by setting `startingBoss = 3`
