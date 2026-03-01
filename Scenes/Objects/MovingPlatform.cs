using Godot;

namespace metvania_1;

public enum PlatformMode
{
	Horizontal,
	Vertical,
	Falling,
}

public partial class MovingPlatform : AnimatableBody2D
{
	[Export] public PlatformMode Mode { get; set; } = PlatformMode.Horizontal;
	[Export] public float MoveDistance { get; set; } = 64f;
	[Export] public float MoveSpeed { get; set; } = 40f;

	// Falling platform config
	private const float FallDelay = 0.5f;
	private const float FallGravity = 800f;
	private const float FallMaxDistance = 200f;
	private const float RespawnTime = 3.0f;

	private Vector2 _startPosition;
	private float _moveProgress; // 0-1 ping-pong
	private int _moveDir = 1; // 1 = forward, -1 = back

	// Falling state
	private bool _playerOnPlatform;
	private float _fallDelayTimer;
	private bool _isFalling;
	private float _fallVelocity;
	private float _fallDistance;
	private bool _isRespawning;
	private float _respawnTimer;
	private float _shakeTimer;

	private CollisionShape2D _collision;
	private Sprite2D _sprite;
	private Area2D _detectionArea;

	public override void _Ready()
	{
		_startPosition = GlobalPosition;
		_collision = GetNode<CollisionShape2D>("CollisionShape2D");
		_sprite = GetNode<Sprite2D>("Sprite");
		_sprite.Texture = SpriteFactory.PlatformSprite();

		if (Mode == PlatformMode.Falling)
		{
			SetupFallingDetection();
		}
	}

	private void SetupFallingDetection()
	{
		_detectionArea = new Area2D();
		_detectionArea.CollisionLayer = 0;
		_detectionArea.CollisionMask = 2; // PlayerBody

		var shape = new CollisionShape2D();
		var rect = new RectangleShape2D();
		rect.Size = new Vector2(32, 4);
		shape.Shape = rect;
		shape.Position = new Vector2(0, -6); // Slightly above platform top
		_detectionArea.AddChild(shape);

		_detectionArea.BodyEntered += (body) =>
		{
			if (body is Player) _playerOnPlatform = true;
		};
		_detectionArea.BodyExited += (body) =>
		{
			if (body is Player) _playerOnPlatform = false;
		};

		AddChild(_detectionArea);
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		switch (Mode)
		{
			case PlatformMode.Horizontal:
				ProcessHorizontal(dt);
				break;
			case PlatformMode.Vertical:
				ProcessVertical(dt);
				break;
			case PlatformMode.Falling:
				ProcessFalling(dt);
				break;
		}
	}

	private void ProcessHorizontal(float dt)
	{
		_moveProgress += (MoveSpeed / MoveDistance) * dt * _moveDir;
		if (_moveProgress >= 1f)
		{
			_moveProgress = 1f;
			_moveDir = -1;
		}
		else if (_moveProgress <= 0f)
		{
			_moveProgress = 0f;
			_moveDir = 1;
		}

		var target = _startPosition + new Vector2(MoveDistance * _moveProgress, 0);
		GlobalPosition = target;
	}

	private void ProcessVertical(float dt)
	{
		_moveProgress += (MoveSpeed / MoveDistance) * dt * _moveDir;
		if (_moveProgress >= 1f)
		{
			_moveProgress = 1f;
			_moveDir = -1;
		}
		else if (_moveProgress <= 0f)
		{
			_moveProgress = 0f;
			_moveDir = 1;
		}

		var target = _startPosition + new Vector2(0, MoveDistance * _moveProgress);
		GlobalPosition = target;
	}

	private void ProcessFalling(float dt)
	{
		if (_isRespawning)
		{
			_respawnTimer -= dt;
			if (_respawnTimer <= 0)
			{
				RespawnPlatform();
			}
			return;
		}

		if (_isFalling)
		{
			_fallVelocity += FallGravity * dt;
			GlobalPosition += new Vector2(0, _fallVelocity * dt);
			_fallDistance += _fallVelocity * dt;

			if (_fallDistance >= FallMaxDistance)
			{
				// Disappear and start respawn timer
				_sprite.Visible = false;
				_collision.SetDeferred("disabled", true);
				_isFalling = false;
				_isRespawning = true;
				_respawnTimer = RespawnTime;
			}
			return;
		}

		if (_playerOnPlatform)
		{
			_fallDelayTimer += dt;

			// Shake effect before falling
			if (_fallDelayTimer > FallDelay * 0.3f)
			{
				_shakeTimer += dt;
				float shakeOffset = (float)GD.RandRange(-1.0, 1.0);
				GlobalPosition = _startPosition + new Vector2(shakeOffset, 0);
			}

			if (_fallDelayTimer >= FallDelay)
			{
				_isFalling = true;
				_fallVelocity = 0;
				_fallDistance = 0;
			}
		}
		else
		{
			_fallDelayTimer = 0;
			_shakeTimer = 0;
			GlobalPosition = _startPosition;
		}
	}

	private void RespawnPlatform()
	{
		_isRespawning = false;
		_isFalling = false;
		_fallDelayTimer = 0;
		_fallVelocity = 0;
		_fallDistance = 0;
		_shakeTimer = 0;
		_playerOnPlatform = false;
		GlobalPosition = _startPosition;
		_sprite.Visible = true;
		_collision.SetDeferred("disabled", false);
	}
}
