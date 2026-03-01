using Godot;
using System;
using System.Collections.Generic;

namespace metvania_1;

public partial class AudioManager : Node
{
	private const int PoolSize = 8;
	private const int SampleRate = 22050;

	// SFX
	private readonly List<AudioStreamPlayer> _pool = new();
	private readonly Dictionary<string, AudioStreamWav> _sounds = new();

	// Music
	private readonly Dictionary<string, AudioStream> _tracks = new();
	private AudioStreamPlayer _musicPlayerA;
	private AudioStreamPlayer _musicPlayerB;
	private bool _musicUseA = true;
	private string _currentTrack = "";
	private Tween _musicFadeTween;

	// Buses
	private int _sfxBusIdx;
	private int _musicBusIdx;

	// ─── Types ──────────────────────────────────────────────────────

	private enum WaveForm { Sine, Square, Triangle, Noise }

	private struct TrackVoice
	{
		public int[] Notes; // MIDI note per beat, 0 = rest
		public WaveForm Wave;
		public float Volume;
		public float Attack;
		public float Decay;
		public float Sustain; // 0-1
		public float Release;
	}

	private struct SoundLayer
	{
		public float Duration;
		public float StartFreq;
		public float EndFreq;
		public WaveForm Wave;
		public float Volume;
	}

	// ─── Init ───────────────────────────────────────────────────────

	public override void _Ready()
	{
		SetupBuses();

		// SFX player pool
		for (int i = 0; i < PoolSize; i++)
		{
			var player = new AudioStreamPlayer();
			player.Bus = "SFX";
			AddChild(player);
			_pool.Add(player);
		}

		// Music players for crossfade
		_musicPlayerA = new AudioStreamPlayer();
		_musicPlayerA.Bus = "Music";
		AddChild(_musicPlayerA);

		_musicPlayerB = new AudioStreamPlayer();
		_musicPlayerB.Bus = "Music";
		AddChild(_musicPlayerB);

		GenerateAllSounds();
		GenerateAllTracks();
	}

	private void SetupBuses()
	{
		_sfxBusIdx = AudioServer.GetBusIndex("SFX");
		if (_sfxBusIdx == -1)
		{
			AudioServer.AddBus();
			_sfxBusIdx = AudioServer.BusCount - 1;
			AudioServer.SetBusName(_sfxBusIdx, "SFX");
			AudioServer.SetBusSend(_sfxBusIdx, "Master");
		}

		_musicBusIdx = AudioServer.GetBusIndex("Music");
		if (_musicBusIdx == -1)
		{
			AudioServer.AddBus();
			_musicBusIdx = AudioServer.BusCount - 1;
			AudioServer.SetBusName(_musicBusIdx, "Music");
			AudioServer.SetBusSend(_musicBusIdx, "Master");
		}
	}

	// ─── Volume Control ─────────────────────────────────────────────

	public void SetSfxVolume(float linear)
	{
		AudioServer.SetBusVolumeDb(_sfxBusIdx, linear > 0.001f ? Mathf.LinearToDb(linear) : -80f);
	}

	public void SetMusicVolume(float linear)
	{
		AudioServer.SetBusVolumeDb(_musicBusIdx, linear > 0.001f ? Mathf.LinearToDb(linear) : -80f);
	}

	public float GetSfxVolume()
	{
		float db = AudioServer.GetBusVolumeDb(_sfxBusIdx);
		return db <= -79f ? 0f : Mathf.DbToLinear(db);
	}

	public float GetMusicVolume()
	{
		float db = AudioServer.GetBusVolumeDb(_musicBusIdx);
		return db <= -79f ? 0f : Mathf.DbToLinear(db);
	}

	// ─── SFX Playback ───────────────────────────────────────────────

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

	// ─── Music Playback ─────────────────────────────────────────────

	private const string MusicAssetRoot = "res://Assets/Music/";
	private static readonly string[] MusicExtensions = { ".ogg", ".mp3", ".wav" };

	private AudioStream LoadMusicAsset(string trackName)
	{
		foreach (var ext in MusicExtensions)
		{
			string path = MusicAssetRoot + trackName + ext;
			if (ResourceLoader.Exists(path))
				return GD.Load<AudioStream>(path);
		}
		return null;
	}

	public void PlayMusic(string trackName)
	{
		if (trackName == _currentTrack) return;

		// Try loading a custom asset first, fall back to procedural track
		AudioStream stream = LoadMusicAsset(trackName);
		if (stream == null && !_tracks.TryGetValue(trackName, out stream)) return;

		_currentTrack = trackName;
		_musicFadeTween?.Kill();
		_musicFadeTween = CreateTween();
		_musicFadeTween.SetParallel(true);

		var incoming = _musicUseA ? _musicPlayerA : _musicPlayerB;
		var outgoing = _musicUseA ? _musicPlayerB : _musicPlayerA;

		// Fade in incoming
		incoming.Stream = stream;
		incoming.VolumeDb = -40f;
		incoming.Play();
		_musicFadeTween.TweenProperty(incoming, "volume_db", 0f, 1.0f);

		// Fade out outgoing
		if (outgoing.Playing)
			_musicFadeTween.TweenProperty(outgoing, "volume_db", -40f, 1.0f);

		_musicUseA = !_musicUseA;
	}

	public void StopMusic(float fadeOut = 1.0f)
	{
		_currentTrack = "";
		_musicFadeTween?.Kill();

		bool aPlaying = _musicPlayerA.Playing;
		bool bPlaying = _musicPlayerB.Playing;
		if (!aPlaying && !bPlaying) return;

		_musicFadeTween = CreateTween();
		_musicFadeTween.SetParallel(true);

		if (aPlaying)
			_musicFadeTween.TweenProperty(_musicPlayerA, "volume_db", -40f, fadeOut);
		if (bPlaying)
			_musicFadeTween.TweenProperty(_musicPlayerB, "volume_db", -40f, fadeOut);

		_musicFadeTween.Chain();
		_musicFadeTween.TweenCallback(Callable.From(() =>
		{
			_musicPlayerA.Stop();
			_musicPlayerB.Stop();
		}));
	}

	// ─── SFX Generation ─────────────────────────────────────────────

	private void GenerateAllSounds()
	{
		// Improved sounds (layered)
		_sounds["slash"] = GenerateLayered(new SoundLayer[]
		{
			new() { Duration = 0.08f, StartFreq = 800f, EndFreq = 200f, Wave = WaveForm.Triangle, Volume = 0.5f },
			new() { Duration = 0.06f, StartFreq = 100f, EndFreq = 100f, Wave = WaveForm.Noise, Volume = 0.3f },
		});

		_sounds["hit"] = GenerateLayered(new SoundLayer[]
		{
			new() { Duration = 0.1f, StartFreq = 400f, EndFreq = 100f, Wave = WaveForm.Square, Volume = 0.4f },
			new() { Duration = 0.12f, StartFreq = 60f, EndFreq = 30f, Wave = WaveForm.Sine, Volume = 0.3f },
		});

		_sounds["enemy_death"] = GenerateLayered(new SoundLayer[]
		{
			new() { Duration = 0.15f, StartFreq = 300f, EndFreq = 50f, Wave = WaveForm.Square, Volume = 0.4f },
			new() { Duration = 0.2f, StartFreq = 200f, EndFreq = 80f, Wave = WaveForm.Sine, Volume = 0.3f },
			new() { Duration = 0.1f, StartFreq = 100f, EndFreq = 100f, Wave = WaveForm.Noise, Volume = 0.2f },
		});

		_sounds["jump"] = GenerateLayered(new SoundLayer[]
		{
			new() { Duration = 0.04f, StartFreq = 250f, EndFreq = 600f, Wave = WaveForm.Square, Volume = 0.25f },
			new() { Duration = 0.05f, StartFreq = 80f, EndFreq = 60f, Wave = WaveForm.Sine, Volume = 0.2f },
		});

		_sounds["land"] = GenerateLayered(new SoundLayer[]
		{
			new() { Duration = 0.04f, StartFreq = 100f, EndFreq = 100f, Wave = WaveForm.Noise, Volume = 0.25f },
			new() { Duration = 0.06f, StartFreq = 60f, EndFreq = 40f, Wave = WaveForm.Sine, Volume = 0.3f },
		});

		// Unchanged sounds
		_sounds["player_hurt"] = GenerateSweep(0.12f, 200f, 80f, WaveForm.Square, 0.5f);
		_sounds["pogo"] = GenerateSweep(0.08f, 300f, 800f, WaveForm.Square, 0.4f);
		_sounds["dash"] = GenerateNoise(0.1f, 0.5f);
		_sounds["wall_slide"] = GenerateNoise(0.03f, 0.2f);
		_sounds["heavy_slash"] = GenerateNoise(0.12f, 0.7f);
		_sounds["deflect"] = GenerateSweep(0.1f, 800f, 1200f, WaveForm.Square, 0.5f);
		_sounds["charge_windup"] = GenerateSweep(0.15f, 100f, 400f, WaveForm.Square, 0.4f);
		_sounds["boss_slam"] = GenerateSweep(0.2f, 150f, 40f, WaveForm.Square, 0.6f);
		_sounds["projectile_fire"] = GenerateSweep(0.08f, 600f, 300f, WaveForm.Square, 0.4f);

		// New sounds
		_sounds["save"] = GenerateArpeggio(new[] { 72, 76, 79 }, 0.12f, WaveForm.Sine, 0.3f);
		_sounds["ability_unlock"] = GenerateArpeggio(new[] { 72, 75, 77, 79, 84 }, 0.1f, WaveForm.Sine, 0.35f);
		_sounds["menu_select"] = GenerateLayered(new SoundLayer[]
		{
			new() { Duration = 0.06f, StartFreq = 880f, EndFreq = 880f, Wave = WaveForm.Sine, Volume = 0.3f },
		});
		_sounds["menu_confirm"] = GenerateArpeggio(new[] { 69, 81 }, 0.08f, WaveForm.Sine, 0.3f);
	}

	// ─── Sound Helpers ──────────────────────────────────────────────

	private AudioStreamWav GenerateNoise(float duration, float volume)
	{
		int sampleCount = (int)(SampleRate * duration);
		var data = new byte[sampleCount * 2];

		for (int i = 0; i < sampleCount; i++)
		{
			float t = (float)i / sampleCount;
			float envelope = 1f - t;
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

	private AudioStreamWav GenerateSweep(float duration, float startFreq, float endFreq, WaveForm waveType, float volume)
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
			if (waveType == WaveForm.Square)
				sample = (Mathf.Sin(phase * Mathf.Tau) >= 0 ? 1f : -1f) * volume * envelope;
			else
				sample = (float)GD.RandRange(-1.0, 1.0) * volume * envelope;

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

	private AudioStreamWav GenerateLayered(SoundLayer[] layers)
	{
		float maxDuration = 0f;
		foreach (var layer in layers)
			maxDuration = Mathf.Max(maxDuration, layer.Duration);

		int totalSamples = (int)(SampleRate * maxDuration);
		var buffer = new float[totalSamples];

		foreach (var layer in layers)
		{
			int layerSamples = (int)(SampleRate * layer.Duration);
			float phase = 0f;
			uint noiseState = 54321;

			for (int i = 0; i < layerSamples && i < totalSamples; i++)
			{
				float t = (float)i / layerSamples;
				float envelope = 1f - t;
				float freq = Mathf.Lerp(layer.StartFreq, layer.EndFreq, t);

				float sample = GenerateWaveSample(layer.Wave, phase, ref noiseState);
				buffer[i] += sample * layer.Volume * envelope;

				phase += freq / SampleRate;
				if (phase > 1f) phase -= 1f;
			}
		}

		var data = new byte[totalSamples * 2];
		for (int i = 0; i < totalSamples; i++)
		{
			short pcm = (short)(Mathf.Clamp(buffer[i], -1f, 1f) * 32767);
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

	private AudioStreamWav GenerateArpeggio(int[] notes, float noteLength, WaveForm wave, float volume)
	{
		float totalDuration = notes.Length * noteLength;
		int totalSamples = (int)(SampleRate * totalDuration);
		var data = new byte[totalSamples * 2];
		float phase = 0f;
		uint noiseState = 12345;

		for (int n = 0; n < notes.Length; n++)
		{
			float freq = NoteToFreq(notes[n]);
			int startSample = (int)(n * noteLength * SampleRate);
			int endSample = (int)((n + 1) * noteLength * SampleRate);

			for (int i = startSample; i < endSample && i < totalSamples; i++)
			{
				float t = (float)(i - startSample) / (endSample - startSample);
				float env = 1f - t * 0.5f;
				float sample = GenerateWaveSample(wave, phase, ref noiseState) * volume * env;

				short pcm = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767);
				data[i * 2] = (byte)(pcm & 0xFF);
				data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);

				phase += freq / SampleRate;
				if (phase > 1f) phase -= 1f;
			}
		}

		var wav = new AudioStreamWav();
		wav.Format = AudioStreamWav.FormatEnum.Format16Bits;
		wav.MixRate = SampleRate;
		wav.Stereo = false;
		wav.Data = data;
		return wav;
	}

	// ─── Wave / Frequency Helpers ───────────────────────────────────

	private static float NoteToFreq(int midiNote)
	{
		return 440f * Mathf.Pow(2f, (midiNote - 69) / 12f);
	}

	private static float GenerateWaveSample(WaveForm type, float phase, ref uint noiseState)
	{
		return type switch
		{
			WaveForm.Sine => Mathf.Sin(phase * Mathf.Tau),
			WaveForm.Square => phase < 0.5f ? 1f : -1f,
			WaveForm.Triangle => 4f * Mathf.Abs(phase - 0.5f) - 1f,
			WaveForm.Noise => NextNoise(ref noiseState),
			_ => 0f,
		};
	}

	private static float NextNoise(ref uint state)
	{
		state = state * 1103515245 + 12345;
		return ((state >> 16) & 0x7FFF) / (float)0x7FFF * 2f - 1f;
	}

	private static float CalculateEnvelope(float time, float noteLength, TrackVoice voice)
	{
		// Attack
		if (time < voice.Attack)
			return voice.Attack > 0f ? time / voice.Attack : 1f;

		// Decay
		float afterAttack = time - voice.Attack;
		if (afterAttack < voice.Decay)
			return 1f + (voice.Sustain - 1f) * (voice.Decay > 0f ? afterAttack / voice.Decay : 1f);

		// Release (at the end of note)
		float releaseStart = noteLength - voice.Release;
		if (time >= releaseStart && voice.Release > 0f)
		{
			float progress = (time - releaseStart) / voice.Release;
			return voice.Sustain * (1f - Mathf.Clamp(progress, 0f, 1f));
		}

		// Sustain
		return voice.Sustain;
	}

	// ─── Music Track Rendering ──────────────────────────────────────

	private AudioStreamWav RenderTrack(int bpm, TrackVoice[] voices)
	{
		const int beatsPerTrack = 32;
		float beatDuration = 60f / bpm;
		float trackDuration = beatsPerTrack * beatDuration;
		int totalSamples = (int)(SampleRate * trackDuration);
		var mixBuffer = new float[totalSamples];

		for (int v = 0; v < voices.Length; v++)
		{
			var voice = voices[v];
			float phase = 0f;
			uint noiseState = (uint)(12345 + v * 7919);

			// Find contiguous note regions for proper sustain
			int beat = 0;
			while (beat < beatsPerTrack)
			{
				int note = voice.Notes[beat % voice.Notes.Length];
				if (note == 0) { beat++; continue; }

				// Find how many consecutive beats have the same note
				int startBeat = beat;
				while (beat < beatsPerTrack && voice.Notes[beat % voice.Notes.Length] == note)
					beat++;

				float noteStartTime = startBeat * beatDuration;
				float noteDuration = (beat - startBeat) * beatDuration;
				int startSample = (int)(noteStartTime * SampleRate);
				int endSample = (int)((noteStartTime + noteDuration) * SampleRate);
				float freq = NoteToFreq(note);

				for (int i = startSample; i < endSample && i < totalSamples; i++)
				{
					float t = (float)(i - startSample) / SampleRate;
					float envelope = CalculateEnvelope(t, noteDuration, voice);
					float sample = GenerateWaveSample(voice.Wave, phase, ref noiseState);
					mixBuffer[i] += sample * envelope * voice.Volume;

					phase += freq / SampleRate;
					if (phase > 1f) phase -= 1f;
				}
			}
		}

		// Normalize if needed
		float maxAmp = 0f;
		for (int i = 0; i < totalSamples; i++)
			maxAmp = Mathf.Max(maxAmp, Mathf.Abs(mixBuffer[i]));

		if (maxAmp > 1f)
		{
			float scale = 0.9f / maxAmp;
			for (int i = 0; i < totalSamples; i++)
				mixBuffer[i] *= scale;
		}

		// Convert to 16-bit PCM
		var data = new byte[totalSamples * 2];
		for (int i = 0; i < totalSamples; i++)
		{
			short pcm = (short)(Mathf.Clamp(mixBuffer[i], -1f, 1f) * 32767);
			data[i * 2] = (byte)(pcm & 0xFF);
			data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
		}

		var wav = new AudioStreamWav();
		wav.Format = AudioStreamWav.FormatEnum.Format16Bits;
		wav.MixRate = SampleRate;
		wav.Stereo = false;
		wav.Data = data;
		wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
		wav.LoopEnd = totalSamples;
		return wav;
	}

	// ─── Track Definitions ──────────────────────────────────────────

	private void GenerateAllTracks()
	{
		// MIDI reference: C2=36, Eb2=39, F2=41, G2=43, Bb2=46
		// C3=48, Eb3=51, F3=53, G3=55, Bb3=58
		// C4=60, Eb4=63, F4=65, G4=67, Bb4=70
		// C5=72, Eb5=75, F5=77, G5=79, C6=84

		_tracks["depths"] = RenderTrack(90, new TrackVoice[]
		{
			// Square bass — slow root movement
			new()
			{
				Notes = new[] { 36, 36, 0, 0, 36, 36, 0, 0, 46, 46, 0, 0, 36, 36, 0, 0,
				                36, 36, 0, 0, 39, 39, 0, 0, 43, 43, 0, 0, 36, 36, 0, 0 },
				Wave = WaveForm.Square, Volume = 0.25f,
				Attack = 0.01f, Decay = 0.1f, Sustain = 0.8f, Release = 0.1f,
			},
			// Sine melody — sparse atmospheric
			new()
			{
				Notes = new[] { 0, 0, 60, 0, 0, 0, 0, 63, 0, 65, 0, 0, 0, 0, 63, 0,
				                0, 0, 0, 67, 0, 0, 63, 0, 0, 60, 0, 0, 0, 0, 0, 0 },
				Wave = WaveForm.Sine, Volume = 0.15f,
				Attack = 0.05f, Decay = 0.1f, Sustain = 0.5f, Release = 0.2f,
			},
			// Sine pad — sustained root
			new()
			{
				Notes = new[] { 48, 48, 48, 48, 48, 48, 48, 48, 46, 46, 46, 46, 48, 48, 48, 48,
				                48, 48, 48, 48, 51, 51, 51, 51, 55, 55, 55, 55, 48, 48, 48, 48 },
				Wave = WaveForm.Sine, Volume = 0.12f,
				Attack = 0.3f, Decay = 0.1f, Sustain = 1.0f, Release = 0.3f,
			},
		});

		_tracks["boss"] = RenderTrack(140, new TrackVoice[]
		{
			// Square bass — driving, aggressive
			new()
			{
				Notes = new[] { 36, 0, 36, 36, 0, 36, 36, 0, 39, 0, 39, 39, 0, 39, 36, 0,
				                36, 0, 36, 36, 0, 36, 36, 0, 43, 0, 39, 0, 36, 0, 36, 0 },
				Wave = WaveForm.Square, Volume = 0.3f,
				Attack = 0.005f, Decay = 0.05f, Sustain = 0.7f, Release = 0.05f,
			},
			// Square melody — aggressive pentatonic
			new()
			{
				Notes = new[] { 60, 0, 63, 0, 60, 63, 65, 0, 63, 0, 60, 0, 63, 65, 67, 0,
				                60, 0, 63, 0, 60, 63, 65, 0, 67, 65, 63, 60, 63, 0, 60, 0 },
				Wave = WaveForm.Square, Volume = 0.2f,
				Attack = 0.005f, Decay = 0.05f, Sustain = 0.5f, Release = 0.05f,
			},
			// Noise percussion — rhythmic hits
			new()
			{
				Notes = new[] { 1, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 1,
				                1, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1, 0, 1, 1 },
				Wave = WaveForm.Noise, Volume = 0.15f,
				Attack = 0.001f, Decay = 0.03f, Sustain = 0.0f, Release = 0.0f,
			},
		});

		// D minor: D2=38, F2=41, G2=43, A2=45, Bb2=46
		// D3=50, F3=53, G3=55, A3=57, Bb3=58
		// D4=62, F4=65, G4=67, A4=69, Bb4=70, C5=72, D5=74
		_tracks["catacombs"] = RenderTrack(100, new TrackVoice[]
		{
			// Square bass — pulsing D minor root, darker feel
			new()
			{
				Notes = new[] { 38, 0, 38, 0, 38, 38, 0, 0, 41, 0, 41, 0, 43, 43, 0, 0,
				                38, 0, 38, 0, 38, 38, 0, 0, 46, 0, 45, 0, 43, 0, 38, 0 },
				Wave = WaveForm.Square, Volume = 0.28f,
				Attack = 0.01f, Decay = 0.08f, Sustain = 0.7f, Release = 0.08f,
			},
			// Triangle melody — eerie, sparse
			new()
			{
				Notes = new[] { 0, 0, 62, 0, 0, 65, 0, 0, 67, 0, 65, 0, 62, 0, 0, 0,
				                0, 0, 0, 69, 0, 0, 67, 0, 65, 0, 62, 0, 0, 0, 0, 0 },
				Wave = WaveForm.Triangle, Volume = 0.16f,
				Attack = 0.03f, Decay = 0.1f, Sustain = 0.4f, Release = 0.15f,
			},
			// Sine pad — sustained minor chord
			new()
			{
				Notes = new[] { 50, 50, 50, 50, 50, 50, 50, 50, 53, 53, 53, 53, 55, 55, 55, 55,
				                50, 50, 50, 50, 50, 50, 50, 50, 58, 58, 58, 58, 50, 50, 50, 50 },
				Wave = WaveForm.Sine, Volume = 0.10f,
				Attack = 0.3f, Decay = 0.1f, Sustain = 1.0f, Release = 0.3f,
			},
			// Noise percussion — subtle rhythm
			new()
			{
				Notes = new[] { 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0,
				                0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 1, 0, 1, 0 },
				Wave = WaveForm.Noise, Volume = 0.08f,
				Attack = 0.001f, Decay = 0.02f, Sustain = 0.0f, Release = 0.0f,
			},
		});

		_tracks["title"] = RenderTrack(80, new TrackVoice[]
		{
			// Sine pad — long sustained chords
			new()
			{
				Notes = new[] { 48, 48, 48, 48, 48, 48, 48, 48, 53, 53, 53, 53, 55, 55, 55, 55,
				                48, 48, 48, 48, 48, 48, 48, 48, 51, 51, 51, 51, 48, 48, 48, 48 },
				Wave = WaveForm.Sine, Volume = 0.12f,
				Attack = 0.5f, Decay = 0.2f, Sustain = 1.0f, Release = 0.5f,
			},
			// Sine melody — sparse, gentle
			new()
			{
				Notes = new[] { 0, 0, 0, 0, 72, 0, 0, 0, 0, 0, 75, 0, 0, 0, 72, 0,
				                0, 0, 0, 0, 0, 0, 77, 0, 0, 75, 0, 0, 72, 0, 0, 0 },
				Wave = WaveForm.Sine, Volume = 0.15f,
				Attack = 0.1f, Decay = 0.15f, Sustain = 0.5f, Release = 0.3f,
			},
		});
	}
}
