using System.Collections;
using UnityEngine;

/// <summary>
/// Hero intro cinematic — GDD §5.1.
/// Plays on first launch. ~15 seconds (coroutine-based, skippable).
/// 
/// Sequence:
/// 1. Black screen, fade in
/// 2. Camera pans across the arena slowly
/// 3. Subtitle: "My daughter's soul is inside the forest..."
/// 4. Camera settles behind player
/// 5. Title card: "NEMESIS: THE ETERNAL GUARDIAN"
/// 6. Fade out → gameplay begins
/// 
/// Attach to any GameObject in the BossFight scene.
/// </summary>
public class IntroSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform boss;

    [Header("Settings")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool onlyPlayOnce = true;

    [Header("Camera Positions")]
    [SerializeField] private Vector3 wideShot = new Vector3(15f, 8f, -10f);
    [SerializeField] private Vector3 wideShotLookAt = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 playerCloseUp = new Vector3(0f, 2.5f, -7f);
    [SerializeField] private Vector3 playerCloseUpLookAt = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private Vector3 gameplayCamPos = new Vector3(0f, 5f, -12f);
    [SerializeField] private Quaternion gameplayCamRot = Quaternion.Euler(15f, 0f, 0f);

    private const string PLAYED_KEY = "HeroIntroPlayed";

    private void Start()
    {
        // Auto-find
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
            if (onlyPlayOnce && PlayerPrefs.GetInt(PLAYED_KEY, 0) == 1)
            {
                Debug.Log("[IntroSequence] Already played once. Skipping.");
                return;
            }
            StartCoroutine(PlayIntro());
        }
    }

    /// <summary>Call this to play the intro manually.</summary>
    public void Play()
    {
        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        CinematicDirector dir = CinematicDirector.Instance;
        if (dir == null)
        {
            Debug.LogWarning("[IntroSequence] No CinematicDirector in scene. Skipping cinematic.");
            yield break;
        }

        Camera cam = Camera.main;
        if (cam == null) yield break;

        dir.BeginSequence();

        // Freeze gameplay during cinematic
        DisableGameplay(true);

        // 1. Start black
        yield return dir.FadeToBlack(0f); // Instant black
        dir.SetLetterbox(1f);

        // 2. Fade in on wide establishing shot
        cam.transform.position = wideShot;
        cam.transform.LookAt(wideShotLookAt);
        yield return dir.FadeFromBlack(2f);

        // 3. Slow pan from wide shot to player close-up
        Quaternion lookRot = Quaternion.LookRotation(playerCloseUpLookAt - playerCloseUp);
        yield return dir.MoveCameraTo(cam, playerCloseUp, lookRot, 4f);

        // 4. Subtitle
        dir.ShowSubtitle("My daughter's soul is inside the forest. One god stands between us.");
        yield return dir.WaitOrSkip(3.5f);
        dir.HideSubtitle();

        // 5. Title card
        dir.ShowSubtitle("NEMESIS: THE ETERNAL GUARDIAN");
        yield return dir.WaitOrSkip(3f);
        dir.HideSubtitle();

        // 6. Move camera to gameplay position
        yield return dir.MoveCameraTo(cam, gameplayCamPos, gameplayCamRot, 1.5f);

        // 7. Fade out letterbox
        dir.SetLetterbox(0f);

        // 8. Done
        dir.EndSequence();
        DisableGameplay(false);

        if (onlyPlayOnce)
        {
            PlayerPrefs.SetInt(PLAYED_KEY, 1);
            PlayerPrefs.Save();
        }

        Debug.Log("[IntroSequence] Hero intro complete.");
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
