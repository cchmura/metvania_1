using Godot;

namespace metvania_1;

public partial class Dropper : EnemyBase
{
	private enum DropperState { Hanging, Dropping, Landed, Rising }

	private const float Gravity = 800f;
	private const float DetectRangeX = 24f;
	private const float RiseSpeed = 60f;
	private const float LandedWaitDuration = 1f;

	private DropperState _dropperState = DropperState.Hanging;
	private float _stateTimer;
	private Vector2 _homePosition;
	private Player _target;

	protected override void EnemyInit()
	{
		Sprite.Texture = AssetLoader.DropperSprite();
		_homePosition = GlobalPosition;
		// Hanging: no body collision so player walks through
		CollisionMask = 0;
		Sprite.Modulate = new Color(1f, 1f, 1f, 0.4f); // Semi-transparent while hanging
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsDead) return;

		float dt = (float)delta;

		// Find player
		if (_target == null || !IsInstanceValid(_target))
		{
			var players = GetTree().GetNodesInGroup("player");
			if (players.Count > 0)
				_target = players[0] as Player;
		}

		switch (_dropperState)
		{
			case DropperState.Hanging:
			{
				Velocity = Vector2.Zero;

				if (_target != null && IsInstanceValid(_target))
				{
					float dx = Mathf.Abs(_target.GlobalPosition.X - GlobalPosition.X);
					bool playerBelow = _target.GlobalPosition.Y > GlobalPosition.Y;

					if (dx < DetectRangeX && playerBelow)
					{
						_dropperState = DropperState.Dropping;
						CollisionMask = 1; // Enable world collision
						Sprite.Modulate = new Color(1f, 1f, 1f, 1f); // Fully visible
					}
				}
				break;
			}

			case DropperState.Dropping:
			{
				var velocity = Velocity;
				velocity.Y += Gravity * dt;
				velocity.X = 0;
				Velocity = velocity;
				MoveAndSlide();

				if (IsOnFloor())
				{
					_dropperState = DropperState.Landed;
					_stateTimer = LandedWaitDuration;
					Velocity = Vector2.Zero;
				}
				break;
			}

			case DropperState.Landed:
			{
				_stateTimer -= dt;
				Velocity = Vector2.Zero;
				MoveAndSlide();

				if (_stateTimer <= 0)
				{
					_dropperState = DropperState.Rising;
					CollisionMask = 0; // Phase through geometry while rising
				}
				break;
			}

			case DropperState.Rising:
			{
				var dir = (_homePosition - GlobalPosition).Normalized();
				GlobalPosition += dir * RiseSpeed * dt;
				Sprite.Modulate = new Color(1f, 1f, 1f, 0.4f); // Semi-transparent

				if (GlobalPosition.DistanceTo(_homePosition) < 2f)
				{
					GlobalPosition = _homePosition;
					_dropperState = DropperState.Hanging;
					Velocity = Vector2.Zero;
				}
				break;
			}
		}
	}
}
