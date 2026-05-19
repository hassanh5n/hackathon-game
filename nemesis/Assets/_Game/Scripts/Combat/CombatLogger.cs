using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

// ── JSON models matching Python backend exactly ──

[Serializable]
public class ThrowablesUsed
{
    public int flask;
    public int tablet;
    public int spear;
}

[Serializable]
public class CombatEvent
{
    public string @event;      // "player_dodge", "player_attack", "boss_attack"
    public string direction;   // "right", "left", "forward", "back"
    public string attack_type; // "light", "heavy"
    public int timestamp_ms;
}

[Serializable]
public class CombatStats
{
    public int dodge_right_count;
    public int dodge_left_count;
    public int dodge_forward_count;
    public int dodge_back_count;
    public int light_attack_count;
    public int heavy_attack_count;
    public float light_attack_ratio;
    public float heavy_attack_ratio;
    public ThrowablesUsed throwables_used = new ThrowablesUsed();
    public float retreat_hp_threshold;
    public float avg_distance_from_boss;
    public int total_fight_duration_seconds;
}

[Serializable]
public class CombatLog
{
    public List<CombatEvent> events = new List<CombatEvent>();
    public CombatStats stats = new CombatStats();
}

[Serializable]
public class DeathAnalysisRequest
{
    public string device_id;
    public string attempt_id;
    public string death_cause;
    public CombatLog combat_log = new CombatLog();
}

// Response matching Python AnalysisResult
[Serializable]
public class NemesisResponse
{
    public List<string> adaptations_applied = new List<string>();
    public string taunt;
    public int total_deaths;
    public string reasoning_trace;
    public bool mercy_applied;
}

// ── CombatLogger ──

public class CombatLogger : MonoBehaviour
{
    public static CombatLogger Instance { get; private set; }

    /// <summary>
    /// Fired when the backend responds with adaptations and a taunt.
    /// Listeners: BossController/NemesisWeightReceiver, DeathScreenUI
    /// </summary>
    public static event Action<NemesisResponse> OnNemesisResponse;

    [Header("Configuration")]
    [SerializeField] private GameConfig config;

    // Scene references — re-acquired on each scene load
    private PlayerCombat playerCombat;
    private PlayerStats playerStats;
    private BossStats bossStats;
    private PlayerController playerController;

    private List<CombatEvent> currentRunEvents = new List<CombatEvent>();
    private float runStartTime;
    private string lastBossAttackId = "unknown";

    private BossAdaptation _bossAdaptation;
    private NemesisAPIManager _nemesisAPIManager;
    private string _lastBossAttackName = "UNKNOWN";

    // Stat counters
    private int dodgeRight, dodgeLeft, dodgeForward, dodgeBack;
    private int lightAttacks, heavyAttacks;

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
            return;
        }

        _bossAdaptation = FindObjectOfType<BossAdaptation>();
        if (_bossAdaptation == null)
        {
            Debug.LogWarning("[CombatLogger] BossAdaptation not found in scene!");
        }

        _nemesisAPIManager = FindObjectOfType<NemesisAPIManager>();
        if (_nemesisAPIManager == null)
        {
            Debug.LogWarning("[CombatLogger] NemesisAPIManager not found in scene!");
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        FindSceneReferences();
        SubscribeEvents();
        runStartTime = Time.time;
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-acquire scene references after a scene reload (e.g. death -> retry)
        UnsubscribeEvents();
        FindSceneReferences();
        SubscribeEvents();
        ResetRunData();
    }

    private void FindSceneReferences()
    {
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerCombat = playerObj.GetComponent<PlayerCombat>();
            playerStats = playerObj.GetComponent<PlayerStats>();
            playerController = playerObj.GetComponent<PlayerController>();
        }

        // Find boss
        GameObject bossObj = GameObject.FindGameObjectWithTag("Boss");
        if (bossObj != null)
        {
            bossStats = bossObj.GetComponent<BossStats>();
        }
    }

    #region Event Subscriptions

    private void SubscribeEvents()
    {
        if (playerCombat != null)
        {
            playerCombat.OnActionRecorded += RecordPlayerAction;
            playerCombat.OnAttackLanded.AddListener(HandleAttackLanded);
            playerCombat.OnParrySuccess.AddListener(HandleParrySuccess);
        }

        if (playerStats != null)
        {
            playerStats.OnPlayerDeath.AddListener(HandlePlayerDeath);
        }
    }

    private void UnsubscribeEvents()
    {
        if (playerCombat != null)
        {
            playerCombat.OnActionRecorded -= RecordPlayerAction;
            playerCombat.OnAttackLanded.RemoveListener(HandleAttackLanded);
            playerCombat.OnParrySuccess.RemoveListener(HandleParrySuccess);
        }

        if (playerStats != null)
        {
            playerStats.OnPlayerDeath.RemoveListener(HandlePlayerDeath);
        }
    }

    #endregion

    #region Recording

    public void SetCurrentBossAttack(string attackName)
    {
        _lastBossAttackName = attackName;
    }

    /// <summary>
    /// Called by PlayerCombat with (actionType, detail).
    /// actionType: "player_dodge", "player_attack", "player_parry", etc.
    /// detail: dodge direction or attack type
    /// </summary>
    public void RecordPlayerAction(string actionType, string detail)
    {
        CombatEvent evt = new CombatEvent
        {
            @event = actionType,
            direction = actionType == "player_dodge" ? detail : "",
            attack_type = actionType == "player_attack" ? detail : "",
            timestamp_ms = Mathf.RoundToInt((Time.time - runStartTime) * 1000f)
        };
        currentRunEvents.Add(evt);

        // Update stat counters
        if (actionType == "player_dodge")
        {
            switch (detail)
            {
                case "right": dodgeRight++; break;
                case "left": dodgeLeft++; break;
                case "forward": dodgeForward++; break;
                case "back": dodgeBack++; break;
            }
            
            _bossAdaptation?.RecordPlayerDodge(lastBossAttackId);
        }
        else if (actionType == "player_attack")
        {
            if (detail == "light") lightAttacks++;
            else if (detail == "heavy") heavyAttacks++;
        }
    }

    /// <summary>
    /// Called by BossController when boss performs an attack.
    /// Records it as a combat event and caches as potential death cause.
    /// </summary>
    public void RecordBossAttack(string attackId)
    {
        lastBossAttackId = attackId;
        CombatEvent evt = new CombatEvent
        {
            @event = "boss_attack",
            attack_type = attackId,
            timestamp_ms = Mathf.RoundToInt((Time.time - runStartTime) * 1000f)
        };
        currentRunEvents.Add(evt);
    }

    private void HandleAttackLanded(float damage)
    {
        RecordPlayerAction("player_attack_landed", "");
    }

    private void HandleParrySuccess()
    {
        RecordPlayerAction("player_parry_success", "");
    }

    #endregion

    #region Reporting

    private void HandlePlayerDeath()
    {
        _bossAdaptation?.RecordKillingBlow(lastBossAttackId);

        float duration = Time.time - runStartTime;
        int totalAttacks = lightAttacks + heavyAttacks;

        CombatStats stats = new CombatStats
        {
            dodge_right_count = dodgeRight,
            dodge_left_count = dodgeLeft,
            dodge_forward_count = dodgeForward,
            dodge_back_count = dodgeBack,
            light_attack_count = lightAttacks,
            heavy_attack_count = heavyAttacks,
            light_attack_ratio = totalAttacks > 0 ? (float)lightAttacks / totalAttacks : 0f,
            heavy_attack_ratio = totalAttacks > 0 ? (float)heavyAttacks / totalAttacks : 0f,
            throwables_used = new ThrowablesUsed(),
            retreat_hp_threshold = 0f, // TODO: track when player retreats
            avg_distance_from_boss = 0f, // TODO: periodic distance sampling
            total_fight_duration_seconds = Mathf.RoundToInt(duration)
        };

        CombatLog combatLog = new CombatLog
        {
            events = new List<CombatEvent>(currentRunEvents),
            stats = stats
        };

        string sessionId = GameManager.Instance != null ? GameManager.Instance.SessionId : Guid.NewGuid().ToString();
        int deathNum = GameManager.Instance != null ? GameManager.Instance.DeathCount : 1;

        DeathAnalysisRequest request = new DeathAnalysisRequest
        {
            device_id = sessionId,
            attempt_id = $"attempt-{deathNum}",
            death_cause = lastBossAttackId,
            combat_log = combatLog
        };

        _nemesisAPIManager?.AnalyzeDeath(request);

        string backendUrl = "http://localhost:8000";
        if (config != null)
        {
            backendUrl = config.backendBaseUrl;
        }
        else if (GameManager.Instance != null && GameManager.Instance.Config != null)
        {
            backendUrl = GameManager.Instance.Config.backendBaseUrl;
        }

        StartCoroutine(PostDeathReport(request, backendUrl + "/analyze-death"));
    }

    private IEnumerator PostDeathReport(DeathAnalysisRequest report, string url)
    {
        string jsonData = JsonUtility.ToJson(report);
        Debug.Log($"[CombatLogger] Sending death report to {url}:\n{jsonData}");

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
                    Debug.Log($"[CombatLogger] Nemesis response: {response.adaptations_applied?.Count ?? 0} adaptations, taunt: {response.taunt}");
                    OnNemesisResponse?.Invoke(response);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CombatLogger] Failed to parse backend response: {e.Message}");
                    OnNemesisResponse?.Invoke(null);
                }
            }
            else
            {
                Debug.LogWarning($"[CombatLogger] Backend request failed: {request.error}");
                OnNemesisResponse?.Invoke(null);
            }
        }
    }

    private void ResetRunData()
    {
        currentRunEvents.Clear();
        runStartTime = Time.time;
        lastBossAttackId = "unknown";
        dodgeRight = dodgeLeft = dodgeForward = dodgeBack = 0;
        lightAttacks = heavyAttacks = 0;
    }

    #endregion
}
