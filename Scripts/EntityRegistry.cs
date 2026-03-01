using Godot;
using System.Collections.Generic;

namespace metvania_1;

public enum SpawnMode { Tile, Marker, Scene, Enemy, Custom }

public class EntityDef
{
	public SpawnMode Mode;
	public Vector2I TileCoord = TileMapBuilder.BackgroundTile;
	public string ScenePath;
	public string EnemyType;
	public string MarkerName;
	public Dictionary<string, Variant> Properties;
}

public static class EntityRegistry
{
	public static readonly Dictionary<char, EntityDef> Entries = new()
	{
		// ─── Tiles ──────────────────────────────────────────────
		['#'] = new() { Mode = SpawnMode.Tile, TileCoord = TileMapBuilder.SolidTile },
		['='] = new() { Mode = SpawnMode.Tile, TileCoord = TileMapBuilder.OneWayTile },
		['.'] = new() { Mode = SpawnMode.Tile, TileCoord = TileMapBuilder.BackgroundTile },

		// ─── Markers ────────────────────────────────────────────
		['P'] = new() { Mode = SpawnMode.Marker, MarkerName = "player_spawn" },
		['L'] = new() { Mode = SpawnMode.Marker, MarkerName = "boss_lock" },
		['B'] = new() { Mode = SpawnMode.Marker, MarkerName = "boss_spawn", ScenePath = "res://Scenes/Enemies/SlimeBoss.tscn" },
		['E'] = new() { Mode = SpawnMode.Marker, MarkerName = "boss_spawn", ScenePath = "res://Scenes/Enemies/Executioner.tscn" },

		// ─── Scenes (objects) ───────────────────────────────────
		['S'] = new() { Mode = SpawnMode.Scene, ScenePath = "res://Scenes/Objects/SavePoint.tscn" },
		['x'] = new() { Mode = SpawnMode.Scene, ScenePath = "res://Scenes/Objects/Spikes.tscn" },
		['C'] = new() { Mode = SpawnMode.Scene, ScenePath = "res://Scenes/Objects/Candle.tscn" },
		['O'] = new() { Mode = SpawnMode.Scene, ScenePath = "res://Scenes/Objects/AbilityOrb.tscn" },
		['D'] = new()
		{
			Mode = SpawnMode.Scene,
			ScenePath = "res://Scenes/Objects/AbilityOrb.tscn",
			Properties = new() { ["OrbId"] = "DashOrb", ["AbilityName"] = "Dash" },
		},
		['H'] = new()
		{
			Mode = SpawnMode.Scene,
			ScenePath = "res://Scenes/Objects/MovingPlatform.tscn",
			Properties = new()
			{
				["Mode"] = (int)PlatformMode.Horizontal,
				["MoveDistance"] = 64f,
				["MoveSpeed"] = 40f,
			},
		},
		['V'] = new()
		{
			Mode = SpawnMode.Scene,
			ScenePath = "res://Scenes/Objects/MovingPlatform.tscn",
			Properties = new()
			{
				["Mode"] = (int)PlatformMode.Vertical,
				["MoveDistance"] = 48f,
				["MoveSpeed"] = 30f,
			},
		},
		['F'] = new()
		{
			Mode = SpawnMode.Scene,
			ScenePath = "res://Scenes/Objects/MovingPlatform.tscn",
			Properties = new()
			{
				["Mode"] = (int)PlatformMode.Falling,
			},
		},

		// ─── Enemies ────────────────────────────────────────────
		['c'] = new() { Mode = SpawnMode.Enemy, EnemyType = "crawler", ScenePath = "res://Scenes/Enemies/Crawler.tscn" },
		['f'] = new() { Mode = SpawnMode.Enemy, EnemyType = "flyer", ScenePath = "res://Scenes/Enemies/Flyer.tscn" },
		['s'] = new() { Mode = SpawnMode.Enemy, EnemyType = "shooter", ScenePath = "res://Scenes/Enemies/Shooter.tscn" },
		['r'] = new() { Mode = SpawnMode.Enemy, EnemyType = "charger", ScenePath = "res://Scenes/Enemies/Charger.tscn" },
		['h'] = new() { Mode = SpawnMode.Enemy, EnemyType = "shielder", ScenePath = "res://Scenes/Enemies/Shielder.tscn" },
		['d'] = new() { Mode = SpawnMode.Enemy, EnemyType = "dropper", ScenePath = "res://Scenes/Enemies/Dropper.tscn" },
		['b'] = new() { Mode = SpawnMode.Enemy, EnemyType = "bat", ScenePath = "res://Scenes/Enemies/Bat.tscn" },
		['k'] = new() { Mode = SpawnMode.Enemy, EnemyType = "skeleton", ScenePath = "res://Scenes/Enemies/Skeleton.tscn" },
		['g'] = new() { Mode = SpawnMode.Enemy, EnemyType = "ghost", ScenePath = "res://Scenes/Enemies/Ghost.tscn" },
		['n'] = new() { Mode = SpawnMode.Enemy, EnemyType = "knight", ScenePath = "res://Scenes/Enemies/Knight.tscn" },

		// ─── Custom (inline spawn code) ─────────────────────────
		['M'] = new() { Mode = SpawnMode.Custom },
		['G'] = new() { Mode = SpawnMode.Custom },
	};
}
