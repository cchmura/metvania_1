using Godot;
using System.Text.Json;
using System.Collections.Generic;

namespace metvania_1;

public partial class SaveManager : Node
{
	private const string SaveDir = "user://saves";
	private const string LegacySavePath = "user://save.json";

	public int ActiveSlot { get; set; }

	private class SaveData
	{
		public int Version { get; set; } = 1;
		public List<string> UnlockedAbilities { get; set; } = new();
		public List<string> CollectedItems { get; set; } = new();
		public float PlayerX { get; set; }
		public float PlayerY { get; set; }
		public string CurrentRoom { get; set; } = "";
		public int MaxHealth { get; set; } = 5;
		public bool BossDefeated { get; set; }
		public List<string> VisitedRooms { get; set; } = new();
		public int WeaponTier { get; set; } = 1;
	}

	public class SlotInfo
	{
		public string CurrentRoom { get; set; }
		public int MaxHealth { get; set; }
		public List<string> Abilities { get; set; }
		public bool BossDefeated { get; set; }
	}

	public override void _Ready()
	{
		// Ensure save directory exists
		using var dir = DirAccess.Open("user://");
		dir?.MakeDir("saves");

		MigrateLegacySave();
	}

	private void MigrateLegacySave()
	{
		if (!FileAccess.FileExists(LegacySavePath)) return;

		if (FileAccess.FileExists(GetSlotPath(0)))
		{
			// Already migrated, just remove legacy
			DirAccess.RemoveAbsolute(LegacySavePath);
			return;
		}

		string content;
		using (var src = FileAccess.Open(LegacySavePath, FileAccess.ModeFlags.Read))
		{
			if (src == null) return;
			content = src.GetAsText();
		}

		using (var dst = FileAccess.Open(GetSlotPath(0), FileAccess.ModeFlags.Write))
		{
			dst?.StoreString(content);
		}

		DirAccess.RemoveAbsolute(LegacySavePath);
		GD.Print("Migrated legacy save to slot 0.");
	}

	private string GetSlotPath(int slot) => $"{SaveDir}/slot_{slot}.json";

	public void Save()
	{
		var gameState = GetNode<GameState>("/root/GameState");
		var data = new SaveData
		{
			Version = 1,
			UnlockedAbilities = new List<string>(gameState.UnlockedAbilities),
			CollectedItems = new List<string>(gameState.CollectedItems),
			PlayerX = gameState.PlayerSpawnPosition.X,
			PlayerY = gameState.PlayerSpawnPosition.Y,
			CurrentRoom = gameState.CurrentRoom,
			MaxHealth = gameState.MaxHealth,
			BossDefeated = gameState.BossDefeated,
			WeaponTier = gameState.WeaponTier,
		};

		var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
		using var file = FileAccess.Open(GetSlotPath(ActiveSlot), FileAccess.ModeFlags.Write);
		file?.StoreString(json);
		GD.Print($"Game saved to slot {ActiveSlot}.");
	}

	public void Load()
	{
		string path = GetSlotPath(ActiveSlot);
		if (!FileAccess.FileExists(path))
		{
			GD.Print($"No save file for slot {ActiveSlot}.");
			return;
		}

		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null) return;
		var json = file.GetAsText();
		var data = JsonSerializer.Deserialize<SaveData>(json);
		if (data == null) return;

		var gameState = GetNode<GameState>("/root/GameState");
		gameState.UnlockedAbilities = new HashSet<string>(data.UnlockedAbilities);
		gameState.CollectedItems = new HashSet<string>(data.CollectedItems);
		gameState.PlayerSpawnPosition = new Vector2(data.PlayerX, data.PlayerY);
		gameState.CurrentRoom = data.CurrentRoom;
		gameState.MaxHealth = data.MaxHealth;
		gameState.BossDefeated = data.BossDefeated;
		gameState.WeaponTier = data.WeaponTier > 0 ? data.WeaponTier : 1;

		GD.Print($"Game loaded from slot {ActiveSlot}.");
	}

	public bool HasSaveFile() => FileAccess.FileExists(GetSlotPath(ActiveSlot));

	public bool HasAnySave()
	{
		for (int i = 0; i < 3; i++)
			if (FileAccess.FileExists(GetSlotPath(i)))
				return true;
		return false;
	}

	public SlotInfo GetSlotInfo(int slot)
	{
		string path = GetSlotPath(slot);
		if (!FileAccess.FileExists(path)) return null;

		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null) return null;
		var json = file.GetAsText();
		var data = JsonSerializer.Deserialize<SaveData>(json);
		if (data == null) return null;

		return new SlotInfo
		{
			CurrentRoom = data.CurrentRoom,
			MaxHealth = data.MaxHealth,
			Abilities = data.UnlockedAbilities,
			BossDefeated = data.BossDefeated,
		};
	}

	public void DeleteSlot(int slot)
	{
		string path = GetSlotPath(slot);
		if (FileAccess.FileExists(path))
		{
			DirAccess.RemoveAbsolute(path);
			GD.Print($"Deleted save slot {slot}.");
		}
	}
}
