using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ScanToggle : MonoBehaviour
{
    public ARPlaneManager planeManager;

    private bool scanningEnabled = true;

    public TMP_Text buttonText;

    public CreateObjectInRandomPlace createObjectScript;

    public void ToggleScanning()
    {
        scanningEnabled = !scanningEnabled;

        planeManager.requestedDetectionMode =
            scanningEnabled ? PlaneDetectionMode.Horizontal : PlaneDetectionMode.None;

        buttonText.text = scanningEnabled ? "Scanning..." : "Stopped";

        if (!scanningEnabled && createObjectScript != null)
        {
            createObjectScript.CreateObject();
        }
        else if (scanningEnabled && createObjectScript != null)
        {
            createObjectScript.ClearPreviousObject();
        }
    }
}