using Godot;

namespace metvania_1;

public partial class Charger : EnemyBase
{
	private enum ChargerState { Patrol, WindUp, Charge, Stunned }

	private const float Gravity = 800f;
	private const float PatrolSpeed = 40f;
	private const float ChargeSpeed = 280f;
	private const float WindUpDuration = 0.3f;
	private const float ChargeDuration = 0.6f;
	private const float StunDuration = 0.8f;
	private const float DetectRangeX = 120f;
	private const float DetectRangeY = 24f;

	private ChargerState _chargerState = ChargerState.Patrol;
	private int _direction = 1;
	private float _stateTimer;
	private int _chargeDirection;
	private Player _target;

	private RayCast2D _wallDetector;
	private RayCast2D _edgeDetector;
	private Color _baseColor = new Color(1f, 1f, 1f, 1f);

	protected override void EnemyInit()
	{
		Sprite.Texture = AssetLoader.ChargerSprite();

		_wallDetector = new RayCast2D();
		_wallDetector.TargetPosition = new Vector2(16, 0);
		_wallDetector.CollisionMask = 1;
		_wallDetector.Enabled = true;
		AddChild(_wallDetector);

		_edgeDetector = new RayCast2D();
		_edgeDetector.Position = new Vector2(14, 0);
		_edgeDetector.TargetPosition = new Vector2(0, 16);
		_edgeDetector.CollisionMask = 1;
		_edgeDetector.Enabled = true;
		AddChild(_edgeDetector);
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

		_stateTimer -= dt;

		switch (_chargerState)
		{
			case ChargerState.Patrol:
				// Patrol like Crawler
				_wallDetector.TargetPosition = new Vector2(16 * _direction, 0);
				_edgeDetector.Position = new Vector2(14 * _direction, 0);

				if (IsOnFloor())
				{
					if (_wallDetector.IsColliding() || !_edgeDetector.IsColliding())
					{
						_direction *= -1;
						_wallDetector.TargetPosition = new Vector2(16 * _direction, 0);
						_edgeDetector.Position = new Vector2(14 * _direction, 0);
					}
				}

				velocity.X = PatrolSpeed * _direction;
				Sprite.Modulate = _baseColor;

				// Check for player to trigger charge
				if (_target != null && IsInstanceValid(_target) && IsOnFloor())
				{
					float dx = Mathf.Abs(_target.GlobalPosition.X - GlobalPosition.X);
					float dy = Mathf.Abs(_target.GlobalPosition.Y - GlobalPosition.Y);
					if (dx < DetectRangeX && dy < DetectRangeY)
					{
						_chargerState = ChargerState.WindUp;
						_stateTimer = WindUpDuration;
						_chargeDirection = _target.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
						velocity.X = 0;

						var audio = GetNodeOrNull<AudioManager>("/root/AudioManager");
						audio?.Play("charge_windup");
					}
				}
				break;

			case ChargerState.WindUp:
				velocity.X = 0;
				// Flash red
				Sprite.Modulate = ((int)(_stateTimer / 0.05f)) % 2 == 0
					? new Color(1f, 0.3f, 0.3f)
					: _baseColor;

				if (_stateTimer <= 0)
				{
					_chargerState = ChargerState.Charge;
					_stateTimer = ChargeDuration;
				}
				break;

			case ChargerState.Charge:
				velocity.X = ChargeSpeed * _chargeDirection;
				Sprite.Modulate = new Color(1f, 0.3f, 0.3f);

				if (IsOnWall() || _stateTimer <= 0)
				{
					_chargerState = ChargerState.Stunned;
					_stateTimer = StunDuration;
					velocity.X = 0;
				}
				break;

			case ChargerState.Stunned:
				velocity.X = 0;
				Sprite.Modulate = new Color(0.5f, 0.5f, 0.5f);

				if (_stateTimer <= 0)
				{
					_chargerState = ChargerState.Patrol;
					_direction = _chargeDirection;
				}
				break;
		}

		// Flip sprite
		int facing = _chargerState == ChargerState.Charge ? _chargeDirection : _direction;
		Sprite.FlipH = (facing == -1);

		Velocity = velocity;
		MoveAndSlide();
	}
}
