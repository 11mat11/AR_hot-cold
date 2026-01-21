using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class BackgroundObjectSpawner : MonoBehaviour
{
    [Header("AR References")]
    public ARPlaneManager planeManager;

    [Header("Background Objects")]
    public List<GameObject> backgroundPrefabs = new List<GameObject>();

    [Header("Spawn Settings")]
    [SerializeField] private int objectCount = 15;
    [SerializeField] private float minDistanceBetweenObjects = 0.5f;
    [SerializeField] private float minDistanceFromEdge = 0.2f;
    [SerializeField] private float heightOffset = 0.001f;

    [SerializeField] private bool randomRotation = true;

    [SerializeField] private bool randomScale = true;
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 0.8f;

    private readonly List<GameObject> spawnedObjects = new();
    private readonly List<Vector3> spawnedPositions = new();
    private readonly List<float> spawnedRadii = new();

    public void SpawnBackgroundObjects()
    {
        if (backgroundPrefabs.Count == 0)
        {
            Debug.LogWarning("BackgroundObjectSpawner: No prefabs!");
            return;
        }

        ClearPreviousObjects();

        var planes = new List<ARPlane>();
        foreach (var plane in planeManager.trackables)
            planes.Add(plane);

        if (planes.Count == 0)
        {
            Debug.LogWarning("BackgroundObjectSpawner: No scanned planes!");
            return;
        }

        int successfulSpawns = 0;

        for (int i = 0; i < objectCount; i++)
        {
            int before = spawnedObjects.Count;
            SpawnSingleObject(planes);
            if (spawnedObjects.Count > before)
                successfulSpawns++;
        }

        Debug.Log($"BackgroundObjectSpawner: Created {successfulSpawns}/{objectCount} background objects");
    }

    private void SpawnSingleObject(List<ARPlane> planes)
    {
        ARPlane randomPlane = planes[Random.Range(0, planes.Count)];
        GameObject prefab = backgroundPrefabs[Random.Range(0, backgroundPrefabs.Count)];

        float scaleMultiplier = randomScale ? Random.Range(minScale, maxScale) : 1f;
        float objectRadius = GetObjectRadius(prefab) * scaleMultiplier;

        Vector3 spawnPosition = Vector3.zero;
        bool validPositionFound = false;

        for (int i = 0; i < 15; i++)
        {
            spawnPosition = GetRandomPointOnPlane(randomPlane);
            if (IsPositionValid(spawnPosition, objectRadius))
            {
                validPositionFound = true;
                break;
            }
        }

        if (!validPositionFound)
            return;

        // ===== ROOT (kfiatek9) =====
        GameObject root = Instantiate(prefab, spawnPosition, Quaternion.identity);

        // LOSOWA ROTACJA ROOT
        if (randomRotation)
        {
            root.transform.rotation = Quaternion.Euler(
                0f,
                Random.Range(0f, 360f),
                0f
            );
        }

        // SKALA ROOT
        if (randomScale)
        {
            root.transform.localScale =
                prefab.transform.localScale * scaleMultiplier;
        }

        // ===== ANIMACJA (NA DZIECKU) =====
        Animator animator = root.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            // LOSOWY KIERUNEK BUJANIA (0–360°)
            float swayDirection = Random.Range(0f, 360f);
            animator.transform.localRotation = Quaternion.Euler(
                0f,
                swayDirection,
                0f
            );

            // LOSOWA FAZA + PRÊDKOŒÆ
            animator.Play("Armature.002Action", 0, Random.value);
            animator.speed = Random.Range(0.8f, 1.2f);
        }

        else
        {
            Debug.LogWarning("Spawned object has NO Animator!");
        }

        spawnedObjects.Add(root);
        spawnedPositions.Add(spawnPosition);
        spawnedRadii.Add(objectRadius);
    }

    private bool IsPositionValid(Vector3 position, float newObjectRadius)
    {
        for (int i = 0; i < spawnedPositions.Count; i++)
        {
            float requiredDistance =
                newObjectRadius + spawnedRadii[i] + minDistanceBetweenObjects;

            if (Vector3.Distance(position, spawnedPositions[i]) < requiredDistance)
                return false;
        }
        return true;
    }

    private float GetObjectRadius(GameObject prefab)
    {
        Renderer renderer = prefab.GetComponentInChildren<Renderer>();
        if (renderer == null)
            return 0.5f;

        Bounds bounds = renderer.bounds;
        return Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f;
    }

    private Vector3 GetRandomPointOnPlane(ARPlane plane)
    {
        Vector2 size = plane.size;

        float maxX = Mathf.Max((size.x * 0.5f) - minDistanceFromEdge, 0.1f);
        float maxY = Mathf.Max((size.y * 0.5f) - minDistanceFromEdge, 0.1f);

        Vector2 random2D = new(
            Random.Range(-maxX, maxX),
            Random.Range(-maxY, maxY)
        );

        return plane.transform.TransformPoint(
            new Vector3(random2D.x, heightOffset, random2D.y)
        );
    }

    public void ClearPreviousObjects()
    {
        foreach (var obj in spawnedObjects)
            if (obj != null)
                Destroy(obj);

        spawnedObjects.Clear();
        spawnedPositions.Clear();
        spawnedRadii.Clear();
    }

    private void OnDestroy()
    {
        ClearPreviousObjects();
    }
}
