using UnityEngine;

/// <summary>
/// Drives an enemy's sprite animation. Put it on the enemy's root object.
/// - Sets the Animator bool "Moving" while the enemy moves.
/// - Flips the sprite to face where it is heading. While standing still it faces the player,
///   so a wind-up looks at its target.
/// - PlayAttack() and PlayHurt() set the Animator triggers "Attack" and "Hurt". Hook them to events
///   in the Inspector, for example Melee Enemy > On Attack and Health > On Damaged.
/// With no Rigidbody 2D (a stationary shooter) it stays idle and just faces the player.
/// </summary>
public class EnemyAnimator : MonoBehaviour
{
    [Header("References (found in the children automatically if left empty)")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Movement")]
    [Tooltip("Slower than this, in units per second, counts as standing still.")]
    [SerializeField, Min(0f)] private float movingThreshold = 0.1f;

    [Header("Facing")]
    [Tooltip("Tick this if the art faces LEFT in the image.")]
    [SerializeField] private bool artFacesLeft = false;
    [Tooltip("Sideways movement smaller than this is ignored, so the sprite does not flicker left and right.")]
    [SerializeField, Min(0f)] private float flipDeadzone = 0.05f;
    [Tooltip("While standing still, the enemy turns to look at whatever has this tag.")]
    [SerializeField] private string faceTag = "Player";

    private static readonly int MovingHash = Animator.StringToHash("Moving");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");

    private Rigidbody2D _body;
    private Transform _faceTarget;
    private float _nextSearchTime;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        Vector2 velocity = _body != null ? _body.linearVelocity : Vector2.zero;
        bool moving = velocity.magnitude > movingThreshold;

        if (animator != null) animator.SetBool(MovingHash, moving);
        if (spriteRenderer == null) return;

        float lookX = moving ? velocity.x : DirectionToTargetX();
        if (Mathf.Abs(lookX) > flipDeadzone) FaceRight(lookX > 0f);
    }

    /// <summary>Plays the attack animation. Hook this to an attack event, for example Melee Enemy > On Attack.</summary>
    public void PlayAttack()
    {
        if (animator != null) animator.SetTrigger(AttackHash);
    }

    /// <summary>
    /// Plays the hurt animation. Hook this to Health > On Damaged. The damage amount is not used here;
    /// the method takes it only because that event passes one.
    /// </summary>
    public void PlayHurt(float damage)
    {
        if (animator != null) animator.SetTrigger(HurtHash);
    }

    private float DirectionToTargetX()
    {
        if (_faceTarget == null)
        {
            FindFaceTarget();
            return 0f;
        }

        return _faceTarget.position.x - transform.position.x;
    }

    private void FindFaceTarget()
    {
        if (Time.time < _nextSearchTime) return;

        _nextSearchTime = Time.time + 0.5f;   // look twice a second, not every frame

        GameObject found = GameObject.FindWithTag(faceTag);
        if (found != null) _faceTarget = found.transform;
    }

    private void FaceRight(bool faceRight)
    {
        spriteRenderer.flipX = artFacesLeft ? faceRight : !faceRight;
    }
}