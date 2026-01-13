using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TMPro;

/// <summary>
/// Manages the AR safety warning dialog that must be shown before AR functionality starts.
/// Required for Google Play Store compliance with Families Policy.
/// </summary>
[DefaultExecutionOrder(-100)] // Ensure this runs before other scripts
public class SafetyWarningManager : MonoBehaviour
{
    public static SafetyWarningManager Instance { get; private set; }

    /// <summary>
    /// Automatically creates and initializes the SafetyWarningManager when the game starts.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        // Create a new GameObject with SafetyWarningManager if it doesn't exist
        if (Instance == null)
        {
            GameObject managerObj = new GameObject("SafetyWarningManager");
            managerObj.AddComponent<SafetyWarningManager>();
            DontDestroyOnLoad(managerObj);
        }
    }

    /// <summary>
    /// Static flag indicating whether the safety warning has been accepted.
    /// Other scripts should check this before starting AR functionality.
    /// </summary>
    public static bool WarningAccepted { get; private set; } = false;

    [Header("UI References")]
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private Button acceptButton;

    [Header("AR References")]
    [SerializeField] private ARSession arSession;
    [SerializeField] private ARPlaneManager planeManager;

    [Header("Optional - Auto Create UI")]
    [SerializeField] private bool autoCreateUI = true;

    private const string PREFS_KEY_WARNING_ACCEPTED = "AR_SafetyWarningAccepted";
    private Canvas warningCanvas;

    void Awake()
    {
        // Singleton pattern - destroy duplicates
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        WarningAccepted = false;
    }

    void Start()
    {
        // Auto-find ARPlaneManager if not assigned (done in Start to ensure scene is loaded)
        if (planeManager == null)
        {
            planeManager = FindFirstObjectByType<ARPlaneManager>();
        }

        // Auto-find ARSession if not assigned
        if (arSession == null)
        {
            arSession = FindFirstObjectByType<ARSession>();
        }

        // Check if warning was previously accepted
        if (PlayerPrefs.GetInt(PREFS_KEY_WARNING_ACCEPTED, 0) == 1)
        {
            // Warning was already accepted - skip showing it
            WarningAccepted = true;
            Debug.Log("Safety warning previously accepted - skipping");
            return;
        }

        // If no warning panel assigned, create one automatically
        if (warningPanel == null && autoCreateUI)
        {
            CreateWarningUI();
        }

        // Disable AR plane detection until warning is accepted
        if (planeManager != null)
        {
            planeManager.enabled = false;
        }

        // Show the warning panel
        if (warningPanel != null)
        {
            warningPanel.SetActive(true);
        }

        // Hook up button if assigned
        if (acceptButton != null)
        {
            acceptButton.onClick.AddListener(OnWarningAccepted);
        }
    }

    void OnDestroy()
    {
        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveListener(OnWarningAccepted);
        }
    }

    /// <summary>
    /// Called when the user accepts the safety warning.
    /// </summary>
    public void OnWarningAccepted()
    {
        WarningAccepted = true;

        // Save acceptance to PlayerPrefs so it won't show again
        PlayerPrefs.SetInt(PREFS_KEY_WARNING_ACCEPTED, 1);
        PlayerPrefs.Save();

        // Hide the warning panel
        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
        }

        // Enable AR plane detection
        if (planeManager != null)
        {
            planeManager.enabled = true;
        }

        Debug.Log("Safety warning accepted - AR functionality enabled");
    }

    /// <summary>
    /// Resets the warning acceptance (for testing or if user wants to see warning again).
    /// Call this to force the warning to show on next app launch.
    /// </summary>
    public static void ResetWarningAcceptance()
    {
        PlayerPrefs.DeleteKey(PREFS_KEY_WARNING_ACCEPTED);
        PlayerPrefs.Save();
        Debug.Log("Safety warning acceptance reset - will show on next launch");
    }

    /// <summary>
    /// Creates the warning UI programmatically if not assigned in inspector.
    /// </summary>
    private void CreateWarningUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("SafetyWarningCanvas");
        warningCanvas = canvasObj.AddComponent<Canvas>();
        warningCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        warningCanvas.sortingOrder = 100; // Ensure it's on top

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Create dark semi-transparent background overlay
        // Note: True blur not possible with AR camera without complex post-processing
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.75f);

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Create dialog panel - compact, centered
        GameObject panelObj = new GameObject("DialogPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelImage = panelObj.AddComponent<Image>();

        // Semi-transparent dark background like "Scan Surfaces"
        panelImage.color = new Color(0.22f, 0.22f, 0.22f, 0.85f);

        // Create rounded corner sprite
        panelImage.sprite = CreateRoundedRectSprite(128, 128, 40);
        panelImage.type = Image.Type.Sliced;
        panelImage.pixelsPerUnitMultiplier = 1f;

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(920, 620);
        panelRect.anchoredPosition = Vector2.zero;

        // Create title text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(panelObj.transform, false);
        TMP_Text titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Safety Warning";
        titleText.fontSize = 52;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.enableWordWrapping = true;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(-60, 70);
        titleRect.anchoredPosition = new Vector2(0, -35);

        // Create message text
        GameObject messageObj = new GameObject("MessageText");
        messageObj.transform.SetParent(panelObj.transform, false);
        TMP_Text messageText = messageObj.AddComponent<TextMeshProUGUI>();
        messageText.text = GetSafetyMessage();
        messageText.fontSize = 34;
        messageText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        messageText.alignment = TextAlignmentOptions.TopLeft;
        messageText.lineSpacing = -5;
        messageText.paragraphSpacing = 8;
        messageText.enableWordWrapping = true;
        messageText.overflowMode = TextOverflowModes.Truncate;
        RectTransform messageRect = messageObj.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0, 0);
        messageRect.anchorMax = new Vector2(1, 1);
        messageRect.pivot = new Vector2(0.5f, 0.5f);
        messageRect.offsetMin = new Vector2(45, 95);
        messageRect.offsetMax = new Vector2(-45, -100);

        // Create accept button
        GameObject buttonObj = new GameObject("AcceptButton");
        buttonObj.transform.SetParent(panelObj.transform, false);
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.25f, 0.6f, 0.25f, 1f);

        // Rounded corners for button
        buttonImage.sprite = CreateRoundedRectSprite(64, 64, 20);
        buttonImage.type = Image.Type.Sliced;

        acceptButton = buttonObj.AddComponent<Button>();
        acceptButton.targetGraphic = buttonImage;

        ColorBlock colors = acceptButton.colors;
        colors.normalColor = new Color(0.25f, 0.6f, 0.25f, 1f);
        colors.highlightedColor = new Color(0.35f, 0.7f, 0.35f, 1f);
        colors.pressedColor = new Color(0.18f, 0.5f, 0.18f, 1f);
        colors.selectedColor = new Color(0.25f, 0.6f, 0.25f, 1f);
        acceptButton.colors = colors;

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0);
        buttonRect.anchorMax = new Vector2(0.5f, 0);
        buttonRect.pivot = new Vector2(0.5f, 0);
        buttonRect.sizeDelta = new Vector2(340, 75);
        buttonRect.anchoredPosition = new Vector2(0, 25);

        // Create button text
        GameObject buttonTextObj = new GameObject("ButtonText");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);
        TMP_Text buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "I Understand";
        buttonText.fontSize = 34;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        // Hook up button click
        acceptButton.onClick.AddListener(OnWarningAccepted);

        // Set the warning panel reference
        warningPanel = canvasObj;

        Debug.Log("Safety warning UI created programmatically");
    }

    private string GetSafetyMessage()
    {
        return @"<b>Parental Supervision</b>
Children should use this app under adult supervision.

<b>Be Aware of Your Surroundings</b>
Always watch where you are going while playing.

<b>Safety Guidelines:</b>
  •  Move carefully and watch for obstacles
  •  Play in a safe, open area
  •  Do not use near stairs, traffic, or water
  •  Take breaks to rest your eyes
  •  Stop if you feel dizzy or unwell";
    }

    /// <summary>
    /// Creates a rounded rectangle sprite at runtime for UI elements.
    /// </summary>
    private Sprite CreateRoundedRectSprite(int width, int height, int radius)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = GetRoundedRectPixel(x, y, width, height, radius);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        // Create sprite with 9-slice borders for proper scaling
        Vector4 border = new Vector4(radius, radius, radius, radius);
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    private Color GetRoundedRectPixel(int x, int y, int width, int height, int radius)
    {
        // Check if pixel is in a corner region
        bool inCorner = false;
        int cornerX = 0, cornerY = 0;

        // Bottom-left corner
        if (x < radius && y < radius)
        {
            inCorner = true;
            cornerX = radius;
            cornerY = radius;
        }
        // Bottom-right corner
        else if (x >= width - radius && y < radius)
        {
            inCorner = true;
            cornerX = width - radius - 1;
            cornerY = radius;
        }
        // Top-left corner
        else if (x < radius && y >= height - radius)
        {
            inCorner = true;
            cornerX = radius;
            cornerY = height - radius - 1;
        }
        // Top-right corner
        else if (x >= width - radius && y >= height - radius)
        {
            inCorner = true;
            cornerX = width - radius - 1;
            cornerY = height - radius - 1;
        }

        if (inCorner)
        {
            float dist = Mathf.Sqrt((x - cornerX) * (x - cornerX) + (y - cornerY) * (y - cornerY));
            if (dist > radius)
                return Color.clear;
            else if (dist > radius - 1.5f)
            {
                // Anti-aliasing at edges
                float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                return new Color(1f, 1f, 1f, alpha);
            }
        }

        return Color.white;
    }
}
