using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon Crawler/Projectile Data", fileName = "Proj_New")]
public class ProjectileData : ScriptableObject
{
    [Header("Combat")]
    [SerializeField, Min(0f)] private float damage = 10f;

    [Tooltip("Seconds before the projectile disappears by itself.")]
    [SerializeField, Min(0.1f)] private float lifetime = 4f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float speed = 6f;

    [Tooltip("Speed change per second. Negative values slow the projectile down.")]
    [SerializeField] private float acceleration = 0f;

    [Tooltip("Degrees per second the path bends. 0 = straight line. Positive = anticlockwise.")]
    [SerializeField] private float curveDegreesPerSecond = 0f;

    [Header("Look")]
    [Tooltip("Leave empty to keep the sprite already on the prefab.")]
    [SerializeField] private Sprite sprite;
    [SerializeField] private Color tint = Color.white;

    [Tooltip("Scales the whole projectile, including its collider.")]
    [SerializeField, Min(0.05f)] private float scale = 0.4f;

    [Tooltip("Optional prefab spawned where the projectile hits. It should destroy itself.")]
    [SerializeField] private GameObject hitEffectPrefab;

    public float Damage => damage;
    public float Lifetime => lifetime;
    public float Speed => speed;
    public float Acceleration => acceleration;
    public float CurveDegreesPerSecond => curveDegreesPerSecond;
    public Sprite Sprite => sprite;
    public Color Tint => tint;
    public float Scale => scale;
    public GameObject HitEffectPrefab => hitEffectPrefab;
}