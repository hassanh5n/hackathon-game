using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Async scene loader with a loading overlay (progress bar + tip text).
/// Persists across scenes via DontDestroyOnLoad.
/// Usage: SceneLoader.Instance.LoadScene("BossFight");
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Loading UI (auto-created if null)")]
    [SerializeField] private Canvas loadingCanvas;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Text loadingText;

    private bool isLoading;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateLoadingUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Loads a scene asynchronously with a loading screen overlay.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (isLoading) return;
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    /// <summary>
    /// Loads a scene with a minimum display time for the loading screen (good for short loads).
    /// </summary>
    public void LoadSceneWithMinDelay(string sceneName, float minDelaySeconds = 1f)
    {
        if (isLoading) return;
        StartCoroutine(LoadSceneAsync(sceneName, minDelaySeconds));
    }

    private IEnumerator LoadSceneAsync(string sceneName, float minDelay = 0.5f)
    {
        isLoading = true;

        // Show loading UI
        if (loadingCanvas != null) loadingCanvas.enabled = true;
        if (progressBar != null) progressBar.value = 0f;
        if (loadingText != null) loadingText.text = "Entering the Cedar Forest...";

        // Restore time scale (in case we're coming from death screen)
        Time.timeScale = 1f;

        float elapsed = 0f;
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);

        if (asyncOp == null)
        {
            Debug.LogError($"[SceneLoader] Failed to start loading scene: {sceneName}");
            if (loadingCanvas != null) loadingCanvas.enabled = false;
            isLoading = false;
            yield break;
        }

        asyncOp.allowSceneActivation = false;

        // Update progress bar until load is ready
        while (!asyncOp.isDone)
        {
            elapsed += Time.unscaledDeltaTime;

            // AsyncOperation.progress goes from 0 to 0.9, then waits for activation
            float progress = Mathf.Clamp01(asyncOp.progress / 0.9f);
            if (progressBar != null) progressBar.value = progress;

            // Activate when loaded AND min delay met
            if (asyncOp.progress >= 0.9f && elapsed >= minDelay)
            {
                if (progressBar != null) progressBar.value = 1f;
                yield return new WaitForSecondsRealtime(0.2f); // Brief flash at 100%
                asyncOp.allowSceneActivation = true;
            }

            yield return null;
        }

        // Hide loading UI
        if (loadingCanvas != null) loadingCanvas.enabled = false;
        isLoading = false;
    }

    /// <summary>
    /// Creates a minimal loading screen UI programmatically.
    /// Only runs if no Canvas was assigned in the Inspector.
    /// </summary>
    private void CreateLoadingUI()
    {
        if (loadingCanvas != null) return;

        // Canvas
        GameObject canvasObj = new GameObject("LoadingCanvas");
        canvasObj.transform.SetParent(transform);
        loadingCanvas = canvasObj.AddComponent<Canvas>();
        loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        loadingCanvas.sortingOrder = 999; // Always on top
        canvasObj.AddComponent<CanvasScaler>();

        // Black background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.03f, 0.02f, 1f); // Dark brown
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Progress bar background
        GameObject barBgObj = new GameObject("ProgressBarBG");
        barBgObj.transform.SetParent(canvasObj.transform, false);
        Image barBgImage = barBgObj.AddComponent<Image>();
        barBgImage.color = new Color(0.15f, 0.12f, 0.08f, 1f);
        RectTransform barBgRect = barBgObj.GetComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0.2f, 0.45f);
        barBgRect.anchorMax = new Vector2(0.8f, 0.48f);
        barBgRect.sizeDelta = Vector2.zero;

        // Progress bar slider
        GameObject sliderObj = new GameObject("ProgressBar");
        sliderObj.transform.SetParent(canvasObj.transform, false);
        progressBar = sliderObj.AddComponent<Slider>();
        progressBar.minValue = 0f;
        progressBar.maxValue = 1f;
        progressBar.value = 0f;
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.2f, 0.45f);
        sliderRect.anchorMax = new Vector2(0.8f, 0.48f);
        sliderRect.sizeDelta = Vector2.zero;

        // Fill area
        GameObject fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.85f, 0.65f, 0.15f, 1f); // Amber gold
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        progressBar.fillRect = fillRect;

        // Loading text
        GameObject textObj = new GameObject("LoadingText");
        textObj.transform.SetParent(canvasObj.transform, false);
        loadingText = textObj.AddComponent<Text>();
        loadingText.text = "Entering the Cedar Forest...";
        loadingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        loadingText.fontSize = 24;
        loadingText.color = new Color(0.9f, 0.8f, 0.5f, 1f); // Gold
        loadingText.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.2f, 0.5f);
        textRect.anchorMax = new Vector2(0.8f, 0.55f);
        textRect.sizeDelta = Vector2.zero;

        // Start hidden
        loadingCanvas.enabled = false;
    }
}
