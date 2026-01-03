using UnityEngine;
using System.Collections;

public class ProximityDetector : MonoBehaviour
{
    public float triggerDistance_point = 0.2f;
    public float triggerDistance_visible = 3.0f;

    public MonoBehaviour destructionScript;
    public string destructionMethodName = "Break";

    public float destructionDelay = 1.0f;
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
            creator = FindFirstObjectByType<CreateObjectInRandomPlace>();
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
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.RegisterObjectFound();

        if (creator != null)
            creator.RemoveObjectFromList(gameObject);

        if (destructionScript != null && !string.IsNullOrEmpty(destructionMethodName))
        {
            destructionScript.Invoke(destructionMethodName, 0f);
        }

        yield return new WaitForSeconds(destructionDelay);

        if (fadeOut)
            yield return StartCoroutine(FadeOutRoutine());

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
