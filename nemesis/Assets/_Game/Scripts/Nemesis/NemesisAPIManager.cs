using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;

/// <summary>
/// Dedicated HTTP client for all Nemesis backend communication.
/// Separates API concerns from CombatLogger's recording responsibility.
/// Attach to a persistent GameObject (e.g., GameManager).
/// </summary>
public class NemesisAPIManager : MonoBehaviour
{
    public static NemesisAPIManager Instance { get; private set; }

    /// <summary>
    /// Fired when the backend responds with adaptations and a taunt.
    /// Listeners: NemesisWeightReceiver, DeathScreenUI
    /// </summary>
    public static event Action<NemesisResponse> OnNemesisResponse;

    [Header("Configuration")]
    [SerializeField] private GameConfig config;

    private string BaseUrl
    {
        get
        {
            if (config != null) return config.backendBaseUrl;
            if (GameManager.Instance != null && GameManager.Instance.Config != null)
                return GameManager.Instance.Config.backendBaseUrl;
            return "http://localhost:8000";
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Sends a death analysis request to the backend.
    /// On success: fires OnNemesisResponse and caches the result.
    /// On failure: fires OnNemesisResponse with cached fallback or null.
    /// </summary>
    public void AnalyzeDeath(DeathAnalysisRequest request)
    {
        StartCoroutine(PostAnalyzeDeath(request));
    }

    /// <summary>
    /// Fetches the current nemesis state for a device from the backend.
    /// </summary>
    public void LoadNemesisState(string deviceId, Action<NemesisResponse> callback)
    {
        StartCoroutine(GetNemesisState(deviceId, callback));
    }

    /// <summary>
    /// Resets the nemesis state on the backend for a device.
    /// </summary>
    public void ResetNemesisState(string deviceId, Action<bool> callback = null)
    {
        StartCoroutine(DeleteNemesisState(deviceId, callback));
    }

    /// <summary>
    /// Health check — verifies backend is reachable.
    /// </summary>
    public void CheckHealth(Action<bool> callback)
    {
        StartCoroutine(HealthCheck(callback));
    }

    #region HTTP Coroutines

    private IEnumerator PostAnalyzeDeath(DeathAnalysisRequest report)
    {
        string url = BaseUrl + "/analyze-death";
        string jsonData = JsonUtility.ToJson(report);

        Debug.Log($"[NemesisAPIManager] POST {url}");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    NemesisResponse response = JsonUtility.FromJson<NemesisResponse>(request.downloadHandler.text);
                    Debug.Log($"[NemesisAPIManager] Success: {response.adaptations_applied?.Count ?? 0} adaptations, taunt: {response.taunt}");

                    // Cache for offline fallback
                    if (NemesisStateCache.Instance != null)
                    {
                        NemesisStateCache.Instance.CacheResponse(response);
                    }

                    OnNemesisResponse?.Invoke(response);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[NemesisAPIManager] Failed to parse response: {e.Message}");
                    FireFallback();
                }
            }
            else
            {
                Debug.LogWarning($"[NemesisAPIManager] Request failed: {request.error}");
                FireFallback();
            }
        }
    }

    private IEnumerator GetNemesisState(string deviceId, Action<NemesisResponse> callback)
    {
        string url = BaseUrl + $"/nemesis-state/{deviceId}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    NemesisResponse response = JsonUtility.FromJson<NemesisResponse>(request.downloadHandler.text);
                    callback?.Invoke(response);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[NemesisAPIManager] Parse error: {e.Message}");
                    callback?.Invoke(null);
                }
            }
            else
            {
                Debug.LogWarning($"[NemesisAPIManager] GET failed: {request.error}");
                callback?.Invoke(null);
            }
        }
    }

    private IEnumerator DeleteNemesisState(string deviceId, Action<bool> callback)
    {
        string url = BaseUrl + $"/nemesis-state/{deviceId}";

        using (UnityWebRequest request = UnityWebRequest.Delete(url))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();

            bool success = request.result == UnityWebRequest.Result.Success;
            Debug.Log($"[NemesisAPIManager] Reset state: {(success ? "OK" : request.error)}");
            callback?.Invoke(success);
        }
    }

    private IEnumerator HealthCheck(Action<bool> callback)
    {
        string url = BaseUrl + "/health";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 3;
            yield return request.SendWebRequest();

            bool healthy = request.result == UnityWebRequest.Result.Success;
            Debug.Log($"[NemesisAPIManager] Health: {(healthy ? "OK" : request.error)}");
            callback?.Invoke(healthy);
        }
    }

    #endregion

    /// <summary>
    /// Falls back to cached response or fires null.
    /// </summary>
    private void FireFallback()
    {
        NemesisResponse cached = NemesisStateCache.Instance != null
            ? NemesisStateCache.Instance.GetCachedResponse()
            : null;

        if (cached != null)
        {
            Debug.Log("[NemesisAPIManager] Using cached response as fallback.");
        }
        else
        {
            Debug.LogWarning("[NemesisAPIManager] No cached response available. Firing null.");
        }

        OnNemesisResponse?.Invoke(cached);
    }
}
