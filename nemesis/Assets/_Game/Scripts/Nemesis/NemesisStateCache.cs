using UnityEngine;
using System.IO;

/// <summary>
/// Caches the last-known NemesisResponse locally so the game can
/// still apply boss adaptations even if the backend is unreachable.
/// Saves to Application.persistentDataPath/nemesis_state_local.json.
/// </summary>
public class NemesisStateCache : MonoBehaviour
{
    public static NemesisStateCache Instance { get; private set; }

    private string CachePath => Path.Combine(Application.persistentDataPath, "nemesis_state_local.json");

    /// <summary>
    /// The last successfully received response from the backend.
    /// </summary>
    public NemesisResponse CachedResponse { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadFromDisk();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Saves a successful backend response to memory and disk.
    /// Call this from CombatLogger after a successful /analyze-death response.
    /// </summary>
    public void CacheResponse(NemesisResponse response)
    {
        if (response == null) return;

        CachedResponse = response;

        try
        {
            string json = JsonUtility.ToJson(response, true);
            File.WriteAllText(CachePath, json);
            Debug.Log($"[NemesisStateCache] Saved to {CachePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[NemesisStateCache] Failed to save cache: {e.Message}");
        }
    }

    /// <summary>
    /// Returns the cached response if available, or null.
    /// Use this as a fallback when the backend is unreachable.
    /// </summary>
    public NemesisResponse GetCachedResponse()
    {
        return CachedResponse;
    }

    /// <summary>
    /// Clears the cache (e.g., when starting a fresh game).
    /// </summary>
    public void ClearCache()
    {
        CachedResponse = null;

        try
        {
            if (File.Exists(CachePath))
            {
                File.Delete(CachePath);
                Debug.Log("[NemesisStateCache] Cache cleared.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[NemesisStateCache] Failed to clear cache: {e.Message}");
        }
    }

    private void LoadFromDisk()
    {
        try
        {
            if (File.Exists(CachePath))
            {
                string json = File.ReadAllText(CachePath);
                CachedResponse = JsonUtility.FromJson<NemesisResponse>(json);
                Debug.Log($"[NemesisStateCache] Loaded cached state: {CachedResponse?.adaptations_applied?.Count ?? 0} adaptations");
            }
            else
            {
                Debug.Log("[NemesisStateCache] No cached state found on disk.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[NemesisStateCache] Failed to load cache: {e.Message}");
            CachedResponse = null;
        }
    }
}
