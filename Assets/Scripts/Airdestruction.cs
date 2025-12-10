using UnityEngine;
using System.Collections;

public class Airdestruction : MonoBehaviour
{
    [Header("Destruction Settings")]
    public float duration = 1.2f;         // czas animacji
    public float expandMultiplier = 2.5f; // o ile większy obiekt się stanie

    private ParticleSystem ps;
    private ParticleSystem.EmissionModule emission;

    private float startEmission;
    private Vector3 originalScale;
    private bool destroyed = false;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            emission = ps.emission;
            startEmission = emission.rateOverTime.constant;   // ← AUTOMATYCZNY ODCZYT
        }

        originalScale = transform.localScale;
    }

    public void Break()
    {
        if (destroyed) return;
        destroyed = true;

        StartCoroutine(DestroyRoutine());
    }

    private IEnumerator DestroyRoutine()
    {
        float t = 0f;

        while (t < duration)
        {
            float k = t / duration;

            // 1. Powiększanie kuli
            transform.localScale = Vector3.Lerp(originalScale, originalScale * expandMultiplier, k);

            // 2. Stopniowe zmniejszanie emisji
            if (ps != null)
            {
                float newEmission = Mathf.Lerp(startEmission, 0f, k);
                emission.rateOverTime = newEmission;
            }

            t += Time.deltaTime;
            yield return null;
        }

    }
}
