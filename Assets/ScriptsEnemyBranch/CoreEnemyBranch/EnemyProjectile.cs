using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A bullet. The shooter calls Launch() right after spawning it.
/// It flies in a straight line and damages the first thing with a Health it touches.
/// On a hit it stops, plays its "Hit" animation if it has an Animator, then removes itself.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Hit")]
    [Tooltip("Seconds the hit animation plays before the bullet is removed. Set it to the length of the hit clip. 0 = removed straight away.")]
    [SerializeField, Min(0f)] private float hitAnimationTime = 0f;

    [Header("Events (hook up audio, VFX)")]
    public UnityEvent onHit = new UnityEvent();

    private static readonly int HitHash = Animator.StringToHash("Hit");

    private Rigidbody2D _body;
    private Collider2D _collider;
    private Animator _animator;
    private float _damage;
    private float _expireTime;
    private bool _hasHit;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _animator = GetComponentInChildren<Animator>();

        _body.bodyType = RigidbodyType2D.Kinematic;
        _body.gravityScale = 0f;
        _collider.isTrigger = true;
    }

    public void Launch(Vector2 direction, float speed, float damage, float lifetime)
    {
        _damage = damage;
        _expireTime = Time.time + lifetime;
        transform.right = direction;                // turn the sprite to face where it is going
        _body.linearVelocity = direction * speed;
    }

    private void Update()
    {
        if (!_hasHit && Time.time >= _expireTime) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasHit) return;
        _hasHit = true;

        Health victim = other.GetComponentInParent<Health>();
        if (victim != null) victim.TakeDamage(_damage);

        _body.linearVelocity = Vector2.zero;
        _collider.enabled = false;

        if (_animator != null) _animator.SetTrigger(HitHash);
        onHit.Invoke();

        Destroy(gameObject, hitAnimationTime);
    }
}