using Godot;

namespace metvania_1;

public partial class Spikes : Area2D
{
	private Hitbox _hitbox;

	public override void _Ready()
	{
		var sprite = GetNode<Sprite2D>("Sprite");
		sprite.Texture = AssetLoader.SpikesSprite();

		// Hitbox is always active — spikes are always dangerous
		_hitbox = GetNode<Hitbox>("Hitbox");
		_hitbox.Activate();
	}
}
