using UnityEngine;
using UnityEngine.Events;
 
/// <summary>
/// Hit points for anything that can be damaged: the player and every enemy.
/// Call TakeDamage to hurt it. Hook audio, VFX and UI to the events in the Inspector.
/// TakeDamage can also be told which way the hit was travelling, so a dying enemy knows which way to fly.
/// </summary>
public class Health : MonoBehaviour
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;
 
    [Header("Events")]
    [Tooltip("Fires on every hit. Passes the damage amount.")]
    public UnityEvent<float> onDamaged = new UnityEvent<float>();
    public UnityEvent onDied = new UnityEvent();
 
    public float Current { get; private set; }
    public float Max => maxHealth;
    public bool IsAlive => Current > 0f;
 
    /// <summary>
    /// The direction of the most recent hit, with a length of 1.
    /// It is zero if that hit did not say which way it was travelling.
    /// It is set before onDamaged and onDied fire, so listeners can read it.
    /// </summary>
    public Vector2 LastHitDirection { get; private set; }
 
    private void Awake()
    {
        Current = maxHealth;
    }
 
    /// <summary>Damage with no direction. Hazards, projectiles and Inspector events use this.</summary>
    public void TakeDamage(float amount)
    {
        TakeDamage(amount, Vector2.zero);
    }
 
    /// <summary>
    /// Damage with a direction. hitDirection is the way the blow was travelling,
    /// for example from the attacker towards the victim. It does not need to be a length of 1.
    /// </summary>
    public void TakeDamage(float amount, Vector2 hitDirection)
    {
        if (!IsAlive || amount <= 0f) return;
 
        LastHitDirection = hitDirection.sqrMagnitude > 0.0001f ? hitDirection.normalized : Vector2.zero;
 
        Current = Mathf.Max(0f, Current - amount);
        onDamaged.Invoke(amount);
 
        if (Current <= 0f) onDied.Invoke();
    }
 
    public void Heal(float amount)
    {
        if (!IsAlive || amount <= 0f) return;
 
        Current = Mathf.Min(maxHealth, Current + amount);
    }
}
 