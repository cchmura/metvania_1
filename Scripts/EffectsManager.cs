using Godot;

namespace metvania_1;

public enum ParticleType
{
	HitSpark,
	EnemyDeath,
	DustPuff,
	PogoSpark,
	WallSlide,
	DashTrail,
	Deflect,
	BossSlam,
}

public partial class EffectsManager : Node
{
	private Camera2D _camera;
	private Vector2 _cameraBaseOffset;
	private float _shakeIntensity;
	private float _shakeTimer;
	private bool _freezeActive;

	public void SetCamera(Camera2D camera)
	{
		_camera = camera;
		_cameraBaseOffset = camera.Offset;
	}

	public void SetCameraBaseOffset(Vector2 offset)
	{
		_cameraBaseOffset = offset;
	}

	public void HitFreeze(float duration = 0.07f)
	{
		if (_freezeActive) return;
		_freezeActive = true;
		Engine.TimeScale = 0.05;

		GetTree().CreateTimer(duration, true, false, true).Timeout += () =>
		{
			Engine.TimeScale = 1.0;
			_freezeActive = false;
		};
	}

	public void Shake(float intensity = 3f, float duration = 0.2f)
	{
		if (intensity > _shakeIntensity)
		{
			_shakeIntensity = intensity;
		}
		_shakeTimer = Mathf.Max(_shakeTimer, duration);
	}

	public override void _Process(double delta)
	{
		if (_camera == null) return;

		if (_shakeTimer > 0)
		{
			_shakeTimer -= (float)delta;
			var offset = new Vector2(
				(float)GD.RandRange(-_shakeIntensity, _shakeIntensity),
				(float)GD.RandRange(-_shakeIntensity, _shakeIntensity)
			);
			_camera.Offset = _cameraBaseOffset + offset;

			if (_shakeTimer <= 0)
			{
				_shakeIntensity = 0;
				_camera.Offset = _cameraBaseOffset;
			}
		}
		else
		{
			_camera.Offset = _cameraBaseOffset;
		}
	}

	public void SpawnParticles(Vector2 position, ParticleType type)
	{
		var particles = new CpuParticles2D();
		particles.Emitting = true;
		particles.OneShot = true;
		particles.Explosiveness = 1.0f;
		particles.GlobalPosition = position;
		particles.ZIndex = 10;

		switch (type)
		{
			case ParticleType.HitSpark:
				particles.Amount = 8;
				particles.Lifetime = 0.15;
				particles.Direction = Vector2.Zero;
				particles.Spread = 180f;
				particles.InitialVelocityMin = 80f;
				particles.InitialVelocityMax = 150f;
				particles.Gravity = Vector2.Zero;
				particles.ScaleAmountMin = 1.5f;
				particles.ScaleAmountMax = 2.5f;
				particles.Color = new Color(1f, 1f, 1f);
				break;

			case ParticleType.EnemyDeath:
				particles.Amount = 12;
				particles.Lifetime = 0.3;
				particles.Direction = Vector2.Zero;
				particles.Spread = 180f;
				particles.InitialVelocityMin = 60f;
				particles.InitialVelocityMax = 120f;
				particles.Gravity = new Vector2(0, 200);
				particles.ScaleAmountMin = 2f;
				particles.ScaleAmountMax = 3f;
				particles.Color = new Color(0.8f, 0.2f, 0.2f);
				break;

			case ParticleType.DustPuff:
				particles.Amount = 4;
				particles.Lifetime = 0.2;
				particles.Direction = new Vector2(0, -1);
				particles.Spread = 40f;
				particles.InitialVelocityMin = 20f;
				particles.InitialVelocityMax = 40f;
				particles.Gravity = Vector2.Zero;
				particles.ScaleAmountMin = 1f;
				particles.ScaleAmountMax = 2f;
				particles.Color = new Color(0.6f, 0.5f, 0.4f);
				break;

			case ParticleType.PogoSpark:
				particles.Amount = 6;
				particles.Lifetime = 0.15;
				particles.Direction = new Vector2(0, 1);
				particles.Spread = 45f;
				particles.InitialVelocityMin = 60f;
				particles.InitialVelocityMax = 100f;
				particles.Gravity = Vector2.Zero;
				particles.ScaleAmountMin = 1.5f;
				particles.ScaleAmountMax = 2.5f;
				particles.Color = new Color(1f, 0.9f, 0.3f);
				break;

			case ParticleType.WallSlide:
				particles.Amount = 2;
				particles.Lifetime = 0.12;
				particles.Direction = new Vector2(0, 1);
				particles.Spread = 20f;
				particles.InitialVelocityMin = 15f;
				particles.InitialVelocityMax = 30f;
				particles.Gravity = Vector2.Zero;
				particles.ScaleAmountMin = 1f;
				particles.ScaleAmountMax = 1.5f;
				particles.Color = new Color(0.6f, 0.5f, 0.4f);
				break;

			case ParticleType.DashTrail:
				particles.Amount = 6;
				particles.Lifetime = 0.2;
				particles.Direction = Vector2.Zero;
				particles.Spread = 180f;
				particles.InitialVelocityMin = 10f;
				particles.InitialVelocityMax = 30f;
				particles.Gravity = Vector2.Zero;
				particles.ScaleAmountMin = 1.5f;
				particles.ScaleAmountMax = 2.5f;
				particles.Color = new Color(0.4f, 0.7f, 1f);
				break;

			case ParticleType.Deflect:
				particles.Amount = 10;
				particles.Lifetime = 0.2;
				particles.Direction = Vector2.Zero;
				particles.Spread = 180f;
				particles.InitialVelocityMin = 100f;
				particles.InitialVelocityMax = 180f;
				particles.Gravity = Vector2.Zero;
				particles.ScaleAmountMin = 1.5f;
				particles.ScaleAmountMax = 2.5f;
				particles.Color = new Color(0.7f, 0.8f, 1f);
				break;

			case ParticleType.BossSlam:
				particles.Amount = 16;
				particles.Lifetime = 0.3;
				particles.Direction = new Vector2(0, -1);
				particles.Spread = 60f;
				particles.InitialVelocityMin = 80f;
				particles.InitialVelocityMax = 160f;
				particles.Gravity = new Vector2(0, 300);
				particles.ScaleAmountMin = 2f;
				particles.ScaleAmountMax = 4f;
				particles.Color = new Color(0.6f, 0.3f, 0.1f);
				break;
		}

		GetTree().CurrentScene.AddChild(particles);

		// Auto-free after lifetime + small buffer
		GetTree().CreateTimer(particles.Lifetime + 0.1, false, false, false).Timeout += () =>
		{
			if (IsInstanceValid(particles))
				particles.QueueFree();
		};
	}
}
