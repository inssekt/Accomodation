using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A slow enemy that walks up to the player, circles them, winds up, then lunges for one hit.
/// Every value below can be tweaked in the Inspector.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MeleeEnemy : EnemyBase
{
    [Header("Detection")]
    [Tooltip("Starts chasing when the player is closer than this.")]
    [SerializeField, Min(0f)] private float aggroRange = 8f;
    [Tooltip("Gives up and stands still when the player is further than this.")]
    [SerializeField, Min(0f)] private float forgetRange = 12f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float approachSpeed = 1.5f;
    [SerializeField, Min(0f)] private float circleSpeed = 2f;
    [Tooltip("Distance from the player it tries to hold while circling.")]
    [SerializeField, Min(0.5f)] private float circleRadius = 2.2f;
    [Tooltip("It circles for a random time between these two values, then attacks.")]
    [SerializeField, Min(0f)] private float minCircleTime = 2f;
    [SerializeField, Min(0f)] private float maxCircleTime = 4f;

    [Header("Attack")]
    [Tooltip("Pause before the lunge. The lunge direction is locked when this ends.")]
    [SerializeField, Min(0f)] private float windupTime = 0.5f;
    [SerializeField] private Color windupColor = new Color(1f, 0.4f, 0.3f);
    [SerializeField, Min(0f)] private float lungeSpeed = 10f;
    [SerializeField, Min(0.05f)] private float lungeTime = 0.3f;
    [Tooltip("The lunge hurts the player if it gets this close (centre to centre).")]
    [SerializeField, Min(0.1f)] private float hitRange = 1.2f;
    [SerializeField, Min(0f)] private float damage = 15f;
    [Tooltip("Rest after a lunge before the enemy moves again.")]
    [SerializeField, Min(0f)] private float recoverTime = 0.8f;

    [Header("Events (hook up audio, VFX, animation)")]
    public UnityEvent onWindup = new UnityEvent();
    public UnityEvent onAttack = new UnityEvent();

    private enum State { Idle, Approach, Circle, WindUp, Lunge, Recover }

    // If the player gets further than circleRadius times this while circling, chase again.
    private const float LeaveCircleMultiplier = 1.5f;

    private Rigidbody2D _body;
    private State _state = State.Idle;
    private float _stateEndTime;
    private float _circleDirection = 1f;   // +1 anticlockwise, -1 clockwise
    private Vector2 _attackDirection;
    private bool _hitDelivered;
    private Vector2 _desiredVelocity;

    protected override void Awake()
    {
        base.Awake();
        _body = GetComponent<Rigidbody2D>();
    }

    private void OnValidate()
    {
        forgetRange = Mathf.Max(forgetRange, aggroRange);
        maxCircleTime = Mathf.Max(maxCircleTime, minCircleTime);
    }

    protected override void Tick()
    {
        if (!HasTarget)
        {
            if (_state != State.Idle) SetState(State.Idle);
            return;
        }

        float distance = Vector2.Distance(transform.position, target.position);

        switch (_state)
        {
            case State.Idle:
                if (distance <= aggroRange) SetState(State.Approach);
                break;

            case State.Approach:
                if (distance > forgetRange) SetState(State.Idle);
                else if (distance <= circleRadius) SetState(State.Circle);
                else _desiredVelocity = DirectionToTarget() * approachSpeed;
                break;

            case State.Circle:
                if (distance > forgetRange) SetState(State.Idle);
                else if (distance > circleRadius * LeaveCircleMultiplier) SetState(State.Approach);
                else if (Time.time >= _stateEndTime) SetState(State.WindUp);
                else _desiredVelocity = CircleVelocity(distance);
                break;

            case State.WindUp:
                if (Time.time >= _stateEndTime)
                {
                    _attackDirection = DirectionToTarget();   // locked here, so the player can dodge
                    SetState(State.Lunge);
                }
                break;

            case State.Lunge:
                if (!_hitDelivered && distance <= hitRange)
                {
                    _hitDelivered = true;
                    targetHealth.TakeDamage(damage);
                }
                if (Time.time >= _stateEndTime) SetState(State.Recover);
                break;

            case State.Recover:
                if (Time.time >= _stateEndTime) SetState(State.Approach);
                break;
        }
    }

    private void FixedUpdate()
    {
        if (!health.IsAlive) return;
        _body.linearVelocity = _desiredVelocity;
    }

    private void SetState(State next)
    {
        _state = next;
        ClearTint();

        switch (next)
        {
            case State.Idle:
                _desiredVelocity = Vector2.zero;
                break;

            case State.Circle:
                _circleDirection = Random.value < 0.5f ? -1f : 1f;
                _stateEndTime = Time.time + Random.Range(minCircleTime, maxCircleTime);
                break;

            case State.WindUp:
                _desiredVelocity = Vector2.zero;
                _stateEndTime = Time.time + windupTime;
                SetTint(windupColor);
                onWindup.Invoke();
                break;

            case State.Lunge:
                _desiredVelocity = _attackDirection * lungeSpeed;
                _stateEndTime = Time.time + lungeTime;
                _hitDelivered = false;
                onAttack.Invoke();
                break;

            case State.Recover:
                _desiredVelocity = Vector2.zero;
                _stateEndTime = Time.time + recoverTime;
                break;
        }
    }

    private Vector2 DirectionToTarget()
    {
        return ((Vector2)target.position - (Vector2)transform.position).normalized;
    }

    // Sideways around the player, nudged in or out to hold circleRadius.
    private Vector2 CircleVelocity(float distance)
    {
        Vector2 outward = ((Vector2)transform.position - (Vector2)target.position) / Mathf.Max(distance, 0.001f);
        Vector2 tangent = new Vector2(-outward.y, outward.x) * _circleDirection;
        float correction = Mathf.Clamp(circleRadius - distance, -1f, 1f);   // positive = too close, push out

        return (tangent + outward * correction).normalized * circleSpeed;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, forgetRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, circleRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRange);
    }
}