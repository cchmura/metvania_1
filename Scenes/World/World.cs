using Godot;
using System.Collections.Generic;

namespace metvania_1;

public partial class World : Node2D
{
	private struct EnemySpawn
	{
		public string Type;
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
	private Guardian _boss;
	private Area2D _bossLockTrigger;

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
		LoadRoom("main");

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
		_gameState.CurrentRoom = "The Depths";
		_hud?.SetRoomName("The Depths");

		// Pass camera to EffectsManager
		_effectsManager = GetNodeOrNull<EffectsManager>("/root/EffectsManager");
		_effectsManager?.SetCamera(_camera);

		// Parallax background
		SetupParallax();

		// Boss arena
		_bossDefeated = _gameState.BossDefeated;
		SetupBossArena();

		// Start music
		_audioManager?.PlayMusic("depths");
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
		targetLead = Mathf.Clamp(targetLead, -15f, 15f);
		_cameraLeadX = Mathf.Lerp(_cameraLeadX, targetLead, 3.0f * (float)delta);
		_effectsManager?.SetCameraBaseOffset(new Vector2(_cameraLeadX, 0));
	}

	// ─── Level Loading ──────────────────────────────────────────────

	private void LoadRoom(string roomId)
	{
		var layout = RoomData.Level;
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
		farSprite.Texture = SpriteFactory.ParallaxFarLayer(320, 180);
		farSprite.Centered = false;
		farLayer.AddChild(farSprite);
		parallax.AddChild(farLayer);

		var midLayer = new ParallaxLayer();
		midLayer.ZIndex = -9;
		midLayer.MotionScale = new Vector2(0.3f, 0.3f);
		midLayer.MotionMirroring = new Vector2(320, 0);
		var midSprite = new Sprite2D();
		midSprite.Texture = SpriteFactory.ParallaxMidLayer(320, 180);
		midSprite.Centered = false;
		midLayer.AddChild(midSprite);
		parallax.AddChild(midLayer);

		var nearLayer = new ParallaxLayer();
		nearLayer.ZIndex = -8;
		nearLayer.MotionScale = new Vector2(0.6f, 0.6f);
		nearLayer.MotionMirroring = new Vector2(320, 0);
		var nearSprite = new Sprite2D();
		nearSprite.Texture = SpriteFactory.ParallaxNearLayer(320, 180);
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

				switch (c)
				{
					case '#':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.SolidTile);
						break;
					case '=':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.OneWayTile);
						break;
					case 'P':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						_levelSpawnPosition = worldPos;
						if (!_saveManager.HasSaveFile())
							_gameState.PlayerSpawnPosition = worldPos;
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
					case 'x':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						SpawnSpikes(worldPos);
						break;
					case 'D':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						SpawnDashOrb(worldPos);
						break;
					case 'H':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						SpawnMovingPlatform(worldPos, PlatformMode.Horizontal);
						break;
					case 'V':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						SpawnMovingPlatform(worldPos, PlatformMode.Vertical);
						break;
					case 'F':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						SpawnMovingPlatform(worldPos, PlatformMode.Falling);
						break;
					case 's':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						_enemySpawns.Add(new EnemySpawn { Type = "shooter", Position = worldPos });
						break;
					case 'r':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						_enemySpawns.Add(new EnemySpawn { Type = "charger", Position = worldPos });
						break;
					case 'h':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						_enemySpawns.Add(new EnemySpawn { Type = "shielder", Position = worldPos });
						break;
					case 'd':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						_enemySpawns.Add(new EnemySpawn { Type = "dropper", Position = worldPos });
						break;
					case 'L':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						_bossLockPosition = worldPos;
						break;
					case 'B':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						_bossSpawnPosition = worldPos;
						break;
					case 'M':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						SpawnMaxHealthUpgrade(worldPos);
						break;
					case 'b':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						_enemySpawns.Add(new EnemySpawn { Type = "bat", Position = worldPos });
						break;
					case 'k':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						_enemySpawns.Add(new EnemySpawn { Type = "skeleton", Position = worldPos });
						break;
					case 'g':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						_enemySpawns.Add(new EnemySpawn { Type = "ghost", Position = worldPos });
						break;
					case 'n':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						_enemySpawns.Add(new EnemySpawn { Type = "knight", Position = worldPos });
						break;
					case 'C':
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						SpawnCandle(worldPos);
						break;
					default:
						_tileMap.SetCell(tileCoord, 0, TileMapBuilder.BackgroundTile);
						break;
				}
			}
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
		PackedScene scene = spawn.Type switch
		{
			"crawler" => GD.Load<PackedScene>("res://Scenes/Enemies/Crawler.tscn"),
			"flyer" => GD.Load<PackedScene>("res://Scenes/Enemies/Flyer.tscn"),
			"shooter" => GD.Load<PackedScene>("res://Scenes/Enemies/Shooter.tscn"),
			"charger" => GD.Load<PackedScene>("res://Scenes/Enemies/Charger.tscn"),
			"shielder" => GD.Load<PackedScene>("res://Scenes/Enemies/Shielder.tscn"),
			"dropper" => GD.Load<PackedScene>("res://Scenes/Enemies/Dropper.tscn"),
			"bat" => GD.Load<PackedScene>("res://Scenes/Enemies/Bat.tscn"),
			"skeleton" => GD.Load<PackedScene>("res://Scenes/Enemies/Skeleton.tscn"),
			"ghost" => GD.Load<PackedScene>("res://Scenes/Enemies/Ghost.tscn"),
			"knight" => GD.Load<PackedScene>("res://Scenes/Enemies/Knight.tscn"),
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

		var bossScene = GD.Load<PackedScene>("res://Scenes/Enemies/Guardian.tscn");
		_boss = bossScene.Instantiate<Guardian>();
		_boss.GlobalPosition = _bossSpawnPosition;
		AddChild(_boss);

		_boss.BossHealthChanged += OnBossHealthChanged;
		_boss.BossDefeated += OnBossDefeated;

		var bossHealth = _boss.GetNode<HealthComponent>("HealthComponent");
		_hud?.ShowBossHealthBar("The Guardian", bossHealth.CurrentHealth, bossHealth.MaxHealth);
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
		_audioManager?.PlayMusic("depths");
	}

	private void UnsealBossArena()
	{
		int lockCol = (int)(_bossLockPosition.X / TileMapBuilder.TileSize) - 2;
		int rows = (int)(_levelBounds.Size.Y / TileMapBuilder.TileSize);
		for (int r = 0; r < rows; r++)
			_tileMap.SetCell(new Vector2I(lockCol, r), 0, TileMapBuilder.BackgroundTile);
	}

	// ─── Object Spawners ────────────────────────────────────────────

	private void SpawnAbilityOrb(Vector2 position)
	{
		var orbScene = GD.Load<PackedScene>("res://Scenes/Objects/AbilityOrb.tscn");
		var orb = orbScene.Instantiate<AbilityOrb>();
		orb.GlobalPosition = position;
		AddChild(orb);
		_activeObjects.Add(orb);
	}

	private void SpawnSavePoint(Vector2 position)
	{
		var saveScene = GD.Load<PackedScene>("res://Scenes/Objects/SavePoint.tscn");
		var savePoint = saveScene.Instantiate<SavePoint>();
		savePoint.GlobalPosition = position;
		AddChild(savePoint);
		_activeObjects.Add(savePoint);
	}

	private void SpawnMovingPlatform(Vector2 position, PlatformMode mode)
	{
		var platScene = GD.Load<PackedScene>("res://Scenes/Objects/MovingPlatform.tscn");
		var platform = platScene.Instantiate<MovingPlatform>();
		platform.Mode = mode;

		switch (mode)
		{
			case PlatformMode.Horizontal:
				platform.MoveDistance = 64f;
				platform.MoveSpeed = 40f;
				break;
			case PlatformMode.Vertical:
				platform.MoveDistance = 48f;
				platform.MoveSpeed = 30f;
				break;
		}

		platform.GlobalPosition = position;
		AddChild(platform);
		_activeObjects.Add(platform);
	}

	private void SpawnDashOrb(Vector2 position)
	{
		var orbScene = GD.Load<PackedScene>("res://Scenes/Objects/AbilityOrb.tscn");
		var orb = orbScene.Instantiate<AbilityOrb>();
		orb.OrbId = "DashOrb";
		orb.AbilityName = "Dash";
		orb.GlobalPosition = position;
		AddChild(orb);
		_activeObjects.Add(orb);
	}

	private void SpawnSpikes(Vector2 position)
	{
		var spikeScene = GD.Load<PackedScene>("res://Scenes/Objects/Spikes.tscn");
		var spikes = spikeScene.Instantiate<Node2D>();
		spikes.GlobalPosition = position;
		AddChild(spikes);
		_activeObjects.Add(spikes);
	}

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
		sprite.Texture = SpriteFactory.MaxHealthUpgradeSprite();
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

	private void SpawnCandle(Vector2 position)
	{
		var candleScene = GD.Load<PackedScene>("res://Scenes/Objects/Candle.tscn");
		var candle = candleScene.Instantiate<Node2D>();
		candle.GlobalPosition = position;
		AddChild(candle);
		_activeObjects.Add(candle);
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
		label.Text = "GOAL";
		label.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
		label.ZIndex = 5;
		AddChild(label);
		_activeObjects.Add(label);

		var goalArea = new Area2D();
		goalArea.Position = position;
		goalArea.CollisionLayer = 128;
		goalArea.CollisionMask = 2;

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
			}
		};
		goalArea.BodyExited += (body) =>
		{
			if (body is Player)
				_hud?.HidePrompt();
		};

		AddChild(goalArea);
		_activeObjects.Add(goalArea);
	}
}
