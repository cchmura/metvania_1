using Godot;

namespace metvania_1;

public partial class Shooter : EnemyBase
{
	private const float Gravity = 800f;
	private const float DetectRange = 130f;
	private const float FireInterval = 2.0f;

	private float _fireTimer;
	private Player _target;
	private PackedScene _projectileScene;

	protected override void EnemyInit()
	{
		Sprite.Texture = SpriteFactory.ShooterSprite();
		_projectileScene = GD.Load<PackedScene>("res://Scenes/Enemies/Projectile.tscn");
		_fireTimer = FireInterval;
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

		velocity.X = 0;
		Velocity = velocity;
		MoveAndSlide();

		// Find player
		if (_target == null || !IsInstanceValid(_target))
		{
			var players = GetTree().GetNodesInGroup("player");
			if (players.Count > 0)
				_target = players[0] as Player;
		}

		if (_target == null || !IsInstanceValid(_target)) return;

		float dist = GlobalPosition.DistanceTo(_target.GlobalPosition);
		if (dist > DetectRange) return;

		// Face player
		int dir = _target.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
		Sprite.FlipH = (dir == -1);

		// Fire timer
		_fireTimer -= dt;
		if (_fireTimer <= 0)
		{
			_fireTimer = FireInterval;
			FireProjectile();
		}
	}

	private void FireProjectile()
	{
		var projectile = _projectileScene.Instantiate<Projectile>();
		var aimDir = (_target.GlobalPosition - GlobalPosition).Normalized();
		projectile.Init(aimDir);
		projectile.GlobalPosition = GlobalPosition + new Vector2(0, -6);
		GetTree().CurrentScene.AddChild(projectile);

		var audio = GetNodeOrNull<AudioManager>("/root/AudioManager");
		audio?.Play("projectile_fire");
	}
}
