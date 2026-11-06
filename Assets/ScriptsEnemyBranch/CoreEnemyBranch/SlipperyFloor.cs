using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A floor zone that stuns anything with a Stunnable component that crosses it while moving.
/// No damage is dealt. What can trigger it is decided by the collision matrix.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SlipperyFloor : MonoBehaviour
{
    [Header("Slip")]
    [Tooltip("How long the victim is stunned after slipping.")]
    [SerializeField, Min(0.1f)] private float stunDuration = 2f;
    [Tooltip("Only slips if moving at least this fast, in units per second. 0 = slips even when standing still.")]
    [SerializeField, Min(0f)] private float minimumSpeed = 0.5f;

    [Header("Events (hook up audio, VFX)")]
    public UnityEvent<GameObject> onSlip = new UnityEvent<GameObject>();

    private void Reset() => GetComponent<Collider2D>().isTrigger = true;
    private void Awake() => GetComponent<Collider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other) => TrySlip(other);
    private void OnTriggerStay2D(Collider2D other) => TrySlip(other);

    private void TrySlip(Collider2D other)
    {
        Stunnable stunnable = other.GetComponentInParent<Stunnable>();
        if (stunnable == null) return;

        Rigidbody2D body = other.attachedRigidbody;
        if (body != null && body.linearVelocity.sqrMagnitude < minimumSpeed * minimumSpeed) return;

        if (stunnable.Stun(stunDuration)) onSlip.Invoke(stunnable.gameObject);
    }
}