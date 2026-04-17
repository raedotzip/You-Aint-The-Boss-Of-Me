# Boss Rush VR

A fast-paced VR boss rush game built in Unity with SteamVR. Fight two bosses back-to-back as fast as possible — dodge projectiles, parry bullets with your sword, and take down each boss to save the world.

**Course:** University of Nebraska-Lincoln — CSCE352 / EMAR440 — Exploring Virtual Reality  
**Authors:** Raegan Scheet, Asher Lahm, Kenny Kouete  
**License:** Apache 2.0

---

## Trailer

TBD

---

## Gameplay

- **Sword combat** — Swing your motion-tracked sword to deal melee damage. Swing wider for more damage; flick attacks deal less.
- **Bullet parrying** — Deflect incoming projectiles back at the boss with your sword blade.
- **Dashing** — Press the right trigger to dash in your movement direction. Use it to dodge attacks and reposition.
- **Life steal** — Land a full-arm wide swing and heal on hit.

### Bosses

| # | Name | Style |
|---|------|-------|
| 1 | Roe Jogan | Physical melee boss. Charges, punches, slams, and fires spiral projectile bursts. Tires out after several attacks. |
| 2 | The Mainframe | Tech boss. Destroy the 5 mini computers to drop its force field, then attack the core with projectiles and obstacles. |

---

## Requirements

- Unity 2022.x or later
- SteamVR plugin (included under `Assets/Plugins/Core/SteamVR`)
- A SteamVR-compatible VR headset (Valve Index, HTC Vive, Meta Quest via Air Link / Link, etc.)

---

## Getting Started

1. Clone the repository
2. Open the project in Unity
3. Open `Assets/Scenes/AnimationTest.unity`
4. Press **Play**
5. Slice one of the menu boxes with your sword to start a boss fight
6. Defeat both bosses to complete the run

---

## Documentation

Full documentation lives in the [`docs/`](docs/) folder (also on the wiki):

| Page | Description |
|------|-------------|
| [Architecture Overview](docs/architecture-overview.md) | Scene structure, game flow, singleton pattern |
| [Managers](docs/managers.md) | BossManager, MenuController, HUDManager, BulletManager, ObstacleManager |
| [Boss System](docs/boss-system.md) | State machine pattern, AI behavior, attack states |
| [Adding a Boss](docs/adding-a-boss.md) | Step-by-step guide to creating a new boss |
| [Combat System](docs/combat-system.md) | Sword mechanics, parrying, menu box interaction |
| [Bullets & Obstacles](docs/bullets-and-obstacles.md) | Bullet struct, visual pool, movement types, warning phase, adding new bullets/obstacles |
| [Player System](docs/player-system.md) | VR locomotion, dash, health, HUD |
