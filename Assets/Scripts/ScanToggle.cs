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
            createObjectScript.CreateObject();
        }
        else if (scanningEnabled && createObjectScript != null)
        {
            createObjectScript.ClearPreviousObject();
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
}