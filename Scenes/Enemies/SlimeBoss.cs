using Godot;

namespace metvania_1;

public partial class SlimeBoss : CharacterBody2D
{
	private enum BossPhase { Phase1, Phase2 }
	private enum SlimeState { Idle, Patrol, Charge, Jump, Bite, Landing }

	// Movement
	private const float Gravity = 800f;
	private const float PatrolSpeed = 30f;
	private const float ChargeSpeedP1 = 250f;
	private const float ChargeSpeedP2 = 350f;
	private const float JumpImpulseY = -450f;
	private const float MaxJumpSpeedXP1 = 200f;
	private const float MaxJumpSpeedXP2 = 280f;

	// Ranges
	private const float AggroRange = 200f;
	private const float BiteRange = 60f;

	// Timers
	private const float IdleDuration = 0.8f;
	private const float PatrolDuration = 2.0f;
	private const float ChargeDuration = 0.6f;
	private const float BiteDuration = 0.5f;
	private const float LandingDuration = 0.4f;

	// Phase 2 jump probability: 60% vs Phase 1: 40%
	private const float JumpChanceP1 = 0.4f;
	private const float JumpChanceP2 = 0.65f;

	// Sprite
	private const int SpriteFrameSize = 96;
	private const string SpritePath = "res://Assets/Sprites/Monster_Slime/No_Shadows/";

	[Export] public int ContactDamage { get; set; } = 2;

	private BossPhase _phase = BossPhase.Phase1;
	private SlimeState _state = SlimeState.Idle;
	private float _stateTimer;
	private int _facingDir = -1;
	private int _patrolDir = 1;
	private Player _target;
	private bool _isDead;

	private AnimatedSprite2D _sprite;
	private HealthComponent _health;
	private Hitbox _contactHitbox;
	private Hurtbox _hurtbox;

	[Signal]
	public delegate void BossHealthChangedEventHandler(int current, int max);

	[Signal]
	public delegate void BossDefeatedEventHandler();

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
		_health.HealthChanged += (current, max) => EmitSignal(SignalName.BossHealthChanged, current, max);
		_health.Died += () => EmitSignal(SignalName.BossDefeated);

		_contactHitbox.Activate();

		SetupAnimations();
		_stateTimer = IdleDuration;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead) return;

		float dt = (float)delta;
		var velocity = Velocity;

		// Gravity
		if (!IsOnFloor())
			velocity.Y += Gravity * dt;
		else
			velocity.Y = 0;

		// Find player
		if (_target == null || !IsInstanceValid(_target))
		{
			var players = GetTree().GetNodesInGroup("player");
			if (players.Count > 0)
				_target = players[0] as Player;
		}

		// Phase check
		if (_health.CurrentHealth <= 6 && _phase == BossPhase.Phase1)
		{
			_phase = BossPhase.Phase2;
			_sprite.Modulate = new Color(1.2f, 0.4f, 0.4f);
		}

		_stateTimer -= dt;
		float distToPlayer = GetDistanceToPlayer();

		switch (_state)
		{
			case SlimeState.Idle:
				velocity.X = 0;
				PlayAnim("idle");
				FacePlayer();

				if (_stateTimer <= 0)
				{
					if (distToPlayer <= BiteRange)
					{
						EnterBite();
					}
					else if (distToPlayer <= AggroRange)
					{
						ChooseAttack();
					}
					else
					{
						_state = SlimeState.Patrol;
						_stateTimer = PatrolDuration;
					}
				}
				break;

			case SlimeState.Patrol:
				PlayAnim("walk");
				velocity.X = PatrolSpeed * _patrolDir;
				_sprite.FlipH = _patrolDir < 0;

				// Reverse on wall
				if (IsOnWall())
					_patrolDir = -_patrolDir;

				// Switch to attack if player in range
				if (distToPlayer <= AggroRange)
				{
					FacePlayer();
					ChooseAttack();
				}
				else if (_stateTimer <= 0)
				{
					_patrolDir = -_patrolDir;
					_stateTimer = PatrolDuration;
				}
				break;

			case SlimeState.Charge:
			{
				PlayAnim("walk");
				float chargeSpeed = _phase == BossPhase.Phase2 ? ChargeSpeedP2 : ChargeSpeedP1;
				velocity.X = chargeSpeed * _facingDir;

				if (IsOnWall() || _stateTimer <= 0)
				{
					velocity.X = 0;
					_state = SlimeState.Idle;
					_stateTimer = IdleDuration;
				}
				break;
			}

			case SlimeState.Jump:
			{
				PlayAnim("jump");
				// Arc velocity was set at launch — just fly

				if (IsOnFloor() && velocity.Y >= 0)
				{
					velocity.X = 0;
					_state = SlimeState.Landing;
					_stateTimer = LandingDuration;
					OnLanded();
				}
				break;
			}

			case SlimeState.Bite:
				velocity.X = 0;
				PlayAnim("attack");

				if (_stateTimer <= 0)
				{
					_state = SlimeState.Idle;
					_stateTimer = IdleDuration;
				}
				break;

			case SlimeState.Landing:
				velocity.X = 0;
				PlayAnim("idle");

				if (_stateTimer <= 0)
				{
					_state = SlimeState.Idle;
					_stateTimer = IdleDuration;
				}
				break;
		}

		_sprite.FlipH = _facingDir < 0;
		Velocity = velocity;
		MoveAndSlide();
	}

	private void ChooseAttack()
	{
		float jumpChance = _phase == BossPhase.Phase2 ? JumpChanceP2 : JumpChanceP1;
		if (GD.Randf() < jumpChance)
		{
			_state = SlimeState.Jump;
			// Calculate ballistic arc to land near the player
			float maxSpeedX = _phase == BossPhase.Phase2 ? MaxJumpSpeedXP2 : MaxJumpSpeedXP1;
			float vx = 0f;
			if (_target != null && IsInstanceValid(_target))
			{
				float dx = _target.GlobalPosition.X - GlobalPosition.X;
				// Total flight time: up + down = 2 * |jumpImpulse| / gravity
				float flightTime = 2f * Mathf.Abs(JumpImpulseY) / Gravity;
				vx = dx / flightTime;
				vx = Mathf.Clamp(vx, -maxSpeedX, maxSpeedX);
			}
			Velocity = new Vector2(vx, JumpImpulseY);
			_sprite.FlipH = vx < 0;
		}
		else
		{
			_state = SlimeState.Charge;
			_stateTimer = ChargeDuration;
		}
	}

	private void EnterBite()
	{
		_state = SlimeState.Bite;
		_stateTimer = BiteDuration;

		var audio = GetNodeOrNull<AudioManager>("/root/AudioManager");
		audio?.Play("hit");
	}

	private void OnLanded()
	{
		var effects = GetNodeOrNull<EffectsManager>("/root/EffectsManager");
		effects?.Shake(4f, 0.2f);

		var audio = GetNodeOrNull<AudioManager>("/root/AudioManager");
		audio?.Play("boss_slam");
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
			_facingDir = _target.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
	}

	private void PlayAnim(string name)
	{
		if (_sprite.Animation != name)
			_sprite.Play(name);
	}

	private void OnDamaged(int amount)
	{
		var tween = CreateTween();
		tween.TweenProperty(_sprite, "modulate", new Color(10, 10, 10, 1), 0.05f);
		tween.TweenProperty(_sprite, "modulate",
			_phase == BossPhase.Phase2 ? new Color(1.2f, 0.4f, 0.4f) : new Color(1, 1, 1, 1), 0.1f);
	}

	private void OnDied()
	{
		_isDead = true;
		_contactHitbox.Deactivate();
		_hurtbox.SetDeferred("monitorable", false);
		CollisionLayer = 0;
		CollisionMask = 0;

		_sprite.Play("death");

		var effects = GetNodeOrNull<EffectsManager>("/root/EffectsManager");
		effects?.SpawnParticles(GlobalPosition, ParticleType.EnemyDeath);
		effects?.Shake(6f, 0.3f);
		var audio = GetNodeOrNull<AudioManager>("/root/AudioManager");
		audio?.Play("enemy_death");

		var tween = CreateTween();
		tween.TweenInterval(1.0f);
		tween.TweenProperty(_sprite, "modulate:a", 0f, 0.3f);
		tween.TweenCallback(Callable.From(QueueFree));
	}

	// ─── Sprite Sheet Loading ──────────────────────────────────────

	private void SetupAnimations()
	{
		_sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		_sprite.Scale = new Vector2(6, 6);
		_sprite.Offset = new Vector2(0, -2);

		var idleFrames = LoadSpriteSheet(SpritePath + "Monster_Slime_Idle-Sheet.png");
		var walkFrames = LoadSpriteSheet(SpritePath + "Monster_Slime_Walk-Sheet.png");
		var attackFrames = LoadSpriteSheet(SpritePath + "Monster_Slime_Attack1-Sheet.png");
		var jumpFallFrames = LoadSpriteSheet(SpritePath + "Monster_Slime_Jump_Fall-Sheet.png");
		var hurtFrames = LoadSpriteSheet(SpritePath + "Monster_Slime_Hurt-Sheet.png");
		var deathFrames = LoadSpriteSheet(SpritePath + "Monster_Slime_Death-Sheet.png");

		var jumpFrames = new[] { jumpFallFrames[0], jumpFallFrames[1], jumpFallFrames[2] };

		var frames = new SpriteFrames();

		AddAnim(frames, "idle", idleFrames, 6, true);
		AddAnim(frames, "walk", walkFrames, 10, true);
		AddAnim(frames, "attack", attackFrames, 15, false);
		AddAnim(frames, "jump", jumpFrames, 8, false);
		AddAnim(frames, "hurt", hurtFrames, 8, false);
		AddAnim(frames, "death", deathFrames, 8, false);

		if (frames.HasAnimation("default"))
			frames.RemoveAnimation("default");

		_sprite.SpriteFrames = frames;
		_sprite.Play("idle");
	}

	private static ImageTexture[] LoadSpriteSheet(string path)
	{
		var img = Image.LoadFromFile(ProjectSettings.GlobalizePath(path));
		if (img == null)
			return new[] { ImageTexture.CreateFromImage(Image.CreateEmpty(SpriteFrameSize, SpriteFrameSize, false, Image.Format.Rgba8)) };
		int count = img.GetWidth() / SpriteFrameSize;
		int h = img.GetHeight();
		var textures = new ImageTexture[count];
		for (int i = 0; i < count; i++)
		{
			var frame = Image.CreateEmpty(SpriteFrameSize, h, false, Image.Format.Rgba8);
			frame.BlitRect(img, new Rect2I(i * SpriteFrameSize, 0, SpriteFrameSize, h), Vector2I.Zero);
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
