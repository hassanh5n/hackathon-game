using System.Collections;
using UnityEngine;

/// <summary>
/// Boss intro cinematic — GDD §5.2.
/// 
/// FIRST encounter (~8 sec):
///   Camera circles Humbaba, title appears: "HUMBABA — GUARDIAN OF THE CEDAR"
/// 
/// RETRY (after death, ~4 sec):
///   Humbaba is waiting. Taunt subtitle plays from Nemesis system.
///   Then fight starts.
/// 
/// Attach to a GameObject in the BossFight scene.
/// </summary>
public class BossIntroSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform boss;

    [Header("Settings")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float orbitRadius = 8f;
    [SerializeField] private float orbitHeight = 4f;
    [SerializeField] private float orbitSpeed = 60f; // degrees per second

    [Header("Camera")]
    [SerializeField] private Vector3 gameplayCamPos = new Vector3(0f, 5f, -12f);
    [SerializeField] private Quaternion gameplayCamRot = Quaternion.Euler(15f, 0f, 0f);

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (boss == null)
        {
            GameObject b = GameObject.FindGameObjectWithTag("Boss");
            if (b != null) boss = b.transform;
        }

        if (playOnStart)
        {
            StartCoroutine(PlayBossIntro());
        }
    }

    public void Play()
    {
        StartCoroutine(PlayBossIntro());
    }

    private IEnumerator PlayBossIntro()
    {
        CinematicDirector dir = CinematicDirector.Instance;
        if (dir == null || boss == null)
        {
            Debug.LogWarning("[BossIntroSequence] Missing director or boss. Skipping.");
            yield break;
        }

        Camera cam = Camera.main;
        if (cam == null) yield break;

        int deathCount = GameManager.Instance != null ? GameManager.Instance.DeathCount : 0;
        bool isFirstEncounter = deathCount == 0;

        dir.BeginSequence();
        DisableGameplay(true);

        if (isFirstEncounter)
        {
            yield return FirstEncounterIntro(dir, cam);
        }
        else
        {
            yield return RetryIntro(dir, cam, deathCount);
        }

        // Transition to gameplay camera
        yield return dir.MoveCameraTo(cam, gameplayCamPos, gameplayCamRot, 1f);
        dir.SetLetterbox(0f);
        dir.EndSequence();
        DisableGameplay(false);

        Debug.Log("[BossIntroSequence] Boss intro complete. FIGHT!");
    }

    /// <summary>
    /// First encounter: dramatic camera orbit around Humbaba with title.
    /// GDD: ~20 seconds (compressed to ~8 for hackathon).
    /// </summary>
    private IEnumerator FirstEncounterIntro(CinematicDirector dir, Camera cam)
    {
        dir.SetLetterbox(1f);
        yield return dir.FadeToBlack(0f); // Start black

        // Position camera for orbit
        Vector3 bossPos = boss.position;
        float angle = 0f;
        Vector3 orbitCenter = bossPos + Vector3.up * orbitHeight;

        // Place camera at starting orbit position
        Vector3 startOrbitPos = orbitCenter + new Vector3(
            Mathf.Sin(angle * Mathf.Deg2Rad) * orbitRadius,
            0f,
            Mathf.Cos(angle * Mathf.Deg2Rad) * orbitRadius
        );
        cam.transform.position = startOrbitPos;
        cam.transform.LookAt(bossPos + Vector3.up * 1.5f);

        // Fade in
        yield return dir.FadeFromBlack(1.5f);

        // Show boss title
        dir.ShowSubtitle("HUMBABA — GUARDIAN OF THE CEDAR");

        // Orbit camera around boss for ~5 seconds
        float orbitDuration = 5f;
        float elapsed = 0f;
        while (elapsed < orbitDuration)
        {
            if (dir.SkipRequested) break;

            elapsed += Time.unscaledDeltaTime;
            angle += orbitSpeed * Time.unscaledDeltaTime;

            Vector3 pos = orbitCenter + new Vector3(
                Mathf.Sin(angle * Mathf.Deg2Rad) * orbitRadius,
                0f,
                Mathf.Cos(angle * Mathf.Deg2Rad) * orbitRadius
            );
            cam.transform.position = pos;
            cam.transform.LookAt(bossPos + Vector3.up * 1.5f);
            yield return null;
        }

        dir.HideSubtitle();

        // Brief pause
        yield return dir.WaitOrSkip(1f);
    }

    /// <summary>
    /// Retry intro: Humbaba is waiting, taunt appears, then fight.
    /// GDD: ~7 seconds.
    /// </summary>
    private IEnumerator RetryIntro(CinematicDirector dir, Camera cam, int deathCount)
    {
        dir.SetLetterbox(1f);

        // Position camera looking at boss from behind player
        Vector3 behindPlayer = Vector3.zero;
        if (player != null)
        {
            behindPlayer = player.position + Vector3.up * 2f + (player.position - boss.position).normalized * 3f;
        }
        else
        {
            behindPlayer = boss.position + new Vector3(0, 3f, -8f);
        }
        cam.transform.position = behindPlayer;
        cam.transform.LookAt(boss.position + Vector3.up * 1.5f);

        yield return dir.FadeFromBlack(0.8f);

        // Get taunt from cache
        string taunt = GetLastTaunt();
        if (!string.IsNullOrEmpty(taunt))
        {
            dir.ShowSubtitle($"\"{taunt}\"");
        }
        else
        {
            dir.ShowSubtitle($"Death #{deathCount}. Humbaba remembers.");
        }

        // Slow push towards boss
        Vector3 pushTarget = Vector3.Lerp(behindPlayer, boss.position + Vector3.up * 2f, 0.3f);
        yield return dir.MoveCameraTo(cam, pushTarget, cam.transform.rotation, 3f);

        dir.HideSubtitle();
        yield return dir.WaitOrSkip(0.5f);
    }

    private string GetLastTaunt()
    {
        // Try NemesisStateCache first
        if (NemesisStateCache.Instance != null)
        {
            NemesisResponse cached = NemesisStateCache.Instance.GetCachedResponse();
            if (cached != null && !string.IsNullOrEmpty(cached.taunt))
            {
                return cached.taunt;
            }
        }
        return null;
    }

    private void DisableGameplay(bool disable)
    {
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.enabled = !disable;
            PlayerCombat combat = player.GetComponent<PlayerCombat>();
            if (combat != null) combat.enabled = !disable;
        }
        if (boss != null)
        {
            BossController bc = boss.GetComponent<BossController>();
            if (bc != null) bc.enabled = !disable;
        }
    }
}
