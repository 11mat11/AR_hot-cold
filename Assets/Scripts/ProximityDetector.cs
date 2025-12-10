using UnityEngine;
using System.Collections;

public class ProximityDetector : MonoBehaviour
{
    public float triggerDistance_point = 0.2f;
    public float triggerDistance_visible = 3.0f;

    public MonoBehaviour destructionScript;  // DOWOLNY skrypt destrukcji
    public string destructionMethodName = "Break"; // Nazwa metody destrukcji

    public float destructionDelay = 1.0f;   // czas na animację destrukcji
    public bool fadeOut = true;
    public float fadeDuration = 3.0f;

    private Renderer[] renderers;
    private bool triggered = false;
    private CreateObjectInRandomPlace creator;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    void Awake()
    {
        if (creator == null)
            creator = FindObjectOfType<CreateObjectInRandomPlace>();
    }

    void Update()
    {
        if (triggered) return;
        if (Camera.main == null) return;

        float dist = Vector3.Distance(Camera.main.transform.position, transform.position);

        if (dist < triggerDistance_visible)
        {
            SetVisibility(true);

            if (dist < triggerDistance_point)
            {
                triggered = true;

                Debug.Log("zdobywasz punkt od:" + gameObject.name);

                if (ScoreManager.Instance != null)
                    ScoreManager.Instance.AddPoint();

                StartCoroutine(HandleDestructionSequence());
            }
        }
        else
        {
            SetVisibility(false);
        }
    }

    IEnumerator HandleDestructionSequence()
    {
        // 1. URUCHOM DESTRUKCJĘ (Break, Explode, Collapse – cokolwiek ustawisz)
        if (destructionScript != null && !string.IsNullOrEmpty(destructionMethodName))
        {
            destructionScript.Invoke(destructionMethodName, 0f);
        }

        // 2. CZEKAJ NA ANIMACJĘ ROZPADU
        yield return new WaitForSeconds(destructionDelay);

        // 3. FADE OUT (opcjonalnie)
        if (fadeOut)
            yield return StartCoroutine(FadeOutRoutine());

        // 4. STWÓRZ NOWY OBIEKT
        if (creator != null)
            creator.CreateObject();

        // 5. USUŃ OBECNY OBIEKT
        Destroy(gameObject);
    }

    IEnumerator FadeOutRoutine()
    {
        float elapsed = 0f;
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();

        while (elapsed < fadeDuration)
        {
            float t = elapsed / fadeDuration;
            float alpha = Mathf.Lerp(1f, 0f, t);

            foreach (var r in renderers)
            {
                r.GetPropertyBlock(mpb);
                mpb.SetFloat("_Alpha", alpha);
                r.SetPropertyBlock(mpb);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void SetVisibility(bool visible)
    {
        foreach (var r in renderers)
            r.enabled = visible;
    }
}
