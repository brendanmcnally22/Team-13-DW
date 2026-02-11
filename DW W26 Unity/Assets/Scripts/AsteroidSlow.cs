using UnityEngine;

public class AsteroidBelt : MonoBehaviour
{
    [Header("Center / Saturn")]
    [SerializeField] Transform center;              // Saturn transform
    [SerializeField] Renderer saturnRenderer;       // optional, for auto radius
    [SerializeField] Collider saturnCollider;       // optional, for auto radius

    [Header("Belt sizing")]
    [SerializeField] float innerRadiusMultiplier = 1.15f; // how far from Saturn surface
    [SerializeField] float beltWidth = 80f;               // thickness outward
    [SerializeField] float beltHeight = 20f;              // vertical scatter

    [Header("Spawning")]
    [SerializeField] GameObject[] asteroidPrefabs;  // your 4 prefabs
    [SerializeField] int asteroidCount = 250;
    [SerializeField] Vector2 randomScale = new Vector2(0.6f, 1.4f);
    [SerializeField] bool randomYaw = true;

    [Header("Rotation")]
    [SerializeField] float degreesPerSecond = 8f;

    [Header("Optional")]
    [SerializeField] int seed = 0; // 0 = random every play, otherwise repeatable

    float baseRadius;

    void Start()
    {
        if (center == null) center = transform;
        transform.position = center.position;

        RecalcRadius();
        Generate();
    }

    void Update()
    {
        if (center == null) return;

        // keep belt centered even if Saturn moves
        transform.position = center.position;

        // spin the belt
        transform.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.World);
    }

    [ContextMenu("Recalc Radius + Regenerate")]
    public void Rebuild()
    {
        RecalcRadius();
        Generate();
    }

    void RecalcRadius()
    {
        float saturnRadius = 300f; // fallback

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

        for (int i = 0; i < asteroidCount; i++)
        {
            GameObject prefab = asteroidPrefabs[Random.Range(0, asteroidPrefabs.Length)];
            if (prefab == null) continue;

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float r = baseRadius + Random.Range(0f, beltWidth);

            float x = Mathf.Cos(angle) * r;
            float z = Mathf.Sin(angle) * r;
            float y = Random.Range(-beltHeight, beltHeight);

            var go = Instantiate(prefab, transform);
            go.transform.localPosition = new Vector3(x, y, z);

            float s = Random.Range(randomScale.x, randomScale.y);
            go.transform.localScale *= s;

            if (randomYaw)
                go.transform.localRotation = Quaternion.Euler(
                    Random.Range(0f, 360f),
                    Random.Range(0f, 360f),
                    Random.Range(0f, 360f)
                );
        }
    }

    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }
}
