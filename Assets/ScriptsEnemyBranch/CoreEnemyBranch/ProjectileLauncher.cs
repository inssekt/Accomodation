using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class ProjectileLauncher : MonoBehaviour
{
    [SerializeField] private Projectile projectilePrefab;

    [Tooltip("Where projectiles spawn. Put it at the tip of the barrel. Defaults to this object.")]
    [SerializeField] private Transform muzzle;

    [Tooltip("Who is credited with hits and ignored by the bullets. Defaults to this object.")]
    [SerializeField] private GameObject owner;

    private static Transform s_container;   // shared parent that keeps the Hierarchy tidy
    private ObjectPool<Projectile> _pool;
    private System.Action<Projectile> _release;

    private void Awake()
    {
        if (muzzle == null) muzzle = transform;
        if (owner == null) owner = gameObject;
        if (projectilePrefab == null) Debug.LogError("ProjectileLauncher has no Projectile Prefab assigned.", this);

        _pool = new ObjectPool<Projectile>(
            createFunc: CreateProjectile,
            actionOnRelease: projectile => projectile.gameObject.SetActive(false),
            actionOnDestroy: projectile => { if (projectile != null) Destroy(projectile.gameObject); },
            collectionCheck: false,
            defaultCapacity: 16,
            maxSize: 256);
        _release = _pool.Release;
    }

    /// <summary>Fires one full volley. Yield on it to wait until the last burst has fired.</summary>
    public IEnumerator FireVolley(FirePatternData pattern, Transform target)
    {
        if (pattern == null || pattern.Projectile == null)
        {
            Debug.LogWarning("Fire pattern or its projectile is missing.", this);
            yield break;
        }

        Vector2 lockedAim = ComputeAim(pattern, target);
        float spin = 0f;

        for (int burst = 0; burst < pattern.BurstCount; burst++)
        {
            Vector2 aim = pattern.AimMode == AimMode.AtTarget ? ComputeAim(pattern, target) : lockedAim;
            float baseAngle = VectorUtils.ToAngle(aim) + spin;

            for (int i = 0; i < pattern.ProjectilesPerBurst; i++)
            {
                float jitter = Random.Range(-pattern.RandomSpread, pattern.RandomSpread);
                float angle = baseAngle + pattern.GetAngleOffset(i) + jitter;
                Spawn(pattern.Projectile, VectorUtils.FromAngle(angle));
            }

            spin += pattern.SpinPerBurst;

            if (burst < pattern.BurstCount - 1)
                yield return new WaitForSeconds(pattern.TimeBetweenBursts);
        }
    }

    private Vector2 ComputeAim(FirePatternData pattern, Transform target)
    {
        Vector2 fallback = VectorUtils.FromAngle(pattern.FixedAngle);
        if (pattern.AimMode == AimMode.FixedAngle || target == null) return fallback;

        Vector2 toTarget = (Vector2)target.position - (Vector2)muzzle.position;
        return toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : fallback;
    }

    private void Spawn(ProjectileData data, Vector2 direction)
    {
        Projectile projectile = _pool.Get();
        projectile.Launch(data, muzzle.position, direction, owner, _release);
    }

    private Projectile CreateProjectile()
    {
        if (s_container == null) s_container = new GameObject("[Projectiles]").transform;

        Projectile instance = Instantiate(projectilePrefab, s_container);
        instance.gameObject.SetActive(false);
        return instance;
    }
}