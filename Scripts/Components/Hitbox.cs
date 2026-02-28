using Godot;

namespace metvania_1;

public partial class Hitbox : Area2D
{
	[Export] public int Damage { get; set; } = 1;

	[Signal]
	public delegate void HitLandedEventHandler(Node2D target);

	public void Activate()
	{
		Monitoring = true;
	}

	public void Deactivate()
	{
		Monitoring = false;
	}

	public override void _Ready()
	{
		Monitoring = false;
		AreaEntered += OnAreaEntered;
	}

	private void OnAreaEntered(Area2D area)
	{
		if (area is Hurtbox hurtbox)
		{
			hurtbox.HandleHit(Damage, this);
			// Notify owner (for pogo etc.)
			var target = hurtbox.GetParent() as Node2D;
			EmitSignal(SignalName.HitLanded, target);

			// Hit spark at midpoint between hitbox and hurtbox
			var midpoint = (GlobalPosition + area.GlobalPosition) / 2f;
			var effects = GetNodeOrNull<EffectsManager>("/root/EffectsManager");
			effects?.SpawnParticles(midpoint, ParticleType.HitSpark);
			effects?.HitFreeze();
			var audio = GetNodeOrNull<AudioManager>("/root/AudioManager");
			audio?.Play("hit");
		}
	}
}
