using UnityEngine;

/// <summary>
/// Shared code for every enemy: finds the player, tints the sprite, and handles death.
/// A new enemy inherits from this and writes its behaviour in Tick().
/// If a child class overrides Awake(), it must call base.Awake() first.
/// </summary>
[RequireComponent(typeof(Health))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The enemy hunts whichever object has this tag.")]
    [SerializeField] private string targetTag = "Player";

    [Header("Visuals")]
    [Tooltip("Used for the wind-up flash. Found in the children automatically if left empty.")]
    [SerializeField] protected SpriteRenderer spriteRenderer;

    [Header("Death")]
    [Tooltip("Seconds between dying and being removed from the scene. Hook death effects to Health > On Died.")]
    [SerializeField, Min(0f)] private float destroyDelay = 0.3f;

    protected Health health;
    protected Transform target;
    protected Health targetHealth;

    private Color _baseColor = Color.white;
    private float _nextSearchTime;

    /// <summary>True while there is a living target to act on.</summary>
    protected bool HasTarget => target != null && targetHealth != null && targetHealth.IsAlive;

    protected virtual void Awake()
    {
        health = GetComponent<Health>();
        health.onDied.AddListener(HandleDeath);

        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) _baseColor = spriteRenderer.color;
    }

    private void Update()
    {
        if (!health.IsAlive) return;

        FindTargetIfNeeded();
        Tick();
    }

    /// <summary>Called every frame while the enemy is alive. Put the behaviour here.</summary>
    protected abstract void Tick();

    protected void SetTint(Color color)
    {
        if (spriteRenderer != null) spriteRenderer.color = color;
    }

    protected void ClearTint()
    {
        if (spriteRenderer != null) spriteRenderer.color = _baseColor;
    }

    private void FindTargetIfNeeded()
    {
        if (target != null || Time.time < _nextSearchTime) return;

        _nextSearchTime = Time.time + 0.5f;   // look twice a second, not every frame

        GameObject found = GameObject.FindWithTag(targetTag);
        if (found == null) return;

        targetHealth = found.GetComponent<Health>();
        if (targetHealth == null)
        {
            Debug.LogWarning($"{name}: the object tagged '{targetTag}' has no Health component.", this);
            return;
        }

        target = found.transform;
    }

    private void HandleDeath()
    {
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>()) col.enabled = false;

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }

        ClearTint();
        Destroy(gameObject, destroyDelay);
    }
}