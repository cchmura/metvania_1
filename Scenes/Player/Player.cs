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
		Dashing,
	}

	private enum AttackDirection
	{
		Forward,
		Up,
		Down,
	}

	// Movement constants
	private const float Speed = 120f;
	private const float Acceleration = 900f;
	private const float Friction = 1400f;
	private const float Gravity = 650f;
	private const float JumpImpulse = -290f;
	private const float JumpReleaseMultiplier = 0.5f;
	private const float CoyoteTime = 0.1f;
	private const float JumpBufferTime = 0.1f;

	// Wall slide/jump constants
	private const float WallSlideMaxSpeed = 35f;
	private const float WallJumpHorizontalImpulse = 150f;
	private const float WallJumpVerticalImpulse = -270f;
	private const float WallJumpInputLockDuration = 0.12f;

	// Dash constants
	private const float DashSpeed = 550f;
	private const float DashDuration = 0.15f;
	private const float DashCooldown = 0.4f;

	// Combat constants
	private const float AttackActiveDuration = 0.25f;
	private const float AttackCooldown = 0.35f;
	private const float PogoImpulse = -260f;
	private const float KnockbackSpeed = 110f;
	private const float KnockbackDuration = 0.2f;
	private const float InvincibilityFlashRate = 0.08f;

	// Combo constants
	private const float Combo1ActiveDuration = 0.2f;
	private const float Combo2ActiveDuration = 0.2f;
	private const float Combo3ActiveDuration = 0.3f;
	private const float ComboWindowDuration = 0.35f;
	private const float ComboEndCooldown = 0.4f;
	private const int Combo3Damage = 2;

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

	// Combo state
	private int _comboStep; // 0 = none, 1-3 = combo hit
	private float _comboWindowTimer;
	private bool _comboQueued;

	// Dash state
	private float _dashTimer;
	private float _dashCooldownTimer;
	private bool _usedAirDash;
	private int _dashDirection;
	private float _dashAfterimageTimer;

	// Wall slide/jump state
	private bool _isWallSliding;
	private int _wallDirection; // -1 left, 1 right, 0 none
	private float _wallJumpInputLockTimer;
	private float _wallSlideParticleTimer;
	private bool _wallSlideSoundPlayed;
	private RayCast2D _wallRayLeft;
	private RayCast2D _wallRayRight;

	// Landing detection
	private bool _wasAirborne;

	// Auto-walk (level transition cutscene)
	private bool _isAutoWalking;
	private int _autoWalkDirection;

	// Weapon tier
	private int WeaponBonus => _gameState.WeaponTier - 1;

	// Node references
	private GameState _gameState;
	private EffectsManager _effects;
	private AudioManager _audio;
	private AnimatedSprite2D _sprite;
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
		_sprite = GetNode<AnimatedSprite2D>("Sprite");
		SetupAnimations();
		_healthComponent = GetNode<HealthComponent>("HealthComponent");
		_attackHitbox = GetNode<Hitbox>("AttackHitbox");
		_hurtbox = GetNode<Hurtbox>("Hurtbox");
		_attackShape = _attackHitbox.GetNode<CollisionShape2D>("CollisionShape2D");
		// Ensure attack shape has its own RectangleShape2D so resizing
		// doesn't mutate the player body's shared shape resource.
		_attackShape.Shape = new RectangleShape2D { Size = new Vector2(26, 16) };
		_attackVisual = GetNode<ColorRect>("AttackVisual");

		AddToGroup("player");

		// Wall detection raycasts (player body half-width is 8px + 2px margin = 10px reach)
		_wallRayLeft = new RayCast2D();
		_wallRayLeft.Position = new Vector2(0, -16);
		_wallRayLeft.TargetPosition = new Vector2(-10, 0);
		_wallRayLeft.CollisionMask = 1; // World only
		AddChild(_wallRayLeft);

		_wallRayRight = new RayCast2D();
		_wallRayRight.Position = new Vector2(0, -16);
		_wallRayRight.TargetPosition = new Vector2(10, 0);
		_wallRayRight.CollisionMask = 1; // World only
		AddChild(_wallRayRight);

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
				if (!_isAutoWalking)
				{
					ProcessAttack(dt);
					ProcessDashInput();
				}
				break;
			case PlayerState.Dashing:
				ProcessDash(dt);
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
			_usedAirDash = false;
		}

		// Wall jump input lock timer
		_wallJumpInputLockTimer -= dt;

		// Wall slide detection
		_isWallSliding = false;
		_wallDirection = 0;
		if (!onFloor && velocity.Y > 0 && _wallJumpInputLockTimer <= 0)
		{
			float wallInput = Input.GetAxis("move_left", "move_right");
			if (wallInput < -0.1f && _wallRayLeft.IsColliding())
			{
				_isWallSliding = true;
				_wallDirection = -1;
			}
			else if (wallInput > 0.1f && _wallRayRight.IsColliding())
			{
				_isWallSliding = true;
				_wallDirection = 1;
			}
		}

		if (_isWallSliding)
		{
			velocity.Y = Mathf.Min(velocity.Y, WallSlideMaxSpeed);
			_usedDoubleJump = false;
			_usedAirDash = false;

			// Wall slide particles
			_wallSlideParticleTimer -= dt;
			if (_wallSlideParticleTimer <= 0)
			{
				_wallSlideParticleTimer = 0.08f;
				_effects?.SpawnParticles(GlobalPosition + new Vector2(_wallDirection * 8, -16), ParticleType.WallSlide);
			}

			// Play slide sound once on start
			if (!_wallSlideSoundPlayed)
			{
				_audio?.Play("wall_slide");
				_wallSlideSoundPlayed = true;
			}
		}
		else
		{
			_wallSlideSoundPlayed = false;
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
			else if (_isWallSliding)
			{
				// Wall jump
				velocity.Y = WallJumpVerticalImpulse;
				velocity.X = -_wallDirection * WallJumpHorizontalImpulse;
				_jumpBufferTimer = 0;
				_wallJumpInputLockTimer = WallJumpInputLockDuration;
				_facingDirection = -_wallDirection;
				_isWallSliding = false;
				_wallDirection = 0;
				_audio?.Play("jump");
				_effects?.SpawnParticles(GlobalPosition + new Vector2(_wallDirection * 8, -16), ParticleType.DustPuff);
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
		float inputDir = _isAutoWalking ? _autoWalkDirection : Input.GetAxis("move_left", "move_right");
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
		_sprite.FlipH = (_facingDirection == -1);

		Velocity = velocity;
		MoveAndSlide();

		UpdateAnimation();
	}

	private void ProcessAttack(float dt)
	{
		_attackCooldownTimer -= dt;

		// Combo window (waiting for next J press between combo hits)
		if (_comboWindowTimer > 0)
		{
			_comboWindowTimer -= dt;

			if (Input.IsActionJustPressed("attack"))
			{
				// Advance combo
				_comboWindowTimer = 0;
				_comboStep++;
				ActivateComboHit();
				return;
			}

			if (_comboWindowTimer <= 0)
			{
				// Window expired — end combo
				EndCombo();
				return;
			}
			return;
		}

		if (_state == PlayerState.Attacking)
		{
			// Queue next combo hit during active frames
			if (_comboStep > 0 && _comboStep < 3 && _attackDir == AttackDirection.Forward)
			{
				if (Input.IsActionJustPressed("attack"))
				{
					_comboQueued = true;
				}
			}

			_attackTimer -= dt;
			if (_attackTimer <= 0)
			{
				if (_attackDir == AttackDirection.Forward && _comboStep > 0 && _comboStep < 3)
				{
					if (_comboQueued)
					{
						// Immediately advance combo
						_comboQueued = false;
						_comboStep++;
						ActivateComboHit();
					}
					else
					{
						// Enter combo window
						_attackHitbox.Deactivate();
						_attackVisual.Visible = false;
						_state = PlayerState.Normal;
						_comboWindowTimer = ComboWindowDuration;
					}
				}
				else
				{
					EndCombo();
				}
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
		_pogoThisAttack = false;

		// Determine direction
		if (Input.IsActionPressed("move_up"))
		{
			_attackDir = AttackDirection.Up;
			_comboStep = 0; // No combo for up/down
		}
		else if (Input.IsActionPressed("move_down") && !IsOnFloor())
		{
			_attackDir = AttackDirection.Down;
			_comboStep = 0; // No combo for up/down
		}
		else
		{
			_attackDir = AttackDirection.Forward;
			_comboStep = 1;
		}

		if (_attackDir == AttackDirection.Forward)
		{
			ActivateComboHit();
		}
		else
		{
			// Up/Down: single hit, original behavior
			_attackTimer = AttackActiveDuration;
			_attackCooldownTimer = AttackCooldown;
			_attackHitbox.Damage = 1 + WeaponBonus;
			_audio?.Play("slash");
			PositionSingleAttack();
			_attackHitbox.Activate();
			_attackVisual.Visible = true;
		}
	}

	private void ActivateComboHit()
	{
		_state = PlayerState.Attacking;
		_comboQueued = false;
		_pogoThisAttack = false;

		var shape = _attackShape.Shape as RectangleShape2D;

		switch (_comboStep)
		{
			case 1:
				shape.Size = new Vector2(26, 16);
				_attackShape.Position = new Vector2(20 * _facingDirection, -16);
				_attackVisual.Size = new Vector2(26, 16);
				_attackVisual.Position = new Vector2(20 * _facingDirection - 13, -28);
				_attackTimer = Combo1ActiveDuration;
				_attackHitbox.Damage = 1 + WeaponBonus;
				_audio?.Play("slash");
				break;
			case 2:
				shape.Size = new Vector2(30, 16);
				_attackShape.Position = new Vector2(22 * _facingDirection, -16);
				_attackVisual.Size = new Vector2(30, 16);
				_attackVisual.Position = new Vector2(22 * _facingDirection - 15, -28);
				_attackTimer = Combo2ActiveDuration;
				_attackHitbox.Damage = 1 + WeaponBonus;
				_audio?.Play("slash");
				break;
			case 3:
				shape.Size = new Vector2(32, 20);
				_attackShape.Position = new Vector2(24 * _facingDirection, -16);
				_attackVisual.Size = new Vector2(32, 20);
				_attackVisual.Position = new Vector2(24 * _facingDirection - 16, -30);
				_attackTimer = Combo3ActiveDuration;
				_attackHitbox.Damage = Combo3Damage + WeaponBonus;
				_audio?.Play("heavy_slash");
				break;
		}

		_attackHitbox.Activate();
		_attackVisual.Visible = true;
	}

	private void PositionSingleAttack()
	{
		var shape = _attackShape.Shape as RectangleShape2D;
		switch (_attackDir)
		{
			case AttackDirection.Up:
				shape.Size = new Vector2(16, 26);
				_attackShape.Position = new Vector2(0, -38);
				_attackVisual.Size = new Vector2(16, 26);
				_attackVisual.Position = new Vector2(-8, -51);
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

	private void EndCombo()
	{
		bool wasCombo = _comboStep > 0;
		_comboStep = 0;
		_comboWindowTimer = 0;
		_comboQueued = false;
		_attackHitbox.Damage = 1 + WeaponBonus;
		_attackCooldownTimer = wasCombo ? ComboEndCooldown : AttackCooldown;
		EndAttack();
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
			_effects?.SpawnParticles(GlobalPosition + new Vector2(0, 16), ParticleType.PogoSpark);
		}
	}

	private void OnDamaged(int amount)
	{
		if (_state == PlayerState.Dead) return;
		_state = PlayerState.Damaged;
		_knockbackTimer = KnockbackDuration;

		// End any active attack/combo
		_comboStep = 0;
		_comboWindowTimer = 0;
		_comboQueued = false;
		_attackHitbox.Damage = 1 + WeaponBonus;
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

	private void ProcessDashInput()
	{
		_dashCooldownTimer -= (float)GetPhysicsProcessDeltaTime();

		if (Input.IsActionJustPressed("dash") && _dashCooldownTimer <= 0
			&& _gameState.HasAbility("Dash") && _state != PlayerState.Dashing)
		{
			bool onFloor = IsOnFloor();
			if (onFloor || !_usedAirDash)
			{
				StartDash(onFloor);
			}
		}
	}

	private void StartDash(bool onFloor)
	{
		_state = PlayerState.Dashing;
		_dashDirection = _facingDirection;
		_dashTimer = DashDuration;
		_dashCooldownTimer = DashCooldown;
		_dashAfterimageTimer = 0;
		_healthComponent.ForceInvincible(DashDuration);

		if (!onFloor)
		{
			_usedAirDash = true;
		}

		// End any active attack
		if (_attackVisual.Visible)
		{
			EndAttack();
		}

		_audio?.Play("dash");
		_effects?.SpawnParticles(GlobalPosition, ParticleType.DashTrail);
	}

	private void ProcessDash(float dt)
	{
		_dashTimer -= dt;

		// Move at dash speed, zero gravity
		Velocity = new Vector2(_dashDirection * DashSpeed, 0);
		MoveAndSlide();

		// Spawn afterimage every 0.03s
		_dashAfterimageTimer -= dt;
		if (_dashAfterimageTimer <= 0)
		{
			_dashAfterimageTimer = 0.03f;
			SpawnDashAfterimage();
		}

		if (_dashTimer <= 0)
		{
			EndDash();
		}
	}

	private void EndDash()
	{
		_state = PlayerState.Normal;
		// Carry momentum at normal speed, zero vertical
		Velocity = new Vector2(_dashDirection * Speed, 0);
	}

	private void SpawnDashAfterimage()
	{
		var afterimage = new Sprite2D();
		afterimage.Texture = _sprite.SpriteFrames?.GetFrameTexture(_sprite.Animation, _sprite.Frame);
		afterimage.Position = GlobalPosition + new Vector2(0, -16);
		afterimage.FlipH = _sprite.FlipH;
		afterimage.Modulate = new Color(0.4f, 0.7f, 1f, 0.5f);
		afterimage.ZIndex = -1;
		GetTree().CurrentScene.AddChild(afterimage);

		var tween = afterimage.CreateTween();
		tween.TweenProperty(afterimage, "modulate:a", 0f, 0.15f);
		tween.TweenCallback(Callable.From(afterimage.QueueFree));
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

	private void SetupAnimations()
	{
		var frames = new SpriteFrames();

		// Idle (2 frames, 3 fps for slow bob)
		frames.AddAnimation("idle");
		frames.SetAnimationSpeed("idle", 3);
		frames.SetAnimationLoop("idle", true);
		var idle = AssetLoader.PlayerIdle();
		foreach (var tex in idle)
			frames.AddFrame("idle", tex);

		// Run (4 frames, 10 fps)
		frames.AddAnimation("run");
		frames.SetAnimationSpeed("run", 10);
		frames.SetAnimationLoop("run", true);
		var run = AssetLoader.PlayerRun();
		foreach (var tex in run)
			frames.AddFrame("run", tex);

		// Jump (1 frame)
		frames.AddAnimation("jump");
		frames.SetAnimationSpeed("jump", 1);
		frames.SetAnimationLoop("jump", false);
		frames.AddFrame("jump", AssetLoader.PlayerJump());

		// Fall (1 frame)
		frames.AddAnimation("fall");
		frames.SetAnimationSpeed("fall", 1);
		frames.SetAnimationLoop("fall", false);
		frames.AddFrame("fall", AssetLoader.PlayerFall());

		// Attack forward (used for all 3 combo steps)
		frames.AddAnimation("attack_forward");
		frames.SetAnimationSpeed("attack_forward", 1);
		frames.SetAnimationLoop("attack_forward", false);
		frames.AddFrame("attack_forward", AssetLoader.PlayerAttackForward());

		// Attack up
		frames.AddAnimation("attack_up");
		frames.SetAnimationSpeed("attack_up", 1);
		frames.SetAnimationLoop("attack_up", false);
		frames.AddFrame("attack_up", AssetLoader.PlayerAttackUp());

		// Attack down
		frames.AddAnimation("attack_down");
		frames.SetAnimationSpeed("attack_down", 1);
		frames.SetAnimationLoop("attack_down", false);
		frames.AddFrame("attack_down", AssetLoader.PlayerAttackDown());

		// Dash
		frames.AddAnimation("dash");
		frames.SetAnimationSpeed("dash", 1);
		frames.SetAnimationLoop("dash", false);
		frames.AddFrame("dash", AssetLoader.PlayerDash());

		// Wall slide
		frames.AddAnimation("wall_slide");
		frames.SetAnimationSpeed("wall_slide", 1);
		frames.SetAnimationLoop("wall_slide", false);
		frames.AddFrame("wall_slide", AssetLoader.PlayerWallSlide());

		// Damaged
		frames.AddAnimation("damaged");
		frames.SetAnimationSpeed("damaged", 1);
		frames.SetAnimationLoop("damaged", false);
		frames.AddFrame("damaged", AssetLoader.PlayerDamaged());

		// Remove the default animation that SpriteFrames creates
		if (frames.HasAnimation("default"))
			frames.RemoveAnimation("default");

		_sprite.SpriteFrames = frames;
		_sprite.Play("idle");
	}

	private void UpdateAnimation()
	{
		string anim;

		if (_state == PlayerState.Dead || _state == PlayerState.Damaged)
			anim = "damaged";
		else if (_state == PlayerState.Dashing)
			anim = "dash";
		else if (_state == PlayerState.Attacking)
		{
			anim = _attackDir switch
			{
				AttackDirection.Up => "attack_up",
				AttackDirection.Down => "attack_down",
				_ => "attack_forward",
			};
		}
		else if (_isWallSliding)
			anim = "wall_slide";
		else if (!IsOnFloor())
			anim = Velocity.Y < 0 ? "jump" : "fall";
		else if (Mathf.Abs(Velocity.X) > 10f)
			anim = "run";
		else
			anim = "idle";

		if (_sprite.Animation != anim)
			_sprite.Play(anim);
	}

	/// <summary>Called by World to start auto-walk during level transition.</summary>
	public void StartAutoWalk(int direction)
	{
		_isAutoWalking = true;
		_autoWalkDirection = direction;
		_state = PlayerState.Normal;
	}

	public void StopAutoWalk()
	{
		_isAutoWalking = false;
		_autoWalkDirection = 0;
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
		_isWallSliding = false;
		_wallDirection = 0;
		_wallJumpInputLockTimer = 0;
		_dashTimer = 0;
		_dashCooldownTimer = 0;
		_usedAirDash = false;
		_comboStep = 0;
		_comboWindowTimer = 0;
		_comboQueued = false;
		_gameState.WeaponTier = 1;
		_attackHitbox.Damage = 1;
		_sprite.Visible = true;
		_sprite.Modulate = new Color(1, 1, 1, 1);
		_hurtbox.SetDeferred("monitorable", true);
	}
}
