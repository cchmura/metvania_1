using Godot;

namespace metvania_1;

public partial class SavePoint : Area2D
{
	private bool _playerInside;
	private GameState _gameState;
	private SaveManager _saveManager;

	public override void _Ready()
	{
		var sprite = GetNode<Sprite2D>("Sprite");
		sprite.Texture = AssetLoader.SavePointSprite();

		_gameState = GetNode<GameState>("/root/GameState");
		_saveManager = GetNode<SaveManager>("/root/SaveManager");

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player) return;
		_playerInside = true;
		var hud = GetNodeOrNull<Hud>("/root/World/Hud");
		hud?.ShowPrompt("Press [E] to save");
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is not Player) return;
		_playerInside = false;
		var hud = GetNodeOrNull<Hud>("/root/World/Hud");
		hud?.HidePrompt();
	}

	public override void _Process(double delta)
	{
		if (!_playerInside) return;

		if (Input.IsActionJustPressed("interact"))
		{
			// Update spawn to this save point position
			_gameState.PlayerSpawnPosition = GlobalPosition + new Vector2(0, -16);
			_saveManager.Save();

			// Heal the player
			var players = GetTree().GetNodesInGroup("player");
			if (players.Count > 0 && players[0] is Player player)
			{
				var health = player.GetNodeOrNull<HealthComponent>("HealthComponent");
				health?.Reset();
			}

			var hud = GetNodeOrNull<Hud>("/root/World/Hud");
			hud?.ShowSaveConfirmation();
		}
	}
}
