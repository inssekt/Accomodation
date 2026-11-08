
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
 
/// <summary>
/// Turns a dead enemy into a corpse instead of deleting it.
/// When the enemy dies, its sprite changes to the death frame and is flung away from the hit.
/// While it flies it damages any enemy it hits and blocks projectiles.
/// When it slows to a stop it lies on the floor for the rest of the level and no longer collides with anything.
/// Put this on the enemy's root object, next to Health and a Rigidbody 2D. EnemyBase starts it when the enemy dies.
/// Which things the flying corpse can touch is decided by the Layer Collision Matrix (Corpse with Wall, Projectile and Enemy).
/// </summary>
[RequireComponent(typeof(Health), typeof(Rigidbody2D))]
public class EnemyCorpse : MonoBehaviour
{
    [Header("Death look")]
    [Tooltip("The sprite shown from the moment of death. Leave empty to keep whichever frame was showing.")]
    [SerializeField] private Sprite deathSprite;
    [Tooltip("The sprite's colour once it has landed. White = unchanged. A darker grey helps the player tell corpses from living enemies.")]
    [SerializeField] private Color settledColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [Tooltip("Added to the sprite's Order in Layer once it has landed. Use -1 if corpses draw on top of living enemies.")]
    [SerializeField] private int settledSortingOrderChange = 0;
    [Tooltip("Found in the children automatically if left empty.")]
    [SerializeField] private SpriteRenderer spriteRenderer;
 
    [Header("Fling")]
    [Tooltip("How fast the body is thrown, in units per second.")]
    [SerializeField, Min(0f)] private float flingSpeed = 16f;
    [Tooltip("How quickly it slows down. Higher = it stops sooner and slides less far.")]
    [SerializeField, Min(0f)] private float slideDrag = 4f;
    [Tooltip("Degrees per second the sprite spins while it flies. 0 = no spin.")]
    [SerializeField, Min(0f)] private float spinSpeed = 540f;
    [Tooltip("How much it bounces off walls. 0 = none, 1 = a perfect bounce.")]
    [SerializeField, Range(0f, 1f)] private float wallBounce = 0.4f;
    [Tooltip("It lands when it slows below this speed.")]
    [SerializeField, Min(0.05f)] private float stopSpeed = 0.5f;
    [Tooltip("It always lands after this many seconds, even if it is still moving.")]
    [SerializeField, Min(0.1f)] private float maxFlingTime = 1.5f;
    [Tooltip("If the killing blow gave no direction, fling away from the object with this tag. Leave empty to drop on the spot instead.")]
    [SerializeField] private string flingAwayFromTag = "Player";
    [Tooltip("The layer the body switches to while it flies. Add it in Project Settings > Tags and Layers.")]
    [SerializeField] private string corpseLayerName = "Corpse";
 
    [Header("Damage while flying")]
    [Tooltip("Damage dealt to each enemy it hits. 0 = it only knocks them.")]
    [SerializeField, Min(0f)] private float damageToEnemies = 1f;
    [Tooltip("It only hurts things while it is moving at least this fast.")]
    [SerializeField, Min(0f)] private float minDamageSpeed = 3f;
    [Tooltip("Share of its speed it keeps after hitting an enemy. 1 = it ploughs straight through at full speed, 0 = it stops dead.")]
    [SerializeField, Range(0f, 1f)] private float speedKeptAfterHit = 0.6f;
 
    [Header("On the floor")]
    [Tooltip("Most corpses kept in the scene. When there are more, the oldest is removed. 0 = never remove any.")]
    [SerializeField, Min(0)] private int maxCorpsesInScene = 50;
 
    [Header("Extra scripts to switch off on death (optional)")]
    [Tooltip("The Animator and the Enemy Animator are switched off automatically. Drag any other scripts here that should stop when the enemy dies.")]
    [SerializeField] private Behaviour[] alsoSwitchOff;
 
    [Header("Events (hook up audio, VFX)")]
    public UnityEvent onLaunched = new UnityEvent();
    public UnityEvent<GameObject> onHitEnemy = new UnityEvent<GameObject>();
    public UnityEvent onSettled = new UnityEvent();
 
    // 1 = dead ahead, 0 = exactly to the side. An enemy must be at least this far in front to be hit.
    private const float InFrontDot = 0.2f;
 
    // Every corpse lying on the floor, oldest first. Used to cap how many there are.
    private static readonly List<EnemyCorpse> SettledCorpses = new List<EnemyCorpse>();
 
    private Health _health;
    private Rigidbody2D _body;
    private Transform _spriteTransform;
    private PhysicsMaterial2D _flyMaterial;
    private readonly HashSet<Health> _alreadyHit = new HashSet<Health>();
    private int _corpseLayer = -1;
    private bool _launched;
    private bool _flying;
    private float _flightTime;
    private float _spinDirection = 1f;
    private Vector2 _velocityBeforeStep;
 
    /// <summary>True while the body is still flying through the air.</summary>
    public bool IsFlying => _flying;
 
    // Makes sure the list starts empty every time Play is pressed.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        SettledCorpses.Clear();
    }
 
    private void Awake()
    {
        _health = GetComponent<Health>();
        _body = GetComponent<Rigidbody2D>();
 
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) _spriteTransform = spriteRenderer.transform;
 
        _corpseLayer = LayerMask.NameToLayer(corpseLayerName);
        if (_corpseLayer < 0)
            Debug.LogWarning($"{name}: there is no layer called '{corpseLayerName}'. Add it in Project Settings > Tags and Layers, or the corpse will keep the Enemy layer.", this);
 
        _flyMaterial = new PhysicsMaterial2D("CorpseFling") { bounciness = wallBounce, friction = 0f };
    }
 
    /// <summary>
    /// Starts the corpse. EnemyBase calls this when the enemy dies.
    /// hitDirection is the way the killing blow was travelling. Zero means it is not known.
    /// </summary>
    public void Launch(Vector2 hitDirection)
    {
        if (_launched) return;
        _launched = true;
 
        SwitchOffLivingBehaviour();
 
        if (deathSprite != null && spriteRenderer != null) spriteRenderer.sprite = deathSprite;
        if (_corpseLayer >= 0) SetLayerOnAll(_corpseLayer);
 
        onLaunched.Invoke();
 
        Vector2 direction = ChooseDirection(hitDirection);
        if (direction.sqrMagnitude < 0.0001f || flingSpeed <= 0f)
        {
            Settle();   // nothing to fling away from, so it just drops where it died
            return;
        }
 
        _body.bodyType = RigidbodyType2D.Dynamic;
        _body.simulated = true;
        _body.gravityScale = 0f;
        _body.linearDamping = slideDrag;
        _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _body.linearVelocity = direction * flingSpeed;
        _body.WakeUp();
 
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>()) col.sharedMaterial = _flyMaterial;
 
        _spinDirection = direction.x >= 0f ? -1f : 1f;   // spins clockwise when thrown to the right
        _velocityBeforeStep = _body.linearVelocity;
        _flightTime = 0f;
        _flying = true;
    }
 
    private void Update()
    {
        if (!_flying || spinSpeed <= 0f || _spriteTransform == null) return;
 
        // The spin slows down as the body slows down.
        float speedShare = Mathf.Clamp01(_body.linearVelocity.magnitude / Mathf.Max(flingSpeed, 0.01f));
        _spriteTransform.Rotate(0f, 0f, _spinDirection * spinSpeed * speedShare * Time.deltaTime);
    }
 
    private void FixedUpdate()
    {
        if (!_flying) return;
 
        // Read before the physics step, so a collision can still see how fast the body was going when it hit.
        _velocityBeforeStep = _body.linearVelocity;
        _flightTime += Time.fixedDeltaTime;
 
        if (_velocityBeforeStep.magnitude < stopSpeed || _flightTime >= maxFlingTime) Settle();
    }
 
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_flying || _velocityBeforeStep.magnitude < minDamageSpeed) return;
 
        Health victim = collision.collider.GetComponentInParent<Health>();
        if (victim == null || victim == _health || !victim.IsAlive) return;
 
        // Only enemies in front of the flight count. An enemy standing beside the body when it died is left alone.
        Vector2 travel = _velocityBeforeStep.normalized;
        Vector2 toVictim = (Vector2)victim.transform.position - (Vector2)transform.position;
        if (Vector2.Dot(toVictim.normalized, travel) < InFrontDot) return;
 
        if (!_alreadyHit.Add(victim)) return;   // each enemy is hit once per throw
 
        victim.TakeDamage(damageToEnemies, travel);
        onHitEnemy.Invoke(victim.gameObject);
 
        _body.linearVelocity *= speedKeptAfterHit;
    }
 
    private void OnDestroy()
    {
        SettledCorpses.Remove(this);
    }
 
    private void SwitchOffLivingBehaviour()
    {
        // An Animator would keep overwriting the death sprite with the idle animation.
        foreach (Animator animator in GetComponentsInChildren<Animator>()) animator.enabled = false;
 
        // The Enemy Animator would flip the sprite to face the way the body is flying.
        EnemyAnimator enemyAnimator = GetComponent<EnemyAnimator>();
        if (enemyAnimator != null) enemyAnimator.enabled = false;
 
        if (alsoSwitchOff == null) return;
        foreach (Behaviour behaviour in alsoSwitchOff)
            if (behaviour != null && behaviour != this) behaviour.enabled = false;
    }
 
    private void SetLayerOnAll(int layer)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>()) t.gameObject.layer = layer;
    }
 
    private Vector2 ChooseDirection(Vector2 hitDirection)
    {
        if (hitDirection.sqrMagnitude > 0.0001f) return hitDirection.normalized;
        if (string.IsNullOrEmpty(flingAwayFromTag)) return Vector2.zero;
 
        GameObject other = GameObject.FindWithTag(flingAwayFromTag);
        if (other == null) return Vector2.zero;
 
        Vector2 away = (Vector2)transform.position - (Vector2)other.transform.position;
        return away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.zero;
    }
 
    private void Settle()
    {
        _flying = false;
 
        _body.linearVelocity = Vector2.zero;
        _body.angularVelocity = 0f;
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>()) col.enabled = false;
        _body.simulated = false;   // from now on the body is only a picture on the floor
 
        if (spriteRenderer != null)
        {
            spriteRenderer.color = settledColor;
            spriteRenderer.sortingOrder += settledSortingOrderChange;
        }
 
        SettledCorpses.Add(this);
        RemoveOldestCorpsesOverTheLimit();
 
        onSettled.Invoke();
        enabled = false;   // nothing left for this script to do
    }
 
    private void RemoveOldestCorpsesOverTheLimit()
    {
        if (maxCorpsesInScene <= 0) return;
 
        while (SettledCorpses.Count > maxCorpsesInScene)
        {
            EnemyCorpse oldest = SettledCorpses[0];
            SettledCorpses.RemoveAt(0);
            if (oldest != null && oldest != this) Destroy(oldest.gameObject);
        }
    }
}
 