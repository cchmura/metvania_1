using Godot;

namespace metvania_1;

public partial class EnemyBase : CharacterBody2D
{
	[Export] public int ContactDamage { get; set; } = 1;

	protected HealthComponent Health;
	protected Hitbox ContactHitbox;
	protected Hurtbox EnemyHurtbox;
	protected ColorRect Sprite;
	protected bool IsDead;

	public override void _Ready()
	{
		AddToGroup("enemies");

		Health = GetNode<HealthComponent>("HealthComponent");
		ContactHitbox = GetNode<Hitbox>("ContactHitbox");
		EnemyHurtbox = GetNode<Hurtbox>("Hurtbox");
		Sprite = GetNode<ColorRect>("Sprite");

		// Wire hurtbox to health
		EnemyHurtbox.Health = Health;
		ContactHitbox.Damage = ContactDamage;

		// Connect signals
		Health.Damaged += OnDamaged;
		Health.Died += OnDied;

		// Contact hitbox always active
		ContactHitbox.Activate();

		EnemyInit();
	}

	protected virtual void EnemyInit() { }

	private void OnDamaged(int amount)
	{
		// Damage flash — briefly turn white
		var tween = CreateTween();
		tween.TweenProperty(Sprite, "modulate", new Color(10, 10, 10, 1), 0.05f);
		tween.TweenProperty(Sprite, "modulate", new Color(1, 1, 1, 1), 0.1f);
	}

	private void OnDied()
	{
		IsDead = true;
		// Disable collision
		ContactHitbox.Deactivate();
		EnemyHurtbox.SetDeferred("monitorable", false);
		CollisionLayer = 0;
		CollisionMask = 0;

		// Effects
		var effects = GetNodeOrNull<EffectsManager>("/root/EffectsManager");
		effects?.SpawnParticles(GlobalPosition, ParticleType.EnemyDeath);
		effects?.Shake(3f, 0.15f);
		var audio = GetNodeOrNull<AudioManager>("/root/AudioManager");
		audio?.Play("enemy_death");

		// Death animation: shrink + fade + free
		var tween = CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(this, "scale", new Vector2(0.1f, 0.1f), 0.3f);
		tween.TweenProperty(Sprite, "modulate:a", 0f, 0.3f);
		tween.SetParallel(false);
		tween.TweenCallback(Callable.From(QueueFree));
	}
}
