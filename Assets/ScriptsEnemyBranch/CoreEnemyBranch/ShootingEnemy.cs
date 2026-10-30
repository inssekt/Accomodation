using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A shooting enemy. Every value below can be tweaked in the Inspector.
/// Fan, ring and spiral patterns come from Projectiles Per Shot, Spread Angle and Spin Per Shot.
/// With Can Move ticked it wanders inside an area around where it was placed, and needs a
/// Rigidbody 2D to do so. With Can Move unticked it stands still like a turret.
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

    [Header("Movement")]
    [Tooltip("Tick to let this shooter wander. It needs a Rigidbody 2D. Unticked, it stands still.")]
    [SerializeField] private bool canMove = false;
    [Tooltip("Width and height of the area it wanders in, in world units. The area is centred on where the enemy is placed.")]
    [SerializeField] private Vector2 areaSize = new Vector2(6f, 4f);
    [SerializeField, Min(0f)] private float moveSpeed = 1.5f;
    [Tooltip("It rests for a random time between these two values at each stop.")]
    [SerializeField, Min(0f)] private float minPauseTime = 1f;
    [SerializeField, Min(0f)] private float maxPauseTime = 3f;
    [Tooltip("Stands still for this long after each shot, so the attack animation can play.")]
    [SerializeField, Min(0f)] private float pauseAfterShot = 0.3f;
    [Tooltip("Off = it stands still and shoots whenever it can see the player. On = it keeps wandering between shots.")]
    [SerializeField] private bool moveWhileTargetVisible = true;

    [Header("Events (hook up audio, VFX, animation)")]
    public UnityEvent onWindup = new UnityEvent();
    public UnityEvent onFire = new UnityEvent();

    // How close counts as having reached the spot it was walking to.
    private const float ArriveDistance = 0.1f;

    private bool _winding;
    private float _fireAt;
    private float _nextShotTime;
    private float _currentFixedAngle;

    private Rigidbody2D _body;
    private bool _movementEnabled;
    private Vector2 _areaCenter;
    private Vector2 _destination;
    private bool _walking;
    private float _walkTimeout;
    private float _pauseUntil;
    private float _holdUntil;
    private Vector2 _desiredVelocity;

    protected override void Awake()
    {
        base.Awake();
        _currentFixedAngle = fixedAngle;
        _body = GetComponent<Rigidbody2D>();

        if (canMove && _body == null)
            Debug.LogWarning($"{name}: Can Move is ticked but there is no Rigidbody 2D, so it will stand still.", this);

        _movementEnabled = canMove && _body != null;
        _pauseUntil = Time.time + Random.Range(minPauseTime, maxPauseTime);
    }

    private void Start()
    {
        // Read the position here, not in Awake, in case a spawner moves the enemy right after creating it.
        _areaCenter = transform.position;
    }

    private void OnValidate()
    {
        maxPauseTime = Mathf.Max(maxPauseTime, minPauseTime);
        areaSize = Vector2.Max(areaSize, Vector2.zero);
    }

    protected override void Tick()
    {
        bool canSee = CanSeeTarget();

        UpdateShooting(canSee);
        UpdateMovement(canSee);
    }

    private void FixedUpdate()
    {
        if (_body == null || !health.IsAlive) return;

        // Always set the velocity, so a shooter that cannot move is not pushed around either.
        _body.linearVelocity = _movementEnabled ? _desiredVelocity : Vector2.zero;
    }

    private void UpdateShooting(bool canSee)
    {
        if (_winding)
        {
            if (!canSee) { CancelWindup(); return; }
            if (Time.time >= _fireAt) Fire();
            return;
        }

        if (canSee && Time.time >= _nextShotTime) BeginWindup();
    }

    private void UpdateMovement(bool canSee)
    {
        if (!_movementEnabled) return;

        bool holdStill = _winding
            || Time.time < _holdUntil
            || (canSee && !moveWhileTargetVisible);

        if (holdStill)
        {
            _desiredVelocity = Vector2.zero;
            return;
        }

        if (_walking)
        {
            Vector2 toDestination = _destination - (Vector2)transform.position;

            if (toDestination.magnitude <= ArriveDistance || Time.time >= _walkTimeout)
            {
                _walking = false;
                _desiredVelocity = Vector2.zero;
                _pauseUntil = Time.time + Random.Range(minPauseTime, maxPauseTime);
            }
            else
            {
                _desiredVelocity = toDestination.normalized * moveSpeed;
            }
        }
        else
        {
            _desiredVelocity = Vector2.zero;
            if (Time.time >= _pauseUntil) PickDestination();
        }
    }

    private void PickDestination()
    {
        Vector2 half = areaSize * 0.5f;
        _destination = _areaCenter + new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y));
        _walking = true;

        // Give up on this spot if something blocks the way, instead of pushing against a wall forever.
        float distance = Vector2.Distance(transform.position, _destination);
        _walkTimeout = Time.time + distance / Mathf.Max(moveSpeed, 0.01f) * 2f + 0.5f;
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
        _holdUntil = Time.time + pauseAfterShot;

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

        if (!canMove) return;

        Vector2 center = Application.isPlaying ? _areaCenter : (Vector2)transform.position;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, areaSize);
    }
}