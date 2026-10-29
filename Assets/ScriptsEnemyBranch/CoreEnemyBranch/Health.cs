using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Hit points for anything that can be damaged: the player and every enemy.
/// Call TakeDamage to hurt it. Hook audio, VFX and UI to the events in the Inspector.
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

    private void Awake()
    {
        Current = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f) return;

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