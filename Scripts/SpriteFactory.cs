using Godot;

namespace metvania_1;

/// <summary>
/// Generates all pixel art as ImageTexture objects procedurally.
/// Sprites defined as byte[,] arrays indexed into color palettes.
/// </summary>
public static class SpriteFactory
{
	// 0 = transparent
	private static readonly Color Transparent = new(0, 0, 0, 0);

	public static ImageTexture CreateTexture(byte[,] pixels, Color[] palette)
	{
		int h = pixels.GetLength(0), w = pixels.GetLength(1);
		var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
		for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
				img.SetPixel(x, y, palette[pixels[y, x]]);
		return ImageTexture.CreateFromImage(img);
	}

	// ─── Player Sprites (16x32) ─────────────────────────────────────

	private static readonly Color[] PlayerPalette = new[]
	{
		new Color(0, 0, 0, 0),           // 0: transparent
		new Color(0.15f, 0.45f, 0.75f),   // 1: body blue
		new Color(0.2f, 0.55f, 0.85f),    // 2: body light blue
		new Color(0.1f, 0.35f, 0.6f),     // 3: body dark blue
		new Color(0.9f, 0.75f, 0.6f),     // 4: skin
		new Color(0.8f, 0.65f, 0.5f),     // 5: skin shadow
		new Color(0.2f, 0.2f, 0.25f),     // 6: hair/dark
		new Color(0.85f, 0.85f, 0.9f),    // 7: eye white
		new Color(0.12f, 0.12f, 0.15f),   // 8: outline
		new Color(0.25f, 0.6f, 0.9f),     // 9: highlight
	};

	public static ImageTexture[] PlayerIdle()
	{
		// Frame 1: standing
		var f1 = new byte[,]
		{
			{0,0,0,0,0,0,8,8,8,8,0,0,0,0,0,0}, // row 0:  hair top
			{0,0,0,0,0,8,6,6,6,6,8,0,0,0,0,0}, // row 1:  hair
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 2:  hair wide
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 3:  hair lower
			{0,0,0,0,8,4,4,7,7,4,4,8,0,0,0,0}, // row 4:  eyes
			{0,0,0,0,8,5,4,8,8,4,5,8,0,0,0,0}, // row 5:  pupils
			{0,0,0,0,0,8,4,4,4,4,8,0,0,0,0,0}, // row 6:  nose/mouth
			{0,0,0,0,0,8,5,4,4,5,8,0,0,0,0,0}, // row 7:  chin
			{0,0,0,0,0,8,4,5,5,4,8,0,0,0,0,0}, // row 8:  neck
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 9:  shoulders
			{0,0,0,8,1,1,2,1,1,2,1,1,8,0,0,0}, // row 10: upper torso
			{0,0,0,8,1,1,2,1,1,2,1,1,8,0,0,0}, // row 11: torso
			{0,0,0,8,1,1,1,1,1,1,1,1,8,0,0,0}, // row 12: torso
			{0,0,0,8,3,1,1,1,1,1,1,3,8,0,0,0}, // row 13: lower torso
			{0,0,0,8,3,1,9,1,1,9,1,3,8,0,0,0}, // row 14: belt detail
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 15: waist
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 16: waist
			{0,0,0,0,8,3,3,1,1,3,3,8,0,0,0,0}, // row 17: hip
			{0,0,0,0,0,8,8,1,1,8,8,0,0,0,0,0}, // row 18: leg split
			{0,0,0,0,0,8,3,1,1,3,8,0,0,0,0,0}, // row 19: upper leg
			{0,0,0,0,0,8,3,1,1,3,8,0,0,0,0,0}, // row 20: upper leg
			{0,0,0,0,0,8,3,0,0,3,8,0,0,0,0,0}, // row 21: mid leg
			{0,0,0,0,0,8,3,0,0,3,8,0,0,0,0,0}, // row 22: mid leg
			{0,0,0,0,8,3,3,0,0,3,3,8,0,0,0,0}, // row 23: knee
			{0,0,0,0,8,1,1,0,0,1,1,8,0,0,0,0}, // row 24: lower leg
			{0,0,0,0,8,1,1,0,0,1,1,8,0,0,0,0}, // row 25: lower leg
			{0,0,0,0,8,1,1,0,0,1,1,8,0,0,0,0}, // row 26: shin
			{0,0,0,0,8,1,1,0,0,1,1,8,0,0,0,0}, // row 27: shin
			{0,0,0,0,8,3,1,0,0,1,3,8,0,0,0,0}, // row 28: ankle
			{0,0,0,8,8,1,1,0,0,1,1,8,8,0,0,0}, // row 29: boot top
			{0,0,0,8,1,1,8,0,0,8,1,1,8,0,0,0}, // row 30: boot
			{0,0,0,8,8,8,8,0,0,8,8,8,8,0,0,0}, // row 31: boot sole
		};
		// Frame 2: slight bob (shift body down by 1, feet wider)
		var f2 = new byte[,]
		{
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 0:  empty (bob down)
			{0,0,0,0,0,0,8,8,8,8,0,0,0,0,0,0}, // row 1:  hair top
			{0,0,0,0,0,8,6,6,6,6,8,0,0,0,0,0}, // row 2:  hair
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 3:  hair wide
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 4:  hair lower
			{0,0,0,0,8,4,4,7,7,4,4,8,0,0,0,0}, // row 5:  eyes
			{0,0,0,0,8,5,4,8,8,4,5,8,0,0,0,0}, // row 6:  pupils
			{0,0,0,0,0,8,4,4,4,4,8,0,0,0,0,0}, // row 7:  nose/mouth
			{0,0,0,0,0,8,5,4,4,5,8,0,0,0,0,0}, // row 8:  chin
			{0,0,0,0,0,8,4,5,5,4,8,0,0,0,0,0}, // row 9:  neck
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 10: shoulders
			{0,0,0,8,1,1,2,1,1,2,1,1,8,0,0,0}, // row 11: upper torso
			{0,0,0,8,1,1,2,1,1,2,1,1,8,0,0,0}, // row 12: torso
			{0,0,0,8,1,1,1,1,1,1,1,1,8,0,0,0}, // row 13: torso
			{0,0,0,8,3,1,1,1,1,1,1,3,8,0,0,0}, // row 14: lower torso
			{0,0,0,8,3,1,9,1,1,9,1,3,8,0,0,0}, // row 15: belt detail
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 16: waist
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 17: waist
			{0,0,0,0,8,3,3,1,1,3,3,8,0,0,0,0}, // row 18: hip
			{0,0,0,0,0,8,8,1,1,8,8,0,0,0,0,0}, // row 19: leg split
			{0,0,0,0,0,8,3,1,1,3,8,0,0,0,0,0}, // row 20: upper leg
			{0,0,0,0,0,8,3,1,1,3,8,0,0,0,0,0}, // row 21: upper leg
			{0,0,0,0,0,8,3,0,0,3,8,0,0,0,0,0}, // row 22: mid leg
			{0,0,0,0,0,8,3,0,0,3,8,0,0,0,0,0}, // row 23: mid leg
			{0,0,0,0,8,3,3,0,0,3,3,8,0,0,0,0}, // row 24: knee
			{0,0,0,0,8,1,1,0,0,1,1,8,0,0,0,0}, // row 25: lower leg
			{0,0,0,0,8,1,1,0,0,1,1,8,0,0,0,0}, // row 26: lower leg
			{0,0,0,0,8,1,1,0,0,1,1,8,0,0,0,0}, // row 27: shin
			{0,0,0,0,8,3,1,0,0,1,3,8,0,0,0,0}, // row 28: ankle
			{0,0,0,8,8,1,1,0,0,1,1,8,8,0,0,0}, // row 29: boot top
			{0,0,8,8,1,1,8,0,0,8,1,1,8,8,0,0}, // row 30: boot (wider)
			{0,0,8,8,8,8,0,0,0,0,8,8,8,8,0,0}, // row 31: boot sole (wider)
		};
		return new[] { CreateTexture(f1, PlayerPalette), CreateTexture(f2, PlayerPalette) };
	}

	public static ImageTexture[] PlayerRun()
	{
		// 4 running frames with leg animation
		var basePixels = new byte[,]
		{
			{0,0,0,0,0,0,8,8,8,8,0,0,0,0,0,0}, // row 0:  hair top
			{0,0,0,0,0,8,6,6,6,6,8,0,0,0,0,0}, // row 1:  hair
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 2:  hair wide
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 3:  hair lower
			{0,0,0,0,8,4,4,7,7,4,4,8,0,0,0,0}, // row 4:  eyes
			{0,0,0,0,8,5,4,8,8,4,5,8,0,0,0,0}, // row 5:  pupils
			{0,0,0,0,0,8,4,4,4,4,8,0,0,0,0,0}, // row 6:  nose/mouth
			{0,0,0,0,0,8,5,4,4,5,8,0,0,0,0,0}, // row 7:  chin
			{0,0,0,0,0,8,4,5,5,4,8,0,0,0,0,0}, // row 8:  neck
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 9:  shoulders
			{0,0,0,8,1,1,2,1,1,2,1,1,8,0,0,0}, // row 10: upper torso
			{0,0,0,8,1,1,2,1,1,2,1,1,8,0,0,0}, // row 11: torso
			{0,0,0,8,1,1,1,1,1,1,1,1,8,0,0,0}, // row 12: torso
			{0,0,0,8,3,1,1,1,1,1,1,3,8,0,0,0}, // row 13: lower torso
			{0,0,0,8,3,1,9,1,1,9,1,3,8,0,0,0}, // row 14: belt detail
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 15: waist
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 16: waist
			{0,0,0,0,8,3,3,1,1,3,3,8,0,0,0,0}, // row 17: hip
			{0,0,0,0,0,8,8,1,1,8,8,0,0,0,0,0}, // row 18: leg split
			{0,0,0,0,0,8,3,1,1,3,8,0,0,0,0,0}, // row 19: upper leg
			{0,0,0,0,0,8,3,1,1,3,8,0,0,0,0,0}, // row 20: upper leg
			{0,0,0,0,0,8,3,0,0,3,8,0,0,0,0,0}, // row 21: mid leg
			{0,0,0,0,0,8,3,0,0,3,8,0,0,0,0,0}, // row 22: mid leg
			{0,0,0,0,8,3,3,0,0,3,3,8,0,0,0,0}, // row 23: knee
			{0,0,0,0,8,1,1,0,0,1,1,8,0,0,0,0}, // row 24: lower leg
			{0,0,0,0,8,1,1,0,0,1,1,8,0,0,0,0}, // row 25: lower leg
			{0,0,0,0,8,1,1,0,0,1,1,8,0,0,0,0}, // row 26: shin
			{0,0,0,0,8,1,1,0,0,1,1,8,0,0,0,0}, // row 27: shin
			{0,0,0,0,8,3,1,0,0,1,3,8,0,0,0,0}, // row 28: ankle
			{0,0,0,8,8,1,1,0,0,1,1,8,8,0,0,0}, // row 29: boot top
			{0,0,0,8,1,1,8,0,0,8,1,1,8,0,0,0}, // row 30: boot
			{0,0,0,8,8,8,8,0,0,8,8,8,8,0,0,0}, // row 31: boot sole
		};

		// Leg variations for running (rows 28-31, cols 4,5 = left foot, cols 10,11 = right foot)
		var legs = new byte[4][,];
		// Frame 0: right leg forward
		legs[0] = ClonePixels(basePixels);
		legs[0][28,4] = 0; legs[0][28,5] = 0; legs[0][28,10] = 3; legs[0][28,11] = 3;
		legs[0][29,4] = 0; legs[0][29,5] = 0; legs[0][29,10] = 1; legs[0][29,11] = 1;
		legs[0][30,4] = 0; legs[0][30,5] = 0; legs[0][30,10] = 1; legs[0][30,11] = 1;
		legs[0][31,4] = 0; legs[0][31,5] = 0; legs[0][31,10] = 8; legs[0][31,11] = 8;
		// Frame 1: neutral
		legs[1] = ClonePixels(basePixels);
		// Frame 2: left leg forward
		legs[2] = ClonePixels(basePixels);
		legs[2][28,4] = 3; legs[2][28,5] = 3; legs[2][28,10] = 0; legs[2][28,11] = 0;
		legs[2][29,4] = 1; legs[2][29,5] = 1; legs[2][29,10] = 0; legs[2][29,11] = 0;
		legs[2][30,4] = 1; legs[2][30,5] = 1; legs[2][30,10] = 0; legs[2][30,11] = 0;
		legs[2][31,4] = 8; legs[2][31,5] = 8; legs[2][31,10] = 0; legs[2][31,11] = 0;
		// Frame 3: neutral again
		legs[3] = ClonePixels(basePixels);

		var textures = new ImageTexture[4];
		for (int i = 0; i < 4; i++)
			textures[i] = CreateTexture(legs[i], PlayerPalette);
		return textures;
	}

	public static ImageTexture PlayerJump()
	{
		var px = new byte[,]
		{
			{0,0,0,0,0,0,8,8,8,8,0,0,0,0,0,0}, // row 0:  hair top
			{0,0,0,0,0,8,6,6,6,6,8,0,0,0,0,0}, // row 1:  hair
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 2:  hair wide
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 3:  hair lower
			{0,0,0,0,8,4,4,7,7,4,4,8,0,0,0,0}, // row 4:  eyes
			{0,0,0,0,8,5,4,8,8,4,5,8,0,0,0,0}, // row 5:  pupils
			{0,0,0,0,0,8,4,4,4,4,8,0,0,0,0,0}, // row 6:  nose/mouth
			{0,0,0,0,0,8,5,4,4,5,8,0,0,0,0,0}, // row 7:  chin
			{0,0,0,0,0,8,4,5,5,4,8,0,0,0,0,0}, // row 8:  neck
			{0,0,0,8,8,1,1,1,1,1,1,8,8,0,0,0}, // row 9:  shoulders (arms up hint)
			{0,0,8,9,1,1,2,1,1,2,1,1,9,8,0,0}, // row 10: upper torso (arms out)
			{0,0,0,8,1,1,2,1,1,2,1,1,8,0,0,0}, // row 11: torso
			{0,0,0,8,1,1,1,1,1,1,1,1,8,0,0,0}, // row 12: torso
			{0,0,0,8,3,1,1,1,1,1,1,3,8,0,0,0}, // row 13: lower torso
			{0,0,0,0,8,1,9,1,1,9,1,8,0,0,0,0}, // row 14: belt
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 15: waist
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 16: waist
			{0,0,0,0,8,3,3,1,1,3,3,8,0,0,0,0}, // row 17: hip
			{0,0,0,0,0,8,3,1,1,3,8,0,0,0,0,0}, // row 18: upper leg
			{0,0,0,0,8,3,3,1,1,3,8,0,0,0,0,0}, // row 19: upper leg
			{0,0,0,8,3,1,0,0,0,0,3,8,0,0,0,0}, // row 20: legs apart
			{0,0,0,8,1,1,0,0,0,0,1,8,0,0,0,0}, // row 21: legs apart
			{0,0,0,8,1,1,0,0,0,0,1,8,0,0,0,0}, // row 22: lower leg
			{0,0,0,8,8,0,0,0,0,0,1,8,0,0,0,0}, // row 23: tucked left
			{0,0,0,0,0,0,0,0,0,0,8,8,0,0,0,0}, // row 24: tucked left
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 25: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 26: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 27: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 28: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 29: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 30: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 31: empty
		};
		return CreateTexture(px, PlayerPalette);
	}

	public static ImageTexture PlayerFall()
	{
		var px = new byte[,]
		{
			{0,0,0,0,0,0,8,8,8,8,0,0,0,0,0,0}, // row 0:  hair top
			{0,0,0,0,0,8,6,6,6,6,8,0,0,0,0,0}, // row 1:  hair
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 2:  hair wide
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 3:  hair lower
			{0,0,0,0,8,4,4,7,7,4,4,8,0,0,0,0}, // row 4:  eyes
			{0,0,0,0,8,5,4,8,8,4,5,8,0,0,0,0}, // row 5:  pupils
			{0,0,0,0,0,8,4,4,4,4,8,0,0,0,0,0}, // row 6:  nose/mouth
			{0,0,0,0,0,8,5,4,4,5,8,0,0,0,0,0}, // row 7:  chin
			{0,0,0,0,0,8,4,5,5,4,8,0,0,0,0,0}, // row 8:  neck
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 9:  shoulders
			{0,0,8,9,1,1,2,1,1,2,1,1,9,8,0,0}, // row 10: arms out
			{0,0,0,8,1,1,2,1,1,2,1,1,8,0,0,0}, // row 11: torso
			{0,0,0,8,1,1,1,1,1,1,1,1,8,0,0,0}, // row 12: torso
			{0,0,0,8,3,1,1,1,1,1,1,3,8,0,0,0}, // row 13: lower torso
			{0,0,0,0,8,1,9,1,1,9,1,8,0,0,0,0}, // row 14: belt
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 15: waist
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 16: waist
			{0,0,0,0,8,3,3,1,1,3,3,8,0,0,0,0}, // row 17: hip
			{0,0,0,0,0,8,3,1,1,3,8,0,0,0,0,0}, // row 18: upper leg
			{0,0,0,0,0,8,3,1,1,3,8,0,0,0,0,0}, // row 19: upper leg
			{0,0,0,0,8,3,0,0,0,0,3,3,8,0,0,0}, // row 20: legs spread
			{0,0,0,8,1,1,0,0,0,0,1,1,8,0,0,0}, // row 21: legs spread
			{0,0,0,8,1,0,0,0,0,0,0,1,8,0,0,0}, // row 22: wide
			{0,0,0,8,8,0,0,0,0,0,0,8,8,0,0,0}, // row 23: feet out
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 24: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 25: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 26: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 27: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 28: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 29: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 30: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 31: empty
		};
		return CreateTexture(px, PlayerPalette);
	}

	public static ImageTexture PlayerAttackForward()
	{
		var px = new byte[,]
		{
			{0,0,0,0,0,0,8,8,8,8,0,0,0,0,0,0}, // row 0:  hair top
			{0,0,0,0,0,8,6,6,6,6,8,0,0,0,0,0}, // row 1:  hair
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 2:  hair wide
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 3:  hair lower
			{0,0,0,0,8,4,4,7,7,4,4,8,0,0,0,0}, // row 4:  eyes
			{0,0,0,0,8,5,4,8,8,4,5,8,0,0,0,0}, // row 5:  pupils
			{0,0,0,0,0,8,4,4,4,4,8,0,0,0,0,0}, // row 6:  nose/mouth
			{0,0,0,0,0,8,5,4,4,5,8,0,0,0,0,0}, // row 7:  chin
			{0,0,0,0,0,8,4,5,5,4,8,0,0,0,0,0}, // row 8:  neck
			{0,0,0,0,8,1,1,1,1,1,1,8,8,8,0,0}, // row 9:  shoulders + arm ext
			{0,0,0,8,1,1,2,1,1,2,1,1,9,9,8,0}, // row 10: torso + slash
			{0,0,0,8,1,1,2,1,1,2,1,1,9,9,8,0}, // row 11: torso + slash
			{0,0,0,8,1,1,1,1,1,1,1,1,8,8,0,0}, // row 12: torso
			{0,0,0,8,3,1,1,1,1,1,1,3,8,0,0,0}, // row 13: lower torso
			{0,0,0,8,3,1,9,1,1,9,1,3,8,0,0,0}, // row 14: belt detail
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 15: waist
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 16: waist
			{0,0,0,0,8,3,3,1,1,3,3,8,0,0,0,0}, // row 17: hip
			{0,0,0,0,0,8,8,1,1,8,8,0,0,0,0,0}, // row 18: leg split
			{0,0,0,0,0,8,3,1,1,3,8,0,0,0,0,0}, // row 19: upper leg
			{0,0,0,0,0,8,3,1,1,3,8,0,0,0,0,0}, // row 20: upper leg
			{0,0,0,0,0,8,3,0,0,3,8,0,0,0,0,0}, // row 21: mid leg
			{0,0,0,0,0,8,3,0,0,3,8,0,0,0,0,0}, // row 22: mid leg
			{0,0,0,0,8,3,3,0,0,3,3,8,0,0,0,0}, // row 23: knee
			{0,0,0,0,8,1,1,0,0,1,1,8,0,0,0,0}, // row 24: lower leg
			{0,0,0,0,8,1,1,0,0,1,1,8,0,0,0,0}, // row 25: lower leg
			{0,0,0,0,8,1,1,0,0,1,1,8,0,0,0,0}, // row 26: shin
			{0,0,0,0,8,1,1,0,0,1,1,8,0,0,0,0}, // row 27: shin
			{0,0,0,0,8,3,1,0,0,1,3,8,0,0,0,0}, // row 28: ankle
			{0,0,0,8,8,1,1,0,0,1,1,8,8,0,0,0}, // row 29: boot top
			{0,0,0,8,1,1,8,0,0,8,1,1,8,0,0,0}, // row 30: boot
			{0,0,0,8,8,8,8,0,0,8,8,8,8,0,0,0}, // row 31: boot sole
		};
		return CreateTexture(px, PlayerPalette);
	}

	public static ImageTexture PlayerAttackUp()
	{
		var px = new byte[,]
		{
			{0,0,0,0,0,0,0,8,8,0,0,0,0,0,0,0}, // row 0:  sword tip
			{0,0,0,0,0,0,8,9,9,8,0,0,0,0,0,0}, // row 1:  sword blade
			{0,0,0,0,0,8,9,9,9,9,8,0,0,0,0,0}, // row 2:  sword blade wide
			{0,0,0,0,0,0,8,8,8,8,0,0,0,0,0,0}, // row 3:  sword hilt
			{0,0,0,0,0,0,8,8,8,8,0,0,0,0,0,0}, // row 4:  hair top
			{0,0,0,0,0,8,6,6,6,6,8,0,0,0,0,0}, // row 5:  hair
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 6:  hair wide
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 7:  hair lower
			{0,0,0,0,8,4,4,7,7,4,4,8,0,0,0,0}, // row 8:  eyes
			{0,0,0,0,8,5,4,8,8,4,5,8,0,0,0,0}, // row 9:  pupils
			{0,0,0,0,0,8,4,4,4,4,8,0,0,0,0,0}, // row 10: nose/mouth
			{0,0,0,0,0,8,5,4,4,5,8,0,0,0,0,0}, // row 11: chin
			{0,0,0,0,0,8,4,5,5,4,8,0,0,0,0,0}, // row 12: neck
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 13: shoulders
			{0,0,0,8,1,1,2,1,1,2,1,1,8,0,0,0}, // row 14: upper torso
			{0,0,0,8,1,1,2,1,1,2,1,1,8,0,0,0}, // row 15: torso
			{0,0,0,8,1,1,1,1,1,1,1,1,8,0,0,0}, // row 16: torso
			{0,0,0,8,3,1,1,1,1,1,1,3,8,0,0,0}, // row 17: lower torso
			{0,0,0,0,8,1,9,1,1,9,1,8,0,0,0,0}, // row 18: belt
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 19: waist
			{0,0,0,0,8,3,3,1,1,3,3,8,0,0,0,0}, // row 20: hip
			{0,0,0,0,0,8,3,1,1,3,8,0,0,0,0,0}, // row 21: upper leg
			{0,0,0,0,0,8,3,1,1,3,8,0,0,0,0,0}, // row 22: upper leg
			{0,0,0,0,0,8,3,0,0,3,8,0,0,0,0,0}, // row 23: mid leg
			{0,0,0,0,8,3,3,0,0,3,3,8,0,0,0,0}, // row 24: knee
			{0,0,0,0,8,1,1,0,0,1,1,8,0,0,0,0}, // row 25: lower leg
			{0,0,0,0,8,1,1,0,0,1,1,8,0,0,0,0}, // row 26: shin
			{0,0,0,0,8,1,1,0,0,1,1,8,0,0,0,0}, // row 27: shin
			{0,0,0,0,8,3,1,0,0,1,3,8,0,0,0,0}, // row 28: ankle
			{0,0,0,8,8,1,1,0,0,1,1,8,8,0,0,0}, // row 29: boot top
			{0,0,0,8,1,1,8,0,0,8,1,1,8,0,0,0}, // row 30: boot
			{0,0,0,8,8,8,8,0,0,8,8,8,8,0,0,0}, // row 31: boot sole
		};
		return CreateTexture(px, PlayerPalette);
	}

	public static ImageTexture PlayerAttackDown()
	{
		var px = new byte[,]
		{
			{0,0,0,0,0,0,8,8,8,8,0,0,0,0,0,0}, // row 0:  hair top
			{0,0,0,0,0,8,6,6,6,6,8,0,0,0,0,0}, // row 1:  hair
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 2:  hair wide
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 3:  hair lower
			{0,0,0,0,8,4,4,7,7,4,4,8,0,0,0,0}, // row 4:  eyes
			{0,0,0,0,8,5,4,8,8,4,5,8,0,0,0,0}, // row 5:  pupils
			{0,0,0,0,0,8,4,4,4,4,8,0,0,0,0,0}, // row 6:  nose/mouth
			{0,0,0,0,0,8,5,4,4,5,8,0,0,0,0,0}, // row 7:  chin
			{0,0,0,0,0,8,4,5,5,4,8,0,0,0,0,0}, // row 8:  neck
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 9:  shoulders
			{0,0,0,8,1,1,2,1,1,2,1,1,8,0,0,0}, // row 10: upper torso
			{0,0,0,8,1,1,2,1,1,2,1,1,8,0,0,0}, // row 11: torso
			{0,0,0,8,1,1,1,1,1,1,1,1,8,0,0,0}, // row 12: torso
			{0,0,0,8,3,1,1,1,1,1,1,3,8,0,0,0}, // row 13: lower torso
			{0,0,0,0,8,1,9,1,1,9,1,8,0,0,0,0}, // row 14: belt
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 15: waist
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 16: waist
			{0,0,0,0,8,3,3,1,1,3,3,8,0,0,0,0}, // row 17: hip
			{0,0,0,0,0,8,3,1,1,3,8,0,0,0,0,0}, // row 18: upper leg
			{0,0,0,0,0,8,3,1,1,3,8,0,0,0,0,0}, // row 19: upper leg
			{0,0,0,0,8,3,0,0,0,0,3,8,0,0,0,0}, // row 20: legs apart
			{0,0,0,8,1,0,0,0,0,0,0,1,8,0,0,0}, // row 21: legs wide
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 22: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 23: empty
			{0,0,0,0,0,0,0,8,8,0,0,0,0,0,0,0}, // row 24: sword tip
			{0,0,0,0,0,0,8,9,9,8,0,0,0,0,0,0}, // row 25: sword blade
			{0,0,0,0,0,8,9,9,9,9,8,0,0,0,0,0}, // row 26: sword blade wide
			{0,0,0,0,0,0,8,9,9,8,0,0,0,0,0,0}, // row 27: sword blade
			{0,0,0,0,0,0,0,8,8,0,0,0,0,0,0,0}, // row 28: sword point
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 29: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 30: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 31: empty
		};
		return CreateTexture(px, PlayerPalette);
	}

	public static ImageTexture PlayerDash()
	{
		var px = new byte[,]
		{
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 0:  empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 1:  empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 2:  empty
			{0,0,0,0,0,0,8,8,8,8,0,0,0,0,0,0}, // row 3:  hair top
			{0,0,0,0,0,8,6,6,6,6,8,0,0,0,0,0}, // row 4:  hair
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 5:  hair wide
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 6:  hair lower
			{0,0,0,0,8,4,4,7,7,4,4,8,0,0,0,0}, // row 7:  eyes
			{0,0,0,0,8,5,4,8,8,4,5,8,0,0,0,0}, // row 8:  pupils
			{0,0,0,0,0,8,4,4,4,4,8,0,0,0,0,0}, // row 9:  nose/mouth
			{0,0,0,0,0,8,5,4,4,5,8,8,8,8,0,0}, // row 10: chin + arm ext
			{0,0,0,0,8,1,1,1,1,1,1,9,9,9,8,0}, // row 11: shoulders + slash
			{0,0,0,8,1,1,2,1,1,2,1,1,9,9,8,0}, // row 12: torso + slash
			{0,0,0,8,1,1,2,1,1,2,1,1,8,8,0,0}, // row 13: torso
			{0,0,0,8,1,1,1,1,1,1,1,1,8,0,0,0}, // row 14: torso
			{0,0,0,8,3,1,1,1,1,1,1,3,8,0,0,0}, // row 15: lower torso
			{0,0,0,0,8,1,9,1,1,9,1,8,0,0,0,0}, // row 16: belt
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 17: waist
			{0,0,0,0,8,3,3,1,1,3,3,8,0,0,0,0}, // row 18: hip
			{0,0,0,0,0,8,3,1,1,3,8,0,0,0,0,0}, // row 19: upper leg
			{0,0,0,0,8,3,1,0,0,1,3,8,0,0,0,0}, // row 20: mid leg
			{0,0,0,8,1,1,0,0,0,0,1,1,8,0,0,0}, // row 21: legs apart
			{0,0,8,8,0,0,0,0,0,0,0,0,8,8,0,0}, // row 22: feet out
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 23: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 24: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 25: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 26: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 27: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 28: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 29: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 30: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 31: empty
		};
		return CreateTexture(px, PlayerPalette);
	}

	public static ImageTexture PlayerWallSlide()
	{
		var px = new byte[,]
		{
			{0,0,0,0,0,0,8,8,8,8,0,0,0,0,0,0}, // row 0:  hair top
			{0,0,0,0,0,8,6,6,6,6,8,0,0,0,0,0}, // row 1:  hair
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 2:  hair wide
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 3:  hair lower
			{0,0,0,0,8,4,4,7,7,4,4,8,0,0,0,0}, // row 4:  eyes
			{0,0,0,0,8,5,4,8,8,4,5,8,0,0,0,0}, // row 5:  pupils
			{0,0,0,0,0,8,4,4,4,4,8,0,0,0,0,0}, // row 6:  nose/mouth
			{0,0,0,0,0,8,5,4,4,5,8,0,0,0,0,0}, // row 7:  chin
			{0,0,0,0,0,8,4,5,5,4,8,0,0,0,0,0}, // row 8:  neck
			{0,0,0,8,8,1,1,1,1,1,1,8,0,0,0,0}, // row 9:  shoulders (arm to wall)
			{0,0,8,9,1,1,2,1,1,2,1,1,8,0,0,0}, // row 10: arm reaching wall
			{0,0,0,8,1,1,2,1,1,2,1,1,8,0,0,0}, // row 11: torso
			{0,0,0,8,1,1,1,1,1,1,1,1,8,0,0,0}, // row 12: torso
			{0,0,0,8,3,1,1,1,1,1,1,3,8,0,0,0}, // row 13: lower torso
			{0,0,0,0,8,1,9,1,1,9,1,8,0,0,0,0}, // row 14: belt
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 15: waist
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 16: waist
			{0,0,0,0,8,3,3,1,1,3,3,8,0,0,0,0}, // row 17: hip
			{0,0,0,0,0,8,3,1,1,3,8,0,0,0,0,0}, // row 18: upper leg
			{0,0,0,0,0,8,3,1,1,3,8,0,0,0,0,0}, // row 19: upper leg
			{0,0,0,0,8,3,1,0,0,3,8,0,0,0,0,0}, // row 20: left leg forward
			{0,0,0,8,3,1,0,0,0,0,3,3,8,0,0,0}, // row 21: legs offset
			{0,0,0,8,1,1,0,0,0,0,1,1,8,0,0,0}, // row 22: lower legs
			{0,0,0,8,8,0,0,0,0,0,1,1,8,0,0,0}, // row 23: left foot up
			{0,0,0,0,0,0,0,0,0,0,1,1,8,0,0,0}, // row 24: right leg ext
			{0,0,0,0,0,0,0,0,0,8,1,1,8,0,0,0}, // row 25: right boot
			{0,0,0,0,0,0,0,0,0,8,8,8,0,0,0,0}, // row 26: right sole
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 27: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 28: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 29: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 30: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 31: empty
		};
		return CreateTexture(px, PlayerPalette);
	}

	public static ImageTexture PlayerDamaged()
	{
		var px = new byte[,]
		{
			{0,0,0,0,0,0,8,8,8,8,0,0,0,0,0,0}, // row 0:  hair top
			{0,0,0,0,0,8,6,6,6,6,8,0,0,0,0,0}, // row 1:  hair
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 2:  hair wide
			{0,0,0,0,8,6,6,6,6,6,6,8,0,0,0,0}, // row 3:  hair lower
			{0,0,0,0,8,4,4,7,7,4,4,8,0,0,0,0}, // row 4:  eyes
			{0,0,0,0,8,5,4,8,8,4,5,8,0,0,0,0}, // row 5:  pupils
			{0,0,0,0,0,8,4,4,4,4,8,0,0,0,0,0}, // row 6:  nose/mouth
			{0,0,0,0,0,8,5,4,4,5,8,0,0,0,0,0}, // row 7:  chin
			{0,0,0,0,0,8,4,5,5,4,8,0,0,0,0,0}, // row 8:  neck
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 9:  shoulders
			{0,0,8,9,1,1,2,1,1,2,1,1,9,8,0,0}, // row 10: arms out (recoil)
			{0,0,0,8,1,1,2,1,1,2,1,1,8,0,0,0}, // row 11: torso
			{0,0,0,8,1,1,1,1,1,1,1,1,8,0,0,0}, // row 12: torso
			{0,0,0,8,3,1,1,1,1,1,1,3,8,0,0,0}, // row 13: lower torso
			{0,0,0,0,8,1,9,1,1,9,1,8,0,0,0,0}, // row 14: belt
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 15: waist
			{0,0,0,0,8,1,1,1,1,1,1,8,0,0,0,0}, // row 16: waist
			{0,0,0,0,8,3,3,1,1,3,3,8,0,0,0,0}, // row 17: hip
			{0,0,0,0,0,8,3,1,1,3,8,0,0,0,0,0}, // row 18: upper leg
			{0,0,0,0,0,8,3,1,1,3,8,0,0,0,0,0}, // row 19: upper leg
			{0,0,0,0,8,3,0,0,0,0,3,3,8,0,0,0}, // row 20: legs stagger
			{0,0,0,8,1,0,0,0,0,0,0,1,8,0,0,0}, // row 21: legs wide (stumble)
			{0,0,8,8,0,0,0,0,0,0,0,0,8,8,0,0}, // row 22: feet out (stumble)
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 23: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 24: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 25: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 26: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 27: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 28: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 29: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 30: empty
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, // row 31: empty
		};
		return CreateTexture(px, PlayerPalette);
	}

	// ─── Enemy Sprites ──────────────────────────────────────────────

	// Crawler: 12x12, orange-red beetle
	private static readonly Color[] CrawlerPalette = new[]
	{
		new Color(0, 0, 0, 0),            // 0: transparent
		new Color(0.8f, 0.3f, 0.2f),      // 1: body
		new Color(0.9f, 0.4f, 0.25f),     // 2: highlight
		new Color(0.6f, 0.2f, 0.15f),     // 3: dark
		new Color(0.12f, 0.12f, 0.15f),   // 4: outline
		new Color(1f, 0.9f, 0.3f),        // 5: eye
	};

	public static ImageTexture CrawlerSprite()
	{
		var px = new byte[,]
		{
			{0,0,0,0,4,4,4,4,0,0,0,0},
			{0,0,0,4,1,1,1,1,4,0,0,0},
			{0,0,4,2,1,5,1,5,1,4,0,0},
			{0,4,2,1,1,1,1,1,1,2,4,0},
			{0,4,1,1,1,1,1,1,1,1,4,0},
			{4,1,1,3,1,1,1,1,3,1,1,4},
			{4,1,3,3,1,1,1,1,3,3,1,4},
			{4,1,1,3,1,1,1,1,3,1,1,4},
			{0,4,1,1,1,1,1,1,1,1,4,0},
			{0,4,3,1,1,3,3,1,1,3,4,0},
			{0,0,4,4,1,4,4,1,4,4,0,0},
			{0,4,4,0,4,0,0,4,0,4,4,0},
		};
		return CreateTexture(px, CrawlerPalette);
	}

	// Flyer: 12x12, purple bat-like
	private static readonly Color[] FlyerPalette = new[]
	{
		new Color(0, 0, 0, 0),            // 0: transparent
		new Color(0.6f, 0.2f, 0.8f),      // 1: body
		new Color(0.7f, 0.3f, 0.9f),      // 2: wing highlight
		new Color(0.4f, 0.15f, 0.6f),     // 3: dark
		new Color(0.12f, 0.12f, 0.15f),   // 4: outline
		new Color(1f, 0.3f, 0.3f),        // 5: eye
	};

	public static ImageTexture FlyerSprite()
	{
		var px = new byte[,]
		{
			{0,0,0,0,0,4,4,0,0,0,0,0},
			{4,4,0,0,4,1,1,4,0,0,4,4},
			{4,2,4,4,1,5,5,1,4,4,2,4},
			{4,2,2,1,1,1,1,1,1,2,2,4},
			{0,4,2,1,1,1,1,1,1,2,4,0},
			{0,4,1,1,3,1,1,3,1,1,4,0},
			{0,0,4,1,1,1,1,1,1,4,0,0},
			{0,0,4,3,1,1,1,1,3,4,0,0},
			{0,0,0,4,3,1,1,3,4,0,0,0},
			{0,0,0,0,4,3,3,4,0,0,0,0},
			{0,0,0,0,0,4,4,0,0,0,0,0},
			{0,0,0,0,0,0,0,0,0,0,0,0},
		};
		return CreateTexture(px, FlyerPalette);
	}

	// Shooter: 12x12, orange turret
	private static readonly Color[] ShooterPalette = new[]
	{
		new Color(0, 0, 0, 0),            // 0: transparent
		new Color(0.9f, 0.5f, 0.2f),      // 1: body
		new Color(1f, 0.65f, 0.3f),       // 2: highlight
		new Color(0.7f, 0.35f, 0.15f),    // 3: dark
		new Color(0.12f, 0.12f, 0.15f),   // 4: outline
		new Color(1f, 0.2f, 0.2f),        // 5: barrel/eye
	};

	public static ImageTexture ShooterSprite()
	{
		var px = new byte[,]
		{
			{0,0,0,0,4,4,4,4,0,0,0,0},
			{0,0,0,4,1,2,2,1,4,0,0,0},
			{0,0,4,1,1,2,2,1,1,4,0,0},
			{0,4,1,1,5,1,1,5,1,1,4,0},
			{0,4,1,1,1,1,1,1,1,1,4,4},
			{4,1,1,1,1,1,1,1,1,1,5,4},
			{4,1,1,3,1,1,1,1,3,1,5,4},
			{0,4,1,3,3,1,1,3,3,1,4,4},
			{0,4,1,1,1,1,1,1,1,1,4,0},
			{0,0,4,3,3,3,3,3,3,4,0,0},
			{0,0,4,1,1,3,3,1,1,4,0,0},
			{0,0,4,4,4,4,4,4,4,4,0,0},
		};
		return CreateTexture(px, ShooterPalette);
	}

	// Charger: 12x12, red bull-like
	private static readonly Color[] ChargerPalette = new[]
	{
		new Color(0, 0, 0, 0),            // 0: transparent
		new Color(0.7f, 0.2f, 0.2f),      // 1: body
		new Color(0.85f, 0.3f, 0.3f),     // 2: highlight
		new Color(0.5f, 0.15f, 0.15f),    // 3: dark
		new Color(0.12f, 0.12f, 0.15f),   // 4: outline
		new Color(1f, 0.9f, 0.3f),        // 5: horn/eye
	};

	public static ImageTexture ChargerSprite()
	{
		var px = new byte[,]
		{
			{0,0,4,0,0,0,0,0,0,4,0,0},
			{0,4,5,4,4,4,4,4,4,5,4,0},
			{0,0,4,1,2,2,2,2,1,4,0,0},
			{0,4,1,1,5,1,1,5,1,1,4,0},
			{4,1,1,1,1,1,1,1,1,1,1,4},
			{4,2,1,1,1,1,1,1,1,1,2,4},
			{4,1,1,3,1,1,1,1,3,1,1,4},
			{0,4,1,3,3,1,1,3,3,1,4,0},
			{0,4,1,1,1,1,1,1,1,1,4,0},
			{0,0,4,3,1,1,1,1,3,4,0,0},
			{0,0,4,1,4,1,1,4,1,4,0,0},
			{0,4,4,0,4,4,4,4,0,4,4,0},
		};
		return CreateTexture(px, ChargerPalette);
	}

	// Shielder: 12x12, blue with front shield
	private static readonly Color[] ShielderPalette = new[]
	{
		new Color(0, 0, 0, 0),            // 0: transparent
		new Color(0.4f, 0.4f, 0.7f),      // 1: body
		new Color(0.5f, 0.5f, 0.85f),     // 2: highlight
		new Color(0.3f, 0.3f, 0.55f),     // 3: dark
		new Color(0.12f, 0.12f, 0.15f),   // 4: outline
		new Color(0.7f, 0.75f, 0.9f),     // 5: shield
		new Color(1f, 0.9f, 0.3f),        // 6: eye
	};

	public static ImageTexture ShielderSprite()
	{
		var px = new byte[,]
		{
			{0,0,0,0,4,4,4,4,4,4,0,0},
			{0,0,0,4,1,2,2,1,5,5,4,0},
			{0,0,4,1,1,6,6,1,5,5,5,4},
			{0,4,1,1,1,1,1,1,5,5,5,4},
			{4,2,1,1,1,1,1,1,5,5,5,4},
			{4,1,1,3,1,1,1,1,5,5,5,4},
			{4,1,3,3,1,1,1,1,5,5,5,4},
			{4,1,1,3,1,1,1,1,5,5,5,4},
			{0,4,1,1,1,1,1,1,5,5,4,0},
			{0,0,4,3,1,1,1,3,4,4,0,0},
			{0,0,4,1,4,1,1,4,1,4,0,0},
			{0,4,4,0,4,4,4,4,0,4,4,0},
		};
		return CreateTexture(px, ShielderPalette);
	}

	// Dropper: 12x12, green blob
	private static readonly Color[] DropperPalette = new[]
	{
		new Color(0, 0, 0, 0),            // 0: transparent
		new Color(0.3f, 0.7f, 0.3f),      // 1: body
		new Color(0.4f, 0.8f, 0.4f),      // 2: highlight
		new Color(0.2f, 0.5f, 0.2f),      // 3: dark
		new Color(0.12f, 0.12f, 0.15f),   // 4: outline
		new Color(1f, 0.9f, 0.3f),        // 5: eye
	};

	public static ImageTexture DropperSprite()
	{
		var px = new byte[,]
		{
			{0,0,4,4,4,4,4,4,4,4,0,0},
			{0,4,3,3,3,3,3,3,3,3,4,0},
			{4,3,1,1,1,1,1,1,1,1,3,4},
			{4,1,1,1,1,1,1,1,1,1,1,4},
			{4,1,1,5,1,1,1,5,1,1,1,4},
			{4,1,1,1,1,1,1,1,1,1,1,4},
			{4,2,1,1,1,4,4,1,1,1,2,4},
			{4,2,1,1,1,1,1,1,1,1,2,4},
			{0,4,2,1,1,1,1,1,1,2,4,0},
			{0,0,4,1,1,1,1,1,1,4,0,0},
			{0,0,0,4,3,1,1,3,4,0,0,0},
			{0,0,0,0,4,4,4,4,0,0,0,0},
		};
		return CreateTexture(px, DropperPalette);
	}

	// Guardian: 24x32, large boss
	private static readonly Color[] GuardianPalette = new[]
	{
		new Color(0, 0, 0, 0),            // 0: transparent
		new Color(0.5f, 0.15f, 0.15f),    // 1: body
		new Color(0.6f, 0.2f, 0.2f),      // 2: highlight
		new Color(0.35f, 0.1f, 0.1f),     // 3: dark
		new Color(0.12f, 0.12f, 0.15f),   // 4: outline
		new Color(1f, 0.3f, 0.15f),       // 5: eye/glow
		new Color(0.8f, 0.8f, 0.85f),     // 6: armor
		new Color(0.6f, 0.6f, 0.65f),     // 7: armor shadow
	};

	public static ImageTexture GuardianSprite()
	{
		var px = new byte[,]
		{
			{0,0,0,0,0,0,0,0,4,4,4,4,4,4,4,4,0,0,0,0,0,0,0,0},
			{0,0,0,0,0,0,0,4,6,6,6,6,6,6,6,6,4,0,0,0,0,0,0,0},
			{0,0,0,0,0,0,4,6,7,6,6,6,6,6,6,7,6,4,0,0,0,0,0,0},
			{0,0,0,0,0,4,6,6,6,6,6,6,6,6,6,6,6,6,4,0,0,0,0,0},
			{0,0,0,0,4,6,6,6,5,5,6,6,6,6,5,5,6,6,6,4,0,0,0,0},
			{0,0,0,0,4,6,6,6,5,5,6,6,6,6,5,5,6,6,6,4,0,0,0,0},
			{0,0,0,0,4,7,6,6,6,6,6,6,6,6,6,6,6,6,7,4,0,0,0,0},
			{0,0,0,0,0,4,7,6,6,6,4,4,4,4,6,6,6,7,4,0,0,0,0,0},
			{0,0,0,0,0,0,4,4,6,6,6,6,6,6,6,6,4,4,0,0,0,0,0,0},
			{0,0,0,0,0,4,1,1,1,1,1,1,1,1,1,1,1,1,4,0,0,0,0,0},
			{0,0,0,0,4,2,1,1,1,1,1,1,1,1,1,1,1,1,2,4,0,0,0,0},
			{0,0,0,4,2,1,1,6,6,1,1,1,1,1,1,6,6,1,1,2,4,0,0,0},
			{0,0,4,1,1,1,6,7,7,6,1,1,1,1,6,7,7,6,1,1,1,4,0,0},
			{0,0,4,1,1,1,6,6,6,6,1,1,1,1,6,6,6,6,1,1,1,4,0,0},
			{0,0,4,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,4,0,0},
			{0,0,4,3,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,3,4,0,0},
			{0,0,4,3,1,1,5,1,1,1,1,1,1,1,1,1,1,5,1,1,3,4,0,0},
			{0,0,0,4,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,4,0,0,0},
			{0,0,0,4,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,4,0,0,0},
			{0,0,0,4,3,3,1,1,1,1,3,3,3,3,1,1,1,1,3,3,4,0,0,0},
			{0,0,0,0,4,4,1,1,1,1,4,0,0,4,1,1,1,1,4,4,0,0,0,0},
			{0,0,0,0,0,4,3,1,1,3,4,0,0,4,3,1,1,3,4,0,0,0,0,0},
			{0,0,0,0,0,4,3,1,1,3,4,0,0,4,3,1,1,3,4,0,0,0,0,0},
			{0,0,0,0,0,4,3,1,1,3,4,0,0,4,3,1,1,3,4,0,0,0,0,0},
			{0,0,0,0,4,3,3,1,1,3,4,0,0,4,3,1,1,3,3,4,0,0,0,0},
			{0,0,0,0,4,1,1,1,1,1,4,0,0,4,1,1,1,1,1,4,0,0,0,0},
			{0,0,0,4,1,1,1,1,1,1,4,0,0,4,1,1,1,1,1,1,4,0,0,0},
			{0,0,0,4,1,1,1,3,3,1,4,0,0,4,1,3,3,1,1,1,4,0,0,0},
			{0,0,0,4,3,3,3,3,3,3,4,0,0,4,3,3,3,3,3,3,4,0,0,0},
			{0,0,4,4,1,1,1,1,1,1,4,4,4,4,1,1,1,1,1,1,4,4,0,0},
			{0,0,4,1,1,1,1,1,1,1,1,4,4,1,1,1,1,1,1,1,1,4,0,0},
			{0,0,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,0,0},
		};
		return CreateTexture(px, GuardianPalette);
	}

	// Projectile: 6x4
	public static ImageTexture ProjectileSprite()
	{
		var palette = new[]
		{
			new Color(0, 0, 0, 0),
			new Color(0.9f, 0.5f, 0.2f),
			new Color(1f, 0.7f, 0.3f),
			new Color(0.12f, 0.12f, 0.15f),
		};
		var px = new byte[,]
		{
			{0,3,2,2,3,0},
			{3,1,2,2,1,3},
			{3,1,2,2,1,3},
			{0,3,2,2,3,0},
		};
		return CreateTexture(px, palette);
	}

	// ─── Object Sprites ─────────────────────────────────────────────

	// Ability Orb: 20x20
	public static ImageTexture AbilityOrbSprite()
	{
		var palette = new[]
		{
			new Color(0, 0, 0, 0),
			new Color(1f, 0.85f, 0.1f),
			new Color(1f, 0.95f, 0.5f),
			new Color(0.8f, 0.65f, 0.05f),
			new Color(0.12f, 0.12f, 0.15f),
		};
		int s = 20;
		var px = new byte[s, s];
		float cx = s / 2f, cy = s / 2f, r = s / 2f - 1;
		for (int y = 0; y < s; y++)
			for (int x = 0; x < s; x++)
			{
				float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
				if (d > r + 0.5f) px[y, x] = 0;
				else if (d > r - 0.5f) px[y, x] = 4;
				else if (d < r * 0.4f) px[y, x] = 2;
				else if (x + y < s * 0.6f) px[y, x] = 2;
				else px[y, x] = 1;
			}
		return CreateTexture(px, palette);
	}

	// Save Point: 24x32
	public static ImageTexture SavePointSprite()
	{
		var palette = new[]
		{
			new Color(0, 0, 0, 0),
			new Color(0.1f, 0.9f, 0.3f),
			new Color(0.2f, 1f, 0.5f),
			new Color(0.05f, 0.6f, 0.2f),
			new Color(0.12f, 0.12f, 0.15f),
			new Color(0.5f, 0.5f, 0.55f),   // 5: stone
			new Color(0.4f, 0.4f, 0.45f),   // 6: stone dark
		};
		var px = new byte[32, 24];
		// Stone base
		for (int y = 20; y < 32; y++)
			for (int x = 2; x < 22; x++)
				px[y, x] = (y + x) % 3 == 0 ? (byte)6 : (byte)5;
		// Outline the base
		for (int x = 2; x < 22; x++) { px[20, x] = 4; px[31, x] = 4; }
		for (int y = 20; y < 32; y++) { px[y, 2] = 4; px[y, 21] = 4; }
		// Crystal on top
		for (int y = 2; y < 20; y++)
		{
			int halfW = (20 - y) * 10 / 18;
			for (int x = 12 - halfW; x < 12 + halfW; x++)
			{
				if (x < 0 || x >= 24) continue;
				if (x == 12 - halfW || x == 12 + halfW - 1)
					px[y, x] = 4;
				else if (x < 12)
					px[y, x] = 2;
				else
					px[y, x] = 1;
			}
		}
		// Top point
		px[1, 11] = 4; px[1, 12] = 4;
		return CreateTexture(px, palette);
	}

	// Spikes: 16x16
	public static ImageTexture SpikesSprite()
	{
		var palette = new[]
		{
			new Color(0, 0, 0, 0),
			new Color(0.8f, 0.15f, 0.15f),
			new Color(0.9f, 0.25f, 0.2f),
			new Color(0.6f, 0.1f, 0.1f),
			new Color(0.12f, 0.12f, 0.15f),
		};
		var px = new byte[16, 16];
		// 4 spike triangles
		int[] spikeCenters = { 2, 6, 10, 14 };
		foreach (int sc in spikeCenters)
		{
			for (int y = 0; y < 12; y++)
			{
				int halfW = y * 2 / 12 + 1;
				for (int dx = -halfW; dx <= halfW; dx++)
				{
					int x = sc + dx;
					if (x < 0 || x >= 16) continue;
					if (dx == -halfW || dx == halfW)
						px[15 - y, x] = 4;
					else if (dx < 0)
						px[15 - y, x] = 2;
					else
						px[15 - y, x] = 1;
				}
			}
			px[4, sc] = 4; // tip
		}
		// Base row
		for (int x = 0; x < 16; x++) px[15, x] = 3;
		return CreateTexture(px, palette);
	}

	// Platform: 32x8
	public static ImageTexture PlatformSprite()
	{
		var palette = new[]
		{
			new Color(0, 0, 0, 0),
			new Color(0.55f, 0.55f, 0.6f),
			new Color(0.65f, 0.65f, 0.7f),
			new Color(0.4f, 0.4f, 0.45f),
			new Color(0.12f, 0.12f, 0.15f),
		};
		var px = new byte[8, 32];
		for (int y = 0; y < 8; y++)
			for (int x = 0; x < 32; x++)
			{
				if (y == 0 || y == 7 || x == 0 || x == 31)
					px[y, x] = 4;
				else if (y == 1)
					px[y, x] = 2;
				else if (y >= 6)
					px[y, x] = 3;
				else
					px[y, x] = 1;
			}
		return CreateTexture(px, palette);
	}

	// ─── Tile Atlas ─────────────────────────────────────────────────

	/// <summary>
	/// Creates an expanded tile atlas with edge-shaded variants.
	/// Layout: 9 tiles wide x 1 tile tall (144x16).
	/// (0,0)=solid, (1,0)=one-way, (2,0)=background,
	/// (3,0)=top-edge, (4,0)=bottom-edge, (5,0)=left-edge,
	/// (6,0)=right-edge, (7,0)=top-left corner, (8,0)=top-right corner
	/// </summary>
	public static Image CreateTileAtlas()
	{
		const int T = 16;
		const int tileCount = 9;
		var image = Image.CreateEmpty(T * tileCount, T, false, Image.Format.Rgba8);

		var solidBase = new Color(0.35f, 0.28f, 0.2f);
		var solidDark = new Color(0.28f, 0.22f, 0.16f);
		var solidLight = new Color(0.42f, 0.34f, 0.26f);
		var oneWayBase = new Color(0.45f, 0.38f, 0.3f);
		var oneWayTop = new Color(0.6f, 0.52f, 0.4f);
		var bgBase = new Color(0.12f, 0.1f, 0.15f);
		var bgDot = new Color(0.15f, 0.13f, 0.18f);

		// Tile 0: Solid — subtle noise
		FillTileWithNoise(image, 0, solidBase, 0.04f);

		// Tile 1: One-way — top edge highlighted
		for (int y = 0; y < T; y++)
			for (int x = 0; x < T; x++)
			{
				Color c = y < 3 ? oneWayTop : oneWayBase;
				if (y == 0) c = c.Lightened(0.1f);
				image.SetPixel(T + x, y, c);
			}

		// Tile 2: Background — faint dot pattern
		for (int y = 0; y < T; y++)
			for (int x = 0; x < T; x++)
			{
				Color c = (x % 4 == 0 && y % 4 == 0) ? bgDot : bgBase;
				image.SetPixel(T * 2 + x, y, c);
			}

		// Tile 3: Solid top-edge
		FillTileWithNoise(image, 3, solidBase, 0.04f);
		for (int x = 0; x < T; x++)
		{
			image.SetPixel(T * 3 + x, 0, solidLight);
			image.SetPixel(T * 3 + x, 1, solidLight.Lerp(solidBase, 0.5f));
		}

		// Tile 4: Solid bottom-edge
		FillTileWithNoise(image, 4, solidBase, 0.04f);
		for (int x = 0; x < T; x++)
		{
			image.SetPixel(T * 4 + x, T - 1, solidDark);
			image.SetPixel(T * 4 + x, T - 2, solidDark.Lerp(solidBase, 0.5f));
		}

		// Tile 5: Solid left-edge
		FillTileWithNoise(image, 5, solidBase, 0.04f);
		for (int y = 0; y < T; y++)
		{
			image.SetPixel(T * 5, y, solidLight);
			image.SetPixel(T * 5 + 1, y, solidLight.Lerp(solidBase, 0.5f));
		}

		// Tile 6: Solid right-edge
		FillTileWithNoise(image, 6, solidBase, 0.04f);
		for (int y = 0; y < T; y++)
		{
			image.SetPixel(T * 6 + T - 1, y, solidDark);
			image.SetPixel(T * 6 + T - 2, y, solidDark.Lerp(solidBase, 0.5f));
		}

		// Tile 7: Top-left corner
		FillTileWithNoise(image, 7, solidBase, 0.04f);
		for (int x = 0; x < T; x++)
		{
			image.SetPixel(T * 7 + x, 0, solidLight);
			image.SetPixel(T * 7 + x, 1, solidLight.Lerp(solidBase, 0.5f));
		}
		for (int y = 0; y < T; y++)
		{
			image.SetPixel(T * 7, y, solidLight);
			image.SetPixel(T * 7 + 1, y, solidLight.Lerp(solidBase, 0.5f));
		}

		// Tile 8: Top-right corner
		FillTileWithNoise(image, 8, solidBase, 0.04f);
		for (int x = 0; x < T; x++)
		{
			image.SetPixel(T * 8 + x, 0, solidLight);
			image.SetPixel(T * 8 + x, 1, solidLight.Lerp(solidBase, 0.5f));
		}
		for (int y = 0; y < T; y++)
		{
			image.SetPixel(T * 8 + T - 1, y, solidDark);
			image.SetPixel(T * 8 + T - 2, y, solidDark.Lerp(solidBase, 0.5f));
		}

		return image;
	}

	private static void FillTileWithNoise(Image image, int tileIndex, Color baseColor, float noiseAmount)
	{
		int startX = tileIndex * 16;
		var rng = new RandomNumberGenerator();
		rng.Seed = (ulong)(tileIndex * 1000 + 42);
		for (int y = 0; y < 16; y++)
			for (int x = 0; x < 16; x++)
			{
				float n = rng.RandfRange(-noiseAmount, noiseAmount);
				var c = new Color(
					Mathf.Clamp(baseColor.R + n, 0, 1),
					Mathf.Clamp(baseColor.G + n, 0, 1),
					Mathf.Clamp(baseColor.B + n, 0, 1));
				image.SetPixel(startX + x, y, c);
			}
	}

	// ─── Parallax Layers ────────────────────────────────────────────

	public static ImageTexture ParallaxFarLayer(int w, int h)
	{
		var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
		// Dark gradient sky + stars
		for (int y = 0; y < h; y++)
		{
			float t = (float)y / h;
			var c = new Color(0.05f + t * 0.03f, 0.03f + t * 0.02f, 0.1f + t * 0.05f);
			for (int x = 0; x < w; x++)
				img.SetPixel(x, y, c);
		}
		// Stars (deterministic)
		var rng = new RandomNumberGenerator();
		rng.Seed = 12345;
		for (int i = 0; i < 40; i++)
		{
			int sx = rng.RandiRange(0, w - 1);
			int sy = rng.RandiRange(0, h / 2);
			float bright = rng.RandfRange(0.4f, 0.9f);
			img.SetPixel(sx, sy, new Color(bright, bright, bright * 0.9f));
		}
		return ImageTexture.CreateFromImage(img);
	}

	public static ImageTexture ParallaxMidLayer(int w, int h)
	{
		var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
		// Transparent base
		for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
				img.SetPixel(x, y, new Color(0, 0, 0, 0));

		// Silhouette mountains/pillars
		var rng = new RandomNumberGenerator();
		rng.Seed = 67890;
		var silColor = new Color(0.08f, 0.06f, 0.12f, 0.7f);
		int numPillars = 8;
		for (int i = 0; i < numPillars; i++)
		{
			int px = rng.RandiRange(0, w - 1);
			int pillarW = rng.RandiRange(20, 50);
			int pillarH = rng.RandiRange(h / 3, h * 2 / 3);
			int top = h - pillarH;
			for (int y = top; y < h; y++)
				for (int x = px - pillarW / 2; x < px + pillarW / 2; x++)
				{
					if (x < 0 || x >= w) continue;
					float fade = 1f - (float)(y - top) / pillarH * 0.3f;
					img.SetPixel(x, y, new Color(silColor.R, silColor.G, silColor.B, silColor.A * fade));
				}
		}
		return ImageTexture.CreateFromImage(img);
	}

	public static ImageTexture ParallaxNearLayer(int w, int h)
	{
		var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
		// Transparent base
		for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
				img.SetPixel(x, y, new Color(0, 0, 0, 0));

		// Stalactites from top
		var rng = new RandomNumberGenerator();
		rng.Seed = 11111;
		var stalColor = new Color(0.1f, 0.08f, 0.14f, 0.4f);
		for (int i = 0; i < 12; i++)
		{
			int sx = rng.RandiRange(0, w - 1);
			int stalH = rng.RandiRange(20, 80);
			int stalW = rng.RandiRange(4, 12);
			for (int y = 0; y < stalH; y++)
			{
				float taper = 1f - (float)y / stalH;
				int curW = (int)(stalW * taper);
				for (int x = sx - curW / 2; x < sx + curW / 2; x++)
				{
					if (x < 0 || x >= w) continue;
					img.SetPixel(x, y, stalColor);
				}
			}
		}
		return ImageTexture.CreateFromImage(img);
	}

	// ─── Max Health Upgrade Sprite: 16x16 ───────────────────────────

	public static ImageTexture MaxHealthUpgradeSprite()
	{
		var palette = new[]
		{
			new Color(0, 0, 0, 0),
			new Color(0.9f, 0.2f, 0.2f),   // red
			new Color(1f, 0.4f, 0.4f),      // light red
			new Color(0.6f, 0.1f, 0.1f),    // dark red
			new Color(0.12f, 0.12f, 0.15f), // outline
		};
		var px = new byte[,]
		{
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
			{0,0,0,4,4,0,0,0,0,0,4,4,0,0,0,0},
			{0,0,4,2,2,4,0,0,0,4,2,2,4,0,0,0},
			{0,4,2,1,1,2,4,0,4,2,1,1,2,4,0,0},
			{0,4,1,1,1,1,1,4,1,1,1,1,1,4,0,0},
			{0,4,1,1,1,1,1,1,1,1,1,1,1,4,0,0},
			{0,0,4,1,1,1,1,1,1,1,1,1,4,0,0,0},
			{0,0,0,4,1,1,1,1,1,1,1,4,0,0,0,0},
			{0,0,0,0,4,1,1,1,1,1,4,0,0,0,0,0},
			{0,0,0,0,0,4,1,1,1,4,0,0,0,0,0,0},
			{0,0,0,0,0,0,4,3,4,0,0,0,0,0,0,0},
			{0,0,0,0,0,0,0,4,0,0,0,0,0,0,0,0},
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
		};
		return CreateTexture(px, palette);
	}

	// ─── Bat Sprite: 12×12 ──────────────────────────────────────────

	private static readonly Color[] BatPalette = new[]
	{
		new Color(0, 0, 0, 0),            // 0: transparent
		new Color(0.35f, 0.25f, 0.2f),    // 1: body brown
		new Color(0.45f, 0.35f, 0.3f),    // 2: wing highlight
		new Color(0.25f, 0.18f, 0.15f),   // 3: dark
		new Color(0.12f, 0.12f, 0.15f),   // 4: outline
		new Color(1f, 0.3f, 0.3f),        // 5: eye
	};

	public static ImageTexture BatSprite()
	{
		var px = new byte[,]
		{
			{0,0,0,0,0,4,4,0,0,0,0,0},
			{4,4,0,0,4,1,1,4,0,0,4,4},
			{4,2,4,4,1,5,5,1,4,4,2,4},
			{4,2,2,1,1,1,1,1,1,2,2,4},
			{0,4,2,1,1,3,3,1,1,2,4,0},
			{0,4,1,3,1,1,1,1,3,1,4,0},
			{0,0,4,1,1,1,1,1,1,4,0,0},
			{0,0,4,3,1,1,1,1,3,4,0,0},
			{0,0,0,4,1,3,3,1,4,0,0,0},
			{0,0,0,4,3,0,0,3,4,0,0,0},
			{0,0,0,0,4,0,0,4,0,0,0,0},
			{0,0,0,0,0,0,0,0,0,0,0,0},
		};
		return CreateTexture(px, BatPalette);
	}

	// ─── Skeleton Sprite: 12×16 ─────────────────────────────────────

	private static readonly Color[] SkeletonPalette = new[]
	{
		new Color(0, 0, 0, 0),            // 0: transparent
		new Color(0.85f, 0.82f, 0.75f),   // 1: bone
		new Color(0.95f, 0.92f, 0.85f),   // 2: bone highlight
		new Color(0.6f, 0.58f, 0.52f),    // 3: bone shadow
		new Color(0.12f, 0.12f, 0.15f),   // 4: outline
		new Color(0.2f, 0.15f, 0.1f),     // 5: eye socket
	};

	public static ImageTexture SkeletonSprite()
	{
		var px = new byte[,]
		{
			{0,0,0,4,4,4,4,4,4,0,0,0},
			{0,0,4,2,1,1,1,1,2,4,0,0},
			{0,0,4,1,5,1,1,5,1,4,0,0},
			{0,0,4,1,1,1,1,1,1,4,0,0},
			{0,0,0,4,1,4,4,1,4,0,0,0},
			{0,0,0,4,1,1,1,1,4,0,0,0},
			{0,0,4,3,1,1,1,1,3,4,0,0},
			{0,0,4,1,1,2,2,1,1,4,0,0},
			{0,0,4,1,3,1,1,3,1,4,0,0},
			{0,0,0,4,1,1,1,1,4,0,0,0},
			{0,0,0,4,1,1,1,1,4,0,0,0},
			{0,0,0,4,3,1,1,3,4,0,0,0},
			{0,0,0,4,1,0,0,1,4,0,0,0},
			{0,0,4,3,1,0,0,1,3,4,0,0},
			{0,0,4,1,1,0,0,1,1,4,0,0},
			{0,0,4,4,4,0,0,4,4,4,0,0},
		};
		return CreateTexture(px, SkeletonPalette);
	}

	// ─── Ghost Sprite: 12×14 ────────────────────────────────────────

	public static ImageTexture GhostSprite()
	{
		var palette = new[]
		{
			new Color(0, 0, 0, 0),                  // 0: transparent
			new Color(0.7f, 0.75f, 0.9f, 0.6f),     // 1: body
			new Color(0.85f, 0.88f, 1f, 0.7f),       // 2: highlight
			new Color(0.5f, 0.55f, 0.7f, 0.5f),      // 3: shadow
			new Color(0.3f, 0.3f, 0.5f, 0.8f),       // 4: outline
			new Color(0.1f, 0.1f, 0.3f, 0.9f),       // 5: eye
		};
		var px = new byte[,]
		{
			{0,0,0,0,4,4,4,4,0,0,0,0},
			{0,0,0,4,2,2,2,2,4,0,0,0},
			{0,0,4,2,1,1,1,1,2,4,0,0},
			{0,4,2,1,5,1,1,5,1,2,4,0},
			{0,4,1,1,5,1,1,5,1,1,4,0},
			{0,4,1,1,1,1,1,1,1,1,4,0},
			{0,4,1,1,1,1,1,1,1,1,4,0},
			{0,4,3,1,1,1,1,1,1,3,4,0},
			{0,4,3,1,1,1,1,1,1,3,4,0},
			{0,0,4,3,1,1,1,1,3,4,0,0},
			{0,0,4,1,3,1,1,3,1,4,0,0},
			{0,0,4,1,0,4,4,0,1,4,0,0},
			{0,0,0,4,0,0,0,0,4,0,0,0},
			{0,0,0,0,0,0,0,0,0,0,0,0},
		};
		return CreateTexture(px, palette);
	}

	// ─── Knight Sprite: 12×16 ───────────────────────────────────────

	private static readonly Color[] KnightPalette = new[]
	{
		new Color(0, 0, 0, 0),            // 0: transparent
		new Color(0.4f, 0.4f, 0.45f),     // 1: armor
		new Color(0.55f, 0.55f, 0.6f),    // 2: armor highlight
		new Color(0.28f, 0.28f, 0.32f),   // 3: armor dark
		new Color(0.12f, 0.12f, 0.15f),   // 4: outline
		new Color(0.9f, 0.3f, 0.2f),      // 5: visor/eye
		new Color(0.65f, 0.5f, 0.3f),     // 6: spear shaft
		new Color(0.8f, 0.8f, 0.85f),     // 7: spear tip
	};

	public static ImageTexture KnightSprite()
	{
		var px = new byte[,]
		{
			{0,0,0,4,4,4,4,4,4,0,0,0},
			{0,0,4,2,2,2,2,2,2,4,0,0},
			{0,0,4,1,1,5,5,1,1,4,0,0},
			{0,0,4,3,1,1,1,1,3,4,0,0},
			{0,0,0,4,4,4,4,4,4,0,0,0},
			{0,0,4,1,2,2,2,2,1,4,7,0},
			{0,4,2,1,1,2,2,1,1,2,6,0},
			{0,4,1,1,3,1,1,3,1,1,6,0},
			{0,4,1,3,3,1,1,3,3,1,6,0},
			{0,0,4,1,1,1,1,1,1,4,0,0},
			{0,0,4,3,1,1,1,1,3,4,0,0},
			{0,0,0,4,3,1,1,3,4,0,0,0},
			{0,0,0,4,1,0,0,1,4,0,0,0},
			{0,0,4,3,1,0,0,1,3,4,0,0},
			{0,0,4,1,1,0,0,1,1,4,0,0},
			{0,0,4,4,4,0,0,4,4,4,0,0},
		};
		return CreateTexture(px, KnightPalette);
	}

	// ─── Bone Projectile Sprite: 8×8 ────────────────────────────────

	public static ImageTexture BoneProjectileSprite()
	{
		var palette = new[]
		{
			new Color(0, 0, 0, 0),
			new Color(0.85f, 0.82f, 0.75f),  // bone
			new Color(0.95f, 0.92f, 0.85f),  // highlight
			new Color(0.12f, 0.12f, 0.15f),  // outline
		};
		var px = new byte[,]
		{
			{0,0,3,2,2,3,0,0},
			{0,3,1,1,1,1,3,0},
			{3,1,0,1,1,0,1,3},
			{3,1,1,1,1,1,1,3},
			{3,1,1,1,1,1,1,3},
			{3,1,0,1,1,0,1,3},
			{0,3,1,1,1,1,3,0},
			{0,0,3,2,2,3,0,0},
		};
		return CreateTexture(px, palette);
	}

	// ─── Candle Sprite: 16×16 ───────────────────────────────────────

	public static ImageTexture CandleSprite()
	{
		var palette = new[]
		{
			new Color(0, 0, 0, 0),            // 0: transparent
			new Color(0.7f, 0.55f, 0.25f),    // 1: brass
			new Color(0.85f, 0.7f, 0.35f),    // 2: brass highlight
			new Color(0.5f, 0.4f, 0.18f),     // 3: brass dark
			new Color(0.12f, 0.12f, 0.15f),   // 4: outline
			new Color(1f, 0.85f, 0.2f),       // 5: flame bright
			new Color(1f, 0.5f, 0.15f),       // 6: flame mid
			new Color(0.9f, 0.3f, 0.1f),      // 7: flame dark
		};
		var px = new byte[,]
		{
			{0,0,0,0,0,0,0,5,0,0,0,0,0,0,0,0},
			{0,0,0,0,0,0,5,5,5,0,0,0,0,0,0,0},
			{0,0,0,0,0,5,6,5,6,5,0,0,0,0,0,0},
			{0,0,0,0,0,6,7,6,7,6,0,0,0,0,0,0},
			{0,0,0,0,0,0,7,6,7,0,0,0,0,0,0,0},
			{0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0},
			{0,0,0,0,0,4,2,1,2,4,0,0,0,0,0,0},
			{0,0,0,0,0,4,1,3,1,4,0,0,0,0,0,0},
			{0,0,0,0,0,4,1,3,1,4,0,0,0,0,0,0},
			{0,0,0,0,4,2,1,3,1,2,4,0,0,0,0,0},
			{0,0,0,4,2,1,1,3,1,1,2,4,0,0,0,0},
			{0,0,0,4,1,1,1,3,1,1,1,4,0,0,0,0},
			{0,0,4,3,1,1,1,3,1,1,1,3,4,0,0,0},
			{0,0,4,3,3,1,1,3,1,1,3,3,4,0,0,0},
			{0,0,0,4,4,4,4,4,4,4,4,4,0,0,0,0},
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
		};
		return CreateTexture(px, palette);
	}

	// ─── Weapon Upgrade Sprite: 16×16 ───────────────────────────────

	public static ImageTexture WeaponUpgradeSprite()
	{
		var palette = new[]
		{
			new Color(0, 0, 0, 0),            // 0: transparent
			new Color(0.3f, 0.5f, 0.9f),      // 1: crystal blue
			new Color(0.5f, 0.7f, 1f),        // 2: crystal highlight
			new Color(0.2f, 0.35f, 0.7f),     // 3: crystal dark
			new Color(0.12f, 0.12f, 0.15f),   // 4: outline
			new Color(0.8f, 0.9f, 1f),        // 5: sparkle
		};
		var px = new byte[,]
		{
			{0,0,0,0,0,0,0,5,0,0,0,0,0,0,0,0},
			{0,0,0,0,0,0,0,0,0,0,0,5,0,0,0,0},
			{0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0},
			{0,0,0,0,0,4,2,2,2,4,0,0,0,0,0,0},
			{0,0,0,0,4,2,1,2,1,2,4,0,0,0,0,0},
			{0,0,0,4,2,1,1,2,1,1,2,4,0,0,0,0},
			{0,0,0,4,1,1,1,1,1,1,1,4,0,0,0,0},
			{0,0,4,1,1,1,3,1,3,1,1,1,4,0,0,0},
			{0,0,4,1,1,3,3,1,3,3,1,1,4,0,0,0},
			{0,0,0,4,1,3,3,3,3,3,1,4,0,0,0,0},
			{0,0,0,4,3,3,3,3,3,3,3,4,0,0,0,0},
			{0,0,0,0,4,3,3,3,3,3,4,0,0,0,0,0},
			{0,0,0,0,0,4,4,4,4,4,0,0,0,0,0,0},
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
			{0,0,5,0,0,0,0,0,0,0,0,0,0,0,0,0},
			{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
		};
		return CreateTexture(px, palette);
	}

	// ─── Helpers ─────────────────────────────────────────────────────

	private static byte[,] ClonePixels(byte[,] src)
	{
		int h = src.GetLength(0), w = src.GetLength(1);
		var dst = new byte[h, w];
		System.Buffer.BlockCopy(src, 0, dst, 0, h * w);
		return dst;
	}
}
