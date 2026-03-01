using Godot;

namespace metvania_1;

public partial class AbilityOrb : Area2D
{
	[Export] public string OrbId = "DoubleJumpOrb";
	[Export] public string AbilityName = "DoubleJump";

	private GameState _gameState;

	public override void _Ready()
	{
		var sprite = GetNode<Sprite2D>("Sprite");
		sprite.Texture = AssetLoader.AbilityOrbSprite();

		_gameState = GetNode<GameState>("/root/GameState");

		if (_gameState.IsCollected(OrbId))
		{
			QueueFree();
			return;
		}

		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player) return;

		_gameState.UnlockAbility(AbilityName);
		_gameState.MarkCollected(OrbId);

		// Scale tween feedback
		var tween = CreateTween();
		tween.TweenProperty(this, "scale", new Vector2(2f, 2f), 0.2f);
		tween.Parallel().TweenProperty(this, "modulate:a", 0f, 0.2f);
		tween.TweenCallback(Callable.From(QueueFree));
	}
}
