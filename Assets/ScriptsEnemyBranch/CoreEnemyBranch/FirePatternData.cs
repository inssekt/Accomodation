using UnityEngine;

public enum AimMode
{
    /// <summary>Re-aims at the target before every burst.</summary>
    AtTarget,

    /// <summary>Aims once when the volley starts, then keeps that direction.</summary>
    AtTargetLocked,

    /// <summary>Ignores the target and fires along Fixed Angle (0 = right, 90 = up).</summary>
    FixedAngle
}

[CreateAssetMenu(menuName = "Dungeon Crawler/Fire Pattern", fileName = "Pattern_New")]
public class FirePatternData : ScriptableObject
{
    [Header("Projectile")]
    [SerializeField] private ProjectileData projectile;

    [Header("One burst")]
    [SerializeField, Min(1)] private int projectilesPerBurst = 1;

    [Tooltip("Total fan angle in degrees. 360 = an evenly spaced ring.")]
    [SerializeField, Range(0f, 360f)] private float spreadAngle = 0f;

    [Tooltip("Random +/- degrees added to every projectile.")]
    [SerializeField, Min(0f)] private float randomSpread = 0f;

    [Header("Volley (several bursts in a row)")]
    [SerializeField, Min(1)] private int burstCount = 1;
    [SerializeField, Min(0f)] private float timeBetweenBursts = 0.15f;

    [Tooltip("Degrees the whole burst rotates after every burst. Creates spirals.")]
    [SerializeField] private float spinPerBurst = 0f;

    [Header("Aiming")]
    [SerializeField] private AimMode aimMode = AimMode.AtTarget;
    [SerializeField] private float fixedAngle = 0f;

    [Header("Rhythm")]
    [Tooltip("Telegraph time before the volley starts. Play a charge-up animation here.")]
    [SerializeField, Min(0f)] private float windupTime = 0.4f;
    [SerializeField, Min(0f)] private float cooldownAfterVolley = 1.5f;

    public ProjectileData Projectile => projectile;
    public int ProjectilesPerBurst => projectilesPerBurst;
    public float RandomSpread => randomSpread;
    public int BurstCount => burstCount;
    public float TimeBetweenBursts => timeBetweenBursts;
    public float SpinPerBurst => spinPerBurst;
    public AimMode AimMode => aimMode;
    public float FixedAngle => fixedAngle;
    public float WindupTime => windupTime;
    public float CooldownAfterVolley => cooldownAfterVolley;

    /// <summary>Angle in degrees, relative to the aim direction, for projectile number 'index'.</summary>
    public float GetAngleOffset(int index)
    {
        if (projectilesPerBurst <= 1) return 0f;

        bool fullRing = spreadAngle >= 360f;
        float step = fullRing ? 360f / projectilesPerBurst : spreadAngle / (projectilesPerBurst - 1);
        float start = fullRing ? 0f : -spreadAngle * 0.5f;
        return start + step * index;
    }
}