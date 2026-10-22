using UnityEngine;

/// <summary>Everything a target needs to know about one hit.</summary>
public readonly struct DamageInfo
{
    public readonly float Amount;
    public readonly GameObject Source;
    public readonly Vector2 HitDirection;

    /// <summary>Set true for hazards that must hurt even during invulnerability frames.</summary>
    public readonly bool IgnoreInvulnerability;

    public DamageInfo(float amount, GameObject source, Vector2 hitDirection, bool ignoreInvulnerability = false)
    {
        Amount = amount;
        Source = source;
        HitDirection = hitDirection;
        IgnoreInvulnerability = ignoreInvulnerability;
    }
}