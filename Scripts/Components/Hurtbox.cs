using Godot;

namespace metvania_1;

public partial class Hurtbox : Area2D
{
	[Export] public HealthComponent Health { get; set; }

	[Signal]
	public delegate void HurtEventHandler(int damage, Hitbox source);

	public override void _Ready()
	{
		Monitorable = true;
		Monitoring = false;
	}

	public void HandleHit(int damage, Hitbox source)
	{
		EmitSignal(SignalName.Hurt, damage, source);
		Health?.TakeDamage(damage);
	}
}
