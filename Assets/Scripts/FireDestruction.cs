using UnityEngine;
using System.Collections;

public class FireDestructionBasic : MonoBehaviour
{
    private ParticleSystem ps;

    public Material defaultParticleMaterial;

    [Header("Shrink settings")]
    public float shrinkDuration = 0.5f;   // jak długo się zmniejsza
    public float targetRadius = 0.01f;
    public float targetStartSize = 0.02f;

    private bool destroyed = false;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    // wywoływane przez Invoke("Break")
    public void Break()
    {
        if (destroyed) return;
        destroyed = true;

        StartCoroutine(ShrinkThenSmoke());
    }

    private IEnumerator ShrinkThenSmoke()
    {
        // --- zapamiętujemy wartości początkowe ---
        ParticleSystem.ShapeModule shape = ps.shape;
        ParticleSystem.MainModule main = ps.main;

        float startRadius = shape.radius;
        float startSize = main.startSize.constant;

        float t = 0f;

        // --- FAZA 1: stopniowe zmniejszanie ---
        while (t < shrinkDuration)
        {
            float k = t / shrinkDuration;

            shape.radius = Mathf.Lerp(startRadius, targetRadius, k);
            main.startSize = Mathf.Lerp(startSize, targetStartSize, k);

            t += Time.deltaTime;
            yield return null;
        }

        // upewniamy się, że doszliśmy dokładnie do celu
        shape.radius = targetRadius;
        main.startSize = targetStartSize;

        // --- FAZA 2: zmiana na dym (kolor + materiał) ---
        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;

        Gradient smokeGradient = new Gradient();
        smokeGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.gray, 0f),
                new GradientColorKey(Color.gray, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        col.enabled = true;
        col.color = smokeGradient;

        ParticleSystemRenderer psRenderer =
            GetComponent<ParticleSystemRenderer>();

        psRenderer.material = defaultParticleMaterial;
    }
}
