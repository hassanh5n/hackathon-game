using System.Collections;
using UnityEngine;

/// <summary>
/// Victory cinematic — GDD §5.4.
/// Triggers when boss HP reaches 0.
/// 
/// Sequence (~20 sec, skippable):
/// 1. Slow-motion final moment
/// 2. Camera orbits fallen boss
/// 3. Humbaba speaks: "Ten thousand warriors. None fought for something real."
/// 4. Boss dissolves (scale down + fade)
/// 5. Golden path — camera pushes forward
/// 6. Daughter reunion subtitle
/// 7. Fade to black → credits or main menu
/// 
/// Attach to any GameObject in the BossFight scene.
/// Listens to BossStats.OnBossDeath.
/// </summary>
public class VictorySequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform boss;
    [SerializeField] private BossStats bossStats;

    [Header("Camera")]
    [SerializeField] private Vector3 victoryOrbitCenter = new Vector3(0f, 3f, 0f);
    [SerializeField] private float orbitRadius = 6f;
    [SerializeField] private float orbitHeight = 3f;

    [Header("Settings")]
    [SerializeField] private bool returnToMainMenu = true;
    [SerializeField] private float menuReturnDelay = 3f;

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
            if (b != null)
            {
                boss = b.transform;
                if (bossStats == null) bossStats = b.GetComponent<BossStats>();
            }
        }

        // Listen for boss death
        if (bossStats != null)
        {
            bossStats.OnBossDeath.AddListener(OnBossDefeated);
        }
    }

    private void OnDestroy()
    {
        if (bossStats != null)
        {
            bossStats.OnBossDeath.RemoveListener(OnBossDefeated);
        }
    }

    private void OnBossDefeated()
    {
        StartCoroutine(PlayVictory());
    }

    public void Play()
    {
        StartCoroutine(PlayVictory());
    }

    private IEnumerator PlayVictory()
    {
        CinematicDirector dir = CinematicDirector.Instance;
        Camera cam = Camera.main;

        if (dir == null || cam == null)
        {
            Debug.LogWarning("[VictorySequence] Missing director or camera.");
            yield return new WaitForSecondsRealtime(2f);
            GoToMenu();
            yield break;
        }

        dir.BeginSequence();

        // Disable player controls
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.enabled = false;
            PlayerCombat combat = player.GetComponent<PlayerCombat>();
            if (combat != null) combat.ForceDisableHitbox();
        }

        // ── 1. Slow-motion impact ──
        Time.timeScale = 0.15f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        yield return new WaitForSecondsRealtime(2f);
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        // ── 2. Letterbox + camera to boss ──
        dir.SetLetterbox(1f);

        Vector3 bossPos = boss != null ? boss.position : Vector3.zero;
        Vector3 closeUp = bossPos + new Vector3(2f, 2f, 3f);
        Quaternion lookAtBoss = Quaternion.LookRotation(bossPos + Vector3.up - closeUp);
        yield return dir.MoveCameraTo(cam, closeUp, lookAtBoss, 2f);

        // ── 3. Humbaba's only dialogue ──
        dir.ShowSubtitle("\"Ten thousand warriors. None fought for something real. You... are different.\"");
        yield return dir.WaitOrSkip(4f);
        dir.HideSubtitle();

        // ── 4. Boss dissolves (shrink + fade) ──
        if (boss != null)
        {
            yield return DissolveBoss(boss, 2f, dir);
        }

        // ── 5. Camera pushes forward (golden path) ──
        dir.ShowSubtitle("The forest parts. A golden path opens.");
        Vector3 forwardPath = bossPos + Vector3.forward * 15f + Vector3.up * 2f;
        Quaternion forwardLook = Quaternion.LookRotation(Vector3.forward);
        yield return dir.MoveCameraTo(cam, forwardPath, forwardLook, 3f);
        dir.HideSubtitle();

        // ── 6. Reunion ──
        dir.ShowSubtitle("He kneels. Takes her hand. She opens her eyes.");
        yield return dir.WaitOrSkip(3.5f);
        dir.HideSubtitle();

        // ── 7. Fade to black ──
        yield return dir.FadeToBlack(2f);

        // ── 8. Credits / title ──
        dir.ShowSubtitle("NEMESIS: THE ETERNAL GUARDIAN");
        yield return dir.WaitOrSkip(3f);
        dir.HideSubtitle();

        dir.EndSequence();

        // Return to menu
        if (returnToMainMenu)
        {
            yield return new WaitForSecondsRealtime(menuReturnDelay);
            GoToMenu();
        }
    }

    private IEnumerator DissolveBoss(Transform bossTransform, float duration, CinematicDirector dir)
    {
        Vector3 originalScale = bossTransform.localScale;
        Renderer rend = bossTransform.GetComponent<Renderer>();
        Color originalColor = Color.white;

        if (rend != null)
        {
            originalColor = rend.material.color;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (dir != null && dir.SkipRequested) break;

            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            // Shrink
            bossTransform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);

            // Fade if possible
            if (rend != null)
            {
                Color c = originalColor;
                c.a = Mathf.Lerp(1f, 0f, t);
                rend.material.color = c;
            }

            // Float upward
            bossTransform.position += Vector3.up * 0.5f * Time.unscaledDeltaTime;

            yield return null;
        }

        bossTransform.gameObject.SetActive(false);
    }

    private void GoToMenu()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.MainMenu);
            GameManager.Instance.LoadScene("MainMenu");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
