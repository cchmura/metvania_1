using Godot;

namespace metvania_1;

public partial class Bat : EnemyBase
{
	private const float HorizontalSpeed = 80f;
	private const float SineAmplitude = 20f;
	private const float SineFrequency = 2.5f;
	private const float DespawnDistance = 500f;

	private Vector2 _startPosition;
	private int _direction = -1;
	private float _timer;

	protected override void EnemyInit()
	{
		Sprite.Texture = SpriteFactory.BatSprite();
		_startPosition = GlobalPosition;

		// Determine initial direction toward player
		var players = GetTree().GetNodesInGroup("player");
		if (players.Count > 0 && players[0] is Player player)
		{
			_direction = player.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsDead) return;

		float dt = (float)delta;
		_timer += dt;

		// Horizontal movement + sine wave vertical
		float baseY = _startPosition.Y;
		float sineY = Mathf.Sin(_timer * SineFrequency * Mathf.Tau) * SineAmplitude;

		GlobalPosition += new Vector2(HorizontalSpeed * _direction * dt, 0);
		GlobalPosition = new Vector2(GlobalPosition.X, baseY + sineY);

		// Flip sprite
		Sprite.FlipH = (_direction == -1);

		// Despawn when far from start
		if (GlobalPosition.DistanceTo(_startPosition) > DespawnDistance)
		{
			QueueFree();
		}
	}
}
