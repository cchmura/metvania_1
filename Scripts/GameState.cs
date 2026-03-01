using Godot;
using System.Collections.Generic;

namespace metvania_1;

public partial class GameState : Node
{
	public HashSet<string> UnlockedAbilities { get; set; } = new();
	public HashSet<string> CollectedItems { get; set; } = new();
	public Vector2 PlayerSpawnPosition { get; set; } = new(40, 136);
	public string CurrentRoom { get; set; } = "The Depths";
	public int MaxHealth { get; set; } = 20;
	public int ActiveSaveSlot { get; set; }
	public int WeaponTier { get; set; } = 1;

	// Multi-level state
	public string CurrentLevelId { get; set; } = "main";
	public HashSet<string> DefeatedBosses { get; set; } = new();

	/// <summary>Backward-compatible: returns whether the current level's boss is defeated.</summary>
	public bool BossDefeated
	{
		get => DefeatedBosses.Contains(CurrentLevelId);
		set
		{
			if (value)
				DefeatedBosses.Add(CurrentLevelId);
			else
				DefeatedBosses.Remove(CurrentLevelId);
		}
	}

	[Signal]
	public delegate void AbilityUnlockedEventHandler(string abilityName);

	[Signal]
	public delegate void ItemCollectedEventHandler(string itemId);

	[Signal]
	public delegate void WeaponTierChangedEventHandler(int tier);

	public bool HasAbility(string ability) => UnlockedAbilities.Contains(ability);

	public void UnlockAbility(string ability)
	{
		if (UnlockedAbilities.Add(ability))
		{
			EmitSignal(SignalName.AbilityUnlocked, ability);
			GD.Print($"Ability unlocked: {ability}");
		}
	}

	public bool IsCollected(string itemId) => CollectedItems.Contains(itemId);

	public void MarkCollected(string itemId)
	{
		if (CollectedItems.Add(itemId))
		{
			EmitSignal(SignalName.ItemCollected, itemId);
		}
	}

	public void Reset()
	{
		UnlockedAbilities.Clear();
		CollectedItems.Clear();
		PlayerSpawnPosition = new Vector2(40, 136);
		CurrentRoom = "The Depths";
		MaxHealth = 20;
		WeaponTier = 1;
		CurrentLevelId = "main";
		DefeatedBosses.Clear();
	}
}
