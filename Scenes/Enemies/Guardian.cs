using Godot;

namespace metvania_1;

public partial class Guardian : EnemyBase
{
	private enum BossPhase { Phase1, Phase2 }
	private enum BossState { Idle, WalkToward, WindUp, Charge, Recover, JumpSlam, Landing }

	// Movement
	private const float Gravity = 800f;
	private const float WalkSpeed = 20f;
	private const float ChargeSpeedP1 = 300f;
	private const float ChargeSpeedP2 = 450f;
	private const float JumpSlamImpulse = -500f;

	// Timers
	private const float IdleDuration = 1f;
	private const float WalkDuration = 1.5f;
	private const float WindUpDuration = 0.5f;
	private const float ChargeDuration = 0.8f;
	private const float RecoverDuration = 0.8f;

	// Shockwave
	private const float ShockwaveDistance = 300f;
	private const float ShockwaveSpeed = 200f;

	private BossPhase _phase = BossPhase.Phase1;
	private BossState _bossState = BossState.Idle;
	private float _stateTimer;
	private int _facingDir = -1;
	private Player _target;
	private PackedScene _projectileScene;
	private Color _baseColor = new Color(1f, 1f, 1f, 1f);

	[Signal]
	public delegate void BossHealthChangedEventHandler(int current, int max);

	[Signal]
	public delegate void BossDefeatedEventHandler();

	protected override void EnemyInit()
	{
		Sprite.Texture = SpriteFactory.GuardianSprite();
		ContactHitbox.Damage = 2;
		_projectileScene = GD.Load<PackedScene>("res://Scenes/Enemies/Projectile.tscn");

		Health.HealthChanged += (current, max) => EmitSignal(SignalName.BossHealthChanged, current, max);
		Health.Died += () => EmitSignal(SignalName.BossDefeated);

		_stateTimer = IdleDuration;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsDead) return;

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
		if (Health.CurrentHealth <= 6 && _phase == BossPhase.Phase1)
		{
			_phase = BossPhase.Phase2;
			Sprite.Modulate = new Color(1.2f, 0.6f, 0.6f); // Phase 2 tint
			_baseColor = new Color(1.2f, 0.6f, 0.6f);
		}

		_stateTimer -= dt;

		switch (_bossState)
		{
			case BossState.Idle:
				velocity.X = 0;
				Sprite.Modulate = _baseColor;
				FacePlayer();

				if (_stateTimer <= 0)
				{
					_bossState = BossState.WalkToward;
					_stateTimer = WalkDuration;
				}
				break;

			case BossState.WalkToward:
				FacePlayer();
				velocity.X = WalkSpeed * _facingDir;
				Sprite.Modulate = _baseColor;

				if (_stateTimer <= 0)
				{
					_bossState = BossState.WindUp;
					_stateTimer = WindUpDuration;
					velocity.X = 0;

					var audio = GetNodeOrNull<AudioManager>("/root/AudioManager");
					audio?.Play("charge_windup");
				}
				break;

			case BossState.WindUp:
				velocity.X = 0;
				// Flash
				Sprite.Modulate = ((int)(_stateTimer / 0.05f)) % 2 == 0
					? new Color(1f, 0.3f, 0.3f)
					: _baseColor;

				if (_stateTimer <= 0)
				{
					_bossState = BossState.Charge;
					_stateTimer = ChargeDuration;
				}
				break;

			case BossState.Charge:
			{
				float chargeSpeed = _phase == BossPhase.Phase2 ? ChargeSpeedP2 : ChargeSpeedP1;
				velocity.X = chargeSpeed * _facingDir;
				Sprite.Modulate = new Color(1f, 0.3f, 0.3f);

				bool hitWall = IsOnWall();
				if (hitWall || _stateTimer <= 0)
				{
					_bossState = BossState.Recover;
					_stateTimer = RecoverDuration;
					velocity.X = 0;

					if (hitWall)
					{
						var effects = GetNodeOrNull<EffectsManager>("/root/EffectsManager");
						effects?.Shake(4f, 0.2f);
					}
				}
				break;
			}

			case BossState.Recover:
				velocity.X = 0;
				Sprite.Modulate = new Color(0.5f, 0.5f, 0.5f); // Grey = stunned

				if (_stateTimer <= 0)
				{
					FireProjectiles();

					if (_phase == BossPhase.Phase2 && GD.Randf() > 0.5f)
					{
						_bossState = BossState.JumpSlam;
						velocity.Y = JumpSlamImpulse;
					}
					else
					{
						_bossState = BossState.Idle;
						_stateTimer = IdleDuration;
					}
				}
				break;

			case BossState.JumpSlam:
				// Arc toward player X position
				FacePlayer();
				if (_target != null && IsInstanceValid(_target))
				{
					float targetX = _target.GlobalPosition.X;
					float moveDir = targetX > GlobalPosition.X ? 1f : -1f;
					velocity.X = moveDir * 100f;
				}

				if (IsOnFloor() && velocity.Y >= 0)
				{
					_bossState = BossState.Landing;
					_stateTimer = 0.3f;
					velocity.X = 0;
					OnSlamLanded();
				}
				break;

			case BossState.Landing:
				velocity.X = 0;

				if (_stateTimer <= 0)
				{
					_bossState = BossState.Idle;
					_stateTimer = IdleDuration;
				}
				break;
		}

		// Flip sprite
		Sprite.FlipH = (_facingDir == -1);

		Velocity = velocity;
		MoveAndSlide();
	}

	private void FacePlayer()
	{
		if (_target != null && IsInstanceValid(_target))
		{
			_facingDir = _target.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
		}
	}

	private void FireProjectiles()
	{
		var audio = GetNodeOrNull<AudioManager>("/root/AudioManager");
		audio?.Play("projectile_fire");

		if (_target == null || !IsInstanceValid(_target)) return;

		var baseDir = (_target.GlobalPosition - GlobalPosition).Normalized();

		var proj1 = _projectileScene.Instantiate<Projectile>();
		proj1.Init(baseDir);
		proj1.GlobalPosition = GlobalPosition + new Vector2(0, -16);
		GetTree().CurrentScene.AddChild(proj1);

		if (_phase == BossPhase.Phase2)
		{
			// Second projectile with angle spread
			float angle = 0.3f; // ~17 degrees
			var rotated = baseDir.Rotated(baseDir.X > 0 ? angle : -angle);
			var proj2 = _projectileScene.Instantiate<Projectile>();
			proj2.Init(rotated);
			proj2.GlobalPosition = GlobalPosition + new Vector2(0, -16);
			GetTree().CurrentScene.AddChild(proj2);
		}
	}

	private void OnSlamLanded()
	{
		var effects = GetNodeOrNull<EffectsManager>("/root/EffectsManager");
		effects?.SpawnParticles(GlobalPosition, ParticleType.BossSlam);
		effects?.Shake(6f, 0.3f);

		var audio = GetNodeOrNull<AudioManager>("/root/AudioManager");
		audio?.Play("boss_slam");

		// Spawn shockwaves left and right
		SpawnShockwave(-1);
		SpawnShockwave(1);
	}

	private void SpawnShockwave(int direction)
	{
		var wave = new Area2D();
		wave.CollisionLayer = 0;
		wave.CollisionMask = 0;
		wave.GlobalPosition = GlobalPosition;

		// Child hitbox for damage
		var hitbox = new Hitbox();
		hitbox.Damage = 1;
		hitbox.CollisionLayer = 16;
		hitbox.CollisionMask = 32;
		hitbox.Monitoring = true;

		var shape = new CollisionShape2D();
		var rect = new RectangleShape2D();
		rect.Size = new Vector2(16, 12);
		shape.Shape = rect;
		shape.Position = new Vector2(0, -6);
		hitbox.AddChild(shape);
		wave.AddChild(hitbox);

		GetTree().CurrentScene.AddChild(wave);
		hitbox.Activate();

		// Tween outward
		float duration = ShockwaveDistance / ShockwaveSpeed;
		var tween = wave.CreateTween();
		tween.TweenProperty(wave, "position:x",
			wave.GlobalPosition.X + direction * ShockwaveDistance, duration);
		tween.Parallel().TweenProperty(wave, "modulate:a", 0f, duration);
		tween.TweenCallback(Callable.From(wave.QueueFree));
	}
}
