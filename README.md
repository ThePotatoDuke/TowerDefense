# Endless Wave Tower Defense Prototype

## Overview
This project is a **prototype Tower Defense game** built in Unity. It demonstrates a wave-based enemy spawning system, multiple enemy types, and a weapon change mechanic. Be sure to check the built game in the releases section.

---
## Engine & Version
- **Unity 6000.6.7f1**  
- Project uses **C#** scripts for enemy, wave, and weapon management.
- 2D sprites are displayed in a 3D world using billboard technique.

---

## Assumptions, Notes, and Explanations 
- Weighted spawn: higher `spawnWeight` increases the likelihood an enemy appears in extra spawns.  
- Extra enemies are randomized per wave but **guarantee at least a few** even in early waves.  
- Weapon drops are always spawned next to the player after wave completion.  
- The spline system is used for enemy pathing; enemies follow the predefined path smoothly.  
- Player movement and basic combat are implemented as part of the prototype.

---

## Player & Weapons
- Player can move freely (WASD).  
- Weapons include:
  - 1 Melee weapon (close combat)
  - 2 Ranged weapons:
    - One can aim upwards
    - One can pierce multiple enemies  
- Weapons are dropped near the player after each wave.

---

## Enemies
The game includes **15 enemy types**:

### Slimes (9 types)
- Hopping enemies with varying visuals stats and movement patterns.
- Spawn in waves with guaranteed counts and extra random enemies.

### Walking Walkers (3 types)
- Ground-based, slower-moving enemies.
- Follow the spline path.

### Spikeballs (3 types)
- Rolling along the path.
- Has high collision area and health to protect the enemies, does not attack the castle
- Roll toward the average progress of all enemies, acting as a moving shield
- Waves can spawn even if you don't destroy them
- Self-destruct after surviving 2 cleared waves (configurable per type via `wavesToSurvive`)


---

## Wave Spawning System
- Each wave has:
  - **Guaranteed base enemies** per type.
  - **Extra enemies** that scale with wave count.
  - **Weighted selection** for random extras.
- The spawn order of all enemies is fully randomized.
- The system ensures **at least a few extra enemies** spawn even on early waves.
- Enemy movement follows a **spline path**.
- A wave is cleared once every non-spike-ball enemy is dead; spike balls carry over into later waves.

---

## Features Implemented
- Wave-based enemy spawning with randomization and scaling.
- Weighted random selection for extra enemies.
- Full shuffle of spawn order for variety.
- Weapon drops near the player after each wave.
- Mix of 2D sprites and 3D environments

---

## Code Architecture
- **Data-driven tuning:** every enemy, weapon and projectile reads its stats from a ScriptableObject (`Assets/ScriptableObjects`), so new variants need no code.
- **Enemies:** `EnemyBase` handles health, hit flash, spline progress and castle damage; `SlimeEnemy`, `WalkerEnemy` and `SpikeBallEnemy` only implement movement.
- **Waves:** `EndlessWaveManager` builds, shuffles and spawns each wave; `EnemyManager` tracks live enemies.
- **Weapons:** `WeaponBase` auto-targets the closest enemy; `MeleeWeaponBase` and `RangedWeaponBase` implement the attack.
- **Game flow:** `GameManager` drives `GameStateManager` (the single game state), reacting to `GameEvents` such as player death, castle death and wave clears.
- **Animation:** DOTween for hops, squash/stretch, knockback and death; Cinemachine for the castle game-over camera.

## Debug Controls
- **E** – spawn the rest of the current wave immediately.

