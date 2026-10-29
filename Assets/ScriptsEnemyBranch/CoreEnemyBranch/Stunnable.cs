using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Lets an object be stunned. Hazards call Stun(). The player controller checks IsStunned
/// and ignores input while it is true.
/// </summary>
public class Stunnable : MonoBehaviour
{
    [Header("Stun")]
    [Tooltip("After a stun ends, the object cannot be stunned again for this many seconds.")]
    [SerializeField, Min(0f)] private float immunityAfterStun = 1f;

    [Header("Placeholder feedback (optional)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color stunnedColor = new Color(1f, 0.92f, 0.3f);

    [Header("Events (hook up animation, audio, VFX)")]
    public UnityEvent onStunned = new UnityEvent();
    public UnityEvent onStunEnded = new UnityEvent();

    private float _stunEndTime;
    private float _immuneUntil;
    private bool _wasStunned;
    private Color _baseColor = Color.white;

    public bool IsStunned => Time.time < _stunEndTime;
    public float TimeRemaining => Mathf.Max(0f, _stunEndTime - Time.time);

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) _baseColor = spriteRenderer.color;
    }

    /// <summary>Returns true if the stun was applied.</summary>
    public bool Stun(float duration)
    {
        if (duration <= 0f || Time.time < _immuneUntil) return false;

        _stunEndTime = Time.time + duration;
        _immuneUntil = _stunEndTime + immunityAfterStun;
        _wasStunned = true;

        if (spriteRenderer != null) spriteRenderer.color = stunnedColor;
        onStunned.Invoke();
        return true;
    }

    private void Update()
    {
        if (!_wasStunned || IsStunned) return;

        _wasStunned = false;
        if (spriteRenderer != null) spriteRenderer.color = _baseColor;
        onStunEnded.Invoke();
    }
}