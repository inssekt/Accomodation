using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A stationary enemy that shoots at the player. Every value below can be tweaked in the Inspector.
/// Fan, ring and spiral patterns come from Projectiles Per Shot, Spread Angle and Spin Per Shot.
/// </summary>
public class ShootingEnemy : EnemyBase
{
    [Header("Detection")]
    [SerializeField, Min(0f)] private float detectionRange = 9f;
    [SerializeField] private bool requireLineOfSight = true;
    [Tooltip("Layers that block the enemy's view. Set this to Wall.")]
    [SerializeField] private LayerMask sightBlockers;

    [Header("Timing")]
    [Tooltip("Seconds between shots.")]
    [SerializeField, Min(0.1f)] private float fireInterval = 1.5f;
    [Tooltip("Warning time before each shot. The sprite flashes the wind-up colour.")]
    [SerializeField, Min(0f)] private float windupTime = 0.4f;
    [SerializeField] private Color windupColor = new Color(1f, 0.6f, 0.2f);

    [Header("Projectile")]
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField, Min(0f)] private float damage = 10f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 6f;
    [Tooltip("Seconds before a projectile that hits nothing disappears.")]
    [SerializeField, Min(0.1f)] private float projectileLifetime = 4f;

    [Header("Pattern")]
    [Tooltip("1 = a single shot. 3 = a fan. 8 with a 360 spread = a ring.")]
    [SerializeField, Min(1)] private int projectilesPerShot = 1;
    [Tooltip("Total angle the projectiles are spread across, in degrees. 360 = a full ring.")]
    [SerializeField, Range(0f, 360f)] private float spreadAngle = 0f;
    [SerializeField] private bool aimAtPlayer = true;
    [Tooltip("Direction in degrees when Aim At Player is off. 0 = right, 90 = up.")]
    [SerializeField] private float fixedAngle = 0f;
    [Tooltip("Degrees added to the fixed angle after every shot. Makes a spiral.")]
    [SerializeField] private float spinPerShot = 0f;

    [Header("Events (hook up audio, VFX, animation)")]
    public UnityEvent onWindup = new UnityEvent();
    public UnityEvent onFire = new UnityEvent();

    private bool _winding;
    private float _fireAt;
    private float _nextShotTime;
    private float _currentFixedAngle;

    protected override void Awake()
    {
        base.Awake();
        _currentFixedAngle = fixedAngle;
    }

    protected override void Tick()
    {
        bool canSee = CanSeeTarget();

        if (_winding)
        {
            if (!canSee) { CancelWindup(); return; }
            if (Time.time >= _fireAt) Fire();
            return;
        }

        if (canSee && Time.time >= _nextShotTime) BeginWindup();
    }

    private bool CanSeeTarget()
    {
        if (!HasTarget) return false;

        Vector2 from = transform.position;
        Vector2 to = target.position;
        if ((to - from).sqrMagnitude > detectionRange * detectionRange) return false;
        if (!requireLineOfSight) return true;

        return !Physics2D.Linecast(from, to, sightBlockers);
    }

    private void BeginWindup()
    {
        _winding = true;
        _fireAt = Time.time + windupTime;
        SetTint(windupColor);
        onWindup.Invoke();
    }

    private void CancelWindup()
    {
        _winding = false;
        ClearTint();
    }

    private void Fire()
    {
        _winding = false;
        ClearTint();
        _nextShotTime = Time.time + fireInterval;

        if (projectilePrefab == null)
        {
            Debug.LogWarning($"{name}: no Projectile Prefab assigned.", this);
            return;
        }

        Vector2 origin = transform.position;

        float baseAngle;
        if (aimAtPlayer)
        {
            Vector2 toTarget = (Vector2)target.position - origin;
            baseAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        }
        else
        {
            baseAngle = _currentFixedAngle;
            _currentFixedAngle += spinPerShot;
        }

        // Spread the projectiles evenly. A full ring spreads over N gaps, so the last shot does not land on the first.
        bool fullRing = spreadAngle >= 360f;
        float step = projectilesPerShot == 1 ? 0f : spreadAngle / (fullRing ? projectilesPerShot : projectilesPerShot - 1);
        float start = (projectilesPerShot == 1 || fullRing) ? 0f : -spreadAngle * 0.5f;

        for (int i = 0; i < projectilesPerShot; i++)
        {
            float radians = (baseAngle + start + step * i) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            EnemyProjectile projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
            projectile.Launch(direction, projectileSpeed, damage, projectileLifetime);
        }

        onFire.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}