namespace metvania_1;

/// <summary>
/// Single scrolling level: 150 columns x 22 rows (2400x352 at 16px tiles).
/// Built from 5 sections of 30 columns each, concatenated per row.
/// # = solid, = = one-way, . = background
/// P = player spawn, O = ability orb, S = save point, G = goal
/// c = crawler enemy, f = flyer enemy
/// </summary>
public static class RoomData
{
	// Section 1 (cols 0-29): Gentle Start
	// Left wall, player spawn, one crawler, one flyer.
	// 4-tile gap in the floor teaches basic pit jumping.
	private static readonly string[] Section1 = new[]
	{
		"##############################",  // row 0  — ceiling
		"#.............................",  // row 1
		"#.............................",  // row 2
		"#.............................",  // row 3
		"#.............................",  // row 4
		"#.............................",  // row 5
		"#.............................",  // row 6
		"#.............................",  // row 7
		"#.............................",  // row 8
		"#.............................",  // row 9
		"#.............................",  // row 10
		"#.............................",  // row 11
		"#.......f.....................",  // row 12 — flyer
		"#.............................",  // row 13
		"#.............................",  // row 14
		"#.............................",  // row 15
		"#.............................",  // row 16
		"#..........c..................",  // row 17 — crawler
		"#.............................",  // row 18
		"#..P..........................",  // row 19 — player spawn
		"############....##############",  // row 20 — floor, 4-tile gap
		"############....##############",  // row 21
	};

	// Section 2 (cols 30-59): Stepping Stones
	// Two 5-tile gaps, one-way platforms above for safety.
	// Crawlers on ground, flyer in the air.
	private static readonly string[] Section2 = new[]
	{
		"##############################",  // row 0
		"..............................",  // row 1
		"..............................",  // row 2
		"..............................",  // row 3
		"..............................",  // row 4
		"..............................",  // row 5
		"..............................",  // row 6
		"..............................",  // row 7
		"..............................",  // row 8
		"..............................",  // row 9
		"..............................",  // row 10
		"..............................",  // row 11
		".............f................",  // row 12 — flyer
		"..............................",  // row 13
		"..............................",  // row 14
		"..............................",  // row 15
		"....=====......=====..........",  // row 16 — one-way platforms above gaps
		".c...........c........c.......",  // row 17 — crawlers on ground
		"..............................",  // row 18
		"..............................",  // row 19
		"####.....######.....##########",  // row 20 — floor, two 5-tile gaps
		"####.....######.....##########",  // row 21
	};

	// Section 3 (cols 60-89): The Shrine
	// Save point on left ground. Ascending one-way staircase to double jump orb.
	// 13-tile gap in floor — requires double jump to cross.
	private static readonly string[] Section3 = new[]
	{
		"##############################",  // row 0
		"..............................",  // row 1
		"..............................",  // row 2
		"..............................",  // row 3
		"...........O..................",  // row 4  — double jump orb
		"..........#####...............",  // row 5  — orb pedestal
		"..............................",  // row 6
		".......====...................",  // row 7  — ascending platform
		"..............................",  // row 8
		"....====......................",  // row 9  — ascending platform
		"..............................",  // row 10
		".====.........................",  // row 11 — ascending platform
		"..............................",  // row 12
		"..............................",  // row 13
		"..............................",  // row 14
		"...===........................",  // row 15 — first step up from ground
		"..............................",  // row 16
		"..............................",  // row 17
		"..............................",  // row 18
		"..S...........................",  // row 19 — save point
		"#######.............##########",  // row 20 — floor, 13-tile gap
		"#######.............##########",  // row 21
	};

	// Section 4 (cols 90-119): Deep Pits
	// Two 8-tile gaps, one-way platforms above, crawlers and flyers.
	// Tests double jump mastery.
	private static readonly string[] Section4 = new[]
	{
		"##############################",  // row 0
		"..............................",  // row 1
		"..............................",  // row 2
		"..............................",  // row 3
		"..............................",  // row 4
		"..............................",  // row 5
		"..............................",  // row 6
		"..............................",  // row 7
		"..............................",  // row 8
		"..............................",  // row 9
		"..............................",  // row 10
		"..............................",  // row 11
		"......f...........f...........",  // row 12 — flyers
		"..............................",  // row 13
		"......====........====........",  // row 14 — one-way platforms above gaps
		"..............................",  // row 15
		"..............................",  // row 16
		"..c..........c..........c.....",  // row 17 — crawlers on ground
		"..............................",  // row 18
		"..............................",  // row 19
		"####........####........######",  // row 20 — floor, two 8-tile gaps
		"####........####........######",  // row 21
	};

	// Section 5 (cols 120-149): The Gauntlet
	// Right wall, 13-tile gap requiring double jump, goal near right wall.
	private static readonly string[] Section5 = new[]
	{
		"##############################",  // row 0
		"............................##",  // row 1
		"............................##",  // row 2
		"............................##",  // row 3
		"............................##",  // row 4
		"............................##",  // row 5
		"............................##",  // row 6
		"............................##",  // row 7
		"............................##",  // row 8
		"............................##",  // row 9
		"............................##",  // row 10
		"............................##",  // row 11
		"............................##",  // row 12
		"............................##",  // row 13
		"............................##",  // row 14
		"............................##",  // row 15
		"............................##",  // row 16
		"......................c.....##",  // row 17 — crawler guarding goal
		"............................##",  // row 18
		"..................G.........##",  // row 19 — goal
		"#####.............############",  // row 20 — floor, 13-tile gap
		"#####.............############",  // row 21
	};

	/// <summary>Combined level array (width x height derived from section data).</summary>
	public static readonly string[] Level = BuildLevel();

	public static int LevelWidthTiles => Level[0].Length;
	public static int LevelHeightTiles => Level.Length;

	private static string[] BuildLevel()
	{
		var sections = new[] { Section1, Section2, Section3, Section4, Section5 };
		int rows = Section1.Length;
		var result = new string[rows];

		for (int row = 0; row < rows; row++)
		{
			var sb = new System.Text.StringBuilder(150);
			foreach (var section in sections)
			{
				sb.Append(section[row]);
			}
			result[row] = sb.ToString();
		}

		return result;
	}
}
