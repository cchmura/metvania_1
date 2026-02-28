using Godot;

namespace metvania_1;

public partial class HealthComponent : Node
{
	[Export] public int MaxHealth { get; set; } = 5;
	[Export] public float InvincibilityDuration { get; set; } = 1.0f;

	public int CurrentHealth { get; private set; }
	public bool IsInvincible { get; private set; }

	private float _invincibilityTimer;

	[Signal]
	public delegate void DamagedEventHandler(int amount);

	[Signal]
	public delegate void DiedEventHandler();

	[Signal]
	public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);

	public override void _Ready()
	{
		CurrentHealth = MaxHealth;
	}

	public override void _Process(double delta)
	{
		if (IsInvincible)
		{
			_invincibilityTimer -= (float)delta;
			if (_invincibilityTimer <= 0)
			{
				IsInvincible = false;
			}
		}
	}

	public void TakeDamage(int amount)
	{
		if (IsInvincible || CurrentHealth <= 0) return;

		CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
		EmitSignal(SignalName.Damaged, amount);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

		if (CurrentHealth <= 0)
		{
			EmitSignal(SignalName.Died);
		}
		else
		{
			IsInvincible = true;
			_invincibilityTimer = InvincibilityDuration;
		}
	}

	public void Heal(int amount)
	{
		if (CurrentHealth <= 0) return;
		CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
	}

	public void Reset()
	{
		CurrentHealth = MaxHealth;
		IsInvincible = false;
		_invincibilityTimer = 0;
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
	}
}
