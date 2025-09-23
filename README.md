# Endless Wave Tower Defense Prototype

## Overview
This project is a **prototype Tower Defense game** built in Unity. It demonstrates a wave-based enemy spawning system, multiple enemy types, and a weapon drop mechanic. All enemies and weapons are represented with 2D sprites in a 3D world (billboard style).

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
The game includes **14 enemy types**:

### Slimes (8 types)
- Hopping enemies with varying visuals stats and movement patterns.
- Spawn in waves with guaranteed counts and extra random enemies.

### Walking Walkers (3 types)
- Ground-based, slower-moving enemies.
- Follow the spline path.

### Spikeballs (3 types)
- Rolling along the path.
- Has high collision area and health to protect the enemies, does not attack the castle
- Waves can spawn even if you don't destroy them


---

## Wave Spawning System
- Each wave has:
  - **Guaranteed base enemies** per type.
  - **Extra enemies** that scale with wave count.
  - **Weighted selection** for random extras.
- The spawn order of all enemies is fully randomized.
- The system ensures **at least a few extra enemies** spawn even on early waves.
- Enemy movement follows a **spline path**.

---

## Features Implemented
- Wave-based enemy spawning with randomization and scaling.
- Weighted random selection for extra enemies.
- Full shuffle of spawn order for variety.
- Weapon drops near the player after each wave.
- Mix of 2D sprites and 3D environments

