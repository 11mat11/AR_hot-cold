using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class CreateObjectInRandomPlace : MonoBehaviour
{
    public ARPlaneManager planeManager;

    public List<GameObject> elementPrefabs = new List<GameObject>();
    public float heightOffset = 1.0f;
    private List<GameObject> currentObjects = new List<GameObject>();

    public void SpawnLevelObjects()
    {
        int countToSpawn = ScoreManager.Instance.ObjectsRequiredPerLevel;

        HashSet<int> result = new HashSet<int>();

        while (result.Count < countToSpawn)
        {
            int r = Random.Range(0, elementPrefabs.Count);
            result.Add(r);
        }

        foreach (var index in result)
        {
            CreateSingleObject(index);
        }
    }

    private void CreateSingleObject(int elementIndex)
    {
        var planes = new List<ARPlane>();
        foreach (var p in planeManager.trackables) planes.Add(p);

        if (planes.Count == 0) return;

        int randomIndex = Random.Range(0, planes.Count);
        var randomPlane = planes[randomIndex];

        Vector3 spawnPos = GetRandomPointOnPlane(randomPlane);
        GameObject newObj = Instantiate(elementPrefabs[elementIndex], spawnPos, Quaternion.identity);
        currentObjects.Add(newObj);
    }

    Vector3 GetRandomPointOnPlane(ARPlane plane)
    {
        Vector2 size = plane.size;
        Vector2 randomPoint2D = new Vector2(
            UnityEngine.Random.Range(-size.x / 2f, size.x / 2f),
            UnityEngine.Random.Range(-size.y / 2f, size.y / 2f)
        );

        return plane.transform.TransformPoint(new Vector3(randomPoint2D.x, heightOffset, randomPoint2D.y));
    }

    public void ClearPreviousObjects()
    {
        foreach (var obj in currentObjects)
        {
            if (obj != null) Destroy(obj);
        }
        currentObjects.Clear();
    }

    public void RemoveObjectFromList(GameObject obj)
    {
        if (currentObjects.Contains(obj))
            currentObjects.Remove(obj);
    }
}
