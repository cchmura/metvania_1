using System.Collections.Generic;

namespace metvania_1;

/// <summary>
/// Single-room level data (Castlevania-style linear stage).
/// # = solid, = = one-way, . = background
/// P = player spawn, O = ability orb, D = dash orb, S = save point, G = goal
/// c = crawler, f = flyer, s = shooter, r = charger, h = shielder, d = dropper
/// b = bat, k = skeleton, g = ghost, n = knight
/// x = floor spikes, B = boss spawn, L = boss lock trigger
/// H = horizontal moving platform, V = vertical moving platform, F = falling platform
/// M = max health upgrade, C = breakable candle
/// </summary>
public static class RoomData
{
	public class RoomDef
	{
		public string Id;
		public string DisplayName;
		public string[] Layout;
	}

	// ─── Zone Layouts (24×11 each) ────────────────────────────────

	// Zone 1: Courtyard (open sky)
	private static readonly string[] Zone1 = new[]
	{
		"#.......................",  // row 0
		"........................",  // row 1
		"........................",  // row 2
		"........................",  // row 3
		".............f.....b....",  // row 4
		"........................",  // row 5
		"........................",  // row 6
		"........................",  // row 7
		"..P.....C...c.....c.....",  // row 8
		"########################",  // row 9
		"########################",  // row 10
	};

	// Zone 2: Castle Entrance (ceiling starts)
	private static readonly string[] Zone2 = new[]
	{
		"########################",  // row 0
		"........................",  // row 1
		"..........====..........",  // row 2
		"..............b.........",  // row 3
		"............f...........",  // row 4
		".......====.............",  // row 5
		"........................",  // row 6
		".S...............C......",  // row 7
		"....c..x....k...c.......",  // row 8
		"####..####..############",  // row 9
		"########################",  // row 10
	};

	// Zone 3: Great Hall (enclosed)
	private static readonly string[] Zone3 = new[]
	{
		"########################",  // row 0
		"........................",  // row 1
		"..====..........====....",  // row 2
		".........g..............",  // row 3
		"......O.....s...........",  // row 4
		"..........H.............",  // row 5
		"...........h.....C......",  // row 6
		"........................",  // row 7
		"..r........n.....c......",  // row 8
		"########################",  // row 9
		"########################",  // row 10
	};

	// Zone 4: Inner Corridor
	private static readonly string[] Zone4 = new[]
	{
		"########################",  // row 0
		"........................",  // row 1
		"..====..........d.......",  // row 2
		"...........b............",  // row 3
		"......D.................",  // row 4
		"..........F.............",  // row 5
		".......s.......g........",  // row 6
		"...C....................",  // row 7
		"..k.x..x......k...c.x...",  // row 8
		"####..####..####..######",  // row 9
		"########################",  // row 10
	};

	// Zone 5: Boss Chamber
	private static readonly string[] Zone5 = new[]
	{
		"########################",  // row 0
		".......................#",  // row 1
		".......................#",  // row 2
		".......................#",  // row 3
		".......................#",  // row 4
		".......................#",  // row 5
		".......................#",  // row 6
		".......................#",  // row 7
		"..L........B...........#",  // row 8
		"########################",  // row 9
		"########################",  // row 10
	};

	/// <summary>Combined main level array (120×11).</summary>
	public static readonly string[] Level = BuildLevel();

	/// <summary>Room registry.</summary>
	public static readonly Dictionary<string, RoomDef> Rooms = BuildRooms();

	public static int LevelWidthTiles => Level[0].Length;
	public static int LevelHeightTiles => Level.Length;

	private static string[] BuildLevel()
	{
		var zones = new[] { Zone1, Zone2, Zone3, Zone4, Zone5 };
		int rows = Zone1.Length;
		var result = new string[rows];

		for (int row = 0; row < rows; row++)
		{
			var sb = new System.Text.StringBuilder(120);
			foreach (var zone in zones)
			{
				sb.Append(zone[row]);
			}
			result[row] = sb.ToString();
		}

		return result;
	}

	private static Dictionary<string, RoomDef> BuildRooms()
	{
		var rooms = new Dictionary<string, RoomDef>();

		var main = new RoomDef
		{
			Id = "main",
			DisplayName = "The Depths",
			Layout = BuildLevel(),
		};
		rooms["main"] = main;

		return rooms;
	}
}
