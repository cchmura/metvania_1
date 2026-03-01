using Godot;
using System.Collections.Generic;

namespace metvania_1;

public partial class Executioner : CharacterBody2D
{
	private enum BossPhase { Phase1, Phase2 }
	private enum ExecState { CornerIdle, ArcFlight, Dash, Returning, Summon, Death }

	// Arena geometry (derived from spawn position in _Ready)
	private float _arenaLeftX;
	private float _arenaRightX;
	private float _cornerY;
	private float _groundY;
	private bool _atLeftCorner;

	// Arc flight (inverted U swoop, corner to corner)
	private const float ArcDurationP1 = 2.0f;
	private const float ArcDurationP2 = 1.4f;
	private const int ArcSwingDamage = 3;
	private const float ArcAttackDuration = 0.35f;
	private float _arcT;
	private float _arcStartX;
	private float _arcEndX;
	private bool _arcSwung;
	private float _arcAttackTimer;

	// Corner idle
	private const float IdleMinP1 = 1.5f;
	private const float IdleMaxP1 = 3.0f;
	private const float IdleMinP2 = 0.8f;
	private const float IdleMaxP2 = 1.8f;

	// Dash (diagonal dive from corner toward player)
	private const float DashSpeedP1 = 350f;
	private const float DashSpeedP2 = 480f;
	private const int DashDamage = 2;
	private Vector2 _dashDir;
	private Vector2 _dashStart;
	private float _dashDist;

	// Returning to corner after dash
	private const float ReturnSpeed = 300f;

	// Summon
	private const int SummonCountP1 = 2;
	private const int SummonCountP2 = 3;

	// Sprite
	private const int FrameSize = 100;
	private const string SpritePath = "res://Assets/Sprites/Executioner/png/";

	[Export] public int ContactDamage { get; set; } = 2;

	private BossPhase _phase = BossPhase.Phase1;
	private ExecState _state = ExecState.CornerIdle;
	private float _stateTimer;
	private int _facingDir = -1;
	private Player _target;
	private bool _isDead;
	private bool _attackHitActive;

	private AnimatedSprite2D _sprite;
	private HealthComponent _health;
	private Hitbox _contactHitbox;
	private Hitbox _attackHitbox;
	private Hurtbox _hurtbox;

	private PackedScene _skeletonScene;
	private readonly List<Node2D> _summonedSkeletons = new();

	[Signal]
	public delegate void BossHealthChangedEventHandler(int current, int max);

	[Signal]
	public delegate void BossDefeatedEventHandler();

	public override void _ExitTree()
	{
		DespawnSkeletons();
	}

	public override void _Ready()
	{
		AddToGroup("enemies");

		_sprite = GetNode<AnimatedSprite2D>("Sprite");
		_health = GetNode<HealthComponent>("HealthComponent");
		_contactHitbox = GetNode<Hitbox>("ContactHitbox");
		_attackHitbox = GetNode<Hitbox>("AttackHitbox");
		_hurtbox = GetNode<Hurtbox>("Hurtbox");

		_hurtbox.Health = _health;
		_contactHitbox.Damage = ContactDamage;

		_health.Damaged += OnDamaged;
		_health.Died += OnDied;
		_health.HealthChanged += (current, max) => EmitSignal(SignalName.BossHealthChanged, current, max);
		_health.Died += () => EmitSignal(SignalName.BossDefeated);

		_contactHitbox.Activate();
		_attackHitbox.Deactivate();

		_skeletonScene = GD.Load<PackedScene>("res://Scenes/Enemies/Skeleton.tscn");

		// Arena: 21 tiles wide (cols 98-118), 8 tiles tall (rows 1-8)
		// Corners: 1 tile inset from walls, 3 tiles above entity row
		_groundY = GlobalPosition.Y;
		_cornerY = _groundY - 32f;
		_arenaLeftX = GlobalPosition.X - 144f;
		_arenaRightX = GlobalPosition.X + 144f;

		// No terrain collision — boss flies freely
		CollisionMask = 0;

		// Start hovering at right corner
		_atLeftCorner = false;
		GlobalPosition = new Vector2(_arenaRightX, _cornerY);

		SetupAnimations();
		_stateTimer = 1.5f;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead) return;

		float dt = (float)delta;

		// Find player
		if (_target == null || !IsInstanceValid(_target))
		{
			var players = GetTree().GetNodesInGroup("player");
			if (players.Count > 0)
				_target = players[0] as Player;
		}

		// Phase transition
		if (_health.CurrentHealth <= 6 && _phase == BossPhase.Phase1)
		{
			_phase = BossPhase.Phase2;
			_sprite.Modulate = new Color(1.3f, 0.5f, 0.5f);
		}

		_stateTimer -= dt;

		switch (_state)
		{
			case ExecState.CornerIdle:
				HandleCornerIdle();
				break;
			case ExecState.ArcFlight:
				HandleArcFlight(dt);
				break;
			case ExecState.Dash:
				HandleDash(dt);
				break;
			case ExecState.Returning:
				HandleReturning(dt);
				break;
			case ExecState.Summon:
				HandleSummon();
				break;
		}

		FacePlayer();
		_sprite.FlipH = _facingDir < 0;
	}

	// ─── State Handlers ─────────────────────────────────────────

	private void HandleCornerIdle()
	{
		PlayAnim("idle");

		if (_stateTimer <= 0)
			ChooseCornerAction();
	}

	private void HandleArcFlight(float dt)
	{
		float duration = _phase == BossPhase.Phase2 ? ArcDurationP2 : ArcDurationP1;
		_arcT += dt / duration;

		if (_arcT >= 1f)
		{
			_atLeftCorner = !_atLeftCorner;
			float cx = _atLeftCorner ? _arenaLeftX : _arenaRightX;
			GlobalPosition = new Vector2(cx, _cornerY);
			DeactivateAttack();
			EnterCornerIdle();
			return;
		}

		// Parametric inverted U: high at corners, low at center
		float x = Mathf.Lerp(_arcStartX, _arcEndX, _arcT);
		float y = _cornerY + (_groundY - _cornerY) * Mathf.Sin(Mathf.Pi * _arcT);
		GlobalPosition = new Vector2(x, y);

		// Swing scythe at bottom of arc
		if (!_arcSwung && _arcT >= 0.35f)
		{
			_arcSwung = true;
			_sprite.Play("attack");
			_attackHitbox.Damage = ArcSwingDamage;
			_attackHitbox.Activate();
			_attackHitActive = true;
			_arcAttackTimer = ArcAttackDuration;

			var audio = GetNodeOrNull<AudioManager>("/root/AudioManager");
			audio?.Play("charge_windup");
		}

		if (_attackHitActive)
		{
			_arcAttackTimer -= dt;
			if (_arcAttackTimer <= 0)
				DeactivateAttack();
		}

		if (!_attackHitActive)
			PlayAnim("idle");

		UpdateAttackHitboxPosition();
	}

	private void HandleDash(float dt)
	{
		float speed = _phase == BossPhase.Phase2 ? DashSpeedP2 : DashSpeedP1;
		float dist = GlobalPosition.DistanceTo(_dashStart);

		if (dist >= _dashDist - 4f)
		{
			DeactivateAttack();
			EnterReturning();
			return;
		}

		GlobalPosition += _dashDir * speed * dt;

		if (!_attackHitActive)
		{
			_attackHitbox.Damage = DashDamage;
			_attackHitbox.Activate();
			_attackHitActive = true;
		}

		UpdateAttackHitboxPosition();
		PlayAnim("skill");
	}

	private void HandleReturning(float dt)
	{
		PlayAnim("idle");

		float cx = _atLeftCorner ? _arenaLeftX : _arenaRightX;
		var target = new Vector2(cx, _cornerY);
		float dist = GlobalPosition.DistanceTo(target);

		if (dist < 4f)
		{
			GlobalPosition = target;
			EnterCornerIdle();
			return;
		}

		var dir = (target - GlobalPosition).Normalized();
		GlobalPosition += dir * ReturnSpeed * dt;
	}

	private void HandleSummon()
	{
		if (!_sprite.IsPlaying() || _sprite.Animation != "summon")
		{
			SpawnSkeletons();
			EnterCornerIdle();
		}
	}

	// ─── State Transitions ──────────────────────────────────────

	private void ChooseCornerAction()
	{
		float roll = GD.Randf();

		if (_phase == BossPhase.Phase2)
		{
			if (roll < 0.35f)
				EnterArcFlight();
			else if (roll < 0.70f)
				EnterDash();
			else if (roll < 0.90f)
				EnterSummon();
			else
				EnterCornerIdle();
		}
		else
		{
			if (roll < 0.40f)
				EnterArcFlight();
			else if (roll < 0.65f)
				EnterDash();
			else if (roll < 0.80f)
				EnterSummon();
			else
				EnterCornerIdle();
		}
	}

	private void EnterCornerIdle()
	{
		_state = ExecState.CornerIdle;
		float min = _phase == BossPhase.Phase2 ? IdleMinP2 : IdleMinP1;
		float max = _phase == BossPhase.Phase2 ? IdleMaxP2 : IdleMaxP1;
		_stateTimer = (float)(GD.Randf() * (max - min) + min);
	}

	private void EnterArcFlight()
	{
		_state = ExecState.ArcFlight;
		_arcT = 0f;
		_arcSwung = false;
		_arcAttackTimer = 0f;
		_arcStartX = _atLeftCorner ? _arenaLeftX : _arenaRightX;
		_arcEndX = _atLeftCorner ? _arenaRightX : _arenaLeftX;
	}

	private void EnterDash()
	{
		_state = ExecState.Dash;
		_dashStart = GlobalPosition;

		// Dive toward player position at ground level with random variance
		float playerX = _target != null && IsInstanceValid(_target)
			? _target.GlobalPosition.X
			: (_arenaLeftX + _arenaRightX) / 2f;

		float offset = (float)((GD.Randf() - 0.3f) * 120f);
		float targetX = Mathf.Clamp(playerX + offset, _arenaLeftX + 20, _arenaRightX - 20);
		var targetPos = new Vector2(targetX, _groundY);

		_dashDir = (targetPos - GlobalPosition).Normalized();
		_dashDist = GlobalPosition.DistanceTo(targetPos);
		_facingDir = _dashDir.X > 0 ? 1 : -1;

		var audio = GetNodeOrNull<AudioManager>("/root/AudioManager");
		audio?.Play("charge_windup");
	}

	private void EnterSummon()
	{
		_state = ExecState.Summon;
		_sprite.Play("summon");

		var audio = GetNodeOrNull<AudioManager>("/root/AudioManager");
		audio?.Play("projectile_fire");
	}

	private void EnterReturning()
	{
		_state = ExecState.Returning;
		float midX = (_arenaLeftX + _arenaRightX) / 2f;
		_atLeftCorner = GlobalPosition.X < midX;
	}

	// ─── Helpers ────────────────────────────────────────────────

	private void DeactivateAttack()
	{
		if (_attackHitActive)
		{
			_attackHitbox.Deactivate();
			_attackHitActive = false;
		}
	}

	private void UpdateAttackHitboxPosition()
	{
		var hitboxShape = _attackHitbox.GetNode<CollisionShape2D>("CollisionShape2D");
		hitboxShape.Position = new Vector2(_facingDir * 40, -32);
	}

	private void SpawnSkeletons()
	{
		int count = _phase == BossPhase.Phase2 ? SummonCountP2 : SummonCountP1;
		var effects = GetNodeOrNull<EffectsManager>("/root/EffectsManager");

		for (int i = 0; i < count; i++)
		{
			var skeleton = _skeletonScene.Instantiate<Node2D>();
			float xOffset = (float)(GD.Randf() * 160f - 80f);
			var spawnPos = new Vector2(GlobalPosition.X + xOffset, _groundY);
			skeleton.GlobalPosition = spawnPos + new Vector2(0, 32);
			GetTree().CurrentScene.AddChild(skeleton);
			_summonedSkeletons.Add(skeleton);

			var tween = skeleton.CreateTween();
			tween.TweenProperty(skeleton, "global_position:y", spawnPos.Y, 0.5f)
				.SetEase(Tween.EaseType.Out)
				.SetTrans(Tween.TransitionType.Back);
			tween.TweenCallback(Callable.From(() =>
			{
				effects?.SpawnParticles(skeleton.GlobalPosition, ParticleType.DustPuff);
			}));
		}
	}

	private void DespawnSkeletons()
	{
		foreach (var skel in _summonedSkeletons)
		{
			if (IsInstanceValid(skel))
				skel.QueueFree();
		}
		_summonedSkeletons.Clear();
	}

	private void FacePlayer()
	{
		if (_target != null && IsInstanceValid(_target))
			_facingDir = _target.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
	}

	private void PlayAnim(string name)
	{
		if (_sprite.Animation != name)
			_sprite.Play(name);
	}

	private void OnDamaged(int amount)
	{
		var baseColor = _phase == BossPhase.Phase2 ? new Color(1.3f, 0.5f, 0.5f) : new Color(1, 1, 1, 1);
		var tween = CreateTween();
		tween.TweenProperty(_sprite, "modulate", new Color(10, 10, 10, 1), 0.05f);
		tween.TweenProperty(_sprite, "modulate", baseColor, 0.1f);
	}

	private void OnDied()
	{
		_isDead = true;
		_contactHitbox.Deactivate();
		_attackHitbox.Deactivate();
		_hurtbox.SetDeferred("monitorable", false);
		CollisionLayer = 0;

		DespawnSkeletons();

		_sprite.Play("death");

		var effects = GetNodeOrNull<EffectsManager>("/root/EffectsManager");
		effects?.SpawnParticles(GlobalPosition, ParticleType.EnemyDeath);
		effects?.Shake(6f, 0.4f);
		var audio = GetNodeOrNull<AudioManager>("/root/AudioManager");
		audio?.Play("enemy_death");

		var tween = CreateTween();
		tween.TweenInterval(2.0f);
		tween.TweenProperty(_sprite, "modulate:a", 0f, 0.5f);
		tween.TweenCallback(Callable.From(QueueFree));
	}

	// ─── Sprite Sheet Loading (grid-based, 100x100 frames) ────────

	private void SetupAnimations()
	{
		_sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		_sprite.Scale = new Vector2(2, 2);
		_sprite.Offset = new Vector2(0, -16);

		var idleFrames = LoadSpriteGrid(SpritePath + "idle.png", 5);
		var attackFrames = LoadSpriteGrid(SpritePath + "attacking.png", 13);
		var skillFrames = LoadSpriteGrid(SpritePath + "skill1.png", 12);
		var summonFrames = LoadSpriteGrid(SpritePath + "summon.png", 5);
		var deathFrames = LoadSpriteGrid(SpritePath + "death.png", 20);

		var frames = new SpriteFrames();

		AddAnim(frames, "idle", idleFrames, 6, true);
		AddAnim(frames, "attack", attackFrames, 18, false);
		AddAnim(frames, "skill", skillFrames, 10, false);
		AddAnim(frames, "summon", summonFrames, 8, false);
		AddAnim(frames, "death", deathFrames, 10, false);

		if (frames.HasAnimation("default"))
			frames.RemoveAnimation("default");

		_sprite.SpriteFrames = frames;
		_sprite.Play("idle");
	}

	private static ImageTexture[] LoadSpriteGrid(string path, int frameCount)
	{
		var img = Image.LoadFromFile(ProjectSettings.GlobalizePath(path));
		if (img == null)
			return new[] { ImageTexture.CreateFromImage(Image.CreateEmpty(FrameSize, FrameSize, false, Image.Format.Rgba8)) };

		int cols = img.GetWidth() / FrameSize;
		var textures = new ImageTexture[frameCount];

		for (int i = 0; i < frameCount; i++)
		{
			int col = i % cols;
			int row = i / cols;
			var frame = Image.CreateEmpty(FrameSize, FrameSize, false, Image.Format.Rgba8);
			frame.BlitRect(img, new Rect2I(col * FrameSize, row * FrameSize, FrameSize, FrameSize), Vector2I.Zero);
			textures[i] = ImageTexture.CreateFromImage(frame);
		}
		return textures;
	}

	private static void AddAnim(SpriteFrames frames, string name, ImageTexture[] textures, double fps, bool loop)
	{
		frames.AddAnimation(name);
		frames.SetAnimationSpeed(name, fps);
		frames.SetAnimationLoop(name, loop);
		foreach (var tex in textures)
			frames.AddFrame(name, tex);
	}
}
