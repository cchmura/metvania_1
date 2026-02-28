using Godot;
using System.Collections.Generic;

namespace metvania_1;

public partial class AudioManager : Node
{
	private const int PoolSize = 8;
	private const int SampleRate = 22050;

	private readonly List<AudioStreamPlayer> _pool = new();
	private readonly Dictionary<string, AudioStreamWav> _sounds = new();

	public override void _Ready()
	{
		// Create player pool
		for (int i = 0; i < PoolSize; i++)
		{
			var player = new AudioStreamPlayer();
			player.Bus = "Master";
			AddChild(player);
			_pool.Add(player);
		}

		// Generate placeholder sounds
		_sounds["slash"] = GenerateNoise(0.08f, 0.6f);
		_sounds["hit"] = GenerateSweep(0.1f, 400f, 100f, WaveType.Square, 0.5f);
		_sounds["enemy_death"] = GenerateSweep(0.15f, 300f, 50f, WaveType.Square, 0.5f);
		_sounds["player_hurt"] = GenerateSweep(0.12f, 200f, 80f, WaveType.Square, 0.5f);
		_sounds["jump"] = GenerateSweep(0.06f, 200f, 600f, WaveType.Square, 0.3f);
		_sounds["pogo"] = GenerateSweep(0.08f, 300f, 800f, WaveType.Square, 0.4f);
		_sounds["land"] = GenerateNoise(0.04f, 0.3f);
	}

	public void Play(string soundName)
	{
		if (!_sounds.TryGetValue(soundName, out var stream)) return;

		foreach (var player in _pool)
		{
			if (!player.Playing)
			{
				player.Stream = stream;
				player.Play();
				return;
			}
		}
	}

	private enum WaveType { Square, Noise }

	private AudioStreamWav GenerateNoise(float duration, float volume)
	{
		int sampleCount = (int)(SampleRate * duration);
		var data = new byte[sampleCount * 2]; // 16-bit mono

		for (int i = 0; i < sampleCount; i++)
		{
			float t = (float)i / sampleCount;
			float envelope = 1f - t; // Linear fade out
			float sample = (float)GD.RandRange(-1.0, 1.0) * volume * envelope;
			short pcm = (short)(sample * 32767);
			data[i * 2] = (byte)(pcm & 0xFF);
			data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
		}

		var wav = new AudioStreamWav();
		wav.Format = AudioStreamWav.FormatEnum.Format16Bits;
		wav.MixRate = SampleRate;
		wav.Stereo = false;
		wav.Data = data;
		return wav;
	}

	private AudioStreamWav GenerateSweep(float duration, float startFreq, float endFreq, WaveType waveType, float volume)
	{
		int sampleCount = (int)(SampleRate * duration);
		var data = new byte[sampleCount * 2];
		float phase = 0f;

		for (int i = 0; i < sampleCount; i++)
		{
			float t = (float)i / sampleCount;
			float envelope = 1f - t;
			float freq = Mathf.Lerp(startFreq, endFreq, t);

			float sample;
			if (waveType == WaveType.Square)
			{
				sample = (Mathf.Sin(phase * Mathf.Tau) >= 0 ? 1f : -1f) * volume * envelope;
			}
			else
			{
				sample = (float)GD.RandRange(-1.0, 1.0) * volume * envelope;
			}

			short pcm = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767);
			data[i * 2] = (byte)(pcm & 0xFF);
			data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);

			phase += freq / SampleRate;
			if (phase > 1f) phase -= 1f;
		}

		var wav = new AudioStreamWav();
		wav.Format = AudioStreamWav.FormatEnum.Format16Bits;
		wav.MixRate = SampleRate;
		wav.Stereo = false;
		wav.Data = data;
		return wav;
	}
}
