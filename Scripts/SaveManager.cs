using Godot;
using System.Text.Json;
using System.Collections.Generic;

namespace metvania_1;

public partial class SaveManager : Node
{
	private const string SavePath = "user://save.json";

	private class SaveData
	{
		public List<string> UnlockedAbilities { get; set; } = new();
		public List<string> CollectedItems { get; set; } = new();
		public float PlayerX { get; set; }
		public float PlayerY { get; set; }
		public string CurrentRoom { get; set; } = "";
		public int MaxHealth { get; set; } = 5;
	}

	public void Save()
	{
		var gameState = GetNode<GameState>("/root/GameState");
		var data = new SaveData
		{
			UnlockedAbilities = new List<string>(gameState.UnlockedAbilities),
			CollectedItems = new List<string>(gameState.CollectedItems),
			PlayerX = gameState.PlayerSpawnPosition.X,
			PlayerY = gameState.PlayerSpawnPosition.Y,
			CurrentRoom = gameState.CurrentRoom,
			MaxHealth = gameState.MaxHealth,
		};

		var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
		using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
		file.StoreString(json);
		GD.Print("Game saved.");
	}

	public void Load()
	{
		if (!HasSaveFile())
		{
			GD.Print("No save file found.");
			return;
		}

		using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
		var json = file.GetAsText();
		var data = JsonSerializer.Deserialize<SaveData>(json);
		if (data == null) return;

		var gameState = GetNode<GameState>("/root/GameState");
		gameState.UnlockedAbilities = new HashSet<string>(data.UnlockedAbilities);
		gameState.CollectedItems = new HashSet<string>(data.CollectedItems);
		gameState.PlayerSpawnPosition = new Vector2(data.PlayerX, data.PlayerY);
		gameState.CurrentRoom = data.CurrentRoom;
		gameState.MaxHealth = data.MaxHealth;

		GD.Print("Game loaded.");
	}

	public bool HasSaveFile()
	{
		return FileAccess.FileExists(SavePath);
	}
}
