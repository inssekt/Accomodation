using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Hit points for anything that can be damaged: player, enemies, destructible props.
/// Other systems go through IDamageable and never edit the numbers directly.
/// </summary>
[DisallowMultipleComponent]
public class Health : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;

    [Tooltip("Seconds of immunity after taking a hit. 0 = none.")]
    [SerializeField, Min(0f)] private float invulnerabilityDuration = 0f;

    [Header("Inspector events (hook up audio, VFX, UI)")]
    public UnityEvent<float> onDamaged;   // passes the damage amount
    public UnityEvent onDied;

    // Code-side events for other scripts.
    public event Action<DamageInfo> Damaged;
    public event Action<float, float> HealthChanged;   // current, max
    public event Action Died;

    public float Current { get; private set; }
    public float Max => maxHealth;
    public float Normalized => Current / maxHealth;
    public bool IsAlive => Current > 0f;
    public bool IsInvulnerable => Time.time < _invulnerableUntil;

    private float _invulnerableUntil;

    private void Awake() => Current = maxHealth;

    public void TakeDamage(DamageInfo info)
    {
        if (!IsAlive || info.Amount <= 0f) return;
        if (IsInvulnerable && !info.IgnoreInvulnerability) return;

        Current = Mathf.Max(0f, Current - info.Amount);
        _invulnerableUntil = Time.time + invulnerabilityDuration;

        Damaged?.Invoke(info);
        onDamaged?.Invoke(info.Amount);
        HealthChanged?.Invoke(Current, maxHealth);

        if (Current <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (!IsAlive || amount <= 0f) return;
        Current = Mathf.Min(maxHealth, Current + amount);
        HealthChanged?.Invoke(Current, maxHealth);
    }

    public void Kill()
    {
        if (!IsAlive) return;
        Current = 0f;
        HealthChanged?.Invoke(Current, maxHealth);
        Die();
    }

    public void ResetHealth()
    {
        Current = maxHealth;
        _invulnerableUntil = 0f;
        HealthChanged?.Invoke(Current, maxHealth);
    }

    private void Die()
    {
        Died?.Invoke();
        onDied?.Invoke();
    }
}