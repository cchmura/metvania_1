using Godot;

namespace metvania_1;

public partial class Flyer : EnemyBase
{
	private const float IdleBobAmplitude = 8f;
	private const float IdleBobFrequency = 2f;
	private const float AggroRange = 120f;
	private const float AggroSpeed = 60f;

	private Vector2 _homePosition;
	private float _bobTimer;
	private Player _target;

	protected override void EnemyInit()
	{
		_homePosition = GlobalPosition;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsDead) return;

		float dt = (float)delta;
		_bobTimer += dt;

		// Find player if not cached
		if (_target == null || !IsInstanceValid(_target))
		{
			var players = GetTree().GetNodesInGroup("player");
			if (players.Count > 0)
				_target = players[0] as Player;
		}

		bool aggro = false;

		if (_target != null && IsInstanceValid(_target))
		{
			float dist = GlobalPosition.DistanceTo(_target.GlobalPosition);
			if (dist < AggroRange)
			{
				aggro = true;
			}
		}

		if (aggro)
		{
			// Move toward player
			var dir = (_target.GlobalPosition - GlobalPosition).Normalized();
			GlobalPosition += dir * AggroSpeed * dt;
		}
		else
		{
			// Idle bob at home position
			float bobY = Mathf.Sin(_bobTimer * IdleBobFrequency * Mathf.Tau) * IdleBobAmplitude;
			GlobalPosition = _homePosition + new Vector2(0, bobY);
		}

		// Face player if aggro
		if (aggro && _target != null)
		{
			int dir = _target.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
			Sprite.Scale = new Vector2(dir, 1);
			if (dir == -1)
				Sprite.Position = new Vector2(6, -6);
			else
				Sprite.Position = new Vector2(-6, -6);
		}
	}
}
