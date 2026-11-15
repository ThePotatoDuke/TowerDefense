# Keep of the Grass

## Overview
**Keep of the Grass** is an endless wave defense game made in Unity. Slimes, walkers, bats and stranger things march along a winding path toward your castle. You fight them off on foot with three auto-aiming weapons, mow the grass for currency, and spend it in a shop between waves. Be sure to check the built game in the releases section.

---

## Engine & Version
- **Unity 6000.6.7f1**, Universal Render Pipeline
- C# scripts; DOTween for animation, Cinemachine for the camera, TextMeshPro for text, the Input System for keyboard, mouse and gamepad
- 2D pixel-art sprites in a 3D world, drawn as camera-facing billboards

---

## Controls
| Action | Keyboard | Gamepad |
|---|---|---|
| Move | WASD | Left stick |
| Next weapon | Q | Right shoulder |
| Previous weapon | – | Left shoulder |
| Pick weapon slot | 1, 2, 3 | – |
| Plant a Crimson Weed seed | E | North button |
| Warp to the Totem (hold) | T | West button |
| Pause | Esc or P | Start |

Weapons aim and fire on their own at the closest enemy they can reach.

---

## Gameplay

### Weapons
The run starts with all three:
- **Sword:** sweeps through every enemy it touches and mows the grass. It can't reach flying enemies.
- **Blaster:** the only weapon that can aim up at high flyers.
- **Hoop Cannon:** its hoops pierce through several enemies.

### Grass (currency)
- Swinging the sword mows grass tufts. Each tuft drops a blade that flies to you when you're close.
- Grass grows back only at the **start of each wave**: a share of the field, rising a little every wave. Nothing grows during a wave, so stalling earns nothing.
- **Harvest bonus:** every wave has a timer. Clear the wave before it runs out and you get up to +100% of the grass picked up that wave, less the longer you take. Boss waves pay double.
- An enemy that spawns with a random shield drops extra grass when killed.

### Shop
- Opens every 3 cleared waves, and always after a boss wave.
- Deals 3 cards. A bought card is sold out until the next visit, and each purchase makes the rest of that visit 25% dearer. Rerolling costs a little more each time.
- 23 upgrades, among them:
  - **Stat upgrades:** max health, speed, the grass magnet, damage, fire rate, range and size for each weapon, and homing shots.
  - **Economy:** Lush Grass, Abundance, Savings Bloom, Lasting Harvest and Bumper Crop.
  - **Repairs:** Heal and Castle Repair.
  - **Items:**
    - **Spore Mines:** mushrooms at the castle that burst when an enemy reaches it.
    - **Totem:** hold T to warp back to it.
    - **Crimson Weed:** a plant that shoots and grows as it kills.

### Waves
- **20 hand-made waves**, introducing the enemies a few at a time. Most waves have an alternative version, picked at random, so runs differ.
- After wave 20 the list loops from wave 9, with more enemies and shorter gaps each time round. Enemy health rises a little every wave.
- From wave 6, enemies have a small, slowly rising chance of a **ranged shield**: shots bounce off, only the sword gets through. Flying enemies and the Hyper Slime never have one.
- **Boss waves** come every 10th wave. There are nine, each with its own banner and music, played in random order without repeats:
  - Konga Express
  - The Horde
  - Air Raid
  - Checkmate
  - Stampede
  - Bomb Squad
  - Bat Cave
  - Spike Rain
  - The Slime Queen

### Enemies
**29 enemies**, each documented in the in-game **Bestiary**. Entries unlock as you meet them, and the unlocks are kept between runs.
- **Slimes (11):** each hops in its own way: steady, swarming, charging, hopping to a rhythm, tricking you, frantic, heavy, sky-high, splitting into Mini Slimes, or leapfrogging in black and white lines. The Slime Queen is a boss.
- **Walkers (3):** steady, heavy and fast.
- **Bats (3):** weaving, diving, or both. Up high only the Blaster can hit them.
- **Frills (3):** sky-high flyers that shoot at you. A marker shows where each shot lands.
- **Konga:** chains that close their gaps when one is destroyed.
- **Molly:** a mole that can only be hit while above ground.
- **Tibbits (2):** frogs that ride other enemies and can't be hit while riding.
- **Roller:** rolls back and forth along the path for a few waves, bumping and flattening other enemies.
- **Spike balls (3):** can't be hurt. They roll with the enemies to shield them and leave after 3 waves; the yellow one bursts into spikes.
- **Bomberr:** a crew carrying a bomb that explodes near where it's beaten, hurting enemies, you and the castle.

---

## Menus
- **Main menu:**
  - A sign slides in and its buttons drop down on chains.
  - The game's enemies wander across the screen.
  - It has its own music.
- **Pause menu:** stops the game and the music. It has Resume, Retry, Menu and Bestiary buttons, plus music and sound effect toggles, which are remembered between runs.
- **Bestiary:** a grid of every enemy. Pick one to see it animated, with its health, damage, traits, a description and a tip.

---

## Code Architecture
- **Data-driven tuning:** every enemy, weapon and projectile reads its stats from a ScriptableObject (`Assets/ScriptableObjects`). The game-wide settings live in `Assets/Resources`:
  - `WaveSettings`: waves, bosses, shields, grass and the harvest bonus;
  - `ShopSettings`: upgrades and prices;
  - `SoundLibrary`: sound effects;
  - `Bestiary`: the bestiary's entries;
  - `FXSettings`: visual effects.
- **Enemies:** `EnemyBase` handles health, hits, shields, splitting, spline progress and castle damage. Each enemy type only implements its own movement.
- **Waves:** `EndlessWaveManager` picks, builds and spawns each wave and runs the harvest timer. `EnemyManager` tracks live enemies.
- **Weapons:** `WeaponBase` auto-targets the closest enemy it can reach. `MeleeWeaponBase` and `RangedWeaponBase` implement the attack. `WeaponInventory` holds the three slots.
- **Game flow:** `GameManager` drives `GameStateManager` (the single game state), reacting to `GameEvents` such as player death, castle death and wave clears.
- **Audio:**
  - `Sounds` plays the library's effects, with distance and stereo panning.
  - `MusicManager` handles the gameplay, shop and boss tracks. The shop's music adds layers as your grass grows.

---

## Visual Effects
Custom URP shaders (`Assets/Shaders`) driven by small components in `Assets/Scripts/VisualFX`:
- **SpriteFX shader:** pixel-snapped hit flash (HDR, glows with bloom), noise dissolve on enemy death, and a shine sweep on pickups, shields and the Totem.
- **Pixelated 3D models:** spike balls and rollers are rendered by their own low-res cameras and shown on camera-facing quads with a pixel outline, matching the sprites.
- **Blob shadows:** soft pixelated shadows under sprites that shrink and fade as they hop.
- **Grass:** tufts that sway as you walk through them, get mown with flying clippings, and sprout back.
- **Castle hits:** shake, white flash, pixel debris and a red pulse at low health. When the castle falls, the music slows to a stop.
- **Trails and post-processing:** projectile trails, a sword swoosh, bat trails, bloom and colour grading.

All colours, durations and sizes are tunable in `Assets/Resources/FXSettings.asset`.

---

## Debug Controls
- **`** (backquote): kill every enemy and skip to the next wave.
- **B**: make the next wave a boss wave. Set **Test Boss Wave** on the `EndlessWaveManager` to pick which one.
- **Log Economy** (on the `EndlessWaveManager`): logs each wave's grass for balancing.
- **Test Starting Upgrades** (in `ShopSettings`): upgrades given free at the start of a run.
- **Show All** (on the `Bestiary` asset): shows every entry, met or not.
