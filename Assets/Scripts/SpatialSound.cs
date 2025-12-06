// INSTRUCTION:
// 1. Find XR Origin -> Camera offset -> Object spawner
// 2. In the inspector find Object prefabs. In there select an object (double click)
// 3. To the prefab add this script
// 4. In the script's settings add audio clib
// 5. Play the game, add this object. Test spatial audio

using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(AudioSource))]
public class SpatialSound : MonoBehaviour
{
    [Header("Clip & cadence")]
    public AudioClip clip;
    public float intervalSeconds = 2f;

    [Header("Volume")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("Distance (meters)")]
    public float fullVolumeDistance = 0.5f;
    public float noVolumeDistance = 5f;

    private AudioSource audioSource;
    private Coroutine pulseRoutine;
    private bool hasStarted = false;
    private XRBaseInteractable interactable;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        ConfigureAudioSource();
    }

    void Start()
    {
        StartPulse();
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
        StopPulse();
    }

    public void StartAfterPlaced()
    {
        if (!hasStarted)
        {
            hasStarted = true;
            StartPulse();
        }
    }

    public void SetDistances(float innerDistance, float outerDistance)
    {
        fullVolumeDistance = Mathf.Max(0.01f, innerDistance);
        noVolumeDistance = Mathf.Max(fullVolumeDistance, outerDistance);
        ConfigureAudioSource();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            ConfigureAudioSource();
        }
        intervalSeconds = Mathf.Max(0.1f, intervalSeconds);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
        Gizmos.DrawWireSphere(Vector3.zero, Mathf.Min(fullVolumeDistance, noVolumeDistance));
        Gizmos.color = new Color(0f, 0f, 1f, 0.25f);
        Gizmos.DrawWireSphere(Vector3.zero, Mathf.Max(fullVolumeDistance, noVolumeDistance));
    }
#endif

    private void ConfigureAudioSource()
    {
        if (audioSource == null) return;
        audioSource.spatialBlend = 1f;
        audioSource.spatialize = true;
        audioSource.dopplerLevel = 0f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = Mathf.Min(fullVolumeDistance, noVolumeDistance);
        audioSource.maxDistance = Mathf.Max(fullVolumeDistance, noVolumeDistance);
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.clip = clip;
    }

    private void StartPulse()
    {
        if (pulseRoutine != null || clip == null) return;
        pulseRoutine = StartCoroutine(PulseRoutine());
    }

    private void StopPulse()
    {
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }
    }

    private IEnumerator PulseRoutine()
    {
        var wait = new WaitForSeconds(Mathf.Max(0.1f, intervalSeconds));
        while (enabled)
        {
            audioSource.PlayOneShot(clip, volume);
            yield return wait;
        }
    }

    private void TryHookInteractable()
    {
        interactable = GetComponent<XRBaseInteractable>();
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

    private void OnSelectExited(SelectExitEventArgs _)
    {
        StartAfterPlaced();
        UnhookInteractable();
    }
}
