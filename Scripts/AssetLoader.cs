using System;
using Godot;

namespace metvania_1;

/// <summary>
/// Drop-in replacement for SpriteFactory that checks for custom image files first,
/// falling back to procedural generation when assets are missing.
///
/// Asset directory: res://Assets/{category}/{name}.png
/// Spritesheets: horizontal strips sliced by frame width.
/// </summary>
public static class AssetLoader
{
	private const string AssetRoot = "res://Assets/";

	// ─── Core Helpers ───────────────────────────────────────────────

	private static ImageTexture LoadOrFallback(string path, Func<ImageTexture> fallback)
	{
		string fullPath = AssetRoot + path;
		if (ResourceLoader.Exists(fullPath))
		{
			var img = Image.LoadFromFile(ProjectSettings.GlobalizePath(fullPath));
			if (img != null)
				return ImageTexture.CreateFromImage(img);
		}
		return fallback();
	}

	private static ImageTexture[] LoadFramesOrFallback(string path, int frameWidth, Func<ImageTexture[]> fallback)
	{
		string fullPath = AssetRoot + path;
		if (ResourceLoader.Exists(fullPath))
		{
			var img = Image.LoadFromFile(ProjectSettings.GlobalizePath(fullPath));
			if (img != null)
			{
				int frameCount = img.GetWidth() / frameWidth;
				int h = img.GetHeight();
				var frames = new ImageTexture[frameCount];
				for (int i = 0; i < frameCount; i++)
				{
					var frame = Image.CreateEmpty(frameWidth, h, false, Image.Format.Rgba8);
					frame.BlitRect(img, new Rect2I(i * frameWidth, 0, frameWidth, h), Vector2I.Zero);
					frames[i] = ImageTexture.CreateFromImage(frame);
				}
				return frames;
			}
		}
		return fallback();
	}

	private static Image LoadImageOrFallback(string path, Func<Image> fallback)
	{
		string fullPath = AssetRoot + path;
		if (ResourceLoader.Exists(fullPath))
		{
			var img = Image.LoadFromFile(ProjectSettings.GlobalizePath(fullPath));
			if (img != null)
				return img;
		}
		return fallback();
	}

	private static ImageTexture LoadOrFallbackWithSize(string path, int w, int h, Func<int, int, ImageTexture> fallback)
	{
		string fullPath = AssetRoot + path;
		if (ResourceLoader.Exists(fullPath))
		{
			var img = Image.LoadFromFile(ProjectSettings.GlobalizePath(fullPath));
			if (img != null)
				return ImageTexture.CreateFromImage(img);
		}
		return fallback(w, h);
	}

	// ─── Player Sprites ─────────────────────────────────────────────

	public static ImageTexture[] PlayerIdle()
		=> LoadFramesOrFallback("Player/player_idle.png", 16, SpriteFactory.PlayerIdle);

	public static ImageTexture[] PlayerRun()
		=> LoadFramesOrFallback("Player/player_run.png", 16, SpriteFactory.PlayerRun);

	public static ImageTexture PlayerJump()
		=> LoadOrFallback("Player/player_jump.png", SpriteFactory.PlayerJump);

	public static ImageTexture PlayerFall()
		=> LoadOrFallback("Player/player_fall.png", SpriteFactory.PlayerFall);

	public static ImageTexture PlayerAttackForward()
		=> LoadOrFallback("Player/player_attack_forward.png", SpriteFactory.PlayerAttackForward);

	public static ImageTexture PlayerAttackUp()
		=> LoadOrFallback("Player/player_attack_up.png", SpriteFactory.PlayerAttackUp);

	public static ImageTexture PlayerAttackDown()
		=> LoadOrFallback("Player/player_attack_down.png", SpriteFactory.PlayerAttackDown);

	public static ImageTexture PlayerDash()
		=> LoadOrFallback("Player/player_dash.png", SpriteFactory.PlayerDash);

	public static ImageTexture PlayerWallSlide()
		=> LoadOrFallback("Player/player_wall_slide.png", SpriteFactory.PlayerWallSlide);

	public static ImageTexture PlayerDamaged()
		=> LoadOrFallback("Player/player_damaged.png", SpriteFactory.PlayerDamaged);

	// ─── Enemy Sprites ──────────────────────────────────────────────

	public static ImageTexture CrawlerSprite()
		=> LoadOrFallback("Enemies/crawler.png", SpriteFactory.CrawlerSprite);

	public static ImageTexture FlyerSprite()
		=> LoadOrFallback("Enemies/flyer.png", SpriteFactory.FlyerSprite);

	public static ImageTexture ShooterSprite()
		=> LoadOrFallback("Enemies/shooter.png", SpriteFactory.ShooterSprite);

	public static ImageTexture ChargerSprite()
		=> LoadOrFallback("Enemies/charger.png", SpriteFactory.ChargerSprite);

	public static ImageTexture ShielderSprite()
		=> LoadOrFallback("Enemies/shielder.png", SpriteFactory.ShielderSprite);

	public static ImageTexture DropperSprite()
		=> LoadOrFallback("Enemies/dropper.png", SpriteFactory.DropperSprite);

	public static ImageTexture GuardianSprite()
		=> LoadOrFallback("Enemies/guardian.png", SpriteFactory.GuardianSprite);

	public static ImageTexture BatSprite()
		=> LoadOrFallback("Enemies/bat.png", SpriteFactory.BatSprite);

	public static ImageTexture SkeletonSprite()
		=> LoadOrFallback("Enemies/skeleton.png", SpriteFactory.SkeletonSprite);

	public static ImageTexture GhostSprite()
		=> LoadOrFallback("Enemies/ghost.png", SpriteFactory.GhostSprite);

	public static ImageTexture KnightSprite()
		=> LoadOrFallback("Enemies/knight.png", SpriteFactory.KnightSprite);

	// ─── Projectiles ────────────────────────────────────────────────

	public static ImageTexture ProjectileSprite()
		=> LoadOrFallback("Projectiles/projectile.png", SpriteFactory.ProjectileSprite);

	public static ImageTexture BoneProjectileSprite()
		=> LoadOrFallback("Projectiles/bone_projectile.png", SpriteFactory.BoneProjectileSprite);

	// ─── Objects ────────────────────────────────────────────────────

	public static ImageTexture AbilityOrbSprite()
		=> LoadOrFallback("Objects/ability_orb.png", SpriteFactory.AbilityOrbSprite);

	public static ImageTexture SavePointSprite()
		=> LoadOrFallback("Objects/save_point.png", SpriteFactory.SavePointSprite);

	public static ImageTexture SpikesSprite()
		=> LoadOrFallback("Objects/spikes.png", SpriteFactory.SpikesSprite);

	public static ImageTexture PlatformSprite()
		=> LoadOrFallback("Objects/platform.png", SpriteFactory.PlatformSprite);

	public static ImageTexture CandleSprite()
		=> LoadOrFallback("Objects/candle.png", SpriteFactory.CandleSprite);

	// ─── Pickups ────────────────────────────────────────────────────

	public static ImageTexture WeaponUpgradeSprite()
		=> LoadOrFallback("Pickups/weapon_upgrade.png", SpriteFactory.WeaponUpgradeSprite);

	public static ImageTexture MaxHealthUpgradeSprite()
		=> LoadOrFallback("Pickups/max_health_upgrade.png", SpriteFactory.MaxHealthUpgradeSprite);

	// ─── Tiles ──────────────────────────────────────────────────────

	public static Image CreateTileAtlas()
		=> LoadImageOrFallback("Tiles/tile_atlas.png", SpriteFactory.CreateTileAtlas);

	// ─── Parallax ───────────────────────────────────────────────────

	public static ImageTexture ParallaxFarLayer(int w, int h)
		=> LoadOrFallbackWithSize("Parallax/far.png", w, h, SpriteFactory.ParallaxFarLayer);

	public static ImageTexture ParallaxMidLayer(int w, int h)
		=> LoadOrFallbackWithSize("Parallax/mid.png", w, h, SpriteFactory.ParallaxMidLayer);

	public static ImageTexture ParallaxNearLayer(int w, int h)
		=> LoadOrFallbackWithSize("Parallax/near.png", w, h, SpriteFactory.ParallaxNearLayer);
}
