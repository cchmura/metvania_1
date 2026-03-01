using Godot;

namespace metvania_1;

public partial class Skeleton : EnemyBase
{
	private const float MoveSpeed = 30f;
	private const float Gravity = 800f;
	private const float DetectRange = 110f;
	private const float FireInterval = 2.5f;
	private const float ThrowPauseDuration = 0.4f;

	private int _direction = 1;
	private RayCast2D _wallDetector;
	private RayCast2D _edgeDetector;
	private float _fireTimer;
	private float _throwPauseTimer;
	private Player _target;

	protected override void EnemyInit()
	{
		Sprite.Texture = AssetLoader.SkeletonSprite();
		_fireTimer = FireInterval;

		// Wall detector
		_wallDetector = new RayCast2D();
		_wallDetector.TargetPosition = new Vector2(16, 0);
		_wallDetector.CollisionMask = 1;
		_wallDetector.Enabled = true;
		AddChild(_wallDetector);

		// Edge detector
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

		// Throw pause
		if (_throwPauseTimer > 0)
		{
			_throwPauseTimer -= dt;
			velocity.X = 0;
			Velocity = velocity;
			MoveAndSlide();
			return;
		}

		// Check if player in range
		bool playerInRange = false;
		if (_target != null && IsInstanceValid(_target))
		{
			float dist = GlobalPosition.DistanceTo(_target.GlobalPosition);
			if (dist < DetectRange)
			{
				playerInRange = true;
				// Face player
				_direction = _target.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
			}
		}

		// Fire timer
		if (playerInRange)
		{
			_fireTimer -= dt;
			if (_fireTimer <= 0)
			{
				_fireTimer = FireInterval;
				ThrowBone();
				_throwPauseTimer = ThrowPauseDuration;
				velocity.X = 0;
				Velocity = velocity;
				MoveAndSlide();
				return;
			}
		}

		// Update ray directions
		_wallDetector.TargetPosition = new Vector2(16 * _direction, 0);
		_edgeDetector.Position = new Vector2(14 * _direction, 0);

		// Wall/edge detection
		if (IsOnFloor())
		{
			if (_wallDetector.IsColliding() || !_edgeDetector.IsColliding())
			{
				_direction *= -1;
				_wallDetector.TargetPosition = new Vector2(16 * _direction, 0);
				_edgeDetector.Position = new Vector2(14 * _direction, 0);
			}
		}

		velocity.X = MoveSpeed * _direction;
		Sprite.FlipH = (_direction == -1);

		Velocity = velocity;
		MoveAndSlide();
	}

	private void ThrowBone()
	{
		if (_target == null || !IsInstanceValid(_target)) return;

		var bone = new BoneProjectile();
		bone.GlobalPosition = GlobalPosition + new Vector2(_direction * 14, -24);

		// Calculate arc velocity for ~0.6s flight
		float flightTime = 0.6f;
		var targetPos = _target.GlobalPosition;
		var startPos = bone.GlobalPosition;
		float dx = targetPos.X - startPos.X;
		float dy = targetPos.Y - startPos.Y;

		float vx = dx / flightTime;
		float vy = (dy - 0.5f * 400f * flightTime * flightTime) / flightTime;

		bone.Init(new Vector2(vx, vy));
		GetTree().CurrentScene.AddChild(bone);

		var audio = GetNodeOrNull<AudioManager>("/root/AudioManager");
		audio?.Play("projectile_fire");
	}
}
