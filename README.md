# You Ain't The Boss Of Me!

A fast-paced VR boss rush game built in Unity with SteamVR. Fight two bosses back-to-back as fast as possible — dodge projectiles, parry bullets with your sword, and take down each boss to save the world.

<table>
  <tr><td><b>Course</b></td><td>University of Nebraska-Lincoln — CSCE352 / EMAR440 — Exploring Virtual Reality</td></tr>
  <tr><td><b>Developers</b></td><td>Raegan Scheet, Asher Lahm, Kenny Kouete</td></tr>
  <tr><td><b>Music</b></td><td><a href="https://soundcloud.com/ferretdot">Ferretdot</a></td></tr>
  <tr><td><b>Instructors</b></td><td>Chris Bourke, Steven Kolbe</td></tr>
  <tr><td><b>License</b></td><td><a href="./LICENSE.md">Apache 2.0</a></td></tr>
</table>

<p align="center">
<img src="./Assets/Textures/YATBOM%20Logo.png" alt="Project Logo" width="500" loading="lazy">
</p>

---

## Trailer

TBD

---

## Gameplay

A fast-paced VR bullet hell boss rush with physical sword fighting. Dodge waves of projectiles, parry bullets back at bosses, and cut them down with your motion-tracked blade — all in full virtual reality.

<table>
  <tr><td><b>Sword combat</b></td><td>Swing your motion-tracked sword to deal melee damage. Swing wider for more damage; flick attacks deal less.</td></tr>
  <tr><td><b>Bullet parrying</b></td><td>Deflect incoming projectiles back at the boss with your sword blade.</td></tr>
  <tr><td><b>Dashing</b></td><td>Press the right trigger to dash in your movement direction. Use it to dodge attacks and reposition.</td></tr>
  <tr><td><b>Life steal</b></td><td>Land a full-arm wide swing and heal on hit.</td></tr>
</table>

### Bosses

| # | Name | Style |
|---|------|-------|
| 1 | **Roe Jogan** | Physical melee boss. Charges, punches, slams, and fires spiral projectile bursts. Tires out after several attacks. |
| 2 | **The Mainframe** | Tech boss. Destroy the 5 mini computers to drop its force field, then attack the core with projectiles and obstacles. |

---

## Download & Play

> **Requires:** Windows PC + SteamVR-compatible headset (Valve Index, HTC Vive, Meta Quest via Air Link/Link, etc.) + [Steam](https://store.steampowered.com/) with SteamVR installed.

1. Go to the [Releases page](../../releases/latest)
2. Download `YouAintTheBossOfMe-Windows.zip`
3. Extract the zip
4. Launch `YouAintTheBossOfMe.exe` with your headset connected and SteamVR running
5. Slice one of the menu boxes with your sword to start a boss fight
6. Defeat both bosses to win

Want to build from source? See [CONTRIBUTING.md](./CONTRIBUTING.md).

---

## Documentation

Full documentation is on the [GitHub Wiki](https://github.com/raedotzip/VRGame/wiki):

| Page | Description |
|------|-------------|
| [Architecture Overview](https://github.com/raedotzip/VRGame/wiki/Architecture-Overview) | Scene structure, game flow, singleton pattern |
| [Managers](https://github.com/raedotzip/VRGame/wiki/Managers) | BossManager, MenuController, HUDManager, BulletManager, ObstacleManager |
| [Boss System](https://github.com/raedotzip/VRGame/wiki/Boss-System) | State machine pattern, AI behavior, attack states |
| [Adding a Boss](https://github.com/raedotzip/VRGame/wiki/Adding-a-Boss) | Step-by-step guide to creating a new boss |
| [Combat System](https://github.com/raedotzip/VRGame/wiki/Combat-System) | Sword mechanics, parrying, menu box interaction |
| [Bullets & Obstacles](https://github.com/raedotzip/VRGame/wiki/Bullets-and-Obstacles) | Bullet struct, visual pool, movement types, warning phase, adding new bullets/obstacles |
| [Player System](https://github.com/raedotzip/VRGame/wiki/Player-System) | VR locomotion, dash, health, HUD |
