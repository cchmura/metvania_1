using Godot;

namespace metvania_1;

public partial class Projectile : Area2D
{
	private const float Speed = 120f;
	private const float Lifetime = 3f;

	private Vector2 _direction;
	private float _timer;
	private Hitbox _hitbox;

	public void Init(Vector2 direction)
	{
		_direction = direction.Normalized();
	}

	public override void _Ready()
	{
		var sprite = GetNode<Sprite2D>("Sprite");
		sprite.Texture = AssetLoader.ProjectileSprite();

		CollisionLayer = 0;
		CollisionMask = 1; // Detect world geometry

		_hitbox = GetNode<Hitbox>("Hitbox");
		_hitbox.Damage = 1;
		_hitbox.Activate();
		_hitbox.HitLanded += OnHitLanded;

		BodyEntered += OnBodyEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		_timer += dt;

		if (_timer >= Lifetime)
		{
			QueueFree();
			return;
		}

		GlobalPosition += _direction * Speed * dt;
	}

	private void OnBodyEntered(Node2D body)
	{
		// Hit world geometry — despawn
		QueueFree();
	}

	private void OnHitLanded(Node2D target)
	{
		// Hit player — despawn
		QueueFree();
	}
}
