using UnityEngine;
using UnityEngine.Events;
 
/// <summary>
/// A bullet. The shooter calls Launch() right after spawning it.
/// It flies in a straight line and damages the first thing with a Health it touches.
/// It flies straight through living enemies (Hurt Enemies is off), so enemies never shoot each other.
/// On a hit it stops, plays its "Hit" animation if it has an Animator, then removes itself.
/// The hit animation keeps playing while the game is frozen by Hit Stop (Play During Freeze).
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("Off = the bullet flies straight through living enemies, including the one that fired it. Tick it only if you want friendly fire. The player, walls and bodies in flight stop it either way.")]
    [SerializeField] private bool hurtEnemies = false;
 
    [Header("Hit")]
    [Tooltip("Seconds the hit animation plays before the bullet is removed. Set it to the length of the hit clip. 0 = removed straight away.")]
    [SerializeField, Min(0f)] private float hitAnimationTime = 0f;
    [Tooltip("Ticked = the hit animation keeps playing while the game is frozen by Hit Stop. Unticked = it freezes with everything else.")]
    [SerializeField] private bool playDuringFreeze = true;
 
    [Header("Events (hook up audio, VFX)")]
    public UnityEvent onHit = new UnityEvent();
 
    private static readonly int HitHash = Animator.StringToHash("Hit");
 
    private Rigidbody2D _body;
    private Collider2D _collider;
    private Animator _animator;
    private Vector2 _direction;
    private float _damage;
    private float _expireTime;
    private float _removeTime;
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
        _direction = direction;
        _damage = damage;
        _expireTime = Time.time + lifetime;
        transform.right = direction;                // turn the sprite to face where it is going
        _body.linearVelocity = direction * speed;
    }
 
    private void Update()
    {
        if (_hasHit)
        {
            // Uses the same clock as the animation, so the bullet is removed when its animation has finished.
            // Update still runs while the game is frozen.
            float now = playDuringFreeze ? Time.unscaledTime : Time.time;
            if (now >= _removeTime) Destroy(gameObject);
            return;
        }
 
        if (Time.time >= _expireTime) Destroy(gameObject);
    }
 
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasHit) return;
 
        Health victim = other.GetComponentInParent<Health>();
 
        // Fly straight through living enemies. A dead body in flight is not counted, so it still blocks the bullet.
        if (!hurtEnemies && victim != null && victim.IsAlive && victim.GetComponent<EnemyBase>() != null) return;
 
        _hasHit = true;
 
        // Stop and start the hit animation first, so nothing that happens when the damage lands can skip it.
        _body.linearVelocity = Vector2.zero;
        _collider.enabled = false;
 
        if (_animator != null)
        {
            // An Animator normally follows game time, so a freeze frame would pause the splat.
            // Unscaled Time makes it follow real time instead, so it plays straight through the freeze.
            if (playDuringFreeze) _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            _animator.SetTrigger(HitHash);
        }
 
        onHit.Invoke();
 
        _removeTime = (playDuringFreeze ? Time.unscaledTime : Time.time) + hitAnimationTime;
 
        if (victim != null) victim.TakeDamage(_damage, _direction);
    }
}