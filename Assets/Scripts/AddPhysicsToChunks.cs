using UnityEngine;
using System.Collections;

public class EarthBreakController : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float explosionForce = 2f;
    public float explosionRadius = 1f;

    [Header("Gravity Settings")]
    public float initialGravity = 0f;      // pocz¹tkowa grawitacja
    public float targetGravity = 9.81f;    // koñcowa grawitacja
    public float gravityGrowTime = 4.0f;   // czas zwiêkszania grawitacji

    private Rigidbody[] chunkBodies;
    private bool broken = false;
    private float currentGravity = 0f;

    void Awake()
    {
        var chunks = GetComponentsInChildren<Transform>();
        var rbList = new System.Collections.Generic.List<Rigidbody>();

        foreach (var t in chunks)
        {
            if (t == transform) continue;

            Rigidbody rb = t.GetComponent<Rigidbody>();
            if (rb == null)
                rb = t.gameObject.AddComponent<Rigidbody>();

            rb.useGravity = false;   // WA¯NE – wy³¹czamy wbudowan¹ grawitacjê
            rb.isKinematic = true;
            rbList.Add(rb);

            MeshCollider col = t.GetComponent<MeshCollider>();
            if (col == null)
            {
                col = t.gameObject.AddComponent<MeshCollider>();
                col.convex = true;
            }
        }

        chunkBodies = rbList.ToArray();
    }

    public void Break()
    {
        if (broken) return;
        broken = true;

        foreach (var rb in chunkBodies)
        {
            rb.isKinematic = false;

            // EXPLOSION
            rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
        }

        currentGravity = initialGravity;
        StartCoroutine(GravityGrowRoutine());
    }

    void FixedUpdate()
    {
        if (!broken) return;

        // Nasza w³asna grawitacja
        Vector3 gravity = Vector3.down * currentGravity;

        foreach (var rb in chunkBodies)
            rb.AddForce(gravity, ForceMode.Acceleration);
    }

    IEnumerator GravityGrowRoutine()
    {
        float timer = 0f;

        while (timer < gravityGrowTime)
        {
            float t = timer / gravityGrowTime;
            currentGravity = Mathf.Lerp(initialGravity, targetGravity, t);

            timer += Time.deltaTime;
            yield return null;
        }

        // po zakoñczeniu — pe³na grawitacja
        currentGravity = targetGravity;
    }
}
