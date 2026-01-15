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

    [SerializeField] private float heightOffset = 0.01f;


    [SerializeField] private bool randomRotation = true;

    [SerializeField] private bool randomScale = true;
    [SerializeField] private float minScale = 0.8f;
    [SerializeField] private float maxScale = 1.2f;

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private List<Vector3> spawnedPositions = new List<Vector3>();
    private List<float> spawnedRadii = new List<float>();

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
        {
            planes.Add(plane);
        }

        if (planes.Count == 0)
        {
            Debug.LogWarning("BackgroundObjectSpawner: No scanned planes!");
            return;
        }

        int successfulSpawns = 0;
        for (int i = 0; i < objectCount; i++)
        {
            int countBefore = spawnedObjects.Count;
            SpawnSingleObject(planes);
            if (spawnedObjects.Count > countBefore)
            {
                successfulSpawns++;
            }
        }

        Debug.Log($"BackgroundObjectSpawner: Created {successfulSpawns}/{objectCount} background objects");
    }


    private void SpawnSingleObject(List<ARPlane> planes)
    {
        if (planes.Count == 0) return;

        ARPlane randomPlane = planes[Random.Range(0, planes.Count)];
        GameObject prefab = backgroundPrefabs[Random.Range(0, backgroundPrefabs.Count)];

        float scaleMultiplier = randomScale ? Random.Range(minScale, maxScale) : 1f;
        float objectRadius = GetObjectRadius(prefab) * scaleMultiplier;

        Vector3 spawnPosition = Vector3.zero;
        bool validPositionFound = false;
        int maxAttempts = 15;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            spawnPosition = GetRandomPointOnPlane(randomPlane);

            if (IsPositionValid(spawnPosition, objectRadius))
            {
                validPositionFound = true;
                break;
            }
        }

        if (!validPositionFound)
        {
            Debug.LogWarning($"BackgroundObjectSpawner: Could not find valid position for object after {maxAttempts} attempts.");
            return;
        }

        Quaternion rotation = randomRotation
            ? Quaternion.Euler(0, Random.Range(0f, 360f), 0)
            : Quaternion.identity;

        GameObject newObject = Instantiate(prefab, spawnPosition, rotation);

        if (randomScale)
        {
            newObject.transform.localScale = prefab.transform.localScale * scaleMultiplier;
        }

        spawnedObjects.Add(newObject);
        spawnedPositions.Add(spawnPosition);
        spawnedRadii.Add(objectRadius);
    }

    private bool IsPositionValid(Vector3 position, float newObjectRadius)
    {
        for (int i = 0; i < spawnedPositions.Count; i++)
        {
            Vector3 existingPos = spawnedPositions[i];
            float existingRadius = spawnedRadii[i];

            float requiredDistance = newObjectRadius + existingRadius + minDistanceBetweenObjects;

            if (Vector3.Distance(position, existingPos) < requiredDistance)
            {
                return false;
            }
        }
        return true;
    }

    private float GetObjectRadius(GameObject prefab)
    {
        Renderer renderer = prefab.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Bounds localBounds = renderer.bounds;
            Vector3 prefabScale = prefab.transform.localScale;

            float sizeX = localBounds.size.x;
            float sizeZ = localBounds.size.z;

            return Mathf.Max(sizeX, sizeZ) / 2f;
        }

        return 0.5f;
    }

    private Vector3 GetRandomPointOnPlane(ARPlane plane)
    {
        Vector2 size = plane.size;

        float maxX = (size.x / 2f) - minDistanceFromEdge;
        float maxY = (size.y / 2f) - minDistanceFromEdge;

        maxX = Mathf.Max(maxX, 0.1f);
        maxY = Mathf.Max(maxY, 0.1f);

        Vector2 randomPoint2D = new Vector2(
            Random.Range(-maxX, maxX),
            Random.Range(-maxY, maxY)
        );

        return plane.transform.TransformPoint(new Vector3(randomPoint2D.x, heightOffset, randomPoint2D.y));
    }

    public void ClearPreviousObjects()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();
        spawnedPositions.Clear();
        spawnedRadii.Clear();
    }

    private void OnDestroy()
    {
        ClearPreviousObjects();
    }
}
