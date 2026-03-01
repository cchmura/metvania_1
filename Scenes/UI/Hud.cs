using Godot;
using System.Collections.Generic;

namespace metvania_1;

public partial class Hud : CanvasLayer
{
	private Label _abilityLabel;
	private Label _roomLabel;
	private Label _promptLabel;
	private Label _saveConfirmLabel;
	private Label _weaponTierLabel;
	private HBoxContainer _healthContainer;
	private GameState _gameState;
	private AudioManager _audioManager;

	// Boss health bar
	private Control _bossHealthContainer;
	private ColorRect _bossHealthBarBg;
	private ColorRect _bossHealthBar;
	private Label _bossNameLabel;
	private int _bossMaxHealth;

	// Pause state machine
	private enum PauseState { None, Menu, Map, Settings }
	private PauseState _pauseState = PauseState.None;
	private int _pauseMenuIndex;

	// Pause menu
	private Control _pauseOverlay;
	private Control _pauseMenuContainer;
	private Label[] _pauseMenuLabels;
	private Label _pauseMenuCursor;

	// Pause map
	private Control _pauseMapContainer;

	// Pause settings
	private Control _pauseSettingsContainer;
	private Label[] _pauseSettingsLabels;
	private Label _pauseSettingsCursor;
	private int _settingsIndex;
	private int _sfxPercent = 100;
	private int _musicPercent = 100;

	private const int PipSize = 4;
	private const int PipGap = 1;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		_abilityLabel = GetNode<Label>("AbilityLabel");
		_roomLabel = GetNode<Label>("RoomLabel");
		_promptLabel = GetNode<Label>("PromptLabel");
		_saveConfirmLabel = GetNode<Label>("SaveConfirmLabel");
		_healthContainer = GetNode<HBoxContainer>("HealthContainer");
		_gameState = GetNode<GameState>("/root/GameState");
		_audioManager = GetNodeOrNull<AudioManager>("/root/AudioManager");

		_gameState.AbilityUnlocked += OnAbilityUnlocked;
		_gameState.WeaponTierChanged += OnWeaponTierChanged;
		UpdateAbilityDisplay();

		// Weapon tier label
		_weaponTierLabel = new Label();
		_weaponTierLabel.Position = new Vector2(4, 24);
		_weaponTierLabel.Size = new Vector2(60, 10);
		_weaponTierLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.8f, 1f));
		_weaponTierLabel.AddThemeFontSizeOverride("font_size", 8);
		AddChild(_weaponTierLabel);
		UpdateWeaponTierDisplay();

		// Boss health bar (hidden by default)
		_bossHealthContainer = new Control();
		_bossHealthContainer.Visible = false;
		AddChild(_bossHealthContainer);

		_bossHealthBarBg = new ColorRect();
		_bossHealthBarBg.Position = new Vector2(30, 168);
		_bossHealthBarBg.Size = new Vector2(260, 5);
		_bossHealthBarBg.Color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
		_bossHealthContainer.AddChild(_bossHealthBarBg);

		_bossHealthBar = new ColorRect();
		_bossHealthBar.Position = new Vector2(30, 168);
		_bossHealthBar.Size = new Vector2(260, 5);
		_bossHealthBar.Color = new Color(0.8f, 0.15f, 0.15f, 1f);
		_bossHealthContainer.AddChild(_bossHealthBar);

		_bossNameLabel = new Label();
		_bossNameLabel.Position = new Vector2(30, 160);
		_bossNameLabel.Size = new Vector2(260, 10);
		_bossNameLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_bossNameLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.8f));
		_bossNameLabel.AddThemeFontSizeOverride("font_size", 8);
		_bossHealthContainer.AddChild(_bossNameLabel);

		// Shared dark overlay (hidden by default)
		_pauseOverlay = new Control();
		_pauseOverlay.Visible = false;
		_pauseOverlay.ProcessMode = ProcessModeEnum.Always;
		_pauseOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(_pauseOverlay);

		var overlay = new ColorRect();
		overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		overlay.Color = new Color(0, 0, 0, 0.8f);
		_pauseOverlay.AddChild(overlay);

		SetupPauseMenu();
		SetupPauseMap();
		SetupPauseSettings();

		// Init volume from audio bus
		if (_audioManager != null)
		{
			_sfxPercent = (int)(_audioManager.GetSfxVolume() * 100f + 0.5f);
			_musicPercent = (int)(_audioManager.GetMusicVolume() * 100f + 0.5f);
		}
	}

	public override void _Process(double delta)
	{
		UpdateAbilityDisplay();
		UpdateWeaponTierDisplay();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("pause"))
		{
			HandlePausePress();
			GetViewport().SetInputAsHandled();
			return;
		}

		if (_pauseState == PauseState.None) return;

		// Navigation in pause states
		if (_pauseState == PauseState.Menu)
		{
			if (@event.IsActionPressed("move_up"))
			{
				_pauseMenuIndex = (_pauseMenuIndex - 1 + _pauseMenuLabels.Length) % _pauseMenuLabels.Length;
				_audioManager?.Play("menu_select");
				UpdatePauseMenuCursor();
				GetViewport().SetInputAsHandled();
			}
			else if (@event.IsActionPressed("move_down"))
			{
				_pauseMenuIndex = (_pauseMenuIndex + 1) % _pauseMenuLabels.Length;
				_audioManager?.Play("menu_select");
				UpdatePauseMenuCursor();
				GetViewport().SetInputAsHandled();
			}
			else if (@event.IsActionPressed("jump") || @event.IsActionPressed("interact"))
			{
				ConfirmPauseMenu();
				GetViewport().SetInputAsHandled();
			}
		}
		else if (_pauseState == PauseState.Map)
		{
			// Any confirm/back returns to menu
			if (@event.IsActionPressed("attack") || @event.IsActionPressed("dash")
				|| @event.IsActionPressed("jump") || @event.IsActionPressed("interact"))
			{
				_audioManager?.Play("menu_select");
				SetPauseState(PauseState.Menu);
				GetViewport().SetInputAsHandled();
			}
		}
		else if (_pauseState == PauseState.Settings)
		{
			if (@event.IsActionPressed("move_up"))
			{
				_settingsIndex = (_settingsIndex - 1 + _pauseSettingsLabels.Length) % _pauseSettingsLabels.Length;
				_audioManager?.Play("menu_select");
				UpdateSettingsCursor();
				GetViewport().SetInputAsHandled();
			}
			else if (@event.IsActionPressed("move_down"))
			{
				_settingsIndex = (_settingsIndex + 1) % _pauseSettingsLabels.Length;
				_audioManager?.Play("menu_select");
				UpdateSettingsCursor();
				GetViewport().SetInputAsHandled();
			}
			else if (@event.IsActionPressed("move_left"))
			{
				AdjustPauseVolume(-10);
				GetViewport().SetInputAsHandled();
			}
			else if (@event.IsActionPressed("move_right"))
			{
				AdjustPauseVolume(10);
				GetViewport().SetInputAsHandled();
			}
			else if (@event.IsActionPressed("attack") || @event.IsActionPressed("dash"))
			{
				_audioManager?.Play("menu_select");
				SetPauseState(PauseState.Menu);
				GetViewport().SetInputAsHandled();
			}
		}
	}

	private void HandlePausePress()
	{
		switch (_pauseState)
		{
			case PauseState.None:
				SetPauseState(PauseState.Menu);
				break;
			case PauseState.Menu:
				SetPauseState(PauseState.None);
				break;
			case PauseState.Map:
			case PauseState.Settings:
				SetPauseState(PauseState.Menu);
				break;
		}
	}

	private void SetPauseState(PauseState state)
	{
		_pauseState = state;

		bool paused = state != PauseState.None;
		GetTree().Paused = paused;
		_pauseOverlay.Visible = paused;
		_pauseMenuContainer.Visible = state == PauseState.Menu;
		_pauseMapContainer.Visible = state == PauseState.Map;
		_pauseSettingsContainer.Visible = state == PauseState.Settings;

		if (state == PauseState.Menu)
		{
			_pauseMenuIndex = 0;
			UpdatePauseMenuCursor();
		}
		else if (state == PauseState.Map)
		{
			RefreshMapDisplay();
		}
		else if (state == PauseState.Settings)
		{
			_settingsIndex = 0;
			RefreshPauseSettingsLabels();
			UpdateSettingsCursor();
		}
	}

	private void ConfirmPauseMenu()
	{
		_audioManager?.Play("menu_confirm");

		switch (_pauseMenuIndex)
		{
			case 0: // Resume
				SetPauseState(PauseState.None);
				break;
			case 1: // Map
				SetPauseState(PauseState.Map);
				break;
			case 2: // Settings
				SetPauseState(PauseState.Settings);
				break;
			case 3: // Quit to Title
				GetTree().Paused = false;
				_pauseState = PauseState.None;
				GetTree().ChangeSceneToFile("res://Scenes/UI/TitleScreen.tscn");
				break;
		}
	}

	// ─── Health Display ─────────────────────────────────────────────

	public void UpdateHealthDisplay(int current, int max)
	{
		foreach (var child in _healthContainer.GetChildren())
			child.QueueFree();

		for (int i = 0; i < max; i++)
		{
			var pip = new ColorRect();
			pip.CustomMinimumSize = new Vector2(PipSize, PipSize);
			pip.Color = i < current
				? new Color(0.9f, 0.2f, 0.2f, 1f)
				: new Color(0.3f, 0.3f, 0.3f, 0.6f);
			_healthContainer.AddChild(pip);
		}
	}

	private void UpdateAbilityDisplay()
	{
		string dblJmp = _gameState.HasAbility("DoubleJump") ? "ON" : "---";
		string dash = _gameState.HasAbility("Dash") ? "ON" : "---";
		_abilityLabel.Text = $"DblJmp: {dblJmp}  Dash: {dash}";
	}

	private void UpdateWeaponTierDisplay()
	{
		int tier = _gameState.WeaponTier;
		_weaponTierLabel.Text = tier > 1 ? $"WPN: Lv{tier}" : "";
	}

	private void OnWeaponTierChanged(int tier)
	{
		UpdateWeaponTierDisplay();
	}

	public void SetRoomName(string roomName) => _roomLabel.Text = roomName;
	public void ShowPrompt(string text) => _promptLabel.Text = text;
	public void HidePrompt() => _promptLabel.Text = "";

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

	// ─── Boss Health Bar ────────────────────────────────────────────

	public void ShowBossHealthBar(string bossName, int current, int max)
	{
		_bossMaxHealth = max;
		_bossNameLabel.Text = bossName;
		_bossHealthBar.Size = new Vector2(260f * current / max, 5);
		_bossHealthContainer.Visible = true;
	}

	public void UpdateBossHealthBar(int current, int max)
	{
		if (_bossMaxHealth <= 0) _bossMaxHealth = max;
		float ratio = (float)current / max;
		_bossHealthBar.Size = new Vector2(260f * ratio, 5);
	}

	public void HideBossHealthBar() => _bossHealthContainer.Visible = false;

	// ─── Pause Menu ─────────────────────────────────────────────────

	private void SetupPauseMenu()
	{
		_pauseMenuContainer = new Control();
		_pauseMenuContainer.Visible = false;
		_pauseMenuContainer.ProcessMode = ProcessModeEnum.Always;
		_pauseOverlay.AddChild(_pauseMenuContainer);

		var title = new Label();
		title.Text = "PAUSED";
		title.Position = new Vector2(130, 26);
		title.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
		title.AddThemeFontSizeOverride("font_size", 10);
		_pauseMenuContainer.AddChild(title);

		string[] options = { "Resume", "Map", "Settings", "Quit to Title" };
		_pauseMenuLabels = new Label[options.Length];
		for (int i = 0; i < options.Length; i++)
		{
			var label = new Label();
			label.Text = options[i];
			label.Position = new Vector2(125, 54 + i * 14);
			label.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
			label.AddThemeFontSizeOverride("font_size", 8);
			_pauseMenuContainer.AddChild(label);
			_pauseMenuLabels[i] = label;
		}

		_pauseMenuCursor = new Label();
		_pauseMenuCursor.Text = ">";
		_pauseMenuCursor.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
		_pauseMenuCursor.AddThemeFontSizeOverride("font_size", 8);
		_pauseMenuContainer.AddChild(_pauseMenuCursor);

		var hint = new Label();
		hint.Text = "ESC to close";
		hint.Position = new Vector2(122, 166);
		hint.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.4f));
		hint.AddThemeFontSizeOverride("font_size", 8);
		_pauseMenuContainer.AddChild(hint);
	}

	private void UpdatePauseMenuCursor()
	{
		_pauseMenuCursor.Position = new Vector2(
			_pauseMenuLabels[_pauseMenuIndex].Position.X - 10,
			_pauseMenuLabels[_pauseMenuIndex].Position.Y);

		for (int i = 0; i < _pauseMenuLabels.Length; i++)
			_pauseMenuLabels[i].AddThemeColorOverride("font_color",
				i == _pauseMenuIndex ? new Color(1f, 1f, 1f) : new Color(0.7f, 0.7f, 0.7f));
	}

	// ─── Pause Map ──────────────────────────────────────────────────

	private static readonly Dictionary<string, Vector2> MapRoomPositions = new()
	{
		["main"] = new Vector2(160, 90),
	};

	private readonly Dictionary<string, ColorRect> _mapRoomRects = new();

	private void SetupPauseMap()
	{
		_pauseMapContainer = new Control();
		_pauseMapContainer.Visible = false;
		_pauseMapContainer.ProcessMode = ProcessModeEnum.Always;
		_pauseOverlay.AddChild(_pauseMapContainer);

		// Title
		var title = new Label();
		title.Text = "MAP";
		title.Position = new Vector2(142, 8);
		title.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
		title.AddThemeFontSizeOverride("font_size", 10);
		_pauseMapContainer.AddChild(title);

		// Room rectangles
		var roomSize = new Vector2(50, 18);
		foreach (var (roomId, pos) in MapRoomPositions)
		{
			if (!RoomData.Rooms.TryGetValue(roomId, out var roomDef)) continue;

			var rect = new ColorRect();
			rect.Position = pos - roomSize / 2;
			rect.Size = roomSize;
			rect.Color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
			_pauseMapContainer.AddChild(rect);
			_mapRoomRects[roomId] = rect;

			var label = new Label();
			label.Text = roomDef.DisplayName;
			label.Position = new Vector2(pos.X - 30, pos.Y + roomSize.Y / 2 + 1);
			label.Size = new Vector2(60, 10);
			label.HorizontalAlignment = HorizontalAlignment.Center;
			label.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
			label.AddThemeFontSizeOverride("font_size", 8);
			_pauseMapContainer.AddChild(label);
		}

		// Hint
		var hint = new Label();
		hint.Text = "[J/L] Back";
		hint.Position = new Vector2(130, 166);
		hint.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.4f));
		hint.AddThemeFontSizeOverride("font_size", 8);
		_pauseMapContainer.AddChild(hint);
	}

	private void RefreshMapDisplay()
	{
		// Single room — always highlight as current
		foreach (var (_, rect) in _mapRoomRects)
			rect.Color = new Color(0.2f, 0.6f, 1f, 0.9f);
	}

	// ─── Pause Settings ─────────────────────────────────────────────

	private void SetupPauseSettings()
	{
		_pauseSettingsContainer = new Control();
		_pauseSettingsContainer.Visible = false;
		_pauseSettingsContainer.ProcessMode = ProcessModeEnum.Always;
		_pauseOverlay.AddChild(_pauseSettingsContainer);

		var title = new Label();
		title.Text = "SETTINGS";
		title.Position = new Vector2(128, 26);
		title.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
		title.AddThemeFontSizeOverride("font_size", 10);
		_pauseSettingsContainer.AddChild(title);

		_pauseSettingsLabels = new Label[2];
		for (int i = 0; i < 2; i++)
		{
			var label = new Label();
			label.Position = new Vector2(90, 64 + i * 14);
			label.Size = new Vector2(140, 14);
			label.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
			label.AddThemeFontSizeOverride("font_size", 8);
			_pauseSettingsContainer.AddChild(label);
			_pauseSettingsLabels[i] = label;
		}

		_pauseSettingsCursor = new Label();
		_pauseSettingsCursor.Text = ">";
		_pauseSettingsCursor.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
		_pauseSettingsCursor.AddThemeFontSizeOverride("font_size", 8);
		_pauseSettingsContainer.AddChild(_pauseSettingsCursor);

		var hint = new Label();
		hint.Text = "[J/L] Back";
		hint.Position = new Vector2(130, 166);
		hint.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.4f));
		hint.AddThemeFontSizeOverride("font_size", 8);
		_pauseSettingsContainer.AddChild(hint);
	}

	private void RefreshPauseSettingsLabels()
	{
		_pauseSettingsLabels[0].Text = $"SFX Volume:   < {_sfxPercent}% >";
		_pauseSettingsLabels[1].Text = $"Music Volume: < {_musicPercent}% >";
	}

	private void UpdateSettingsCursor()
	{
		_pauseSettingsCursor.Position = new Vector2(
			_pauseSettingsLabels[_settingsIndex].Position.X - 10,
			_pauseSettingsLabels[_settingsIndex].Position.Y);

		for (int i = 0; i < _pauseSettingsLabels.Length; i++)
			_pauseSettingsLabels[i].AddThemeColorOverride("font_color",
				i == _settingsIndex ? new Color(1f, 1f, 1f) : new Color(0.7f, 0.7f, 0.7f));
	}

	private void AdjustPauseVolume(int delta)
	{
		if (_settingsIndex == 0)
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

		RefreshPauseSettingsLabels();
		UpdateSettingsCursor();
	}
}
