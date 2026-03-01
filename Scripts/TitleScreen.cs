using Godot;

namespace metvania_1;

public partial class TitleScreen : Node2D
{
	private enum MenuState { Main, SlotSelect, Settings }
	private enum SlotAction { NewGame, Continue }

	private MenuState _state = MenuState.Main;
	private int _selectedIndex;
	private SlotAction _slotAction;

	private AudioManager _audioManager;
	private SaveManager _saveManager;
	private GameState _gameState;

	// UI
	private CanvasLayer _ui;
	private Label _titleLabel;
	private Label _cursor;
	private Label _backHint;

	// Menu labels
	private Label[] _mainLabels;
	private Label[] _slotLabels;
	private Label[] _settingsLabels;

	// Volume
	private int _sfxPercent = 100;
	private int _musicPercent = 100;

	public override void _Ready()
	{
		_audioManager = GetNode<AudioManager>("/root/AudioManager");
		_saveManager = GetNode<SaveManager>("/root/SaveManager");
		_gameState = GetNode<GameState>("/root/GameState");

		SetupBackground();

		_ui = new CanvasLayer();
		_ui.Layer = 10;
		AddChild(_ui);

		// Title
		_titleLabel = new Label();
		_titleLabel.Text = "METVANIA";
		_titleLabel.Position = new Vector2(100, 20);
		_titleLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.9f, 1f));
		_titleLabel.AddThemeFontSizeOverride("font_size", 14);
		_ui.AddChild(_titleLabel);

		// Subtitle
		var sub = new Label();
		sub.Text = "The Depths Await";
		sub.Position = new Vector2(115, 40);
		sub.AddThemeColorOverride("font_color", new Color(0.5f, 0.55f, 0.65f));
		sub.AddThemeFontSizeOverride("font_size", 8);
		_ui.AddChild(sub);

		// Cursor
		_cursor = new Label();
		_cursor.Text = ">";
		_cursor.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
		_cursor.AddThemeFontSizeOverride("font_size", 8);
		_ui.AddChild(_cursor);

		// Back hint
		_backHint = new Label();
		_backHint.Text = "[J/L] Back";
		_backHint.Position = new Vector2(122, 166);
		_backHint.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.4f));
		_backHint.AddThemeFontSizeOverride("font_size", 8);
		_backHint.Visible = false;
		_ui.AddChild(_backHint);

		SetupMainMenu();
		SetupSlotSelect();
		SetupSettings();

		// Init volume from audio bus
		_sfxPercent = (int)(_audioManager.GetSfxVolume() * 100f + 0.5f);
		_musicPercent = (int)(_audioManager.GetMusicVolume() * 100f + 0.5f);

		ShowState(MenuState.Main);

		_audioManager.PlayMusic("title");
	}

	private void SetupBackground()
	{
		var bg = new ColorRect();
		bg.Size = new Vector2(320, 180);
		bg.Color = new Color(0.02f, 0.02f, 0.06f);
		AddChild(bg);

		var stars = new Sprite2D();
		stars.Texture = SpriteFactory.ParallaxFarLayer(320, 180);
		stars.Centered = false;
		AddChild(stars);
	}

	private void SetupMainMenu()
	{
		string[] options = { "New Game", "Continue", "Settings" };
		_mainLabels = new Label[options.Length];
		for (int i = 0; i < options.Length; i++)
		{
			var label = new Label();
			label.Text = options[i];
			label.Position = new Vector2(125, 72 + i * 14);
			label.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
			label.AddThemeFontSizeOverride("font_size", 8);
			_ui.AddChild(label);
			_mainLabels[i] = label;
		}
	}

	private void SetupSlotSelect()
	{
		_slotLabels = new Label[3];
		for (int i = 0; i < 3; i++)
		{
			var label = new Label();
			label.Position = new Vector2(80, 72 + i * 14);
			label.Size = new Vector2(160, 14);
			label.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
			label.AddThemeFontSizeOverride("font_size", 8);
			_ui.AddChild(label);
			_slotLabels[i] = label;
		}
	}

	private void SetupSettings()
	{
		_settingsLabels = new Label[2];
		for (int i = 0; i < 2; i++)
		{
			var label = new Label();
			label.Position = new Vector2(90, 72 + i * 14);
			label.Size = new Vector2(140, 14);
			label.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
			label.AddThemeFontSizeOverride("font_size", 8);
			_ui.AddChild(label);
			_settingsLabels[i] = label;
		}
	}

	private void ShowState(MenuState state)
	{
		_state = state;
		_selectedIndex = 0;

		foreach (var l in _mainLabels) l.Visible = false;
		foreach (var l in _slotLabels) l.Visible = false;
		foreach (var l in _settingsLabels) l.Visible = false;
		_backHint.Visible = false;

		switch (state)
		{
			case MenuState.Main:
				foreach (var l in _mainLabels) l.Visible = true;
				// Dim "Continue" if no saves
				_mainLabels[1].AddThemeColorOverride("font_color",
					_saveManager.HasAnySave() ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.3f, 0.3f, 0.3f));
				break;

			case MenuState.SlotSelect:
				RefreshSlotLabels();
				foreach (var l in _slotLabels) l.Visible = true;
				_backHint.Visible = true;
				break;

			case MenuState.Settings:
				RefreshSettingsLabels();
				foreach (var l in _settingsLabels) l.Visible = true;
				_backHint.Visible = true;
				break;
		}

		UpdateCursor();
	}

	private void RefreshSlotLabels()
	{
		for (int i = 0; i < 3; i++)
		{
			var info = _saveManager.GetSlotInfo(i);
			if (info != null)
			{
				string abilities = info.Abilities.Count > 0 ? string.Join(", ", info.Abilities) : "none";
				_slotLabels[i].Text = $"Slot {i + 1}: {info.CurrentRoom}  HP:{info.MaxHealth}  [{abilities}]";
			}
			else
			{
				_slotLabels[i].Text = $"Slot {i + 1}: Empty";
			}
		}
	}

	private void RefreshSettingsLabels()
	{
		_settingsLabels[0].Text = $"SFX Volume:   < {_sfxPercent}% >";
		_settingsLabels[1].Text = $"Music Volume: < {_musicPercent}% >";
	}

	private Label[] GetCurrentLabels() => _state switch
	{
		MenuState.Main => _mainLabels,
		MenuState.SlotSelect => _slotLabels,
		MenuState.Settings => _settingsLabels,
		_ => _mainLabels,
	};

	private void UpdateCursor()
	{
		var labels = GetCurrentLabels();
		_cursor.Position = new Vector2(labels[_selectedIndex].Position.X - 10, labels[_selectedIndex].Position.Y);

		for (int i = 0; i < labels.Length; i++)
		{
			// Skip dimmed "Continue" in main menu
			if (_state == MenuState.Main && i == 1 && !_saveManager.HasAnySave()) continue;
			labels[i].AddThemeColorOverride("font_color",
				i == _selectedIndex ? new Color(1f, 1f, 1f) : new Color(0.7f, 0.7f, 0.7f));
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("move_up"))
		{
			_selectedIndex = (_selectedIndex - 1 + GetCurrentLabels().Length) % GetCurrentLabels().Length;
			_audioManager?.Play("menu_select");
			UpdateCursor();
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed("move_down"))
		{
			_selectedIndex = (_selectedIndex + 1) % GetCurrentLabels().Length;
			_audioManager?.Play("menu_select");
			UpdateCursor();
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed("jump") || @event.IsActionPressed("interact"))
		{
			Confirm();
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed("attack") || @event.IsActionPressed("dash"))
		{
			GoBack();
			GetViewport().SetInputAsHandled();
		}
		else if (_state == MenuState.Settings)
		{
			if (@event.IsActionPressed("move_left"))
			{
				AdjustVolume(-10);
				GetViewport().SetInputAsHandled();
			}
			else if (@event.IsActionPressed("move_right"))
			{
				AdjustVolume(10);
				GetViewport().SetInputAsHandled();
			}
		}
	}

	private void Confirm()
	{
		_audioManager?.Play("menu_confirm");

		switch (_state)
		{
			case MenuState.Main:
				switch (_selectedIndex)
				{
					case 0: // New Game
						_slotAction = SlotAction.NewGame;
						ShowState(MenuState.SlotSelect);
						break;
					case 1: // Continue
						if (!_saveManager.HasAnySave()) return;
						_slotAction = SlotAction.Continue;
						ShowState(MenuState.SlotSelect);
						break;
					case 2: // Settings
						ShowState(MenuState.Settings);
						break;
				}
				break;

			case MenuState.SlotSelect:
				int slot = _selectedIndex;
				_saveManager.ActiveSlot = slot;
				_gameState.ActiveSaveSlot = slot;

				if (_slotAction == SlotAction.NewGame)
				{
					_gameState.Reset();
					_audioManager?.StopMusic(0.5f);
					GetTree().ChangeSceneToFile("res://Scenes/World/World.tscn");
				}
				else
				{
					if (_saveManager.GetSlotInfo(slot) == null) return;
					_saveManager.Load();
					_audioManager?.StopMusic(0.5f);
					GetTree().ChangeSceneToFile("res://Scenes/World/World.tscn");
				}
				break;
		}
	}

	private void GoBack()
	{
		if (_state != MenuState.Main)
		{
			_audioManager?.Play("menu_select");
			ShowState(MenuState.Main);
		}
	}

	private void AdjustVolume(int delta)
	{
		if (_selectedIndex == 0)
		{
			_sfxPercent = Mathf.Clamp(_sfxPercent + delta, 0, 100);
			_audioManager?.SetSfxVolume(_sfxPercent / 100f);
			_audioManager?.Play("menu_select");
		}
		else
		{
			_musicPercent = Mathf.Clamp(_musicPercent + delta, 0, 100);
			_audioManager?.SetMusicVolume(_musicPercent / 100f);
		}

		RefreshSettingsLabels();
		UpdateCursor();
	}
}
