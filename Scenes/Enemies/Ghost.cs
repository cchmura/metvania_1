using Godot;

namespace metvania_1;

public partial class Ghost : EnemyBase
{
	private const float AggroRange = 90f;
	private const float DriftSpeed = 35f;
	private const float IdleBobAmplitude = 4f;
	private const float IdleBobFrequency = 1.5f;

	private Vector2 _homePosition;
	private float _bobTimer;
	private Player _target;

	protected override void EnemyInit()
	{
		Sprite.Texture = SpriteFactory.GhostSprite();
		_homePosition = GlobalPosition;

		// Semi-transparent
		Modulate = new Color(1, 1, 1, 0.5f);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsDead) return;

		float dt = (float)delta;
		_bobTimer += dt;

		// Find player
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
				aggro = true;
		}

		if (aggro)
		{
			// Drift toward player (direct position, phases through walls)
			var dir = (_target.GlobalPosition - GlobalPosition).Normalized();
			GlobalPosition += dir * DriftSpeed * dt;

			// Face player
			Sprite.FlipH = _target.GlobalPosition.X < GlobalPosition.X;
		}
		else
		{
			// Idle bob at home
			float bobY = Mathf.Sin(_bobTimer * IdleBobFrequency * Mathf.Tau) * IdleBobAmplitude;
			GlobalPosition = _homePosition + new Vector2(0, bobY);
		}
	}
}
