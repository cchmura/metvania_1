using Godot;

namespace metvania_1;

public partial class Hud : CanvasLayer
{
	private Label _abilityLabel;
	private Label _roomLabel;
	private Label _promptLabel;
	private Label _saveConfirmLabel;
	private HBoxContainer _healthContainer;
	private GameState _gameState;

	private const int PipSize = 10;
	private const int PipGap = 3;

	public override void _Ready()
	{
		_abilityLabel = GetNode<Label>("AbilityLabel");
		_roomLabel = GetNode<Label>("RoomLabel");
		_promptLabel = GetNode<Label>("PromptLabel");
		_saveConfirmLabel = GetNode<Label>("SaveConfirmLabel");
		_healthContainer = GetNode<HBoxContainer>("HealthContainer");
		_gameState = GetNode<GameState>("/root/GameState");

		_gameState.AbilityUnlocked += OnAbilityUnlocked;
		UpdateAbilityDisplay();
	}

	public override void _Process(double delta)
	{
		UpdateAbilityDisplay();
	}

	public void UpdateHealthDisplay(int current, int max)
	{
		// Clear existing pips
		foreach (var child in _healthContainer.GetChildren())
		{
			child.QueueFree();
		}

		// Create pips
		for (int i = 0; i < max; i++)
		{
			var pip = new ColorRect();
			pip.CustomMinimumSize = new Vector2(PipSize, PipSize);
			pip.Color = i < current
				? new Color(0.9f, 0.2f, 0.2f, 1f)  // Filled — red
				: new Color(0.3f, 0.3f, 0.3f, 0.6f); // Empty — dark
			_healthContainer.AddChild(pip);
		}
	}

	private void UpdateAbilityDisplay()
	{
		if (_gameState.HasAbility("DoubleJump"))
			_abilityLabel.Text = "Double Jump: UNLOCKED";
		else
			_abilityLabel.Text = "Double Jump: ---";
	}

	public void SetRoomName(string roomName)
	{
		_roomLabel.Text = roomName;
	}

	public void ShowPrompt(string text)
	{
		_promptLabel.Text = text;
	}

	public void HidePrompt()
	{
		_promptLabel.Text = "";
	}

	public void ShowSaveConfirmation()
	{
		_saveConfirmLabel.Text = "Game Saved!";
		_saveConfirmLabel.Modulate = new Color(1, 1, 1, 1);

		var tween = CreateTween();
		tween.TweenInterval(1.0f);
		tween.TweenProperty(_saveConfirmLabel, "modulate:a", 0f, 0.5f);
	}

	private void OnAbilityUnlocked(string abilityName)
	{
		_saveConfirmLabel.Text = $"{abilityName} unlocked!";
		_saveConfirmLabel.Modulate = new Color(1, 0.85f, 0.1f, 1);

		var tween = CreateTween();
		tween.TweenInterval(2.0f);
		tween.TweenProperty(_saveConfirmLabel, "modulate:a", 0f, 0.5f);
	}
}
