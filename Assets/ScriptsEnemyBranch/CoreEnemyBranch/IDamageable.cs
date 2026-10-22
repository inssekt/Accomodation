/// <summary>Implemented by anything that can take damage. Attackers only ever talk to this interface.</summary>
public interface IDamageable
{
    bool IsAlive { get; }
    void TakeDamage(DamageInfo info);
}