using UnityEngine;
 
/// <summary>
/// Game feel for when this object takes damage. Put it next to a Health component.
/// It shakes the camera (Camera Shake) and freezes the game for a split second (Hit Stop).
/// Kind Hit = the short sharp version. Put it on enemies, so it plays when an attack connects.
/// Kind Damaged = the heavier version. Put it on the player, so it plays when the player is hurt.
/// Needs a Camera Shake on the Main Camera. The Hit Stop is optional.
/// </summary>
[RequireComponent(typeof(Health))]
public class ShakeOnDamage : MonoBehaviour
{
    [Tooltip("Hit = short and sharp (use on enemies). Damaged = longer and heavier (use on the player).")]
    [SerializeField] private CameraShake.Kind kind = CameraShake.Kind.Hit;
    [Tooltip("Makes this object's shake bigger or smaller than the Camera Shake settings. 1 = as set there. It does not change the freeze.")]
    [SerializeField, Min(0f)] private float strengthMultiplier = 1f;
 
    private Health _health;
    private bool _warned;
 
    private void Awake()
    {
        _health = GetComponent<Health>();
    }
 
    private void OnEnable()
    {
        _health.onDamaged.AddListener(OnDamaged);
    }
 
    private void OnDisable()
    {
        if (_health != null) _health.onDamaged.RemoveListener(OnDamaged);
    }
 
    // Health > On Damaged passes the damage amount. It is not needed here.
    private void OnDamaged(float amount)
    {
        CameraShake shake = CameraShake.Instance;
        if (shake != null)
        {
            shake.Play(kind, strengthMultiplier);
        }
        else if (!_warned)
        {
            _warned = true;
            Debug.LogWarning($"{name}: no Camera Shake found. Add one to the Main Camera.", this);
        }
 
        HitStop stop = HitStop.Instance;
        if (stop == null) return;
 
        if (kind == CameraShake.Kind.Hit) stop.PlayHit();
        else stop.PlayDamaged();
    }
}