using Godot;

namespace metvania_1;

public static class TileMapBuilder
{
	public const int TileSize = 16;

	// Tile IDs in the atlas
	public static readonly Vector2I SolidTile = new(0, 0);
	public static readonly Vector2I OneWayTile = new(1, 0);
	public static readonly Vector2I BackgroundTile = new(2, 0);

	public static TileMapLayer CreateTileMapLayer()
	{
		var tileSet = CreateTileSet();
		var layer = new TileMapLayer();
		layer.TileSet = tileSet;
		layer.CollisionEnabled = true;
		// World geometry on layer 1
		layer.CollisionVisibilityMode = TileMapLayer.DebugVisibilityMode.Default;
		return layer;
	}

	private static TileSet CreateTileSet()
	{
		var tileSet = new TileSet();
		tileSet.TileSize = new Vector2I(TileSize, TileSize);

		// Add physics layer — world geometry (layer bit 0 = layer 1)
		tileSet.AddPhysicsLayer();
		tileSet.SetPhysicsLayerCollisionLayer(0, 1); // Layer 1: World
		tileSet.SetPhysicsLayerCollisionMask(0, 0);  // Static — no mask needed

		// Create the atlas image: 48x16 (3 tiles wide, 1 tile tall)
		var image = Image.CreateEmpty(48, TileSize, false, Image.Format.Rgba8);

		// Tile 0: Solid — brown
		var solidColor = new Color(0.35f, 0.28f, 0.2f);
		FillTileRegion(image, 0, solidColor);

		// Tile 1: One-way — lighter brown
		var oneWayColor = new Color(0.45f, 0.38f, 0.3f);
		FillTileRegion(image, 1, oneWayColor);

		// Tile 2: Background — dark
		var bgColor = new Color(0.12f, 0.1f, 0.15f);
		FillTileRegion(image, 2, bgColor);

		var texture = ImageTexture.CreateFromImage(image);

		// Create atlas source
		var source = new TileSetAtlasSource();
		source.Texture = texture;
		source.TextureRegionSize = new Vector2I(TileSize, TileSize);

		// Create tiles in the atlas
		// Solid tile (0,0)
		source.CreateTile(SolidTile);
		// One-way tile (1,0)
		source.CreateTile(OneWayTile);
		// Background tile (2,0) — no collision
		source.CreateTile(BackgroundTile);

		int sourceId = tileSet.AddSource(source);

		// Set up collision polygons after tiles are created
		// Solid tile — full collision
		var solidData = source.GetTileData(SolidTile, 0);
		solidData.AddCollisionPolygon(0);
		solidData.SetCollisionPolygonPoints(0, 0, new Vector2[]
		{
			new(-8, -8), new(8, -8), new(8, 8), new(-8, 8)
		});

		// One-way tile — top-half collision with one-way enabled
		var oneWayData = source.GetTileData(OneWayTile, 0);
		oneWayData.AddCollisionPolygon(0);
		oneWayData.SetCollisionPolygonPoints(0, 0, new Vector2[]
		{
			new(-8, -8), new(8, -8), new(8, 0), new(-8, 0)
		});
		oneWayData.SetCollisionPolygonOneWay(0, 0, true);

		// Background tile — no collision (nothing to set)

		return tileSet;
	}

	private static void FillTileRegion(Image image, int tileIndex, Color color)
	{
		int startX = tileIndex * TileSize;
		for (int y = 0; y < TileSize; y++)
		{
			for (int x = 0; x < TileSize; x++)
			{
				image.SetPixel(startX + x, y, color);
			}
		}
	}
}
