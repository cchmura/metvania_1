using Godot;

namespace metvania_1;

public partial class Crawler : EnemyBase
{
	private const float MoveSpeed = 40f;
	private const float Gravity = 800f;

	private int _direction = 1;
	private RayCast2D _wallDetector;
	private RayCast2D _edgeDetector;

	protected override void EnemyInit()
	{
		// Wall detector — horizontal ray in movement direction
		_wallDetector = new RayCast2D();
		_wallDetector.TargetPosition = new Vector2(10, 0);
		_wallDetector.CollisionMask = 1; // World geometry
		_wallDetector.Enabled = true;
		AddChild(_wallDetector);

		// Edge detector — downward ray ahead of movement to detect floor edge
		_edgeDetector = new RayCast2D();
		_edgeDetector.Position = new Vector2(8, 0);
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
		{
			velocity.Y += Gravity * dt;
		}
		else
		{
			velocity.Y = 0;
		}

		// Update ray directions for current facing
		_wallDetector.TargetPosition = new Vector2(10 * _direction, 0);
		_edgeDetector.Position = new Vector2(8 * _direction, 0);

		// Check for wall or edge — reverse direction
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

		// Flip sprite
		Sprite.Scale = new Vector2(_direction, 1);
		if (_direction == -1)
			Sprite.Position = new Vector2(6, -12);
		else
			Sprite.Position = new Vector2(-6, -12);

		Velocity = velocity;
		MoveAndSlide();
	}
}
