using Godot;
using System.Collections.Generic;

namespace metvania_1;

public static class RoomData
{
	public class RoomDef
	{
		public string Id;
		public string DisplayName;
		public string Music;
		public string[] Layout;
	}

	private static readonly Dictionary<string, RoomDef> _cache = new();

	public static string[] Level => GetLevel().Layout;
	public static int LevelWidthTiles => Level[0].Length;
	public static int LevelHeightTiles => Level.Length;

	// Backward compat for Hud map code
	public static Dictionary<string, RoomDef> Rooms
	{
		get
		{
			var level = GetLevel();
			return new Dictionary<string, RoomDef> { [level.Id] = level };
		}
	}

	public static RoomDef GetLevel(string levelId = "main")
	{
		if (_cache.TryGetValue(levelId, out var cached))
			return cached;

		var loaded = LoadLevel(levelId);
		_cache[levelId] = loaded;
		return loaded;
	}

	private static RoomDef LoadLevel(string levelId)
	{
		var path = $"res://Levels/{levelId}.json";
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PrintErr($"RoomData: Failed to load level file: {path}");
			return new RoomDef
			{
				Id = levelId,
				DisplayName = "Error",
				Music = "depths",
				Layout = new[] { "####################", "..P.................", "####################" },
			};
		}

		var jsonText = file.GetAsText();
		var json = new Json();
		var error = json.Parse(jsonText);
		if (error != Error.Ok)
		{
			GD.PrintErr($"RoomData: JSON parse error in {path}: {json.GetErrorMessage()}");
			return new RoomDef
			{
				Id = levelId,
				DisplayName = "Error",
				Music = "depths",
				Layout = new[] { "####################", "..P.................", "####################" },
			};
		}

		var data = json.Data.AsGodotDictionary();
		var layoutArray = data["layout"].AsGodotArray<string>();
		var layout = new string[layoutArray.Count];
		for (int i = 0; i < layoutArray.Count; i++)
			layout[i] = layoutArray[i];

		return new RoomDef
		{
			Id = data.ContainsKey("id") ? data["id"].AsString() : levelId,
			DisplayName = data.ContainsKey("name") ? data["name"].AsString() : levelId,
			Music = data.ContainsKey("music") ? data["music"].AsString() : "depths",
			Layout = layout,
		};
	}
}
