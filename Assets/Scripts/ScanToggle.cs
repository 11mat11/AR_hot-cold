using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ScanToggle : MonoBehaviour
{
    public ARPlaneManager planeManager;

    private bool scanningEnabled = true;

    [Header("UI References")]
    public TMP_Text buttonText;
    public Image buttonIcon;

    [Header("Icon Sprites")]
    public Sprite scanningIcon;
    public Sprite stoppedIcon;

    [Header("Button Text")]
    public string scanningText = "Scanning";
    public string stoppedText = "Stopped";

    public CreateObjectInRandomPlace createObjectScript;

    void Start()
    {
        UpdateButtonVisuals();
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
        if (buttonText != null)
        {
            buttonText.text = scanningEnabled ? scanningText : stoppedText;
        }

        if (buttonIcon != null)
        {
            buttonIcon.sprite = scanningEnabled ? scanningIcon : stoppedIcon;
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
        if (Social.localUser.authenticated)
        {
            long scoreToSend = (long)(areaInSquareMeters * 100.0f);

            Social.ReportScore(scoreToSend, GPGSIds.leaderboard_largest_scanned_area, (bool success) =>
            {
                if (success)
                {
                    Debug.Log($"Wys³ano wynik obszaru: {areaInSquareMeters} m2 (jako wartoœæ {scoreToSend})");
                }
            });
        }
    }
}