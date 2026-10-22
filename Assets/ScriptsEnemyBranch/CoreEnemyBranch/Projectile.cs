using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Optional. Cleared on launch so pooled bullets do not leave a streak.")]
    [SerializeField] private TrailRenderer trail;

    [Tooltip("Layers that stop the projectile: whatever it can hurt, plus walls.")]
    [SerializeField] private LayerMask collisionMask;

    private Rigidbody2D _body;
    private ProjectileData _data;
    private GameObject _owner;
    private Action<Projectile> _release;
    private Vector2 _direction;
    private float _speed;
    private float _despawnTime;
    private bool _active;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _body.bodyType = RigidbodyType2D.Kinematic;
        _body.gravityScale = 0f;
        GetComponent<Collider2D>().isTrigger = true;

        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>Called by the launcher every time this projectile is fired.</summary>
    public void Launch(ProjectileData data, Vector2 position, Vector2 direction,
                       GameObject owner, Action<Projectile> release)
    {
        _data = data;
        _owner = owner;
        _release = release;
        _direction = direction.normalized;
        _speed = data.Speed;
        _despawnTime = Time.time + data.Lifetime;

        float angle = VectorUtils.ToAngle(_direction);
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
        transform.localScale = Vector3.one * data.Scale;

        if (spriteRenderer != null)
        {
            if (data.Sprite != null) spriteRenderer.sprite = data.Sprite;
            spriteRenderer.color = data.Tint;
        }

        gameObject.SetActive(true);

        // Sync the physics body after activation so the first FixedUpdate starts in the right place.
        _body.position = position;
        _body.rotation = angle;
        if (trail != null) trail.Clear();

        _active = true;
    }

    private void FixedUpdate()
    {
        if (!_active) return;
        float dt = Time.fixedDeltaTime;

        if (_data.CurveDegreesPerSecond != 0f)
            _direction = VectorUtils.Rotate(_direction, _data.CurveDegreesPerSecond * dt);

        _speed = Mathf.Max(0f, _speed + _data.Acceleration * dt);

        _body.MovePosition(_body.position + _direction * (_speed * dt));
        _body.MoveRotation(VectorUtils.ToAngle(_direction));
    }

    private void Update()
    {
        if (_active && Time.time >= _despawnTime) Despawn();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_active) return;
        if (((1 << other.gameObject.layer) & collisionMask.value) == 0) return;
        if (_owner != null && other.transform.IsChildOf(_owner.transform)) return;

        IDamageable target = other.GetComponentInParent<IDamageable>();
        target?.TakeDamage(new DamageInfo(_data.Damage, _owner, _direction));

        if (_data.HitEffectPrefab != null)
            Instantiate(_data.HitEffectPrefab, transform.position, Quaternion.identity);

        Despawn();
    }

    private void Despawn()
    {
        if (!_active) return;
        _active = false;

        if (_release != null) _release(this);
        else gameObject.SetActive(false);
    }
}