using Godot;
using System.Collections.Generic;

namespace metvania_1;

public partial class GameState : Node
{
	public HashSet<string> UnlockedAbilities { get; set; } = new();
	public HashSet<string> CollectedItems { get; set; } = new();
	public Vector2 PlayerSpawnPosition { get; set; } = new(56, 312);
	public string CurrentRoom { get; set; } = "The Depths";
	public int MaxHealth { get; set; } = 5;

	[Signal]
	public delegate void AbilityUnlockedEventHandler(string abilityName);

	[Signal]
	public delegate void ItemCollectedEventHandler(string itemId);

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
		PlayerSpawnPosition = new Vector2(56, 312);
		CurrentRoom = "The Depths";
		MaxHealth = 5;
	}
}
