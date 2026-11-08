using UnityEngine;

[RequireComponent(typeof(StatusController))]
public class StatusVisualFeedback : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Child object rotated when the actor falls. Never the object with the Rigidbody.")]
    [SerializeField] private Transform visualRoot;

    [SerializeField] private Color stunnedTint = new Color(1f, 0.9f, 0.4f);
    [SerializeField] private float fallenAngle = 90f;

    private StatusController _status;
    private Color _baseColor = Color.white;
    private bool _showingStun;

    private void Awake()
    {
        _status = GetComponent<StatusController>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) _baseColor = spriteRenderer.color;
    }

    private void Update()
    {
        bool stunned = _status.IsStunned;
        if (stunned == _showingStun) return;
        _showingStun = stunned;

        if (spriteRenderer != null) spriteRenderer.color = stunned ? stunnedTint : _baseColor;
        if (visualRoot != null) visualRoot.localRotation = Quaternion.Euler(0f, 0f, stunned ? fallenAngle : 0f);
    }
}