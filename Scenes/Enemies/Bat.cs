using Godot;

namespace metvania_1;

public partial class Bat : CharacterBody2D
{
	private enum BatState { Idle, WakingUp, Flying, Attacking, Hurt, Dying }

	// Movement (unchanged)
	private const float HorizontalSpeed = 80f;
	private const float SineAmplitude = 20f;
	private const float SineFrequency = 2.5f;
	private const float DespawnDistance = 500f;
	private const float DescentSpeed = 50f;

	// Ranges
	private const float AggroRange = 80f;
	private const float AttackRange = 20f;

	// Sprite
	private const int FrameSize = 64;
	private const string SpritePath = "res://Assets/Sprites/Bat/Bat without VFX/";

	[Export] public int ContactDamage { get; set; } = 1;

	private BatState _state = BatState.Idle;
	private Vector2 _startPosition;
	private float _baseY;
	private int _direction = -1;
	private float _timer;
	private Player _target;
	private bool _isDead;

	private AnimatedSprite2D _sprite;
	private HealthComponent _health;
	private Hitbox _contactHitbox;
	private Hurtbox _hurtbox;

	public override void _Ready()
	{
		AddToGroup("enemies");

		_sprite = GetNode<AnimatedSprite2D>("Sprite");
		_health = GetNode<HealthComponent>("HealthComponent");
		_contactHitbox = GetNode<Hitbox>("ContactHitbox");
		_hurtbox = GetNode<Hurtbox>("Hurtbox");

		_hurtbox.Health = _health;
		_contactHitbox.Damage = ContactDamage;

		_health.Damaged += OnDamaged;
		_health.Died += OnDied;

		_contactHitbox.Activate();

		_startPosition = GlobalPosition;
		_baseY = GlobalPosition.Y;

		SetupAnimations();
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

		float dist = GetDistanceToPlayer();

		switch (_state)
		{
			case BatState.Idle:
				PlayAnim("idle_fly");
				if (dist <= AggroRange)
				{
					_state = BatState.WakingUp;
					_sprite.Play("wakeup");
					FacePlayer();
				}
				break;

			case BatState.WakingUp:
				if (!_sprite.IsPlaying() || _sprite.Animation != "wakeup")
				{
					_state = BatState.Flying;
					_baseY = GlobalPosition.Y;
					_timer = 0;
				}
				break;

			case BatState.Flying:
				PlayAnim("fly");
				UpdateFlight(dt);

				if (dist <= AttackRange)
				{
					_state = BatState.Attacking;
					_sprite.Play("attack1");
				}

				if (GlobalPosition.DistanceTo(_startPosition) > DespawnDistance)
					QueueFree();
				break;

			case BatState.Attacking:
				UpdateFlight(dt);
				if (!_sprite.IsPlaying() || _sprite.Animation != "attack1")
					_state = BatState.Flying;
				break;

			case BatState.Hurt:
				UpdateFlight(dt);
				if (!_sprite.IsPlaying() || _sprite.Animation != "hurt")
					_state = BatState.Flying;
				break;
		}

		_sprite.FlipH = _direction == -1;
	}

	private void UpdateFlight(float dt)
	{
		_timer += dt;

		// Track player
		FacePlayer();

		// Descend toward player's Y level
		if (_target != null && IsInstanceValid(_target))
			_baseY = Mathf.MoveToward(_baseY, _target.GlobalPosition.Y, DescentSpeed * dt);

		// Horizontal movement + sine wave vertical
		float sineY = Mathf.Sin(_timer * SineFrequency * Mathf.Tau) * SineAmplitude;
		GlobalPosition += new Vector2(HorizontalSpeed * _direction * dt, 0);
		GlobalPosition = new Vector2(GlobalPosition.X, _baseY + sineY);
	}

	private float GetDistanceToPlayer()
	{
		if (_target == null || !IsInstanceValid(_target))
			return float.MaxValue;
		return GlobalPosition.DistanceTo(_target.GlobalPosition);
	}

	private void FacePlayer()
	{
		if (_target != null && IsInstanceValid(_target))
			_direction = _target.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
	}

	private void PlayAnim(string name)
	{
		if (_sprite.Animation != name)
			_sprite.Play(name);
	}

	private void OnDamaged(int amount)
	{
		if (_state == BatState.Dying) return;

		// Hit while idle → skip to flying
		if (_state == BatState.Idle)
		{
			_baseY = GlobalPosition.Y;
			_timer = 0;
			FacePlayer();
		}

		_state = BatState.Hurt;
		_sprite.Play("hurt");

		var tween = CreateTween();
		tween.TweenProperty(_sprite, "modulate", new Color(10, 10, 10, 1), 0.05f);
		tween.TweenProperty(_sprite, "modulate", new Color(1, 1, 1, 1), 0.1f);
	}

	private void OnDied()
	{
		_isDead = true;
		_state = BatState.Dying;
		_contactHitbox.Deactivate();
		_hurtbox.SetDeferred("monitorable", false);
		CollisionLayer = 0;
		CollisionMask = 0;

		_sprite.Play("die");

		var effects = GetNodeOrNull<EffectsManager>("/root/EffectsManager");
		effects?.SpawnParticles(GlobalPosition, ParticleType.EnemyDeath);
		effects?.Shake(3f, 0.15f);
		var audio = GetNodeOrNull<AudioManager>("/root/AudioManager");
		audio?.Play("enemy_death");

		var tween = CreateTween();
		tween.TweenInterval(1.2f);
		tween.TweenProperty(_sprite, "modulate:a", 0f, 0.3f);
		tween.TweenCallback(Callable.From(QueueFree));
	}

	// ─── Sprite Sheet Loading ──────────────────────────────────────

	private void SetupAnimations()
	{
		_sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		_sprite.Scale = new Vector2(1f, 1f);

		var idleFlyFrames = LoadSpriteSheet(SpritePath + "Bat-IdleFly.png", 9);
		var wakeupFrames = LoadSpriteSheet(SpritePath + "Bat-WakeUp.png", 16);
		var flyFrames = LoadSpriteSheet(SpritePath + "Bat-Run.png", 8);
		var attack1Frames = LoadSpriteSheet(SpritePath + "Bat-Attack1.png", 8);
		var hurtFrames = LoadSpriteSheet(SpritePath + "Bat-Hurt.png", 5);
		var dieFrames = LoadSpriteSheet(SpritePath + "Bat-Die.png", 12);

		var frames = new SpriteFrames();

		AddAnim(frames, "idle_fly", idleFlyFrames, 8, true);
		AddAnim(frames, "wakeup", wakeupFrames, 12, false);
		AddAnim(frames, "fly", flyFrames, 12, true);
		AddAnim(frames, "attack1", attack1Frames, 15, false);
		AddAnim(frames, "hurt", hurtFrames, 10, false);
		AddAnim(frames, "die", dieFrames, 10, false);

		if (frames.HasAnimation("default"))
			frames.RemoveAnimation("default");

		_sprite.SpriteFrames = frames;
		_sprite.Play("idle_fly");
	}

	private static ImageTexture[] LoadSpriteSheet(string path, int frameCount)
	{
		var img = Image.LoadFromFile(ProjectSettings.GlobalizePath(path));
		if (img == null)
			return new[] { ImageTexture.CreateFromImage(Image.CreateEmpty(FrameSize, FrameSize, false, Image.Format.Rgba8)) };

		int h = img.GetHeight();
		var textures = new ImageTexture[frameCount];
		for (int i = 0; i < frameCount; i++)
		{
			var frame = Image.CreateEmpty(FrameSize, h, false, Image.Format.Rgba8);
			frame.BlitRect(img, new Rect2I(i * FrameSize, 0, FrameSize, h), Vector2I.Zero);
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
