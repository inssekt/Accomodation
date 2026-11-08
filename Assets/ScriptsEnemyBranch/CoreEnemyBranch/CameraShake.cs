using UnityEngine;
 
/// <summary>
/// Shakes the camera. Put it on the Main Camera.
/// It has two shakes you can tune in the Inspector:
/// Hit = short and sharp, for when an attack connects.
/// Damaged = longer and heavier, for when the player is hurt.
/// Shake On Damage plays them automatically. Other scripts can call Play, PlayHit or PlayDamaged.
/// It runs after other scripts, so it also works with a camera that follows the player.
/// </summary>
[DefaultExecutionOrder(1000)]
public class CameraShake : MonoBehaviour
{
    public enum Kind { Hit, Damaged }
 
    [System.Serializable]
    public class Preset
    {
        [Tooltip("How long the shake lasts, in seconds.")]
        [Min(0f)] public float duration = 0.1f;
        [Tooltip("The furthest the camera moves from its place, in world units.")]
        [Min(0f)] public float strength = 0.1f;
        [Tooltip("How fast it jitters. Higher = a faster, rougher shake. Lower = a slow rumble.")]
        [Min(0.1f)] public float frequency = 40f;
    }
 
    [Header("Shakes")]
    [Tooltip("Played when an attack connects.")]
    [SerializeField] private Preset hit = new Preset { duration = 0.10f, strength = 0.12f, frequency = 45f };
    [Tooltip("Played when the player takes damage.")]
    [SerializeField] private Preset damaged = new Preset { duration = 0.35f, strength = 0.35f, frequency = 30f };
 
    [Header("Settings")]
    [Tooltip("Turns every shake up or down. 0 = no shaking at all, which is handy for an accessibility option.")]
    [SerializeField, Range(0f, 2f)] private float intensity = 1f;
    [Tooltip("How the shake dies away over its duration. Left = the start, right = the end. Height 1 = full strength.")]
    [SerializeField] private AnimationCurve fadeOut = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    [Tooltip("Keep shaking at normal speed even if the game is slowed down or paused with Time.timeScale.")]
    [SerializeField] private bool useUnscaledTime = true;
 
    /// <summary>The camera shake in the scene, so other scripts can find it.</summary>
    public static CameraShake Instance { get; private set; }
 
    private bool _active;
    private float _duration;
    private float _strength;
    private float _frequency;
    private float _elapsed;
    private float _noiseStart;
 
    private bool _offsetApplied;
    private Vector3 _basePosition;
    private Vector3 _positionAfterShake;
 
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning($"{name}: there is more than one Camera Shake in the scene. Shake On Damage will use this one.", this);
 
        Instance = this;
    }
 
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
 
    private void OnDisable()
    {
        RemoveOffset();
        _active = false;
    }
 
    /// <summary>Plays one of the two shakes. strengthMultiplier makes it bigger or smaller.</summary>
    public void Play(Kind kind, float strengthMultiplier)
    {
        Preset preset = kind == Kind.Hit ? hit : damaged;
        Play(preset.duration, preset.strength * strengthMultiplier, preset.frequency);
    }
 
    public void Play(Kind kind)
    {
        Play(kind, 1f);
    }
 
    /// <summary>Plays a shake with your own numbers.</summary>
    public void Play(float duration, float strength, float frequency)
    {
        strength *= intensity;
        if (duration <= 0f || strength <= 0f) return;
 
        // A small shake must not cut off a bigger one that is still going.
        if (_active && strength < CurrentStrength()) return;
 
        _duration = duration;
        _strength = strength;
        _frequency = Mathf.Max(frequency, 0.1f);
        _elapsed = 0f;
        _noiseStart = Random.value * 100f;   // a different wobble every time
        _active = true;
    }
 
    /// <summary>For Inspector events: plays the Hit shake.</summary>
    public void PlayHit()
    {
        Play(Kind.Hit, 1f);
    }
 
    /// <summary>For Inspector events: plays the Damaged shake.</summary>
    public void PlayDamaged()
    {
        Play(Kind.Damaged, 1f);
    }
 
    private float CurrentStrength()
    {
        return _strength * fadeOut.Evaluate(Mathf.Clamp01(_elapsed / _duration));
    }
 
    private void LateUpdate()
    {
        RemoveOffset();
 
        if (!_active) return;
 
        _elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (_elapsed >= _duration)
        {
            _active = false;
            return;
        }
 
        // Smooth random wobble, a different one for sideways and up/down.
        float noiseTime = _noiseStart + _elapsed * _frequency;
        float x = Mathf.PerlinNoise(noiseTime, 0f) * 2f - 1f;
        float y = Mathf.PerlinNoise(noiseTime, 7.3f) * 2f - 1f;
 
        _basePosition = transform.localPosition;
        transform.localPosition = _basePosition + new Vector3(x, y, 0f) * CurrentStrength();
        _positionAfterShake = transform.localPosition;
        _offsetApplied = true;
    }
 
    // Puts the camera back where it was before last frame's shake.
    // If another script has moved the camera since, that script's position is kept instead.
    private void RemoveOffset()
    {
        if (!_offsetApplied) return;
        _offsetApplied = false;
 
        bool untouched = (transform.localPosition - _positionAfterShake).sqrMagnitude < 0.000001f;
        if (untouched) transform.localPosition = _basePosition;
    }
}
 