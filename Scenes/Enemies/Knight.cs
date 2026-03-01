using Godot;

namespace metvania_1;

public partial class Knight : EnemyBase
{
	private enum KnightState { Patrol, WindUp, Thrust, Cooldown }

	private const float MoveSpeed = 20f;
	private const float Gravity = 800f;
	private const float DetectRange = 50f;
	private const float WindUpDuration = 0.3f;
	private const float ThrustDuration = 0.5f;
	private const float CooldownDuration = 1.5f;

	private KnightState _state = KnightState.Patrol;
	private int _direction = 1;
	private float _stateTimer;
	private RayCast2D _wallDetector;
	private RayCast2D _edgeDetector;
	private Player _target;
	private Hitbox _spearHitbox;

	protected override void EnemyInit()
	{
		Sprite.Texture = SpriteFactory.KnightSprite();

		// Wall detector
		_wallDetector = new RayCast2D();
		_wallDetector.TargetPosition = new Vector2(10, 0);
		_wallDetector.CollisionMask = 1;
		_wallDetector.Enabled = true;
		AddChild(_wallDetector);

		// Edge detector
		_edgeDetector = new RayCast2D();
		_edgeDetector.Position = new Vector2(8, 0);
		_edgeDetector.TargetPosition = new Vector2(0, 16);
		_edgeDetector.CollisionMask = 1;
		_edgeDetector.Enabled = true;
		AddChild(_edgeDetector);

		// Spear hitbox (separate from contact hitbox)
		_spearHitbox = new Hitbox();
		_spearHitbox.CollisionLayer = 16;
		_spearHitbox.CollisionMask = 32;
		_spearHitbox.Damage = 1;
		var spearShape = new CollisionShape2D();
		var spearRect = new RectangleShape2D();
		spearRect.Size = new Vector2(24, 8);
		spearShape.Shape = spearRect;
		spearShape.Position = new Vector2(18, -8);
		_spearHitbox.AddChild(spearShape);
		AddChild(_spearHitbox);
		_spearHitbox.Deactivate();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsDead)
		{
			_spearHitbox.Deactivate();
			return;
		}

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

		switch (_state)
		{
			case KnightState.Patrol:
				ProcessPatrol(dt, ref velocity);
				break;
			case KnightState.WindUp:
				ProcessWindUp(dt, ref velocity);
				break;
			case KnightState.Thrust:
				ProcessThrust(dt, ref velocity);
				break;
			case KnightState.Cooldown:
				ProcessCooldown(dt, ref velocity);
				break;
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void ProcessPatrol(float dt, ref Vector2 velocity)
	{
		// Update ray directions
		_wallDetector.TargetPosition = new Vector2(10 * _direction, 0);
		_edgeDetector.Position = new Vector2(8 * _direction, 0);

		// Wall/edge detection
		if (IsOnFloor())
		{
			if (_wallDetector.IsColliding() || !_edgeDetector.IsColliding())
			{
				_direction *= -1;
				_wallDetector.TargetPosition = new Vector2(10 * _direction, 0);
				_edgeDetector.Position = new Vector2(8 * _direction, 0);
			}
		}

		velocity.X = MoveSpeed * _direction;
		Sprite.FlipH = (_direction == -1);

		// Update spear hitbox position for facing
		UpdateSpearPosition();

		// Check for player in range
		if (_target != null && IsInstanceValid(_target))
		{
			float dist = GlobalPosition.DistanceTo(_target.GlobalPosition);
			if (dist < DetectRange)
			{
				// Face player
				_direction = _target.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
				Sprite.FlipH = (_direction == -1);
				UpdateSpearPosition();
				_state = KnightState.WindUp;
				_stateTimer = WindUpDuration;
				velocity.X = 0;
			}
		}
	}

	private void ProcessWindUp(float dt, ref Vector2 velocity)
	{
		velocity.X = 0;
		_stateTimer -= dt;

		// Flash during wind-up
		float flashPhase = Mathf.Sin(_stateTimer * 30f);
		Sprite.Modulate = flashPhase > 0 ? new Color(2, 2, 2, 1) : new Color(1, 1, 1, 1);

		if (_stateTimer <= 0)
		{
			Sprite.Modulate = new Color(1, 1, 1, 1);
			_state = KnightState.Thrust;
			_stateTimer = ThrustDuration;
			_spearHitbox.Activate();
		}
	}

	private void ProcessThrust(float dt, ref Vector2 velocity)
	{
		velocity.X = 0;
		_stateTimer -= dt;

		if (_stateTimer <= 0)
		{
			_spearHitbox.Deactivate();
			_state = KnightState.Cooldown;
			_stateTimer = CooldownDuration;
		}
	}

	private void ProcessCooldown(float dt, ref Vector2 velocity)
	{
		velocity.X = 0;
		_stateTimer -= dt;

		if (_stateTimer <= 0)
		{
			_state = KnightState.Patrol;
		}
	}

	private void UpdateSpearPosition()
	{
		var spearShape = _spearHitbox.GetChild<CollisionShape2D>(0);
		spearShape.Position = new Vector2(18 * _direction, -8);
	}
}
