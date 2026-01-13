using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections;
using UnityEngine.Events;

public class ScanToggle : MonoBehaviour
{
    public ARPlaneManager planeManager;

    private bool scanningEnabled = true;

    [Header("UI References")]
    public TMP_Text buttonText;
    public Image buttonIcon;
    public RectTransform buttonRectTransform;

    [Header("Icon Sprites")]
    public Sprite scanningIcon;
    public Sprite stoppedIcon;

    [Header("Button Text")]
    public string scanningText = "Scanning";
    public string stoppedText = "Stopped";

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseMinScale = 0.85f;
    [SerializeField] private float pulseMaxScale = 1.15f;

    [Header("Button Position Settings")]
    [SerializeField] private float transitionDuration = 0.4f;
    [SerializeField] private float transitionDelay = 0.3f;

    [Header("Scanning State (Bottom Center - Easy Access)")]
    [SerializeField] private Vector2 scanningAnchor = new Vector2(0.5f, 0f);
    [SerializeField] private Vector2 scanningPosition = new Vector2(0f, 100f);
    [SerializeField] private Vector2 scanningSize = new Vector2(220f, 60f);

    [Header("Gameplay State (Top Right - Under Debug Menu)")]
    [SerializeField] private Vector2 gameplayAnchor = new Vector2(1f, 1f);
    [SerializeField] private Vector2 gameplayPosition = new Vector2(-100f, -200f);
    [SerializeField] private Vector2 gameplaySize = new Vector2(50f, 50f);

    public CreateObjectInRandomPlace createObjectScript;

    private Coroutine fadeCoroutine;
    private Coroutine pulseCoroutine;
    private Coroutine transitionCoroutine;
    private Vector3 iconOriginalScale;
    private bool isFirstUpdate = true;
    private bool isInGameplayPosition = false;

    void Start()
    {
        if (buttonIcon != null)
        {
            iconOriginalScale = buttonIcon.transform.localScale;
        }

        // Get RectTransform from button icon's parent (the button itself)
        if (buttonRectTransform == null && buttonIcon != null)
        {
            buttonRectTransform = buttonIcon.transform.parent.GetComponent<RectTransform>();
        }

        // Don't start scanning until safety warning is accepted
        StartCoroutine(WaitForSafetyWarningAndStart());
    }

    private IEnumerator WaitForSafetyWarningAndStart()
    {
        // Initially disable plane detection until warning is accepted
        if (planeManager != null)
        {
            planeManager.requestedDetectionMode = PlaneDetectionMode.None;
        }

        // Wait for safety warning to be accepted
        yield return new WaitUntil(() => SafetyWarningManager.WarningAccepted);

        // Now enable scanning
        if (planeManager != null && scanningEnabled)
        {
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        }

        UpdateButtonVisuals();

        // Start pulsing if scanning is enabled
        if (scanningEnabled)
        {
            StartPulsing();
        }
    }

    void OnDisable()
    {
        // Clean up coroutines
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
    }

    public void ToggleScanning()
    {
        scanningEnabled = !scanningEnabled;

        planeManager.requestedDetectionMode =
            scanningEnabled ? PlaneDetectionMode.Horizontal : PlaneDetectionMode.None;

        UpdateButtonVisuals();

        if (!scanningEnabled && createObjectScript != null)
        {
            float currentTotalArea = createObjectScript.GetTotalScanArea();
            CheckVeryEasyModeAchievement(currentTotalArea);
            CheckExplorerAchievement(currentTotalArea);
            SubmitAreaToLeaderboard(currentTotalArea);
            createObjectScript.SpawnLevelObjects();
        }
        else if (scanningEnabled && createObjectScript != null)
        {
            createObjectScript.ClearPreviousObjects();
            createObjectScript.ResetScanCenter();
            ScoreManager.Instance.Reset();
        }
    }

    private void UpdateButtonVisuals()
    {
        string newText = scanningEnabled ? scanningText : stoppedText;
        Sprite newSprite = scanningEnabled ? scanningIcon : stoppedIcon;

        if (isFirstUpdate)
        {
            // No animation on first load
            if (buttonText != null) buttonText.text = newText;
            if (buttonIcon != null) buttonIcon.sprite = newSprite;
            isFirstUpdate = false;
        }
        else
        {
            // Animate the transition
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeTransition(newText, newSprite));
        }

        // Handle pulsing and position
        if (scanningEnabled)
        {
            StartPulsing();
            // Move to scanning position (bottom center, big)
            MoveToScanningPosition();
        }
        else
        {
            StopPulsing();
            // Move to gameplay position after delay (top corner, small)
            ScheduleGameplayTransition();
        }
    }

    private void ScheduleGameplayTransition()
    {
        StopTransitionCoroutine();
        transitionCoroutine = StartCoroutine(TransitionToGameplayAfterDelay());
    }

    private IEnumerator TransitionToGameplayAfterDelay()
    {
        yield return new WaitForSeconds(transitionDelay);

        if (isInGameplayPosition) yield break;

        yield return AnimateToPosition(gameplayAnchor, gameplayPosition, gameplaySize, true);
    }

    private void MoveToScanningPosition()
    {
        StopTransitionCoroutine();

        if (!isInGameplayPosition) return;

        transitionCoroutine = StartCoroutine(AnimateToPosition(
            scanningAnchor, scanningPosition, scanningSize, false));
    }

    private void StopTransitionCoroutine()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
    }

    private IEnumerator AnimateToPosition(Vector2 targetAnchor, Vector2 targetPos, Vector2 targetSize, bool toGameplay)
    {
        if (buttonRectTransform == null) yield break;

        // Store starting values
        Vector2 startAnchorMin = buttonRectTransform.anchorMin;
        Vector2 startAnchorMax = buttonRectTransform.anchorMax;
        Vector2 startPos = buttonRectTransform.anchoredPosition;
        Vector2 startSize = buttonRectTransform.sizeDelta;

        float elapsed = 0f;

        // Fade text based on direction
        float startAlpha = toGameplay ? 1f : 0f;
        float endAlpha = toGameplay ? 0f : 1f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            // Ease out cubic for smooth deceleration
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);

            // Animate anchor
            buttonRectTransform.anchorMin = Vector2.Lerp(startAnchorMin, targetAnchor, smoothT);
            buttonRectTransform.anchorMax = Vector2.Lerp(startAnchorMax, targetAnchor, smoothT);

            // Animate position and size
            buttonRectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);
            buttonRectTransform.sizeDelta = Vector2.Lerp(startSize, targetSize, smoothT);

            // Animate text alpha
            if (buttonText != null)
            {
                Color c = buttonText.color;
                c.a = Mathf.Lerp(startAlpha, endAlpha, smoothT);
                buttonText.color = c;
            }

            yield return null;
        }

        // Ensure final values
        buttonRectTransform.anchorMin = targetAnchor;
        buttonRectTransform.anchorMax = targetAnchor;
        buttonRectTransform.anchoredPosition = targetPos;
        buttonRectTransform.sizeDelta = targetSize;

        if (buttonText != null)
        {
            Color c = buttonText.color;
            c.a = endAlpha;
            buttonText.color = c;
        }

        isInGameplayPosition = toGameplay;
        transitionCoroutine = null;
    }

    private IEnumerator FadeTransition(string newText, Sprite newSprite)
    {
        float elapsed = 0f;
        float halfDuration = fadeDuration / 2f;

        Color textStartColor = buttonText != null ? buttonText.color : Color.white;
        Color iconStartColor = buttonIcon != null ? buttonIcon.color : Color.white;

        // Phase 1: Fade out
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float alpha = Mathf.Lerp(1f, 0f, t);

            if (buttonText != null)
                buttonText.color = new Color(textStartColor.r, textStartColor.g, textStartColor.b, alpha);
            if (buttonIcon != null)
                buttonIcon.color = new Color(iconStartColor.r, iconStartColor.g, iconStartColor.b, alpha);

            yield return null;
        }

        // Swap content at the middle
        if (buttonText != null) buttonText.text = newText;
        if (buttonIcon != null) buttonIcon.sprite = newSprite;

        // Phase 2: Fade in
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float alpha = Mathf.Lerp(0f, 1f, t);

            if (buttonText != null)
                buttonText.color = new Color(textStartColor.r, textStartColor.g, textStartColor.b, alpha);
            if (buttonIcon != null)
                buttonIcon.color = new Color(iconStartColor.r, iconStartColor.g, iconStartColor.b, alpha);

            yield return null;
        }

        // Ensure full opacity
        if (buttonText != null)
            buttonText.color = new Color(textStartColor.r, textStartColor.g, textStartColor.b, 1f);
        if (buttonIcon != null)
            buttonIcon.color = new Color(iconStartColor.r, iconStartColor.g, iconStartColor.b, 1f);

        fadeCoroutine = null;
    }

    private void StartPulsing()
    {
        if (pulseCoroutine != null) return; // Already pulsing
        if (buttonIcon == null) return;

        pulseCoroutine = StartCoroutine(PulseAnimation());
    }

    private void StopPulsing()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        // Reset to original scale
        if (buttonIcon != null)
        {
            buttonIcon.transform.localScale = iconOriginalScale;
        }
    }

    private IEnumerator PulseAnimation()
    {
        float time = 0f;

        while (true)
        {
            time += Time.deltaTime * pulseSpeed;

            // Smooth sine wave pulse
            float t = (Mathf.Sin(time) + 1f) / 2f; // 0 to 1
            float scale = Mathf.Lerp(pulseMinScale, pulseMaxScale, t);

            buttonIcon.transform.localScale = iconOriginalScale * scale;

            yield return null;
        }
    }

    public bool IsScanning()
    {
        return scanningEnabled;
    }

    private void CheckVeryEasyModeAchievement(float totalArea)
    {
        if (totalArea > 0 && totalArea < 2.0f)
        {
            GPGSManager.Instance?.UnlockAchievement(GPGSIds.achievement_very_easy_mode);
        }
    }

    private void CheckExplorerAchievement(float totalArea)
    {
        if (createObjectScript == null) return;

        if (totalArea >= 50.0f)
        {
            GPGSManager.Instance?.UnlockAchievement(GPGSIds.achievement_explorer);
        }
    }
    private void SubmitAreaToLeaderboard(float areaInSquareMeters)
    {
#if UNITY_ANDROID
        if (Social.localUser.authenticated)
        {
            long scoreToSend = (long)(areaInSquareMeters * 100.0f);

            Social.ReportScore(scoreToSend, GPGSIds.leaderboard_largest_scanned_area, (bool success) =>
            {
                if (success)
                {
                    Debug.Log($"Wysłano wynik obszaru: {areaInSquareMeters} m2 (jako wartość {scoreToSend})");
                }
            });
        }
#endif
    }
}