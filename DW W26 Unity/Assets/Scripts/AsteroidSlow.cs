using UnityEngine;

public class AsteroidBelt : MonoBehaviour
{
    [Header("Center (Saturn) + Ring tilt")]
    [SerializeField] Transform center;          // Saturn transform
    [SerializeField] Transform ringPlane;       // ring mesh OR an empty tilted like the ring
    [SerializeField] bool followRingPlaneRotation = true;

    [Header("Offset (keeps asteroids out of the ring collider)")]
    [SerializeField] float ringNormalOffset = 8f; // + = "above" ringPlane.up, - = below
    [SerializeField] float extraRandomOffset = 0f; // optional little randomness (0-2) looks nice

    [Header("Auto radius from Saturn (optional)")]
    [SerializeField] Renderer saturnRenderer;
    [SerializeField] Collider saturnCollider;

    [Header("Belt sizing")]
    [SerializeField] float innerRadiusMultiplier = 1.15f;
    [SerializeField] float beltRadialWidth = 150f;
    [SerializeField] float beltThickness = 25f;
    [SerializeField] float tangentJitter = 10f;

    [Header("Spawning")]
    [SerializeField] GameObject[] asteroidPrefabs;
    [SerializeField] int asteroidCount = 300;
    [SerializeField] Vector2 randomScale = new Vector2(0.6f, 1.6f);
    [SerializeField] bool randomRotation = true;

    [Header("Rotation")]
    [SerializeField] float degreesPerSecond = 8f;

    [Header("Optional")]
    [SerializeField] int seed = 0; // 0 = random each run, otherwise repeatable
    [SerializeField] bool pressRToRebuild = false;

    float baseRadius;

    void Start()
    {
        if (center == null) center = transform;
        RecalcRadius();
        Generate();
    }

    void Update()
    {
        if (center == null) return;

        // match ring tilt if wanted
        if (followRingPlaneRotation && ringPlane != null)
            transform.rotation = ringPlane.rotation;

        // IMPORTANT: belt position is CENTER + offset along ring normal
        Vector3 normal = (ringPlane != null) ? ringPlane.up : transform.up;
        transform.position = center.position + normal * ringNormalOffset;

        // spin around the ring's normal (transform.up once tilted)
        transform.Rotate(transform.up, degreesPerSecond * Time.deltaTime, Space.World);

        if (pressRToRebuild && Input.GetKeyDown(KeyCode.R))
            Rebuild();
    }

    [ContextMenu("Recalc Radius + Regenerate")]
    public void Rebuild()
    {
        RecalcRadius();
        Generate();
    }

    void RecalcRadius()
    {
        float saturnRadius = 300f;

        if (saturnRenderer != null)
        {
            var ext = saturnRenderer.bounds.extents;
            saturnRadius = Mathf.Max(ext.x, ext.z);
        }
        else if (saturnCollider != null)
        {
            var ext = saturnCollider.bounds.extents;
            saturnRadius = Mathf.Max(ext.x, ext.z);
        }

        baseRadius = saturnRadius * innerRadiusMultiplier;
    }

    public void Generate()
    {
        Clear();

        if (asteroidPrefabs == null || asteroidPrefabs.Length == 0)
        {
            Debug.LogError("AsteroidBelt: no asteroid prefabs assigned.");
            return;
        }

        if (seed != 0) Random.InitState(seed);

        float r1 = baseRadius;
        float r2 = baseRadius + beltRadialWidth;

        for (int i = 0; i < asteroidCount; i++)
        {
            GameObject prefab = asteroidPrefabs[Random.Range(0, asteroidPrefabs.Length)];
            if (prefab == null) continue;

            // uniform distribution across ring band area
            float u = Random.value;
            float r = Mathf.Sqrt(Mathf.Lerp(r1 * r1, r2 * r2, u));
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

            Vector3 local = new Vector3(
                Mathf.Cos(angle) * r,
                0f,
                Mathf.Sin(angle) * r
            );

            if (tangentJitter > 0f)
            {
                Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                local += tangent * Random.Range(-tangentJitter, tangentJitter);
            }

            // thickness in local Y (ring normal direction)
            local.y = Random.Range(-beltThickness, beltThickness);

            // extra nudge away from ring collider (still along normal)
            if (extraRandomOffset > 0f)
                local.y += Random.Range(-extraRandomOffset, extraRandomOffset);

            var go = Instantiate(prefab, transform);
            go.transform.localPosition = local;

            float s = Random.Range(randomScale.x, randomScale.y);
            go.transform.localScale *= s;

            if (randomRotation)
            {
                go.transform.localRotation = Quaternion.Euler(
                    Random.Range(0f, 360f),
                    Random.Range(0f, 360f),
                    Random.Range(0f, 360f)
                );
            }
        }
    }

    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }
}
