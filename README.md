# Destruction Royale

A third-person fast-paced survival / battle royale shooter with fully destructible environments.

## Core Features

- **Fully Destructible Buildings** - Every wall is split into segmented health blocks. Shoot holes, create breaches, collapse sections
- **Third-Person Combat** - Fast movement, smooth aiming, 4 weapon classes (SMG, AR, Shotgun, Sniper)
- **Resource Gathering** - Harvest wood, stone, metal from the environment
- **Deployable Defenses** - Tactical walls, barricades, repair systems
- **Grenade/Explosive System** - AoE destruction for breaching and flushing
- **Battle Royale Loop** - Shrinking safe zone, loot, intense endgame destruction warfare
- **Multiplayer Ready** - Built on Unity Netcode for GameObjects, scalable 10-50 players

## Tech Stack

- **Engine**: Unity 6 (6000.x)
- **Networking**: Unity Netcode for GameObjects 2.x
- **Input**: Unity Input System 1.x
- **Camera**: Cinemachine 3.x
- **Rendering**: Universal Render Pipeline (URP)
- **UI**: TextMeshPro + Unity UI

## Project Structure

```
Assets/
  Scripts/
    Player/          - Third-person controller, camera, health, inventory
    Weapons/         - Weapon system, shooting, projectiles, weapon data
    Destruction/     - Segmented wall system, structural integrity, debris
    Building/        - Deployable walls, repair, defense structures
    Resources/       - Resource gathering, harvestable objects
    Match/           - Game loop, safe zone, loot spawning, match state
    Multiplayer/     - Network manager, spawning, sync
    UI/              - HUD, inventory UI, health bars, match UI
    Utils/           - Helpers, object pooling, extensions
    Data/            - ScriptableObject definitions for weapons, resources, etc.
  Prefabs/           - Player, weapons, buildings, effects
  Scenes/            - Game scenes
  Materials/         - Placeholder materials
  Animations/        - Animation controllers
  Audio/             - Sound effects and music
```

## Getting Started

1. Open the project in Unity 6 (2024.x or later)
2. Open `Assets/Scenes/MainGame.unity`
3. Press Play to test singleplayer
4. For multiplayer testing, build a standalone and connect

## Controls

| Action | Key |
|--------|-----|
| Move | WASD |
| Sprint | Left Shift |
| Jump | Space |
| Aim | Right Mouse |
| Shoot | Left Mouse |
| Reload | R |
| Weapon Swap | 1-4 / Scroll |
| Grenade | G |
| Deploy Wall | Q |
| Harvest | E (hold near resource) |
| Repair | F (hold near damaged structure) |
| Inventory | Tab |
| Slide | Left Ctrl (while sprinting) |

## Architecture

The game uses a component-based architecture with ScriptableObjects for data-driven design. All systems are multiplayer-ready using Unity Netcode's `NetworkBehaviour` base class with `NetworkVariable` state synchronization and `ServerRpc`/`ClientRpc` patterns.

### Destruction System

Buildings are composed of modular `DestructibleWall` components, each containing a grid of `WallSegment` blocks with individual HP. When segments are destroyed:

1. Visual holes appear at the segment position
2. Connected segments check for structural integrity
3. If enough adjacent segments are destroyed, breach openings form
4. If structural support is removed, sections collapse with physics

### Combat Balance

| Weapon | Damage | Fire Rate | Range | Role |
|--------|--------|-----------|-------|------|
| SMG | 14 | 12/s | Short | Close quarters |
| AR | 18 | 8/s | Medium | General purpose |
| Shotgun | 8x8 | 1.5/s | Short | High burst |
| Sniper | 55 | 0.8/s | Long | Precision |

## License

Proprietary - All rights reserved
