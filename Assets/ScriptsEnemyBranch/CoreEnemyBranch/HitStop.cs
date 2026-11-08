using UnityEngine;
 
/// <summary>
/// A freeze frame: the whole game stops for a split second when something is hit, which makes hits feel heavy.
/// Put it on the Main Camera, next to Camera Shake (or on any object that stays in the scene).
/// Shake On Damage plays it automatically. Hit freezes for Hit Duration, Damaged for Damaged Duration.
/// It works by setting Time.timeScale to 0, so movement, physics and animation all stop.
/// Audio carries on, and so does Camera Shake while its Use Unscaled Time is ticked.
/// </summary>
public class HitStop : MonoBehaviour
{
    [Header("Freeze lengths (seconds)")]
    [Tooltip("Freeze when an attack connects. 0.04 to 0.08 is a tiny hitch. Above 0.12 it starts to feel like lag. 0 = no freeze.")]
    [SerializeField, Range(0f, 0.3f)] private float hitDuration = 0.05f;
    [Tooltip("Freeze when the player takes damage. 0 = no freeze.")]
    [SerializeField, Range(0f, 0.3f)] private float damagedDuration = 0f;
 
    /// <summary>The freeze in the scene, so other scripts can find it.</summary>
    public static HitStop Instance { get; private set; }
 
    private bool _frozen;
    private float _endTime;           // real time, so the freeze ends even though game time has stopped
    private float _speedBefore = 1f;
 
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning($"{name}: there is more than one Hit Stop in the scene. Shake On Damage will use this one.", this);
 
        Instance = this;
    }
 
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
 
    private void OnDisable()
    {
        // Never leave the game frozen if this is switched off or the scene reloads mid-freeze.
        EndFreeze();
    }
 
    /// <summary>For Inspector events: freezes for Hit Duration.</summary>
    public void PlayHit()
    {
        Play(hitDuration);
    }
 
    /// <summary>For Inspector events: freezes for Damaged Duration.</summary>
    public void PlayDamaged()
    {
        Play(damagedDuration);
    }
 
    /// <summary>Freezes the game for this many real seconds.</summary>
    public void Play(float duration)
    {
        if (duration <= 0f) return;
 
        if (_frozen)
        {
            // Already frozen: make it last until the later of the two endings.
            _endTime = Mathf.Max(_endTime, Time.unscaledTime + duration);
            return;
        }
 
        // The game is paused already, so there is nothing to freeze.
        if (Time.timeScale <= 0f) return;
 
        _speedBefore = Time.timeScale;
        Time.timeScale = 0f;
        _endTime = Time.unscaledTime + duration;
        _frozen = true;
    }
 
    private void Update()
    {
        // Update still runs while Time.timeScale is 0.
        if (_frozen && Time.unscaledTime >= _endTime) EndFreeze();
    }
 
    private void EndFreeze()
    {
        if (!_frozen) return;
        _frozen = false;
 
        // Only restore the speed if nothing else has changed it in the meantime.
        if (Time.timeScale <= 0f) Time.timeScale = _speedBefore;
    }
}
 