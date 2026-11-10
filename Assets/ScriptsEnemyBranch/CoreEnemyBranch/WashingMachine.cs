using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A washing machine that opens and closes at random. While open, it stuns the player
/// if they are within Stun Radius. No damage is dealt.
/// </summary>
public class WashingMachine : MonoBehaviour
{
    [Header("Cycle")]
    [Tooltip("Stays closed for a random time between these two values.")]
    [SerializeField, Min(0.1f)] private float minClosedTime = 2.5f;
    [SerializeField, Min(0.1f)] private float maxClosedTime = 5f;
    [Tooltip("Stays open for a random time between these two values.")]
    [SerializeField, Min(0.1f)] private float minOpenTime = 1.5f;
    [SerializeField, Min(0.1f)] private float maxOpenTime = 3f;
    [Tooltip("Warning time before the door opens, so the player can see it coming.")]
    [SerializeField, Min(0f)] private float warningTime = 0.6f;

    [Header("Stun")]
    [Tooltip("While the machine is open, anyone closer than this is stunned.")]
    [SerializeField, Min(0.1f)] private float stunRadius = 2.2f;
    [SerializeField, Min(0.1f)] private float stunDuration = 2f;
    [Tooltip("Set this to the Player layer.")]
    [SerializeField] private LayerMask victimLayers;

    [Header("Placeholder visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color closedColor = new Color(0.7f, 0.7f, 0.75f);
    [SerializeField] private Color warningColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Color openColor = new Color(0.9f, 0.3f, 0.3f);

    [Header("Events (hook up animation, audio, VFX)")]
    public UnityEvent onWarning = new UnityEvent();
    public UnityEvent onOpen = new UnityEvent();
    public UnityEvent onClose = new UnityEvent();
    public UnityEvent<GameObject> onStun = new UnityEvent<GameObject>();

    private enum State { Closed, Warning, Open }

    private State _state = State.Closed;
    private float _nextChangeTime;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        // A random first delay stops a room full of machines opening in sync.
        _nextChangeTime = Time.time + Random.Range(0f, maxClosedTime);
        SetColor(closedColor);
    }

    private void OnValidate()
    {
        maxClosedTime = Mathf.Max(maxClosedTime, minClosedTime);
        maxOpenTime = Mathf.Max(maxOpenTime, minOpenTime);
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
                StunPlayerIfClose();
                if (Time.time >= _nextChangeTime) SetState(State.Closed);
                break;
        }
    }

    private void SetState(State next)
    {
        _state = next;

        switch (next)
        {
            case State.Closed:
                _nextChangeTime = Time.time + Random.Range(minClosedTime, maxClosedTime);
                SetColor(closedColor);
                onClose.Invoke();
                break;

            case State.Warning:
                _nextChangeTime = Time.time + warningTime;
                SetColor(warningColor);
                onWarning.Invoke();
                break;

            case State.Open:
                _nextChangeTime = Time.time + Random.Range(minOpenTime, maxOpenTime);
                SetColor(openColor);
                onOpen.Invoke();
                break;
        }
    }

    private void StunPlayerIfClose()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, stunRadius, victimLayers);
        if (hit == null) return;

        Stunnable stunnable = hit.GetComponentInParent<Stunnable>();
        if (stunnable != null && stunnable.Stun(stunDuration)) onStun.Invoke(stunnable.gameObject);
    }

    private void SetColor(Color color)
    {
        if (spriteRenderer != null) spriteRenderer.color = color;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stunRadius);
    }
}