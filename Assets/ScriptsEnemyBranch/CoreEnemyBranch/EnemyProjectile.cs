using UnityEngine;

/// <summary>
/// A bullet. The shooter calls Launch() right after spawning it.
/// It flies in a straight line, damages the first thing with a Health it touches, then disappears.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    private Rigidbody2D _body;
    private float _damage;
    private bool _hasHit;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _body.bodyType = RigidbodyType2D.Kinematic;
        _body.gravityScale = 0f;
        GetComponent<Collider2D>().isTrigger = true;
    }

    public void Launch(Vector2 direction, float speed, float damage, float lifetime)
    {
        _damage = damage;
        transform.right = direction;                // turn the sprite to face where it is going
        _body.linearVelocity = direction * speed;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasHit) return;
        _hasHit = true;

        Health victim = other.GetComponentInParent<Health>();
        if (victim != null) victim.TakeDamage(_damage);

        Destroy(gameObject);
    }
}