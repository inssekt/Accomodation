using UnityEngine;
using UnityEngine.Events;
 
/// <summary>
/// A washing machine hazard. It cycles Closed, Warning, Open. While it is open it pulls the player
/// toward it. A player who reaches it is hidden inside, takes damage over time, then is spat out
/// and stunned for a moment. While someone is inside, the machine shows a different sprite and
/// animation. Every number below can be tweaked in the Inspector.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DefaultExecutionOrder(100)]   // runs after the player's own movement script, so the pull wins
public class WashingMachine : MonoBehaviour
{
    [Header("Cycle")]
    [Tooltip("Stays closed for a random time between these two values.")]
    [SerializeField, Min(0.1f)] private float minClosedTime = 2.5f;
    [SerializeField, Min(0.1f)] private float maxClosedTime = 5f;
    [Tooltip("Stays open for a random time between these two values. It only pulls while open.")]
    [SerializeField, Min(0.1f)] private float minOpenTime = 1.5f;
    [SerializeField, Min(0.1f)] private float maxOpenTime = 3f;
    [Tooltip("Warning time before the door opens, so the player can see it coming.")]
    [SerializeField, Min(0f)] private float warningTime = 0.6f;
 
    [Header("Pull")]
    [Tooltip("Set this to the Player layer.")]
    [SerializeField] private LayerMask victimLayers;
    [Tooltip("While the machine is open, anyone inside this radius is dragged toward it.")]
    [SerializeField, Min(0.1f)] private float pullRadius = 4f;
    [Tooltip("How fast the player is dragged, in units per second.")]
    [SerializeField, Min(0f)] private float pullSpeed = 3.5f;
    [Tooltip("Layers that block the pull, so the machine cannot drag the player through a wall. Set this to Wall.")]
    [SerializeField] private LayerMask pullBlockers;
    [Tooltip("The player is taken in when the gap between their collider and the machine's collider is this small.")]
    [SerializeField, Min(0f)] private float captureDistance = 0.15f;
 
    [Header("Inside the machine")]
    [Tooltip("The player stays inside for a random time between these two values.")]
    [SerializeField, Min(0.1f)] private float minHoldTime = 2f;
    [SerializeField, Min(0.1f)] private float maxHoldTime = 3f;
    [Tooltip("Damage taken every second while inside.")]
    [SerializeField, Min(0f)] private float damagePerSecond = 10f;
    [Tooltip("Seconds between damage hits. Each hit deals Damage Per Second times this. A smaller number means smoother damage.")]
    [SerializeField, Min(0.05f)] private float damageTickInterval = 0.5f;
    [Tooltip("The player stays stunned for this long after being spat out.")]
    [SerializeField, Min(0f)] private float stunAfterRelease = 1f;
    [Tooltip("Where the player is spat out, relative to the machine. (0, -1.5) is just below it.")]
    [SerializeField] private Vector2 releaseOffset = new Vector2(0f, -1.5f);
 
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color closedColor = new Color(0.7f, 0.7f, 0.75f);
    [SerializeField] private Color warningColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Color openColor = new Color(0.9f, 0.3f, 0.3f);
    [SerializeField] private Color occupiedColor = new Color(0.55f, 0.25f, 0.6f);
 
    [Header("While someone is inside")]
    [Tooltip("Optional. This sprite is shown while the player is inside. Leave it empty if your Animator already changes the sprite.")]
    [SerializeField] private Sprite occupiedSprite;
    [Tooltip("The object that shakes while occupied. Leave it empty to use the sprite's object. Use a child, so the machine's collider does not move.")]
    [SerializeField] private Transform visualRoot;
    [Tooltip("How far the shake moves, in world units. 0 = no shake.")]
    [SerializeField, Min(0f)] private float shakeStrength = 0.06f;
    [SerializeField, Min(0f)] private float shakeSpeed = 40f;
    [Tooltip("Optional. Found in the children automatically if left empty.")]
    [SerializeField] private Animator animator;
    [Tooltip("The Animator Bool that is true while someone is inside. Make an Animator parameter with exactly this name.")]
    [SerializeField] private string occupiedParameter = "Occupied";
 
    [Header("Events (hook up animation, audio, VFX)")]
    public UnityEvent onWarning = new UnityEvent();
    public UnityEvent onOpen = new UnityEvent();
    public UnityEvent onClose = new UnityEvent();
    public UnityEvent<GameObject> onCapture = new UnityEvent<GameObject>();
    public UnityEvent<GameObject> onRelease = new UnityEvent<GameObject>();
 
    private enum State { Closed, Warning, Open, Occupied }
 
    private State _state = State.Closed;
    private float _nextChangeTime;
    private float _nextDamageTime;
 
    private Collider2D _collider;
    private Sprite _idleSprite;
    private Vector3 _visualRestPosition;
    private int _occupiedHash;
    private bool _animatorHasParameter;
 
    private Rigidbody2D _victimBody;
    private Health _victimHealth;
    private Renderer[] _victimRenderers;
    private bool[] _victimRendererStates;
 
    private bool CanShake => visualRoot != null && visualRoot != transform;
 
    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
 
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            _idleSprite = spriteRenderer.sprite;
            if (visualRoot == null && spriteRenderer.transform != transform) visualRoot = spriteRenderer.transform;
        }
        if (visualRoot != null) _visualRestPosition = visualRoot.localPosition;
 
        if (animator == null) animator = GetComponentInChildren<Animator>();
        _occupiedHash = Animator.StringToHash(occupiedParameter);
        _animatorHasParameter = AnimatorHasBool(_occupiedHash);
    }
 
    private void Start()
    {
        // A random first delay stops a room full of machines opening in sync.
        _nextChangeTime = Time.time + Random.Range(0f, maxClosedTime);
        ApplyVisuals();
    }
 
    private void OnValidate()
    {
        maxClosedTime = Mathf.Max(maxClosedTime, minClosedTime);
        maxOpenTime = Mathf.Max(maxOpenTime, minOpenTime);
        maxHoldTime = Mathf.Max(maxHoldTime, minHoldTime);
    }
 
    private void OnDisable()
    {
        // If the machine is switched off or destroyed with someone inside, give them back.
        if (_state == State.Occupied) ReleaseVictim();
    }
 
    private void Update()
    {
        switch (_state)
        {
            case State.Closed:
                if (Time.time >= _nextChangeTime) SetState(State.Warning);
                break;
 
            case State.Warning:
                if (Time.time >= _nextChangeTime) SetState(State.Open);
                break;
 
            case State.Open:
                if (Time.time >= _nextChangeTime) SetState(State.Closed);
                break;
 
            case State.Occupied:
                UpdateOccupied();
                break;
        }
    }
 
    private void FixedUpdate()
    {
        if (_state != State.Open) return;
 
        Collider2D hit = Physics2D.OverlapCircle(transform.position, pullRadius, victimLayers);
        if (hit == null) return;
 
        Rigidbody2D body = hit.attachedRigidbody;
        if (body == null) return;
 
        Vector2 center = transform.position;
        if (pullBlockers.value != 0 && Physics2D.Linecast(center, body.position, pullBlockers)) return;
 
        // The machine is solid, so measure the gap between the two colliders, not the distance to the centre.
        ColliderDistance2D gap = hit.Distance(_collider);
        if (gap.isValid && gap.distance <= captureDistance)
        {
            Capture(body);
            return;
        }
 
        // Drag the player toward the centre. This replaces their own velocity for this step.
        Vector2 toMachine = center - body.position;
        float speed = Mathf.Min(pullSpeed, toMachine.magnitude / Time.fixedDeltaTime);
        body.linearVelocity = toMachine.normalized * speed;
    }
 
    private void SetState(State next)
    {
        _state = next;
        ApplyVisuals();
 
        switch (next)
        {
            case State.Closed:
                _nextChangeTime = Time.time + Random.Range(minClosedTime, maxClosedTime);
                onClose.Invoke();
                break;
 
            case State.Warning:
                _nextChangeTime = Time.time + warningTime;
                onWarning.Invoke();
                break;
 
            case State.Open:
                _nextChangeTime = Time.time + Random.Range(minOpenTime, maxOpenTime);
                onOpen.Invoke();
                break;
 
            case State.Occupied:
                // Capture() has already set how long the player stays inside.
                break;
        }
    }
 
    private void Capture(Rigidbody2D body)
    {
        _victimBody = body;
        _victimHealth = body.GetComponentInParent<Health>();
        Stunnable stunnable = body.GetComponentInParent<Stunnable>();
 
        float holdTime = Random.Range(minHoldTime, maxHoldTime);
        _nextChangeTime = Time.time + holdTime;
        _nextDamageTime = Time.time + damageTickInterval;
 
        // One stun covers the whole stay plus the recovery after being spat out.
        if (stunnable != null) stunnable.Stun(holdTime + stunAfterRelease);
 
        HideVictim();
        SetState(State.Occupied);
        onCapture.Invoke(body.gameObject);
    }
 
    private void UpdateOccupied()
    {
        bool victimMissing = _victimBody == null;
        bool victimDead = _victimHealth != null && !_victimHealth.IsAlive;
 
        if (victimMissing || victimDead || Time.time >= _nextChangeTime)
        {
            Release();
            return;
        }
 
        // Keep the player inside the drum.
        Vector3 inside = _victimBody.transform.position;
        _victimBody.transform.position = new Vector3(transform.position.x, transform.position.y, inside.z);
 
        if (CanShake)
        {
            Vector2 shake = new Vector2(Mathf.Sin(Time.time * shakeSpeed), Mathf.Cos(Time.time * shakeSpeed * 1.3f)) * shakeStrength;
            visualRoot.localPosition = _visualRestPosition + (Vector3)shake;
        }
 
        if (_victimHealth != null && Time.time >= _nextDamageTime)
        {
            _nextDamageTime = Time.time + damageTickInterval;
            _victimHealth.TakeDamage(damagePerSecond * damageTickInterval);
        }
    }
 
    private void Release()
    {
        GameObject victim = _victimBody != null ? _victimBody.gameObject : null;
 
        ReleaseVictim();
        SetState(State.Closed);
 
        if (victim != null) onRelease.Invoke(victim);
    }
 
    private void HideVictim()
    {
        // Remember which renderers were showing, so exactly those come back.
        _victimRenderers = _victimBody.GetComponentsInChildren<Renderer>();
        _victimRendererStates = new bool[_victimRenderers.Length];
        for (int i = 0; i < _victimRenderers.Length; i++)
        {
            _victimRendererStates[i] = _victimRenderers[i].enabled;
            _victimRenderers[i].enabled = false;
        }
 
        Vector3 position = _victimBody.transform.position;
        _victimBody.transform.position = new Vector3(transform.position.x, transform.position.y, position.z);
 
        // A body that is not simulated has no collisions, so bullets and enemies pass through.
        _victimBody.linearVelocity = Vector2.zero;
        _victimBody.simulated = false;
    }
 
    private void ReleaseVictim()
    {
        if (_victimBody != null)
        {
            Vector3 spitOut = transform.TransformPoint(releaseOffset);
            _victimBody.transform.position = new Vector3(spitOut.x, spitOut.y, _victimBody.transform.position.z);
            _victimBody.simulated = true;
            _victimBody.linearVelocity = Vector2.zero;
        }
 
        if (_victimRenderers != null)
        {
            for (int i = 0; i < _victimRenderers.Length; i++)
            {
                if (_victimRenderers[i] != null) _victimRenderers[i].enabled = _victimRendererStates[i];
            }
        }
 
        _victimBody = null;
        _victimHealth = null;
        _victimRenderers = null;
        _victimRendererStates = null;
    }
 
    private void ApplyVisuals()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = _state switch
            {
                State.Warning => warningColor,
                State.Open => openColor,
                State.Occupied => occupiedColor,
                _ => closedColor
            };
 
            if (occupiedSprite != null)
                spriteRenderer.sprite = _state == State.Occupied ? occupiedSprite : _idleSprite;
        }
 
        if (_animatorHasParameter) animator.SetBool(_occupiedHash, _state == State.Occupied);
 
        if (_state != State.Occupied && CanShake) visualRoot.localPosition = _visualRestPosition;
    }
 
    private bool AnimatorHasBool(int nameHash)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
 
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == nameHash && parameter.type == AnimatorControllerParameterType.Bool) return true;
        }
        return false;
    }
 
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, pullRadius);
 
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.TransformPoint(releaseOffset), 0.25f);
    }
}