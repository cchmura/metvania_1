using Godot;

namespace metvania_1;

public partial class BoneProjectile : Area2D
{
	private const float BoneGravity = 400f;
	private const float Lifetime = 3f;

	private Vector2 _velocity;
	private float _timer;
	private Sprite2D _sprite;
	private Hitbox _hitbox;

	public void Init(Vector2 velocity)
	{
		_velocity = velocity;
	}

	public override void _Ready()
	{
		CollisionLayer = 0;
		CollisionMask = 1; // Detect world geometry

		_sprite = new Sprite2D();
		_sprite.Texture = AssetLoader.BoneProjectileSprite();
		AddChild(_sprite);

		var bodyShape = new CollisionShape2D();
		var bodyRect = new RectangleShape2D();
		bodyRect.Size = new Vector2(6, 6);
		bodyShape.Shape = bodyRect;
		AddChild(bodyShape);

		_hitbox = new Hitbox();
		_hitbox.CollisionLayer = 16;
		_hitbox.CollisionMask = 32;
		_hitbox.Damage = 1;
		var hitShape = new CollisionShape2D();
		var hitRect = new RectangleShape2D();
		hitRect.Size = new Vector2(6, 6);
		hitShape.Shape = hitRect;
		_hitbox.AddChild(hitShape);
		AddChild(_hitbox);
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

		// Apply gravity
		_velocity.Y += BoneGravity * dt;

		GlobalPosition += _velocity * dt;

		// Visual spin
		_sprite.RotationDegrees += 720f * dt;
	}

	private void OnBodyEntered(Node2D body)
	{
		QueueFree();
	}

	private void OnHitLanded(Node2D target)
	{
		QueueFree();
	}
}
