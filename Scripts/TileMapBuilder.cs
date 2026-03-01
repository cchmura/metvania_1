using Godot;

namespace metvania_1;

public static class TileMapBuilder
{
	public const int TileSize = 16;

	// Tile IDs in the atlas
	public static readonly Vector2I SolidTile = new(0, 0);
	public static readonly Vector2I OneWayTile = new(1, 0);
	public static readonly Vector2I BackgroundTile = new(2, 0);

	// Edge variants (cosmetic — same collision as SolidTile)
	public static readonly Vector2I SolidTopEdge = new(3, 0);
	public static readonly Vector2I SolidBottomEdge = new(4, 0);
	public static readonly Vector2I SolidLeftEdge = new(5, 0);
	public static readonly Vector2I SolidRightEdge = new(6, 0);
	public static readonly Vector2I SolidTopLeftCorner = new(7, 0);
	public static readonly Vector2I SolidTopRightCorner = new(8, 0);

	public static TileMapLayer CreateTileMapLayer()
	{
		var tileSet = CreateTileSet();
		var layer = new TileMapLayer();
		layer.TileSet = tileSet;
		layer.CollisionEnabled = true;
		layer.CollisionVisibilityMode = TileMapLayer.DebugVisibilityMode.Default;
		return layer;
	}

	private static TileSet CreateTileSet()
	{
		var tileSet = new TileSet();
		tileSet.TileSize = new Vector2I(TileSize, TileSize);

		// Add physics layer
		tileSet.AddPhysicsLayer();
		tileSet.SetPhysicsLayerCollisionLayer(0, 1);
		tileSet.SetPhysicsLayerCollisionMask(0, 0);

		// Use SpriteFactory to generate the expanded atlas
		var image = AssetLoader.CreateTileAtlas();
		var texture = ImageTexture.CreateFromImage(image);

		var source = new TileSetAtlasSource();
		source.Texture = texture;
		source.TextureRegionSize = new Vector2I(TileSize, TileSize);

		// Create all 9 tiles
		source.CreateTile(SolidTile);
		source.CreateTile(OneWayTile);
		source.CreateTile(BackgroundTile);
		source.CreateTile(SolidTopEdge);
		source.CreateTile(SolidBottomEdge);
		source.CreateTile(SolidLeftEdge);
		source.CreateTile(SolidRightEdge);
		source.CreateTile(SolidTopLeftCorner);
		source.CreateTile(SolidTopRightCorner);

		tileSet.AddSource(source);

		// Full collision for solid tiles (base + all edge variants)
		var solidCoords = new[] {
			SolidTile, SolidTopEdge, SolidBottomEdge, SolidLeftEdge,
			SolidRightEdge, SolidTopLeftCorner, SolidTopRightCorner
		};
		foreach (var coord in solidCoords)
		{
			var data = source.GetTileData(coord, 0);
			data.AddCollisionPolygon(0);
			data.SetCollisionPolygonPoints(0, 0, new Vector2[]
			{
				new(-8, -8), new(8, -8), new(8, 8), new(-8, 8)
			});
		}

		// One-way collision
		var oneWayData = source.GetTileData(OneWayTile, 0);
		oneWayData.AddCollisionPolygon(0);
		oneWayData.SetCollisionPolygonPoints(0, 0, new Vector2[]
		{
			new(-8, -8), new(8, -8), new(8, 0), new(-8, 0)
		});
		oneWayData.SetCollisionPolygonOneWay(0, 0, true);

		return tileSet;
	}

	/// <summary>
	/// Post-process solid tiles to use edge variants based on neighbors.
	/// Call after painting the level.
	/// </summary>
	public static void PostProcessTileEdges(TileMapLayer tileMap, string[] layout)
	{
		int rows = layout.Length;
		int cols = layout[0].Length;

		for (int row = 0; row < rows; row++)
		{
			for (int col = 0; col < cols; col++)
			{
				char c = layout[row][col];
				if (c != '#') continue;

				var coord = new Vector2I(col, row);
				bool aboveEmpty = row == 0 || !IsSolid(layout, row - 1, col);
				bool belowEmpty = row == rows - 1 || !IsSolid(layout, row + 1, col);
				bool leftEmpty = col == 0 || !IsSolid(layout, row, col - 1);
				bool rightEmpty = col == cols - 1 || !IsSolid(layout, row, col + 1);

				Vector2I tileId;
				if (aboveEmpty && leftEmpty)
					tileId = SolidTopLeftCorner;
				else if (aboveEmpty && rightEmpty)
					tileId = SolidTopRightCorner;
				else if (aboveEmpty)
					tileId = SolidTopEdge;
				else if (belowEmpty)
					tileId = SolidBottomEdge;
				else if (leftEmpty)
					tileId = SolidLeftEdge;
				else if (rightEmpty)
					tileId = SolidRightEdge;
				else
					continue; // Interior tile — keep base solid

				tileMap.SetCell(coord, 0, tileId);
			}
		}
	}

	private static bool IsSolid(string[] layout, int row, int col)
	{
		if (row < 0 || row >= layout.Length || col < 0 || col >= layout[0].Length)
			return false;
		char c = layout[row][col];
		return c == '#' || c == '=';
	}
}
