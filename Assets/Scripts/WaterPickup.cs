using UnityEngine;

public class WaterPickup : MonoBehaviour
{
    [Header("Efekt chluśnięcia")]
    public GameObject splashPrefab;

    private bool broken = false;
    private Rigidbody rb;
    private SphereCollider col;

    // To będzie wywołane przez ProximityDetector
    public void Break()
    {
        if (broken) return;
        broken = true;

        // dodaj Rigidbody jeśli nie istnieje
        rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = 0.1f;
        rb.linearDamping = 0.2f;
        rb.angularDamping = 0.2f;

        // upewnij się, że collider działa
        col = GetComponent<SphereCollider>();
        if (col == null)
        {
            col = gameObject.AddComponent<SphereCollider>();
            col.radius = 0.1f;
        }
        col.isTrigger = false; // żeby obiekt mógł dotknąć podłogi
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!broken) return;

        if (splashPrefab != null && collision.contacts.Length > 0)
        {
            Vector3 hitPoint = collision.contacts[0].point;
            Instantiate(splashPrefab, hitPoint, Quaternion.identity);
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.enabled = false;
        }

        //Destroy(gameObject);
    }
}