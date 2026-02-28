using Godot;
using System.Collections.Generic;

namespace metvania_1;

public partial class World : Node2D
{
	private static readonly float LevelPixelW = RoomData.LevelWidthTiles * TileMapBuilder.TileSize;
	private static readonly float LevelPixelH = RoomData.LevelHeightTiles * TileMapBuilder.TileSize;

	private struct EnemySpawn
	{
		public string Type; // "crawler" or "flyer"
		public Vector2 Position;
	}

	private Rect2 _levelBounds;
	private Camera2D _camera;
	private Player _player;
	private Hud _hud;
	private GameState _gameState;
	private SaveManager _saveManager;
	private TileMapLayer _tileMap;

	private List<EnemySpawn> _enemySpawns = new();
	private List<Node2D> _activeEnemies = new();

	// Spawn position found from 'P' tile in level data
	private Vector2 _levelSpawnPosition;

	// Respawn support
	private bool _respawning;

	// Camera lead
	private EffectsManager _effectsManager;
	private float _cameraLeadX;

	public override void _Ready()
	{
		_gameState = GetNode<GameState>("/root/GameState");
		_saveManager = GetNode<SaveManager>("/root/SaveManager");

		_levelBounds = new Rect2(0, 0, LevelPixelW, LevelPixelH);

		// Load save if exists
		if (_saveManager.HasSaveFile())
		{
			_saveManager.Load();
		}

		// Build tilemap
		_tileMap = TileMapBuilder.CreateTileMapLayer();
		AddChild(_tileMap);

		// Paint the level and collect spawn points
		PaintLevel();

		// Validate spawn position is within level bounds (guards against stale saves)
		if (!_levelBounds.HasPoint(_gameState.PlayerSpawnPosition))
		{
			_gameState.PlayerSpawnPosition = _levelSpawnPosition;
		}

		// Spawn player
		SpawnPlayer();

		// Camera
		_camera = new Camera2D();
		_camera.PositionSmoothingEnabled = true;
		_camera.PositionSmoothingSpeed = 8f;
		_player.AddChild(_camera);

		// Set camera limits to level bounds
		_camera.LimitLeft = (int)_levelBounds.Position.X;
		_camera.LimitTop = (int)_levelBounds.Position.Y;
		_camera.LimitRight = (int)(_levelBounds.Position.X + _levelBounds.Size.X);
		_camera.LimitBottom = (int)(_levelBounds.Position.Y + _levelBounds.Size.Y);

		// HUD
		var hudScene = GD.Load<PackedScene>("res://Scenes/UI/Hud.tscn");
		_hud = hudScene.Instantiate<Hud>();
		AddChild(_hud);

		// Spawn enemies
		SpawnAllEnemies();

		// Wire health display
		var playerHealth = _player.GetNodeOrNull<HealthComponent>("HealthComponent");
		if (playerHealth != null)
		{
			playerHealth.HealthChanged += (current, max) => _hud?.UpdateHealthDisplay(current, max);
			_hud?.UpdateHealthDisplay(playerHealth.CurrentHealth, playerHealth.MaxHealth);
		}

		// Room name (single level)
		_gameState.CurrentRoom = "The Depths";
		_hud?.SetRoomName("The Depths");

		// Pass camera to EffectsManager
		_effectsManager = GetNodeOrNull<EffectsManager>("/root/EffectsManager");
		_effectsManager?.SetCamera(_camera);
	}

	public override void _Process(double delta)
	{
		if (_player == null || _respawning) return;

		// Pit death: fell below the level
		if (_player.GlobalPosition.Y > _levelBounds.Position.Y + _levelBounds.Size.Y + 32)
		{
			OnPlayerDied();
			return;
		}

		// Camera lead offset
		float targetLead = _player.Velocity.X * 0.15f;
		targetLead = Mathf.Clamp(targetLead, -30f, 30f);
		_cameraLeadX = Mathf.Lerp(_cameraLeadX, targetLead, 3.0f * (float)delta);
		_effectsManager?.SetCameraBaseOffset(new Vector2(_cameraLeadX, 0));
	}

	private void PaintLevel()
	{
		var data = RoomData.Level;

		for (int row = 0; row < data.Length; row++)
		{
			for (int col = 0; col < data[row].Length; col++)
			{
				char c = data[row][col];
				var tileCoord = new Vector2I(col, row);
				var worldPos = new Vector2(col * TileMapBuilder.TileSize + TileMapBuilder.TileSize / 2f, row * TileMapBuilder.TileSize + TileMapBuilder.TileSize / 2f);

				switch (c)
				{
					case '#':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.SolidTile);
						break;
					case '=':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.OneWayTile);
						break;
					case '.':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						break;
					case 'P':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						_levelSpawnPosition = worldPos;
						if (!_saveManager.HasSaveFile())
						{
							_gameState.PlayerSpawnPosition = worldPos;
						}
						break;
					case 'O':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						SpawnAbilityOrb(worldPos);
						break;
					case 'S':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						SpawnSavePoint(worldPos);
						break;
					case 'G':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						SpawnGoalMarker(worldPos);
						break;
					case 'c':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						_enemySpawns.Add(new EnemySpawn { Type = "crawler", Position = worldPos });
						break;
					case 'f':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						_enemySpawns.Add(new EnemySpawn { Type = "flyer", Position = worldPos });
						break;
				}
			}
		}
	}

	private void SpawnPlayer()
	{
		var playerScene = GD.Load<PackedScene>("res://Scenes/Player/Player.tscn");
		_player = playerScene.Instantiate<Player>();
		_player.GlobalPosition = _gameState.PlayerSpawnPosition;
		AddChild(_player);

		// Connect death signal
		var health = _player.GetNodeOrNull<HealthComponent>("HealthComponent");
		if (health != null)
		{
			health.Died += OnPlayerDied;
		}
	}

	private void SpawnAllEnemies()
	{
		foreach (var spawn in _enemySpawns)
		{
			SpawnEnemy(spawn);
		}
	}

	private void SpawnEnemy(EnemySpawn spawn)
	{
		PackedScene scene = spawn.Type switch
		{
			"crawler" => GD.Load<PackedScene>("res://Scenes/Enemies/Crawler.tscn"),
			"flyer" => GD.Load<PackedScene>("res://Scenes/Enemies/Flyer.tscn"),
			_ => null,
		};

		if (scene == null) return;

		var enemy = scene.Instantiate<Node2D>();
		enemy.GlobalPosition = spawn.Position;
		AddChild(enemy);
		_activeEnemies.Add(enemy);
	}

	private void RespawnEnemies()
	{
		// Remove existing enemies
		foreach (var enemy in _activeEnemies)
		{
			if (IsInstanceValid(enemy))
			{
				enemy.QueueFree();
			}
		}
		_activeEnemies.Clear();

		// Respawn all
		SpawnAllEnemies();
	}

	public void OnPlayerDied()
	{
		if (_respawning) return;
		_respawning = true;

		// Wait, then respawn
		var tween = CreateTween();
		tween.TweenInterval(1.5f);
		tween.TweenCallback(Callable.From(() =>
		{
			// Reset player
			_player.GlobalPosition = _gameState.PlayerSpawnPosition;
			var health = _player.GetNodeOrNull<HealthComponent>("HealthComponent");
			health?.Reset();
			_player.Respawn();

			// Respawn enemies
			RespawnEnemies();

			_respawning = false;
		}));
	}

	private void SpawnAbilityOrb(Vector2 position)
	{
		var orbScene = GD.Load<PackedScene>("res://Scenes/Objects/AbilityOrb.tscn");
		var orb = orbScene.Instantiate<AbilityOrb>();
		orb.GlobalPosition = position;
		AddChild(orb);
	}

	private void SpawnSavePoint(Vector2 position)
	{
		var saveScene = GD.Load<PackedScene>("res://Scenes/Objects/SavePoint.tscn");
		var savePoint = saveScene.Instantiate<SavePoint>();
		savePoint.GlobalPosition = position;
		AddChild(savePoint);
	}

	private void SpawnGoalMarker(Vector2 position)
	{
		// Goal visual
		var marker = new ColorRect();
		marker.Position = position - new Vector2(16, 32);
		marker.Size = new Vector2(32, 32);
		marker.Color = new Color(1f, 0.8f, 0f, 1f);
		marker.ZIndex = 5;
		AddChild(marker);

		var label = new Label();
		label.Position = position - new Vector2(30, 48);
		label.Text = "GOAL";
		label.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
		label.ZIndex = 5;
		AddChild(label);

		// Goal trigger area
		var goalArea = new Area2D();
		goalArea.Position = position;
		goalArea.CollisionLayer = 128; // Pickups layer
		goalArea.CollisionMask = 2;    // Detect player body

		var goalShape = new CollisionShape2D();
		var rect = new RectangleShape2D();
		rect.Size = new Vector2(32, 32);
		goalShape.Shape = rect;
		goalShape.Position = new Vector2(0, -16);
		goalArea.AddChild(goalShape);

		goalArea.BodyEntered += (body) =>
		{
			if (body is Player)
			{
				_hud?.ShowPrompt("You reached the goal! Demo complete!");
				GD.Print("=== DEMO COMPLETE ===");
			}
		};
		goalArea.BodyExited += (body) =>
		{
			if (body is Player)
				_hud?.HidePrompt();
		};

		AddChild(goalArea);
	}
}
