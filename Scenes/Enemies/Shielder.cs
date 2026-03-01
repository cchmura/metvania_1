using Godot;

namespace metvania_1;

public partial class Shielder : EnemyBase
{
	private const float Gravity = 800f;
	private const float MoveSpeed = 25f;
	private const float DetectRange = 100f;
	private const float StunDuration = 0.5f;

	private Player _target;
	private int _facingDir = 1;
	private bool _stunned;
	private float _stunTimer;

	protected override void EnemyInit()
	{
		Sprite.Texture = SpriteFactory.ShielderSprite();

		// Intercept damage manually — set hurtbox Health to null
		EnemyHurtbox.Health = null;
		EnemyHurtbox.Hurt += OnShielderHurt;
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

		if (_stunned)
		{
			_stunTimer -= dt;
			velocity.X = 0;
			Sprite.Modulate = new Color(0.5f, 0.5f, 0.5f);

			if (_stunTimer <= 0)
			{
				_stunned = false;
				Sprite.Modulate = new Color(1f, 1f, 1f, 1f);
			}
		}
		else if (_target != null && IsInstanceValid(_target))
		{
			float dist = GlobalPosition.DistanceTo(_target.GlobalPosition);

			// Always face player
			_facingDir = _target.GlobalPosition.X > GlobalPosition.X ? 1 : -1;

			if (dist < DetectRange && IsOnFloor())
			{
				// Walk toward player
				velocity.X = MoveSpeed * _facingDir;
			}
			else
			{
				velocity.X = 0;
			}
		}
		else
		{
			velocity.X = 0;
		}

		// Flip sprite
		Sprite.FlipH = (_facingDir == -1);

		Velocity = velocity;
		MoveAndSlide();
	}

	private void OnShielderHurt(int damage, Hitbox source)
	{
		if (IsDead || Health.IsInvincible) return;

		bool frontalHit = false;

		if (source != null)
		{
			float sourceX = source.GlobalPosition.X;
			float sourceY = source.GlobalPosition.Y;

			// Check if hit is from the front (same side as facing)
			bool fromRight = sourceX > GlobalPosition.X;
			bool fromAbove = sourceY < GlobalPosition.Y - 12;

			if (fromAbove)
			{
				// Above attacks always penetrate
				frontalHit = false;
			}
			else if ((_facingDir == 1 && fromRight) || (_facingDir == -1 && !fromRight))
			{
				frontalHit = true;
			}
		}

		if (frontalHit)
		{
			// Deflect!
			var effects = GetNodeOrNull<EffectsManager>("/root/EffectsManager");
			effects?.SpawnParticles(GlobalPosition + new Vector2(_facingDir * 8, -6), ParticleType.Deflect);
			var audio = GetNodeOrNull<AudioManager>("/root/AudioManager");
			audio?.Play("deflect");
		}
		else
		{
			// Hit from behind or above — take damage and stun
			Health.TakeDamage(damage);
			_stunned = true;
			_stunTimer = StunDuration;
		}
	}
}
