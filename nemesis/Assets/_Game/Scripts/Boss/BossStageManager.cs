using System.Collections;
using UnityEngine;

/// <summary>
/// Handles boss stage transition effects: camera shake, slow-motion,
/// material swaps, sapling spawning (Stage 2), and visual feedback.
/// Attach to the Boss (Humbaba) GameObject alongside BossController and BossStats.
/// </summary>
[RequireComponent(typeof(BossStats))]
public class BossStageManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossStats bossStats;
    [SerializeField] private Renderer bossRenderer;

    [Header("Stage Materials")]
    [SerializeField] private Material stage1Material;
    [SerializeField] private Material stage2Material;
    [SerializeField] private Material stage3Material;

    [Header("Stage 2 — Sapling Spawning")]
    [SerializeField] private GameObject saplingPrefab;
    [SerializeField] private int saplingsToSpawn = 2;
    [SerializeField] private float saplingSpawnRadius = 5f;

    [Header("Transition Settings")]
    [SerializeField] private float transitionSlowMoDuration = 1.5f;
    [SerializeField] private float transitionSlowMoScale = 0.2f;
    [SerializeField] private float cameraShakeIntensity = 0.3f;
    [SerializeField] private float cameraShakeDuration = 0.5f;

    [Header("Stage 3 — Visual")]
    [SerializeField] private Light bossGlowLight;
    [SerializeField] private Color stage3GlowColor = new Color(1f, 0.85f, 0.3f, 1f);

    private Transform player;
    private PlayerStats playerStats;
    private int lastPhase = 1;

    private void Awake()
    {
        if (bossStats == null) bossStats = GetComponent<BossStats>();
        if (bossRenderer == null) bossRenderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerStats = playerObj.GetComponent<PlayerStats>();
        }

        // Subscribe to phase changes
        if (bossStats != null)
        {
            bossStats.OnPhaseChanged.AddListener(HandlePhaseTransition);
        }

        // Apply initial material
        ApplyMaterial(stage1Material);
    }

    private void OnDestroy()
    {
        if (bossStats != null)
        {
            bossStats.OnPhaseChanged.RemoveListener(HandlePhaseTransition);
        }
    }

    private void HandlePhaseTransition(int newPhase)
    {
        if (newPhase == lastPhase) return;

        Debug.Log($"[BossStageManager] Transitioning to Phase {newPhase}");

        switch (newPhase)
        {
            case 2:
                StartCoroutine(TransitionToStage2());
                break;
            case 3:
                StartCoroutine(TransitionToStage3());
                break;
        }

        lastPhase = newPhase;
    }

    /// <summary>
    /// Stage 1 → Stage 2: "The Merged"
    /// GDD: Humbaba plunges fist into ground. Roots erupt. Left arm becomes tree.
    /// Spawns 2 sapling mini-enemies.
    /// </summary>
    private IEnumerator TransitionToStage2()
    {
        Debug.Log("[BossStageManager] === STAGE 2: THE MERGED ===");

        // 1. Slow motion
        Time.timeScale = transitionSlowMoScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // 2. Camera shake
        StartCoroutine(CameraShake(cameraShakeDuration, cameraShakeIntensity));

        // 3. Wait for dramatic pause (real-time)
        yield return new WaitForSecondsRealtime(transitionSlowMoDuration);

        // 4. Apply Stage 2 material
        ApplyMaterial(stage2Material);

        // 5. Scale boss slightly to show growth
        transform.localScale *= 1.1f;

        // 6. Spawn saplings
        SpawnSaplings();

        // 7. Play audio
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBossTransition();
        }

        // 8. Restore time
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    /// <summary>
    /// Stage 2 → Stage 3: "The Divine"
    /// GDD: Arena shakes. Body becomes translucent. Cuneiform glows. Floats slightly.
    /// </summary>
    private IEnumerator TransitionToStage3()
    {
        Debug.Log("[BossStageManager] === STAGE 3: THE DIVINE ===");

        // 1. Slow motion
        Time.timeScale = transitionSlowMoScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // 2. Intense camera shake
        StartCoroutine(CameraShake(cameraShakeDuration * 1.5f, cameraShakeIntensity * 2f));

        // 3. Dramatic pause
        yield return new WaitForSecondsRealtime(transitionSlowMoDuration * 1.2f);

        // 4. Apply Stage 3 material (translucent/glowing)
        ApplyMaterial(stage3Material);

        // 5. Float the boss slightly off ground
        Vector3 pos = transform.position;
        pos.y += 0.5f;
        transform.position = pos;

        // 6. Add glow light if assigned
        if (bossGlowLight != null)
        {
            bossGlowLight.enabled = true;
            bossGlowLight.color = stage3GlowColor;
            bossGlowLight.intensity = 3f;
        }

        // 7. Play audio
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBossTransition();
        }

        // 8. Restore time
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        Debug.Log("[BossStageManager] Phase 3 transition complete.");
    }

    private void SpawnSaplings()
    {
        for (int i = 0; i < saplingsToSpawn; i++)
        {
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * saplingSpawnRadius;
            spawnPos.y = 0.5f; // Ground level

            GameObject saplingObj;

            if (saplingPrefab != null)
            {
                saplingObj = Instantiate(saplingPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                // Fallback: create a capsule placeholder
                saplingObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                saplingObj.name = $"Sapling_{i + 1}";
                saplingObj.transform.position = spawnPos;
                saplingObj.transform.localScale = new Vector3(0.4f, 0.6f, 0.4f);

                // Color it green
                Renderer rend = saplingObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = new Color(0.2f, 0.6f, 0.15f);
                }

                // Add collider as trigger for player attacks
                Collider col = saplingObj.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;

                // Add rigidbody so trigger events work
                Rigidbody rb = saplingObj.AddComponent<Rigidbody>();
                rb.isKinematic = true;
            }

            // Add SaplingEnemy script
            SaplingEnemy sapling = saplingObj.GetComponent<SaplingEnemy>();
            if (sapling == null) sapling = saplingObj.AddComponent<SaplingEnemy>();
            sapling.Initialize(player, playerStats, bossStats);

            Debug.Log($"[BossStageManager] Spawned sapling at {spawnPos}");
        }
    }

    private void ApplyMaterial(Material mat)
    {
        if (bossRenderer == null || mat == null) return;
        bossRenderer.material = mat;
    }

    private IEnumerator CameraShake(float duration, float magnitude)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Vector3 originalPos = cam.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            cam.transform.localPosition = originalPos + new Vector3(x, y, 0f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        cam.transform.localPosition = originalPos;
    }
}
