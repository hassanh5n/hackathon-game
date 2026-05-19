using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Master cinematic controller. Provides shared utilities:
/// - Full-screen fade overlay (fade to black / fade from black)
/// - Letterbox bars for cinematic aspect ratio
/// - Skip input detection (tap/click to skip)
/// - Camera lerp helpers
/// Attach to a persistent GameObject or to a per-scene CinematicManager.
/// </summary>
public class CinematicDirector : MonoBehaviour
{
    public static CinematicDirector Instance { get; private set; }

    [Header("Auto-created UI")]
    [SerializeField] private Canvas cinematicCanvas;
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private Image topBar;
    [SerializeField] private Image bottomBar;
    [SerializeField] private Text subtitleText;

    /// <summary>True while any cinematic sequence is playing.</summary>
    public bool IsPlaying { get; private set; }

    /// <summary>Set to true when the player taps/clicks during a cinematic.</summary>
    public bool SkipRequested { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        BuildUI();
    }

    private void Update()
    {
        // Detect skip input (mouse click or any touch)
        if (IsPlaying && !SkipRequested)
        {
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            {
                SkipRequested = true;
            }
        }
    }

    // ─── PUBLIC API ─────────────────────────────────────────

    /// <summary>Call before starting a sequence.</summary>
    public void BeginSequence()
    {
        IsPlaying = true;
        SkipRequested = false;
        if (cinematicCanvas != null) cinematicCanvas.enabled = true;
    }

    /// <summary>Call when a sequence finishes.</summary>
    public void EndSequence()
    {
        IsPlaying = false;
        SkipRequested = false;
        HideSubtitle();
        SetLetterbox(0f);
        if (cinematicCanvas != null) cinematicCanvas.enabled = false;
    }

    /// <summary>Fade the screen to black over duration seconds.</summary>
    public IEnumerator FadeToBlack(float duration)
    {
        yield return FadeTo(1f, duration);
    }

    /// <summary>Fade the screen from black to clear over duration seconds.</summary>
    public IEnumerator FadeFromBlack(float duration)
    {
        yield return FadeTo(0f, duration);
    }

    /// <summary>Show/hide letterbox bars (0 = hidden, 1 = full cinematic bars).</summary>
    public void SetLetterbox(float amount)
    {
        float barHeight = Mathf.Lerp(0f, 0.12f, amount); // 12% of screen height
        if (topBar != null)
        {
            RectTransform rt = topBar.rectTransform;
            rt.anchorMin = new Vector2(0, 1f - barHeight);
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
        }
        if (bottomBar != null)
        {
            RectTransform rt = bottomBar.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1, barHeight);
            rt.sizeDelta = Vector2.zero;
        }
    }

    /// <summary>Show subtitle text at bottom of screen.</summary>
    public void ShowSubtitle(string text)
    {
        if (subtitleText != null)
        {
            subtitleText.text = text;
            subtitleText.enabled = true;
        }
    }

    /// <summary>Hide the subtitle.</summary>
    public void HideSubtitle()
    {
        if (subtitleText != null)
        {
            subtitleText.text = "";
            subtitleText.enabled = false;
        }
    }

    /// <summary>
    /// Smoothly moves a camera from its current position to a target over duration.
    /// Checks SkipRequested to allow early termination.
    /// </summary>
    public IEnumerator MoveCameraTo(Camera cam, Vector3 targetPos, Quaternion targetRot, float duration)
    {
        if (cam == null) yield break;

        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (SkipRequested) break;

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            cam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        cam.transform.position = targetPos;
        cam.transform.rotation = targetRot;
    }

    /// <summary>Wait for seconds in real-time, but break early if skip is requested.</summary>
    public IEnumerator WaitOrSkip(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (SkipRequested) yield break;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    // ─── PRIVATE ────────────────────────────────────────────

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeOverlay == null) yield break;

        fadeOverlay.enabled = true;
        Color c = fadeOverlay.color;
        float startAlpha = c.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            fadeOverlay.color = c;
            yield return null;
        }

        c.a = targetAlpha;
        fadeOverlay.color = c;

        if (targetAlpha <= 0.01f) fadeOverlay.enabled = false;
    }

    private void BuildUI()
    {
        if (cinematicCanvas != null) return;

        // Canvas
        GameObject canvasObj = new GameObject("CinematicCanvas");
        canvasObj.transform.SetParent(transform);
        cinematicCanvas = canvasObj.AddComponent<Canvas>();
        cinematicCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cinematicCanvas.sortingOrder = 900;
        canvasObj.AddComponent<CanvasScaler>();

        // Fade overlay
        fadeOverlay = CreateFullscreenImage(canvasObj.transform, "FadeOverlay", new Color(0, 0, 0, 0));
        fadeOverlay.enabled = false;

        // Letterbox bars
        topBar = CreateFullscreenImage(canvasObj.transform, "TopBar", Color.black);
        bottomBar = CreateFullscreenImage(canvasObj.transform, "BottomBar", Color.black);
        SetLetterbox(0f);

        // Subtitle
        GameObject textObj = new GameObject("Subtitle");
        textObj.transform.SetParent(canvasObj.transform, false);
        subtitleText = textObj.AddComponent<Text>();
        subtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subtitleText.fontSize = 28;
        subtitleText.color = new Color(0.95f, 0.9f, 0.7f);
        subtitleText.alignment = TextAnchor.MiddleCenter;
        subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        subtitleText.enabled = false;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.05f);
        textRect.anchorMax = new Vector2(0.9f, 0.15f);
        textRect.sizeDelta = Vector2.zero;

        cinematicCanvas.enabled = false;
    }

    private Image CreateFullscreenImage(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        return img;
    }
}
