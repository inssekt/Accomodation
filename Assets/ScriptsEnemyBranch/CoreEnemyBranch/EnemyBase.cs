using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Health))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private string targetTag = "Player";

    [Tooltip("Layers that block sight (walls). Leave empty to ignore line of sight.")]
    [SerializeField] private LayerMask sightBlockers;

    [Header("Death")]
    [Tooltip("Seconds between dying and being removed. Use it to play a death animation.")]
    [SerializeField, Min(0f)] private float destroyDelay = 0.3f;
    public UnityEvent onDeath;

    [Header("Placeholder feedback")]
    [SerializeField] private SpriteRenderer tintRenderer;

    protected Health HealthComponent { get; private set; }
    protected Transform Target { get; private set; }

    private IDamageable _targetDamageable;
    private float _nextTargetSearchTime;
    private Color _baseTint = Color.white;

    /// <summary>True when there is a living target. Searches for one (twice a second) if needed.</summary>
    protected bool HasValidTarget
    {
        get
        {
            if (Target == null) SearchForTarget();
            return Target != null && (_targetDamageable == null || _targetDamageable.IsAlive);
        }
    }

    protected virtual void Awake()
    {
        HealthComponent = GetComponent<Health>();
        if (tintRenderer == null) tintRenderer = GetComponentInChildren<SpriteRenderer>();
        if (tintRenderer != null) _baseTint = tintRenderer.color;
    }

    protected virtual void OnEnable() => HealthComponent.Died += HandleDied;

    protected virtual void OnDisable()
    {
        if (HealthComponent != null) HealthComponent.Died -= HandleDied;
    }

    protected bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        if (sightBlockers.value == 0) return true;
        return Physics2D.Linecast(from, to, sightBlockers).collider == null;
    }

    protected void SetTint(Color color)
    {
        if (tintRenderer != null) tintRenderer.color = color;
    }

    protected void ClearTint()
    {
        if (tintRenderer != null) tintRenderer.color = _baseTint;
    }

    /// <summary>Optional hook for subclasses: drop loot, spawn effects.</summary>
    protected virtual void OnDied() { }

    private void SearchForTarget()
    {
        if (Time.time < _nextTargetSearchTime) return;
        _nextTargetSearchTime = Time.time + 0.5f;

        GameObject found = GameObject.FindGameObjectWithTag(targetTag);
        Target = found != null ? found.transform : null;
        _targetDamageable = found != null ? found.GetComponent<IDamageable>() : null;
    }

    private void HandleDied()
    {
        StopAllCoroutines();
        onDeath?.Invoke();
        OnDied();

        foreach (Collider2D col in GetComponentsInChildren<Collider2D>()) col.enabled = false;

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.SetVelocity(Vector2.zero);
            body.simulated = false;
        }

        enabled = false;
        Destroy(gameObject, destroyDelay);
    }
}