using UnityEngine;
 
/// <summary>
/// Game feel for when something takes damage: a camera shake (Camera Shake) and a freeze frame (Hit Stop).
/// Put it on the same object as the Health component: the player's root, and each enemy's root.
/// Kind Hit = the short sharp shake. Use it on enemies, so it plays when an attack connects.
/// Kind Damaged = the heavier shake. Use it on the player, so it plays when the player is hurt.
/// Every object sets its own Freeze Time, so a hit on an enemy and a hit on the player can feel different.
/// Needs a Camera Shake on the Main Camera. The Hit Stop is optional.
/// </summary>
public class ShakeOnDamage : MonoBehaviour
{
    [Header("Camera shake")]
    [Tooltip("Hit = short and sharp (use on enemies). Damaged = longer and heavier (use on the player).")]
    [SerializeField] private CameraShake.Kind kind = CameraShake.Kind.Hit;
    [Tooltip("Makes this object's shake bigger or smaller than the Camera Shake settings. 1 = as set there.")]
    [SerializeField, Min(0f)] private float strengthMultiplier = 1f;
 
    [Header("Freeze frame")]
    [Tooltip("Seconds the whole game freezes when this object takes damage. 0.04 to 0.08 is a tiny hitch. 0 = no freeze. Needs a Hit Stop in the scene.")]
    [SerializeField, Range(0f, 0.3f)] private float freezeTime = 0.05f;
 
    [Header("Checking")]
    [Tooltip("Tick this to print a line in the Console every time this object takes damage. It shows whether the Camera Shake and Hit Stop were found.")]
    [SerializeField] private bool logToConsole = false;
 
    private Health _health;
    private bool _warned;
 
    private void Awake()
    {
        // Looks on this object first, then on its parents, so it also works if it sits on a child.
        _health = GetComponentInParent<Health>();
 
        if (_health == null)
            Debug.LogWarning($"{name}: no Health found on this object or its parents. Put Shake On Damage on the same object as Health.", this);
    }
 
    private void OnEnable()
    {
        if (_health != null) _health.onDamaged.AddListener(OnDamaged);
    }
 
    private void OnDisable()
    {
        if (_health != null) _health.onDamaged.RemoveListener(OnDamaged);
    }
 
    // Health > On Damaged passes the damage amount. It is not needed here.
    private void OnDamaged(float amount)
    {
        CameraShake shake = CameraShake.Instance;
        HitStop stop = HitStop.Instance;
 
        if (logToConsole)
        {
            string shakeState = shake != null ? "found" : "MISSING";
            string stopState = stop != null ? "found" : "MISSING";
            Debug.Log($"{name} took damage. Camera Shake: {shakeState}. Hit Stop: {stopState}. Freeze: {freezeTime}s.", this);
        }
 
        if (shake != null)
        {
            shake.Play(kind, strengthMultiplier);
        }
        else if (!_warned)
        {
            _warned = true;
            Debug.LogWarning($"{name}: no Camera Shake found. Add one to the Main Camera.", this);
        }
 
        if (stop != null) stop.Play(freezeTime);
    }
}
 