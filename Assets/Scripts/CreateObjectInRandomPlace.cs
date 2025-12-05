using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class CreateObjectInRandomPlace : MonoBehaviour
{
    public ARPlaneManager planeManager;

    public List<GameObject> elementPrefabs = new List<GameObject>();
    public float heightOffset = 1.0f;
    private GameObject currentObject;
    private ElementType currentElementType;

    public void CreateObject()
    {
        var planes = new List<ARPlane>();
        foreach (var p in planeManager.trackables)
            planes.Add(p);

        if (planes.Count == 0)
        {
            Debug.Log("No planes available to place the object.");
            return;
        }

        int randomIndex = Random.Range(0, planes.Count);
        var randomPlane = planes[randomIndex];
        int random_item = UnityEngine.Random.Range(0, elementPrefabs.Count);
        Vector3 spawnPos = GetRandomPointOnPlane(randomPlane);
        currentObject = Instantiate(elementPrefabs[random_item], spawnPos, Quaternion.identity);

        Debug.Log("Object created at: " + spawnPos);
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

    public void ClearPreviousObject()
    {
        if (currentObject != null)
        {
            Destroy(currentObject);
            currentObject = null;
        }
    }

    /// <summary>
    /// Get the element type of the currently spawned object.
    /// </summary>
    public ElementType GetCurrentElementType()
    {
        return currentElementType;
    }

    /// <summary>
    /// Get reference to the currently spawned object.
    /// </summary>
    public GameObject GetCurrentObject()
    {
        return currentObject;
    }
}
