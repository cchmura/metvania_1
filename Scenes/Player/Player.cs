using Godot;

namespace metvania_1;

public partial class Player : CharacterBody2D
{
	private enum PlayerState
	{
		Normal,
		Attacking,
		Damaged,
		Dead,
	}

	private enum AttackDirection
	{
		Forward,
		Up,
		Down,
	}

	// Movement constants
	private const float Speed = 200f;
	private const float Acceleration = 1200f;
	private const float Friction = 1000f;
	private const float Gravity = 800f;
	private const float JumpImpulse = -400f;
	private const float JumpReleaseMultiplier = 0.5f;
	private const float CoyoteTime = 0.1f;
	private const float JumpBufferTime = 0.1f;

	// Combat constants
	private const float AttackActiveDuration = 0.25f;
	private const float AttackCooldown = 0.35f;
	private const float PogoImpulse = -350f;
	private const float KnockbackSpeed = 150f;
	private const float KnockbackDuration = 0.2f;
	private const float InvincibilityFlashRate = 0.08f;

	// State
	private PlayerState _state = PlayerState.Normal;
	private float _coyoteTimer;
	private float _jumpBufferTimer;
	private bool _hasDoubleJump;
	private bool _usedDoubleJump;
	private int _facingDirection = 1;

	// Combat state
	private float _attackTimer;
	private float _attackCooldownTimer;
	private AttackDirection _attackDir;
	private float _knockbackTimer;
	private Vector2 _knockbackVelocity;
	private float _flashTimer;
	private bool _pogoThisAttack;

	// Landing detection
	private bool _wasAirborne;

	// Node references
	private GameState _gameState;
	private EffectsManager _effects;
	private AudioManager _audio;
	private ColorRect _sprite;
	private HealthComponent _healthComponent;
	private Hitbox _attackHitbox;
	private Hurtbox _hurtbox;
	private CollisionShape2D _attackShape;
	private ColorRect _attackVisual;

	public override void _Ready()
	{
		_gameState = GetNode<GameState>("/root/GameState");
		_effects = GetNodeOrNull<EffectsManager>("/root/EffectsManager");
		_audio = GetNodeOrNull<AudioManager>("/root/AudioManager");
		_sprite = GetNode<ColorRect>("Sprite");
		_healthComponent = GetNode<HealthComponent>("HealthComponent");
		_attackHitbox = GetNode<Hitbox>("AttackHitbox");
		_hurtbox = GetNode<Hurtbox>("Hurtbox");
		_attackShape = _attackHitbox.GetNode<CollisionShape2D>("CollisionShape2D");
		_attackVisual = GetNode<ColorRect>("AttackVisual");

		AddToGroup("player");

		// Wire hurtbox to health component
		_hurtbox.Health = _healthComponent;

		// Connect signals
		_healthComponent.Damaged += OnDamaged;
		_healthComponent.Died += OnDied;
		_hurtbox.Hurt += OnHurt;
		_attackHitbox.HitLanded += OnAttackHit;

		// Start with attack hidden
		_attackVisual.Visible = false;
		_attackHitbox.Deactivate();
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		switch (_state)
		{
			case PlayerState.Normal:
			case PlayerState.Attacking:
				ProcessMovement(dt);
				ProcessAttack(dt);
				break;
			case PlayerState.Damaged:
				ProcessKnockback(dt);
				break;
			case PlayerState.Dead:
				// Fall with gravity, no input
				var deadVel = Velocity;
				deadVel.Y += Gravity * dt;
				Velocity = deadVel;
				MoveAndSlide();
				return;
		}

		// Invincibility flash
		if (_healthComponent.IsInvincible && _state != PlayerState.Dead)
		{
			_flashTimer += dt;
			_sprite.Visible = ((int)(_flashTimer / InvincibilityFlashRate)) % 2 == 0;
		}
		else
		{
			_sprite.Visible = true;
			_flashTimer = 0;
		}
	}

	private void ProcessMovement(float dt)
	{
		var velocity = Velocity;

		// Landing detection
		bool onFloor = IsOnFloor();
		if (_wasAirborne && onFloor)
		{
			_audio?.Play("land");
			_effects?.SpawnParticles(GlobalPosition, ParticleType.DustPuff);
		}
		_wasAirborne = !onFloor;

		// Gravity
		if (!onFloor)
		{
			velocity.Y += Gravity * dt;
			_coyoteTimer -= dt;
		}
		else
		{
			_coyoteTimer = CoyoteTime;
			_usedDoubleJump = false;
		}

		// Jump buffer
		_jumpBufferTimer -= dt;
		if (Input.IsActionJustPressed("jump"))
		{
			_jumpBufferTimer = JumpBufferTime;
		}

		// Check double jump availability
		_hasDoubleJump = _gameState.HasAbility("DoubleJump");

		// Jump logic
		if (_jumpBufferTimer > 0)
		{
			if (_coyoteTimer > 0)
			{
				velocity.Y = JumpImpulse;
				_jumpBufferTimer = 0;
				_coyoteTimer = 0;
				_audio?.Play("jump");
				_effects?.SpawnParticles(GlobalPosition, ParticleType.DustPuff);
			}
			else if (_hasDoubleJump && !_usedDoubleJump)
			{
				velocity.Y = JumpImpulse;
				_usedDoubleJump = true;
				_jumpBufferTimer = 0;
				_audio?.Play("jump");
				_effects?.SpawnParticles(GlobalPosition, ParticleType.DustPuff);
			}
		}

		// Variable jump height
		if (Input.IsActionJustReleased("jump") && velocity.Y < 0)
		{
			velocity.Y *= JumpReleaseMultiplier;
		}

		// Horizontal movement
		float inputDir = Input.GetAxis("move_left", "move_right");
		if (Mathf.Abs(inputDir) > 0.01f)
		{
			velocity.X = Mathf.MoveToward(velocity.X, inputDir * Speed, Acceleration * dt);
			if (_state != PlayerState.Attacking)
			{
				_facingDirection = inputDir > 0 ? 1 : -1;
			}
		}
		else
		{
			velocity.X = Mathf.MoveToward(velocity.X, 0, Friction * dt);
		}

		// Facing direction (flip sprite)
		_sprite.Scale = new Vector2(_facingDirection, 1);
		if (_facingDirection == -1)
			_sprite.Position = new Vector2(7, -24);
		else
			_sprite.Position = new Vector2(-7, -24);

		Velocity = velocity;
		MoveAndSlide();
	}

	private void ProcessAttack(float dt)
	{
		_attackCooldownTimer -= dt;

		if (_state == PlayerState.Attacking)
		{
			_attackTimer -= dt;
			if (_attackTimer <= 0)
			{
				EndAttack();
			}
			return;
		}

		// Start attack
		if (Input.IsActionJustPressed("attack") && _attackCooldownTimer <= 0)
		{
			StartAttack();
		}
	}

	private void StartAttack()
	{
		_state = PlayerState.Attacking;
		_attackTimer = AttackActiveDuration;
		_attackCooldownTimer = AttackCooldown;
		_pogoThisAttack = false;
		_audio?.Play("slash");

		// Determine direction
		if (Input.IsActionPressed("move_up"))
		{
			_attackDir = AttackDirection.Up;
		}
		else if (Input.IsActionPressed("move_down") && !IsOnFloor())
		{
			_attackDir = AttackDirection.Down;
		}
		else
		{
			_attackDir = AttackDirection.Forward;
		}

		// Position hitbox and visual
		PositionAttack();
		_attackHitbox.Activate();
		_attackVisual.Visible = true;
	}

	private void PositionAttack()
	{
		var shape = _attackShape.Shape as RectangleShape2D;
		switch (_attackDir)
		{
			case AttackDirection.Forward:
				shape.Size = new Vector2(26, 16);
				_attackShape.Position = new Vector2(20 * _facingDirection, -12);
				_attackVisual.Size = new Vector2(26, 16);
				_attackVisual.Position = new Vector2(20 * _facingDirection - 13, -20);
				break;
			case AttackDirection.Up:
				shape.Size = new Vector2(16, 26);
				_attackShape.Position = new Vector2(0, -30);
				_attackVisual.Size = new Vector2(16, 26);
				_attackVisual.Position = new Vector2(-8, -43);
				break;
			case AttackDirection.Down:
				shape.Size = new Vector2(16, 26);
				_attackShape.Position = new Vector2(0, 8);
				_attackVisual.Size = new Vector2(16, 26);
				_attackVisual.Position = new Vector2(-8, -5);
				break;
		}
	}

	private void EndAttack()
	{
		_state = PlayerState.Normal;
		_attackHitbox.Deactivate();
		_attackVisual.Visible = false;
	}

	/// <summary>Called by Hitbox when it overlaps an enemy hurtbox — check for pogo.</summary>
	public void OnAttackHit(Node2D target)
	{
		if (_attackDir == AttackDirection.Down && !_pogoThisAttack)
		{
			_pogoThisAttack = true;
			var velocity = Velocity;
			velocity.Y = PogoImpulse;
			Velocity = velocity;
			_usedDoubleJump = false; // Reset double jump on pogo

			_audio?.Play("pogo");
			_effects?.HitFreeze();
			_effects?.Shake(2f, 0.1f);
			_effects?.SpawnParticles(GlobalPosition + new Vector2(0, 12), ParticleType.PogoSpark);
		}
	}

	private void OnDamaged(int amount)
	{
		if (_state == PlayerState.Dead) return;
		_state = PlayerState.Damaged;
		_knockbackTimer = KnockbackDuration;

		// End any active attack
		if (_attackVisual.Visible)
		{
			EndAttack();
		}

		_audio?.Play("player_hurt");
		_effects?.HitFreeze(0.1f);
		_effects?.Shake(5f, 0.3f);
	}

	private void OnHurt(int damage, Hitbox source)
	{
		if (_healthComponent.IsInvincible || _state == PlayerState.Dead) return;

		// Determine knockback direction from source
		if (source != null)
		{
			var dir = (GlobalPosition - source.GlobalPosition).Normalized();
			_knockbackVelocity = new Vector2(dir.X * KnockbackSpeed, -150f);
		}
		else
		{
			_knockbackVelocity = new Vector2(-_facingDirection * KnockbackSpeed, -150f);
		}
	}

	private void ProcessKnockback(float dt)
	{
		_knockbackTimer -= dt;

		var velocity = _knockbackVelocity;
		velocity.Y += Gravity * dt;
		_knockbackVelocity = velocity;
		Velocity = velocity;
		MoveAndSlide();

		if (_knockbackTimer <= 0)
		{
			_state = PlayerState.Normal;
		}
	}

	private void OnDied()
	{
		_state = PlayerState.Dead;
		// End any active attack
		if (_attackVisual.Visible)
		{
			EndAttack();
		}
		// Disable hurtbox so we don't take more damage
		_hurtbox.SetDeferred("monitorable", false);

		_effects?.Shake(6f, 0.4f);
	}

	/// <summary>Called by World after death respawn.</summary>
	public void Respawn()
	{
		_state = PlayerState.Normal;
		Velocity = Vector2.Zero;
		_usedDoubleJump = false;
		_coyoteTimer = 0;
		_jumpBufferTimer = 0;
		_attackCooldownTimer = 0;
		_sprite.Visible = true;
		_sprite.Modulate = new Color(1, 1, 1, 1);
		_hurtbox.SetDeferred("monitorable", true);
	}
}
