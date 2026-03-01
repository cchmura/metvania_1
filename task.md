# Add Verticality & Castlevania-Style Stairs + JSON Level Files

## Context
The level is currently 120x11 tiles — flat with no vertical scrolling. Level data is hardcoded as C# string arrays in RoomData.cs. Two changes needed:
1. Add verticality (22 rows) with stair-stepped terrain like Castlevania
2. Move level data to external JSON zone files for cleaner separation

## Tasks

### JSON Zone File System
- [ ] Create `Levels/` directory with `zone1.json` through `zone5.json` (layout grid + metadata: id, name, width, height)
- [ ] Modify `Scripts/RoomData.cs` — remove hardcoded Zone1–Zone5 arrays, add `LoadZone()` using `Godot.FileAccess` to read JSON from `res://Levels/`, concatenate in `BuildLevel()`

### Vertical Level Layout (22 rows per zone)
- [ ] Zone 1 (Courtyard): Open sky, ground-level spawn, ascending stairs to elevated parapet
- [ ] Zone 2 (Castle Entrance): Upper walkway with save point, staircase down to ground
- [ ] Zone 3 (Great Hall): Most vertical — double-jump orb high up, staircases spiraling
- [ ] Zone 4 (Inner Corridor): Dash orb on upper platform, descending stairs, spikes below
- [ ] Zone 5 (Boss Chamber): Taller arena with elevated side ledges for tactical play

### Code Changes
- [ ] `Scenes/World/World.cs` — Add `_cameraLeadY` field, compute Y lead from `Velocity.Y * 0.08f` (clamp ±10px), change parallax `MotionMirroring` from `(320, 0)` to `(320, 180)` on all 3 layers
- [ ] `Scripts/GameState.cs` — Update default spawn position to match new P tile location
- [ ] `Scenes/UI/Hud.cs` — Update pause map room rectangle to reflect taller level proportions

### Verification
- [ ] `dotnet build` — no compile errors
- [ ] Vertical camera scrolling when jumping/falling
- [ ] Stairs walkable in all zones
- [ ] All ability orbs/save points reachable
- [ ] Pit death still works at bottom
- [ ] Parallax scrolls vertically without gaps

## Notes
- Stair pattern: 2-tile-wide steps, 1 tile high — players jump up naturally
- Camera limits, pit death, tile painting, edge post-processing, boss arena seal, entity spawning, player physics all auto-adapt from level dimensions (no changes needed)
