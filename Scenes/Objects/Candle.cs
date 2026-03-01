using Godot;

namespace metvania_1;

public partial class Candle : StaticBody2D
{
	private HealthComponent _health;
	private Sprite2D _sprite;

	public override void _Ready()
	{
		CollisionLayer = 0;
		CollisionMask = 0;

		_sprite = new Sprite2D();
		_sprite.Texture = AssetLoader.CandleSprite();
		AddChild(_sprite);

		_health = new HealthComponent();
		_health.MaxHealth = 1;
		_health.InvincibilityDuration = 0f;
		AddChild(_health);

		var hurtbox = new Hurtbox();
		hurtbox.CollisionLayer = 64;  // Enemy hurtbox layer (player attack hits it)
		hurtbox.CollisionMask = 0;
		hurtbox.Health = _health;
		var hurtShape = new CollisionShape2D();
		var hurtRect = new RectangleShape2D();
		hurtRect.Size = new Vector2(14, 14);
		hurtShape.Shape = hurtRect;
		hurtbox.AddChild(hurtShape);
		AddChild(hurtbox);

		_health.Died += OnDied;
	}

	private void OnDied()
	{
		// Effects
		var effects = GetNodeOrNull<EffectsManager>("/root/EffectsManager");
		effects?.SpawnParticles(GlobalPosition, ParticleType.EnemyDeath);
		var audio = GetNodeOrNull<AudioManager>("/root/AudioManager");
		audio?.Play("enemy_death");

		// Spawn weapon upgrade pickup if not at max tier
		var gameState = GetNode<GameState>("/root/GameState");
		if (gameState.WeaponTier < 5)
		{
			SpawnWeaponUpgrade(GlobalPosition);
		}

		// Death animation
		var tween = CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(this, "scale", new Vector2(0.1f, 0.1f), 0.2f);
		tween.TweenProperty(_sprite, "modulate:a", 0f, 0.2f);
		tween.SetParallel(false);
		tween.TweenCallback(Callable.From(QueueFree));
	}

	private void SpawnWeaponUpgrade(Vector2 position)
	{
		var pickup = new Area2D();
		pickup.GlobalPosition = position;
		pickup.CollisionLayer = 128;  // Pickup layer
		pickup.CollisionMask = 2;     // Player body

		var shape = new CollisionShape2D();
		var circle = new CircleShape2D();
		circle.Radius = 8f;
		shape.Shape = circle;
		pickup.AddChild(shape);

		var sprite = new Sprite2D();
		sprite.Texture = AssetLoader.WeaponUpgradeSprite();
		pickup.AddChild(sprite);

		// Bob animation
		var bobTween = pickup.CreateTween();
		bobTween.SetLoops();
		bobTween.TweenProperty(sprite, "position:y", -4f, 0.5f)
			.SetTrans(Tween.TransitionType.Sine);
		bobTween.TweenProperty(sprite, "position:y", 0f, 0.5f)
			.SetTrans(Tween.TransitionType.Sine);

		pickup.BodyEntered += (body) =>
		{
			if (body is not Player) return;

			var gs = pickup.GetNode<GameState>("/root/GameState");
			if (gs.WeaponTier < 5)
			{
				gs.WeaponTier++;
				gs.EmitSignal(GameState.SignalName.WeaponTierChanged, gs.WeaponTier);
			}

			var audio2 = pickup.GetNodeOrNull<AudioManager>("/root/AudioManager");
			audio2?.Play("ability_unlock");
			var fx = pickup.GetNodeOrNull<EffectsManager>("/root/EffectsManager");
			fx?.SpawnParticles(pickup.GlobalPosition, ParticleType.HitSpark);

			// Collect animation
			var collectTween = pickup.CreateTween();
			collectTween.SetParallel(true);
			collectTween.TweenProperty(pickup, "scale", new Vector2(1.5f, 1.5f), 0.2f);
			collectTween.TweenProperty(sprite, "modulate:a", 0f, 0.2f);
			collectTween.SetParallel(false);
			collectTween.TweenCallback(Callable.From(pickup.QueueFree));
		};

		GetTree().CurrentScene.AddChild(pickup);

		// Auto-despawn after 10s
		var despawnTween = pickup.CreateTween();
		despawnTween.TweenInterval(8f);
		despawnTween.TweenProperty(sprite, "modulate:a", 0f, 2f);
		despawnTween.TweenCallback(Callable.From(() =>
		{
			if (IsInstanceValid(pickup))
				pickup.QueueFree();
		}));
	}
}
