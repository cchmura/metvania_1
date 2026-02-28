# Metvania-1

2D metroidvania vertical slice in Godot 4.6 with C#.

## Architecture
- Single long scrolling level — 150x22 tiles (2400x352px), built from 5 sections in RoomData.cs
- Level defined as ASCII art in RoomData.cs, painted via TileMapLayer
- Camera2D follows player, limits clamp to level bounds, scrolls horizontally
- Pit death: falling below the level triggers death/respawn
- Autoloads: GameState (ability/item tracking), SaveManager (JSON to user://), EffectsManager (hit freeze/shake/particles), AudioManager (sound pool + procedural sounds)
- Reusable combat components: HealthComponent, Hitbox, Hurtbox
- Camera lead: offset tracks player velocity direction (max ~30px)

## Level Layout (150x22 tiles = 2400x352px)
Single left-to-right scrolling level built from 5 sections of 30 columns each:
- **Section 1 — Gentle Start** (cols 0-29): Left wall, player spawn, 4-tile gap teaches pit jumping
- **Section 2 — Stepping Stones** (cols 30-59): Two 5-tile gaps, one-way platforms above, crawlers + flyer
- **Section 3 — The Shrine** (cols 60-89): Save point, ascending platform staircase to double jump orb, 13-tile gap (progression gate)
- **Section 4 — Deep Pits** (cols 90-119): Two 8-tile gaps, floating platforms, tests double jump mastery
- **Section 5 — The Gauntlet** (cols 120-149): Right wall, 13-tile gap, goal marker at far right

## Collision Layers
| Layer | Bit | Purpose |
|-------|-----|---------|
| 1 | 1 | World geometry (TileMap) |
| 2 | 2 | Player body |
| 3 | 4 | Enemy bodies |
| 4 | 8 | Player attack hitbox |
| 5 | 16 | Enemy hitbox (contact damage) |
| 6 | 32 | Player hurtbox |
| 7 | 64 | Enemy hurtbox |
| 8 | 128 | Pickups/triggers |

## Combat System
- Attack (J): directional slash (forward/up/down based on held input)
- Attack timing: 0.25s active, 0.35s cooldown
- Down-slash pogo: bounce off enemies, reset double jump
- Player: 5 HP, 1s invincibility on hit, knockback, death → respawn
- Enemies: Crawler (2 HP, ground patrol), Flyer (1 HP, sine bob + aggro)

## Physics
- Jump impulse: -400 (100px single jump, 200px double jump)
- Gravity: 800, Speed: 200, Acceleration: 1200, Friction: 1000
- Coyote time: 0.1s, Jump buffer: 0.1s, Variable jump height on early release
- Pogo impulse: -350

## Input Actions
- move_left (A/Left), move_right (D/Right), move_up (W/Up), move_down (S/Down)
- jump (Space), interact (E), attack (J)

## Display
- 640x352 viewport, 2x window (1280x704), viewport stretch
- TileMap: 16x16 tiles, level is 150x22 tiles (5 sections of 30 cols)

## File Structure
```
Scenes/Player/    Player.tscn + Player.cs (CharacterBody2D, state machine, combat)
Scenes/World/     World.tscn + World.cs (TileMap room painting, entity spawning, death/respawn)
Scenes/Objects/   AbilityOrb, SavePoint (.tscn + .cs each)
Scenes/Enemies/   EnemyBase.cs, Crawler (.tscn + .cs), Flyer (.tscn + .cs)
Scenes/UI/        Hud.tscn + Hud.cs (CanvasLayer, health pips)
Scripts/          GameState.cs, SaveManager.cs, EffectsManager.cs, AudioManager.cs (autoloads)
Scripts/Components/ HealthComponent.cs, Hitbox.cs, Hurtbox.cs
Scripts/          TileMapBuilder.cs, RoomData.cs
```

## Build
- Godot.NET.Sdk/4.4.0, net8.0
- `dotnet build` from project root

## Status
- [x] Project structure and build files
- [x] Autoloads (GameState, SaveManager) with health persistence
- [x] Player movement with double jump
- [x] TileMap-based rooms (replaced programmatic rectangles)
- [x] Game objects (AbilityOrb, SavePoint)
- [x] HUD with health pips
- [x] Combat system (directional attack, pogo)
- [x] Enemies (Crawler, Flyer)
- [x] Health/damage/invincibility system
- [x] Death and respawn flow
- [x] Game feel / juice (hit freeze, screen shake, particles, sounds, camera lead)
- [x] Single scrolling level with pit death (replaced multi-room layout)
- [ ] Save/load tested end-to-end
- [ ] Combat balance tuning
