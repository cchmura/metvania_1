# Metvania-1

2D metroidvania vertical slice in Godot 4.6 with C#.

## Architecture
- Single linear level: 120x11 tiles (Castlevania-style, horizontal scroll only)
- Level defined as ASCII art in RoomData.cs (`RoomDef`), painted via TileMapLayer
- Camera2D follows player, limits clamp to level bounds, scrolls horizontally only
- Pit death: falling below the level triggers death/respawn
- Autoloads: GameState (ability/item/room tracking), SaveManager (3-slot JSON to user://saves/), EffectsManager (hit freeze/shake/particles), AudioManager (SFX/Music buses + procedural music + crossfade)
- Title screen: main menu → slot select → settings, launches World scene
- Pause menu: Escape → Resume/Map/Settings/Quit to Title
- Reusable combat components: HealthComponent, Hitbox, Hurtbox
- Camera lead: offset tracks player velocity direction (max ~15px)
- All visuals generated procedurally via SpriteFactory (no image assets)

## Level Layout
Single "main" room, 120×11 tiles, 5 zones (24 cols each):

| Zone | Cols | Theme | Contents |
|------|------|-------|----------|
| 1 | 0-23 | Courtyard (open sky) | P spawn, 2 crawlers, 1 flyer, 1 bat, 1 candle |
| 2 | 24-47 | Castle Entrance | Save point, spikes, 2 crawlers, 1 flyer, 1 skeleton, 1 bat, 1 candle |
| 3 | 48-71 | Great Hall | Double jump orb, H-platform, shooter, shielder, charger, 1 ghost, 1 knight, 1 candle |
| 4 | 72-95 | Inner Corridor | Dash orb, F-platform, dropper, shooter, 2 skeletons, 1 ghost, 1 bat, 1 candle, spikes, pits |
| 5 | 96-119 | Boss Chamber | Boss lock, Guardian spawn, enclosed arena |

## Visual System
- **SpriteFactory** (`Scripts/SpriteFactory.cs`): Static utility generating all pixel art as `ImageTexture` from `byte[,]` arrays + color palettes
- **Player**: `AnimatedSprite2D` with programmatic `SpriteFrames` (idle, run, jump, fall, attack, dash, wall_slide, damaged)
- **Enemies/Objects**: `Sprite2D` with textures from SpriteFactory, facing via `FlipH`, state colors via `Modulate`
- **Tiles**: 9-tile atlas (solid + 6 edge variants + one-way + background), edge post-processing in `TileMapBuilder.PostProcessTileEdges()`
- **Parallax**: 3-layer `ParallaxBackground` (far=stars, mid=pillars, near=stalactites)

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
- Forward combo: 3-hit chain (J-J-J), hit 3 deals 2 damage with larger hitbox. Up/down remain single hits
- Combo timing: 0.2s/0.2s/0.3s active, 0.35s window between hits, 0.4s cooldown after chain
- Down-slash pogo: bounce off enemies, reset double jump
- Weapon power-up: breakable candles drop weapon upgrades, 5 tiers max, resets to 1 on death
- Effective damage: BaseDamage + (WeaponTier - 1). At tier 5: forward hits deal 5/5/6
- Player: 20 HP (upgradeable), 1s invincibility on hit, knockback, death → respawn
- Enemies: Crawler (2 HP, 35 speed), Flyer (1 HP, 100px aggro, 50 speed), Shooter (2 HP, projectile every 2.0s at 130px), Charger (3 HP, 280 charge speed), Shielder (4 HP, 25 speed, frontal deflect), Dropper (1 HP, 24px trigger), Bat (1 HP, 80px/s sine-wave flight, despawns at 500px), Skeleton (2 HP, 30px/s patrol, bone arc projectile every 2.5s at 110px), Ghost (2 HP, 35px/s drift, 90px aggro, phases through walls, semi-transparent), Knight (3 HP, 20px/s patrol, 0.3s wind-up flash, 0.5s spear thrust, 1.5s cooldown)
- Boss: Guardian (12 HP, phase 2 at 6 HP: 450 charge + jump slam + shockwave + double projectile, 0.8s recover)
- Breakable candles: 1 HP destructible objects, drop weapon upgrade pickups (auto-despawn 10s)

## Physics
- Jump impulse: -290 (~4 tile jump height), Gravity: 650, Speed: 120, Accel: 900, Friction: 1400
- Coyote time: 0.1s, Jump buffer: 0.1s, Variable jump height on early release
- Pogo impulse: -260
- Wall slide max speed: 35, Wall jump: -270 vertical + 150 horizontal, 0.12s input lock
- Dash: 550 speed, 0.15s duration, 0.4s cooldown, i-frames during, once per air + resets on land/wall

## Input Actions
- move_left (A/Left), move_right (D/Right), move_up (W/Up), move_down (S/Down)
- jump (Space), interact (E), attack (J), dash (L), pause (Escape)

## Display
- 320x180 viewport, 4x window (1280x720), viewport stretch
- TileMap: 16x16 tiles (20x11 visible)
- Pause map: Escape toggles overlay showing single room

## File Structure
```
Scenes/Player/    Player.tscn + Player.cs (CharacterBody2D, AnimatedSprite2D, state machine, combat)
Scenes/World/     World.tscn + World.cs (single-room level, TileMap, entity spawning, parallax)
Scenes/Objects/   AbilityOrb, SavePoint, Spikes, MovingPlatform, Candle (.tscn + .cs each)
Scenes/Enemies/   EnemyBase.cs, Crawler, Flyer, Shooter, Charger, Shielder, Dropper, Bat, Skeleton, Ghost, Knight, Guardian, Projectile, BoneProjectile (.tscn + .cs each)
Scenes/UI/        Hud.tscn + Hud.cs (CanvasLayer, health pips, boss bar, pause menu)
Scenes/UI/        TitleScreen.tscn + TitleScreen.cs (main menu, slot select, settings)
Scripts/          GameState.cs, SaveManager.cs, EffectsManager.cs, AudioManager.cs (autoloads)
Scripts/          SpriteFactory.cs (procedural pixel art), TileMapBuilder.cs, RoomData.cs, TitleScreen.cs
Scripts/Components/ HealthComponent.cs, Hitbox.cs, Hurtbox.cs
```

## Tile Codes (RoomData.cs)
- `#` solid, `=` one-way, `.` background, `P` player spawn, `G` goal
- `O` double jump orb, `D` dash orb, `S` save point, `M` max health upgrade
- `c` crawler, `f` flyer, `s` shooter, `r` charger, `h` shielder, `d` dropper
- `b` bat, `k` skeleton, `g` ghost, `n` knight
- `x` floor spikes, `B` boss spawn, `L` boss lock trigger
- `H` horizontal platform, `V` vertical platform, `F` falling platform
- `C` breakable candle (drops weapon upgrade)
- `>` door right, `<` door left (unused in current layout)

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
- [x] Single scrolling level with pit death (120x11, Castlevania-style)
- [x] Spikes (floor hazard, 1 HP damage + knockback)
- [x] Wall slide / wall jump (always available, no ability gate)
- [x] Dash ability (orb pickup, i-frames, afterimage trail)
- [x] Moving platforms (horizontal, vertical, falling)
- [x] 3-hit forward combo system (hit 3 = 2 damage)
- [x] New enemies: Shooter, Charger, Shielder, Dropper
- [x] Projectile system (enemy ranged attacks)
- [x] Boss arena (Zone 5) with lock-in trigger (full-column seal)
- [x] Guardian boss (2-phase: charge/projectile, jump slam/shockwave)
- [x] Boss health bar HUD
- [x] Save/load with boss defeat + visited rooms persistence
- [x] SpriteFactory procedural pixel art (all entities, tiles, parallax)
- [x] Viewport refactor (320x180, 4x scale, Castlevania-style)
- [x] Player visual overhaul (AnimatedSprite2D, state animations)
- [x] Enemy/object visual overhaul (Sprite2D, textures, FlipH, Modulate)
- [x] Tile edge variants (9-tile atlas, cosmetic post-processing)
- [x] Parallax background (3-layer scrolling)
- [x] Pause map overlay (Escape, single room display)
- [x] Combat balance tuning
- [x] Audio buses (SFX + Music, volume control)
- [x] Procedural music generator (6 tracks, ADSR envelopes, crossfade)
- [x] Expanded SFX (layered sounds, arpeggios, menu sounds)
- [x] Save system hardening (3 slots, versioning, legacy migration)
- [x] Title screen (main menu, slot select, settings)
- [x] Pause menu overhaul (Resume/Map/Settings/Quit states)
- [x] Music hooks (room music, boss music, crossfade transitions)
- [x] New enemies: Bat (sine-wave flight), Skeleton (bone arc projectile), Ghost (phases through walls), Knight (spear thrust)
- [x] Breakable candles (drop weapon upgrade pickups)
- [x] Weapon power-up system (5 tiers, resets on death, HUD display)
- [x] BoneProjectile (arc + gravity + spin)
