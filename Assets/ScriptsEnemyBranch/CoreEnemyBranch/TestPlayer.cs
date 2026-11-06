using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// A bare-bones top-down player for testing enemies and hazards.
/// Controls: WASD or arrow keys to move, Space to swing.
/// Replace it with the team's real controller later. That controller needs a Health, a Stunnable,
/// and one rule: ignore input while Stunnable.IsStunned is true.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(Stunnable))]
public class TestPlayer : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [Tooltip("How fast the player slides to a stop while stunned. Lower = a longer slide.")]
    [SerializeField, Min(0f)] private float stunnedDeceleration = 15f;

    [Header("Attack (so you can kill enemies)")]
    [SerializeField, Min(0f)] private float attackDamage = 25f;
    [SerializeField, Min(0f)] private float attackReach = 0.9f;
    [SerializeField, Min(0.05f)] private float attackRadius = 0.7f;
    [SerializeField, Min(0f)] private float attackCooldown = 0.35f;
    [Tooltip("Set this to the Enemy layer.")]
    [SerializeField] private LayerMask enemyLayers;

    private Rigidbody2D _body;
    private Health _health;
    private Stunnable _stunnable;
    private Vector2 _moveInput;
    private Vector2 _facing = Vector2.down;
    private float _nextAttackTime;
    private GUIStyle _hudStyle;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _health = GetComponent<Health>();
        _stunnable = GetComponent<Stunnable>();
    }

    private void Update()
    {
        bool canAct = _health.IsAlive && !_stunnable.IsStunned;

        _moveInput = canAct ? Vector2.ClampMagnitude(ReadMove(), 1f) : Vector2.zero;
        if (_moveInput.sqrMagnitude > 0.01f) _facing = _moveInput.normalized;

        if (canAct && AttackPressed() && Time.time >= _nextAttackTime) Attack();
    }

    private void FixedUpdate()
    {
        if (_health.IsAlive && !_stunnable.IsStunned)
            _body.linearVelocity = _moveInput * moveSpeed;
        else
            _body.linearVelocity = Vector2.MoveTowards(_body.linearVelocity, Vector2.zero, stunnedDeceleration * Time.fixedDeltaTime);
    }

    private void Attack()
    {
        _nextAttackTime = Time.time + attackCooldown;

        Vector2 center = (Vector2)transform.position + _facing * attackReach;
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(center, attackRadius, enemyLayers))
        {
            Health victim = hit.GetComponentInParent<Health>();
            if (victim != null) victim.TakeDamage(attackDamage);
        }
    }

    private static Vector2 ReadMove()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return Vector2.zero;

        float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
        float y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
        return new Vector2(x, y);
#else
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
    }

    private static bool AttackPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private void OnGUI()
    {
        _hudStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        string status = !_health.IsAlive ? "DEAD"
            : _stunnable.IsStunned ? $"STUNNED {_stunnable.TimeRemaining:0.0}s"
            : "OK";

        GUI.Label(new Rect(12f, 10f, 500f, 30f), $"HP {_health.Current:0} / {_health.Max:0}    {status}", _hudStyle);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere((Vector2)transform.position + _facing * attackReach, attackRadius);
    }
}