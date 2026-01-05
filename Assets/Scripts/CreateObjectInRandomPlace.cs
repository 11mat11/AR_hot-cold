using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class CreateObjectInRandomPlace : MonoBehaviour
{
    public ARPlaneManager planeManager;

    public List<GameObject> elementPrefabs = new List<GameObject>();
    public float heightOffset = 1.0f;
    private List<GameObject> currentObjects = new List<GameObject>();
    private Vector3 scanCenter = Vector3.zero;
    private bool scanCenterCalculated = false;

    public void SpawnLevelObjects()
    {
        if (!scanCenterCalculated)
        {
            CalculateScanCenter();
        }

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

        CheckOneStepAheadAchievement(newObj);
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

    private void CalculateScanCenter()
    {
        var planes = new List<ARPlane>();
        foreach (var p in planeManager.trackables) planes.Add(p);

        if (planes.Count == 0)
        {
            scanCenter = Vector3.zero;
            return;
        }

        Vector3 sum = Vector3.zero;
        foreach (var plane in planes)
        {
            sum += plane.center;
        }
        scanCenter = sum / planes.Count;
        scanCenterCalculated = true;
    }

    public Vector3 GetScanCenter()
    {
        if (!scanCenterCalculated)
            CalculateScanCenter();
        return scanCenter;
    }

    public float GetTotalScanArea()
    {
        var planes = new List<ARPlane>();
        foreach (var p in planeManager.trackables) planes.Add(p);

        float totalArea = 0f;
        foreach (var plane in planes)
        {
            totalArea += plane.size.x * plane.size.y;
        }
        return totalArea;
    }

    public void ResetScanCenter()
    {
        scanCenterCalculated = false;
    }

    private void CheckOneStepAheadAchievement(GameObject spawnedObject)
    {
        ProximityDetector proximity = spawnedObject.GetComponent<ProximityDetector>();
        if (proximity != null && proximity.IsPlayerWithinRange())
        {
            GPGSManager.Instance?.UnlockAchievement(GPGSIds.achievement_one_step_ahead);
        }
    }
}
