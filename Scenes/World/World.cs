using Godot;
using System.Collections.Generic;

namespace metvania_1;

public partial class World : Node2D
{
	private struct EnemySpawn
	{
		public string Type;
		public string ScenePath;
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
	private List<Node> _activeObjects = new();

	private Vector2 _levelSpawnPosition;

	// Respawn support
	private bool _respawning;

	// Camera lead
	private EffectsManager _effectsManager;
	private AudioManager _audioManager;
	private float _cameraLeadX;

	// Boss arena
	private Vector2 _bossLockPosition;
	private Vector2 _bossSpawnPosition;
	private bool _bossActive;
	private bool _bossDefeated;
	private Node2D _boss;
	private string _bossScenePath;
	private Area2D _bossLockTrigger;

	// Level transitions
	private static readonly Dictionary<string, string> NextLevel = new()
	{
		["main"] = "catacombs",
		["catacombs"] = "level2",
		["level2"] = null,
	};
	private ColorRect _fadeOverlay;
	private bool _transitioning;
	private Area2D _goalArea;
	private bool _playerInGoal;

	public override void _Ready()
	{
		_gameState = GetNode<GameState>("/root/GameState");
		_saveManager = GetNode<SaveManager>("/root/SaveManager");
		_audioManager = GetNodeOrNull<AudioManager>("/root/AudioManager");

		// Title screen handles save loading — no auto-load here

		// Build tilemap
		_tileMap = TileMapBuilder.CreateTileMapLayer();
		AddChild(_tileMap);

		// Paint the level
		LoadRoom(_gameState.CurrentLevelId);

		// Spawn player
		SpawnPlayer();

		// Camera
		_camera = new Camera2D();
		_camera.PositionSmoothingEnabled = true;
		_camera.PositionSmoothingSpeed = 8f;
		_player.AddChild(_camera);
		UpdateCameraLimits();

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

		// Room name
		_gameState.CurrentRoom = _currentRoom.DisplayName;
		_hud?.SetRoomName(_currentRoom.DisplayName);

		// Pass camera to EffectsManager
		_effectsManager = GetNodeOrNull<EffectsManager>("/root/EffectsManager");
		_effectsManager?.SetCamera(_camera);

		// Parallax background
		SetupParallax();

		// Boss arena
		_bossDefeated = _gameState.BossDefeated;
		SetupBossArena();

		// Fade overlay (CanvasLayer so it's always on top)
		var fadeCanvas = new CanvasLayer();
		fadeCanvas.Layer = 20;
		AddChild(fadeCanvas);
		_fadeOverlay = new ColorRect();
		_fadeOverlay.Color = new Color(0, 0, 0, 1);
		_fadeOverlay.AnchorRight = 1;
		_fadeOverlay.AnchorBottom = 1;
		_fadeOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
		fadeCanvas.AddChild(_fadeOverlay);

		// Fade in from black on level start
		var fadeIn = CreateTween();
		fadeIn.TweenProperty(_fadeOverlay, "color:a", 0f, 0.5f);

		// Start music
		_audioManager?.PlayMusic(_currentRoom.Music);
	}

	public override void _Process(double delta)
	{
		if (_player == null || _respawning || _transitioning) return;

		// Pit death: fell below the level
		if (_player.GlobalPosition.Y > _levelBounds.Position.Y + _levelBounds.Size.Y + 32)
		{
			OnPlayerDied();
			return;
		}

		// Goal interaction check
		if (_playerInGoal && Input.IsActionJustPressed("interact"))
		{
			StartLevelTransition();
			return;
		}

		// Camera lead offset
		float targetLead = _player.Velocity.X * 0.15f;
		targetLead = Mathf.Clamp(targetLead, -15f, 15f);
		_cameraLeadX = Mathf.Lerp(_cameraLeadX, targetLead, 3.0f * (float)delta);
		_effectsManager?.SetCameraBaseOffset(new Vector2(_cameraLeadX, 0));
	}

	// ─── Level Loading ──────────────────────────────────────────────

	private RoomData.RoomDef _currentRoom;

	private void LoadRoom(string roomId)
	{
		_currentRoom = RoomData.GetLevel(roomId);
		var layout = _currentRoom.Layout;
		int cols = layout[0].Length;
		int rows = layout.Length;

		// Level bounds
		_levelBounds = new Rect2(0, 0, cols * TileMapBuilder.TileSize, rows * TileMapBuilder.TileSize);

		// Paint tilemap
		PaintRoom(layout);
		TileMapBuilder.PostProcessTileEdges(_tileMap, layout);

		// Spawn position: use level P tile unless save has a valid position
		if (!_saveManager.HasSaveFile() || !_levelBounds.HasPoint(_gameState.PlayerSpawnPosition))
		{
			_gameState.PlayerSpawnPosition = _levelSpawnPosition;
		}
	}

	private void UpdateCameraLimits()
	{
		if (_camera == null) return;
		_camera.LimitLeft = (int)_levelBounds.Position.X;
		_camera.LimitTop = (int)_levelBounds.Position.Y;
		_camera.LimitRight = (int)(_levelBounds.Position.X + _levelBounds.Size.X);
		_camera.LimitBottom = (int)(_levelBounds.Position.Y + _levelBounds.Size.Y);
	}

	// ─── Parallax ───────────────────────────────────────────────────

	private void SetupParallax()
	{
		var parallax = new ParallaxBackground();
		AddChild(parallax);

		var farLayer = new ParallaxLayer();
		farLayer.ZIndex = -10;
		farLayer.MotionScale = new Vector2(0.1f, 0.1f);
		farLayer.MotionMirroring = new Vector2(320, 0);
		var farSprite = new Sprite2D();
		farSprite.Texture = AssetLoader.ParallaxFarLayer(320, 180);
		farSprite.Centered = false;
		farLayer.AddChild(farSprite);
		parallax.AddChild(farLayer);

		var midLayer = new ParallaxLayer();
		midLayer.ZIndex = -9;
		midLayer.MotionScale = new Vector2(0.3f, 0.3f);
		midLayer.MotionMirroring = new Vector2(320, 0);
		var midSprite = new Sprite2D();
		midSprite.Texture = AssetLoader.ParallaxMidLayer(320, 180);
		midSprite.Centered = false;
		midLayer.AddChild(midSprite);
		parallax.AddChild(midLayer);

		var nearLayer = new ParallaxLayer();
		nearLayer.ZIndex = -8;
		nearLayer.MotionScale = new Vector2(0.6f, 0.6f);
		nearLayer.MotionMirroring = new Vector2(320, 0);
		var nearSprite = new Sprite2D();
		nearSprite.Texture = AssetLoader.ParallaxNearLayer(320, 180);
		nearSprite.Centered = false;
		nearLayer.AddChild(nearSprite);
		parallax.AddChild(nearLayer);
	}

	// ─── Room Painting ──────────────────────────────────────────────

	private void PaintRoom(string[] layout)
	{
		for (int row = 0; row < layout.Length; row++)
		{
			for (int col = 0; col < layout[row].Length; col++)
			{
				char c = layout[row][col];
				var tileCoord = new Vector2I(col, row);
				var worldPos = new Vector2(
					col * TileMapBuilder.TileSize + TileMapBuilder.TileSize / 2f,
					row * TileMapBuilder.TileSize + TileMapBuilder.TileSize / 2f);

				if (!EntityRegistry.Entries.TryGetValue(c, out var def))
				{
					_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
					continue;
				}

				// Paint tile
				_tileMap.SetCell(tileCoord, 0, def.TileCoord);

				switch (def.Mode)
				{
					case SpawnMode.Tile:
						break;

					case SpawnMode.Marker:
						HandleMarker(def, worldPos);
						break;

					case SpawnMode.Scene:
						SpawnSceneEntity(def, worldPos);
						break;

					case SpawnMode.Enemy:
						_enemySpawns.Add(new EnemySpawn
						{
							Type = def.EnemyType,
							ScenePath = def.ScenePath,
							Position = worldPos,
						});
						break;

					case SpawnMode.Custom:
						HandleCustomSpawn(c, worldPos);
						break;
				}
			}
		}
	}

	private void HandleMarker(EntityDef def, Vector2 worldPos)
	{
		switch (def.MarkerName)
		{
			case "player_spawn":
				_levelSpawnPosition = worldPos;
				if (!_saveManager.HasSaveFile())
					_gameState.PlayerSpawnPosition = worldPos;
				break;
			case "boss_lock":
				_bossLockPosition = worldPos;
				break;
			case "boss_spawn":
				_bossSpawnPosition = worldPos;
				if (!string.IsNullOrEmpty(def.ScenePath))
					_bossScenePath = def.ScenePath;
				break;
		}
	}

	private void SpawnSceneEntity(EntityDef def, Vector2 worldPos)
	{
		var scene = GD.Load<PackedScene>(def.ScenePath);
		var node = scene.Instantiate<Node2D>();
		node.GlobalPosition = worldPos;

		if (def.Properties != null)
		{
			foreach (var (key, value) in def.Properties)
				node.Set(key, value);
		}

		AddChild(node);
		_activeObjects.Add(node);
	}

	private void HandleCustomSpawn(char c, Vector2 worldPos)
	{
		switch (c)
		{
			case 'M':
				SpawnMaxHealthUpgrade(worldPos);
				break;
			case 'G':
				SpawnGoalMarker(worldPos);
				break;
		}
	}

	// ─── Player ─────────────────────────────────────────────────────

	private void SpawnPlayer()
	{
		var playerScene = GD.Load<PackedScene>("res://Scenes/Player/Player.tscn");
		_player = playerScene.Instantiate<Player>();
		_player.GlobalPosition = _gameState.PlayerSpawnPosition;
		AddChild(_player);

		var health = _player.GetNodeOrNull<HealthComponent>("HealthComponent");
		if (health != null)
		{
			health.MaxHealth = _gameState.MaxHealth;
			health.Reset();
			health.Died += OnPlayerDied;
		}
	}

	// ─── Enemies ────────────────────────────────────────────────────

	private void SpawnAllEnemies()
	{
		foreach (var spawn in _enemySpawns)
			SpawnEnemy(spawn);
	}

	private void SpawnEnemy(EnemySpawn spawn)
	{
		if (string.IsNullOrEmpty(spawn.ScenePath)) return;

		var scene = GD.Load<PackedScene>(spawn.ScenePath);
		if (scene == null) return;

		var enemy = scene.Instantiate<Node2D>();
		enemy.GlobalPosition = spawn.Position;
		AddChild(enemy);
		_activeEnemies.Add(enemy);
	}

	private void RespawnEnemies()
	{
		foreach (var enemy in _activeEnemies)
		{
			if (IsInstanceValid(enemy))
				enemy.QueueFree();
		}
		_activeEnemies.Clear();
		SpawnAllEnemies();
	}

	// ─── Death / Respawn ────────────────────────────────────────────

	public void OnPlayerDied()
	{
		if (_respawning) return;
		_respawning = true;

		_gameState.WeaponTier = 1;

		if (_bossActive)
		{
			if (_boss != null && IsInstanceValid(_boss))
			{
				_boss.QueueFree();
				_boss = null;
			}
			_hud?.HideBossHealthBar();
			UnsealBossArena();
			_bossActive = false;
		}

		var tween = CreateTween();
		tween.TweenInterval(1.5f);
		tween.TweenCallback(Callable.From(() =>
		{
			_player.GlobalPosition = _gameState.PlayerSpawnPosition;
			var health = _player.GetNodeOrNull<HealthComponent>("HealthComponent");
			health?.Reset();
			_player.Respawn();
			RespawnEnemies();
			_respawning = false;
		}));
	}

	// ─── Boss Arena ─────────────────────────────────────────────────

	private void SetupBossArena()
	{
		if (_bossLockPosition == Vector2.Zero) return;

		_bossLockTrigger = new Area2D();
		_bossLockTrigger.CollisionLayer = 128;
		_bossLockTrigger.CollisionMask = 2;

		var shape = new CollisionShape2D();
		var rect = new RectangleShape2D();
		rect.Size = new Vector2(16, 32);
		shape.Shape = rect;
		shape.Position = new Vector2(0, -16);
		_bossLockTrigger.AddChild(shape);
		_bossLockTrigger.GlobalPosition = _bossLockPosition;
		AddChild(_bossLockTrigger);

		_bossLockTrigger.BodyEntered += (body) =>
		{
			if (body is Player && !_bossActive && !_bossDefeated)
				StartBossFight();
		};
	}

	private void StartBossFight()
	{
		_bossActive = true;

		// Seal entire column to the left of the trigger
		int lockCol = (int)(_bossLockPosition.X / TileMapBuilder.TileSize) - 2;
		int rows = (int)(_levelBounds.Size.Y / TileMapBuilder.TileSize);
		for (int r = 0; r < rows; r++)
			_tileMap.SetCell(new Vector2I(lockCol, r), 0, TileMapBuilder.SolidTile);

		string scenePath = _bossScenePath ?? "res://Scenes/Enemies/SlimeBoss.tscn";
		var bossScene = GD.Load<PackedScene>(scenePath);
		_boss = bossScene.Instantiate<Node2D>();
		_boss.GlobalPosition = _bossSpawnPosition;
		AddChild(_boss);

		var bossHealth = _boss.GetNode<HealthComponent>("HealthComponent");
		bossHealth.HealthChanged += OnBossHealthChanged;
		bossHealth.Died += OnBossDefeated;

		string bossName = _gameState.CurrentLevelId switch
		{
			"level2" => "The Executioner",
			"catacombs" => "The Ooze",
			_ => "The Slime",
		};
		_hud?.ShowBossHealthBar(bossName, bossHealth.CurrentHealth, bossHealth.MaxHealth);
		_effectsManager?.Shake(4f, 0.3f);
		_audioManager?.PlayMusic("boss");
	}

	private void OnBossHealthChanged(int current, int max)
	{
		_hud?.UpdateBossHealthBar(current, max);
	}

	private void OnBossDefeated()
	{
		_bossActive = false;
		_bossDefeated = true;
		_gameState.BossDefeated = true;

		_hud?.HideBossHealthBar();
		UnsealBossArena();
		SpawnGoalMarker(_bossSpawnPosition);
		_effectsManager?.Shake(6f, 0.4f);
		_audioManager?.PlayMusic(_currentRoom?.Music ?? "depths");
	}

	private void UnsealBossArena()
	{
		int lockCol = (int)(_bossLockPosition.X / TileMapBuilder.TileSize) - 2;
		int rows = (int)(_levelBounds.Size.Y / TileMapBuilder.TileSize);
		for (int r = 0; r < rows; r++)
			_tileMap.SetCell(new Vector2I(lockCol, r), 0, TileMapBuilder.BackgroundTile);
	}

	// ─── Object Spawners ────────────────────────────────────────────

	private void SpawnMaxHealthUpgrade(Vector2 position)
	{
		string itemId = $"MaxHP_main_{(int)position.X}_{(int)position.Y}";
		if (_gameState.IsCollected(itemId)) return;

		var pickup = new Area2D();
		pickup.GlobalPosition = position;
		pickup.CollisionLayer = 128;
		pickup.CollisionMask = 2;

		var shape = new CollisionShape2D();
		var circle = new CircleShape2D();
		circle.Radius = 10f;
		shape.Shape = circle;
		pickup.AddChild(shape);

		var sprite = new Sprite2D();
		sprite.Texture = AssetLoader.MaxHealthUpgradeSprite();
		pickup.AddChild(sprite);

		pickup.BodyEntered += (body) =>
		{
			if (body is not Player) return;
			_gameState.MaxHealth++;
			_gameState.MarkCollected(itemId);

			// Heal and update HUD
			var players = GetTree().GetNodesInGroup("player");
			if (players.Count > 0 && players[0] is Player player)
			{
				var health = player.GetNodeOrNull<HealthComponent>("HealthComponent");
				if (health != null)
				{
					health.MaxHealth = _gameState.MaxHealth;
					health.Reset();
				}
			}
			_hud?.ShowPrompt("Max HP increased!");
			var tween = pickup.CreateTween();
			tween.TweenProperty(pickup, "modulate:a", 0f, 0.3f);
			tween.TweenCallback(Callable.From(pickup.QueueFree));
		};

		AddChild(pickup);
		_activeObjects.Add(pickup);
	}

	private void SpawnGoalMarker(Vector2 position)
	{
		var marker = new ColorRect();
		marker.Position = position - new Vector2(16, 32);
		marker.Size = new Vector2(32, 32);
		marker.Color = new Color(1f, 0.8f, 0f, 1f);
		marker.ZIndex = 5;
		AddChild(marker);
		_activeObjects.Add(marker);

		var label = new Label();
		label.Position = position - new Vector2(30, 48);
		label.ZIndex = 5;
		AddChild(label);
		_activeObjects.Add(label);

		string levelId = _gameState.CurrentLevelId;
		bool hasNextLevel = NextLevel.TryGetValue(levelId, out var next) && next != null;
		label.Text = hasNextLevel ? "CONTINUE" : "GOAL";

		_goalArea = new Area2D();
		_goalArea.Position = position;
		_goalArea.CollisionLayer = 128;
		_goalArea.CollisionMask = 2;

		var goalShape = new CollisionShape2D();
		var rect = new RectangleShape2D();
		rect.Size = new Vector2(32, 32);
		goalShape.Shape = rect;
		goalShape.Position = new Vector2(0, -16);
		_goalArea.AddChild(goalShape);

		_goalArea.BodyEntered += (body) =>
		{
			if (body is Player)
			{
				_playerInGoal = true;
				_hud?.ShowPrompt("Press [E] to continue");
			}
		};
		_goalArea.BodyExited += (body) =>
		{
			if (body is Player)
			{
				_playerInGoal = false;
				_hud?.HidePrompt();
			}
		};

		AddChild(_goalArea);
		_activeObjects.Add(_goalArea);
	}

	private void StartLevelTransition()
	{
		if (_transitioning) return;
		_transitioning = true;
		_playerInGoal = false;
		_hud?.HidePrompt();

		string levelId = _gameState.CurrentLevelId;
		bool hasNextLevel = NextLevel.TryGetValue(levelId, out var nextLevelId) && nextLevelId != null;

		// Disable player collision with world so they walk through arena walls
		_player.CollisionMask &= ~1u;
		_player.StartAutoWalk(1);

		var tween = CreateTween();
		// Auto-walk for 0.8s
		tween.TweenInterval(0.8f);
		// Fade to black
		tween.TweenProperty(_fadeOverlay, "color:a", 1f, 0.5f);
		tween.TweenCallback(Callable.From(() =>
		{
			_player.StopAutoWalk();
			_audioManager?.StopMusic(0.3f);

			if (hasNextLevel)
			{
				// Transition to next level
				_gameState.CurrentLevelId = nextLevelId;
				// Set spawn to default; the new level's P marker will set it
				_gameState.PlayerSpawnPosition = new Vector2(40, 136);
				_saveManager.Save();
				GetTree().ChangeSceneToFile("res://Scenes/World/World.tscn");
			}
			else
			{
				// No more levels — return to title
				_saveManager.Save();
				GetTree().ChangeSceneToFile("res://Scenes/UI/TitleScreen.tscn");
			}
		}));
	}
}
