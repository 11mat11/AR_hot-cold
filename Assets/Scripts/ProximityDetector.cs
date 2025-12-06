using UnityEngine;

public class ProximityDetector : MonoBehaviour
{
    public float triggerDistance_point = 0.2f; // odległość w metrach
    public float triggerDistance_visable = 3.0f;
    private Renderer[] renderers;
    void Start()
    {
        // Pobieramy wszystkie rendery obiektu i dzieci
        renderers = GetComponentsInChildren<Renderer>();
    }
    void Update()
    {
        if (Camera.main == null) return;

        Vector3 camPos = Camera.main.transform.position;
        float dist = Vector3.Distance(camPos, transform.position);
        if (dist < triggerDistance_visable)
        {
            SetVisibility(true);
            if (dist < triggerDistance_point)
            {
                Debug.Log("zdobywasz punkt od:" + gameObject.name);

            }
        }
        else
        {
            SetVisibility(false);
        }
    }
    private void SetVisibility(bool visible)
    {
        foreach (var r in renderers)
            r.enabled = visible;
    }
}
