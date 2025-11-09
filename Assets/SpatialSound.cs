using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit; // for placement (select) events

// INSTRUCTION:
// 1. Find XR Origin -> Camera offset -> Object spawner
// 2. In the inspector find Object prefabs. In there select an object (double click)
// 3. To the prefab add this script
// 4. In the script's settings add audio clib
// 5. Play the game, add this object. Test spatial audio

[RequireComponent(typeof(AudioSource))]
public class SpatialSound : MonoBehaviour
{
    [Header("Clip & cadence")]
    public AudioClip clip;
    [Tooltip("Seconds between plays after the object is placed.")]
    public float intervalSeconds = 2f;

    [Header("Distance (meters)")]
    public float d1 = 0.5f;   // full volume inside this
    public float d2 = 5f;     // 0 volume beyond this

    private AudioSource src;
    private Coroutine pulseRoutine;
    private bool hasStarted = false;

    // Optional: start automatically after first Select Exit (when user releases the object)
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        ConfigureAudioSource();
    }

    void OnEnable()
    {
        TryHookInteractable();
    }

    void OnDisable()
    {
        UnhookInteractable();
        StopPulse();
    }

    void OnDestroy()
    {
        UnhookInteractable();
    }

    // ----------------- Public API -----------------

    /// <summary>Call this to manually start the beeps (e.g., from your spawner)</summary>
    public void StartAfterPlaced()
    {
        if (!hasStarted)
        {
            hasStarted = true;
            StartPulse();
        }
    }

    public void SetDistances(float inner, float outer)
    {
        d1 = Mathf.Max(0.01f, inner);
        d2 = Mathf.Max(d1, outer);
        ConfigureAudioSource();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (src == null) src = GetComponent<AudioSource>();
            ConfigureAudioSource();
        }
        intervalSeconds = Mathf.Max(0.1f, intervalSeconds);
    }

    // Visualize ranges in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
        Gizmos.DrawWireSphere(Vector3.zero, Mathf.Min(d1, d2));
        Gizmos.color = new Color(0f, 0f, 1f, 0.25f);
        Gizmos.DrawWireSphere(Vector3.zero, Mathf.Max(d1, d2));
    }
#endif

    // ----------------- Internals -----------------

    private void ConfigureAudioSource()
    {
        if (src == null) return;

        // 3D settings
        src.spatialBlend = 1f;      // 3D
        src.spatialize = true;    // use spatializer plugin if configured
        src.dopplerLevel = 0f;

        // Linear falloff from d1 -> d2
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = Mathf.Min(d1, d2);
        src.maxDistance = Mathf.Max(d1, d2);

        // Playback behavior: single shots only, never auto-play
        src.loop = false;
        src.playOnAwake = false;
        src.clip = clip;
    }

    private void StartPulse()
    {
        if (pulseRoutine != null || clip == null) return;
        pulseRoutine = StartCoroutine(Pulse());
    }

    private void StopPulse()
    {
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }
    }

    private IEnumerator Pulse()
    {
        var wait = new WaitForSeconds(Mathf.Max(0.1f, intervalSeconds));
        while (enabled)
        {
            // Play one-shot so it never loops; respects 3D settings
            src.PlayOneShot(clip);
            yield return wait;
        }
    }

    // ---------- Auto-start after placement via XRI ----------

    private void TryHookInteractable()
    {
        // Works with XRGrabInteractable and other XRBaseInteractable types.
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        if (interactable != null)
        {
            interactable.selectExited.AddListener(OnSelectExited);
        }
    }

    private void UnhookInteractable()
    {
        if (interactable != null)
        {
            interactable.selectExited.RemoveListener(OnSelectExited);
            interactable = null;
        }
    }

    // Called when the user releases the object for the first time (i.e., "placed")
    private void OnSelectExited(SelectExitEventArgs _)
    {
        StartAfterPlaced();
        // If you only want to react the first time, you can unhook now:
        UnhookInteractable();
    }
}
