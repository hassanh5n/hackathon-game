using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class HudUI : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement rootElement;

    // Boss UI Elements
    private Label bossNameLabel;
    private Label bossStageLabel;
    private VisualElement bossHealthFill;
    private VisualElement bossHealthFlash;
    private VisualElement pip1;
    private VisualElement pip2;
    private VisualElement pip3;

    // Taunt UI Elements
    private Label tauntLabel;
    private Coroutine tauntCoroutine;

    // Player UI Elements
    private Label playerNameLabel;
    private VisualElement playerHealthFill;
    private VisualElement playerStaminaFill;

    // References
    private PlayerStats playerStats;
    private BossStats bossStats;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void Start()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponentInChildren<UIDocument>();
        }

        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            var root = uiDocument.rootVisualElement;
            rootElement = root.Q<VisualElement>("HUD-Root");

            // Boss Info
            bossNameLabel = root.Q<Label>("BossNameLabel");
            bossStageLabel = root.Q<Label>("BossStageLabel");
            bossHealthFill = root.Q<VisualElement>("BossHealthFill");
            bossHealthFlash = root.Q<VisualElement>("BossHealthFlash");
            pip1 = root.Q<VisualElement>("Pip1");
            pip2 = root.Q<VisualElement>("Pip2");
            pip3 = root.Q<VisualElement>("Pip3");

            // Center Taunt
            tauntLabel = root.Q<Label>("TauntLabel");
            if (tauntLabel != null)
            {
                tauntLabel.style.opacity = 0f; // Hidden initially
            }

            // Player Info
            playerNameLabel = root.Q<Label>("PlayerNameLabel");
            playerHealthFill = root.Q<VisualElement>("PlayerHealthFill");
            playerStaminaFill = root.Q<VisualElement>("PlayerStaminaFill");
        }

        // Find Player & Boss
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerStats = playerObj.GetComponent<PlayerStats>();
        }

        GameObject bossObj = GameObject.FindGameObjectWithTag("Boss");
        if (bossObj != null)
        {
            bossStats = bossObj.GetComponent<BossStats>();
            if (bossStats != null)
            {
                bossStats.OnPhaseChanged.AddListener(OnBossPhaseChanged);
            }
        }

        // Connect to Nemesis API manager for dynamic taunts
        NemesisAPIManager.OnNemesisResponse += HandleNemesisResponse;

        // Initialize state
        UpdateBossStagePips(1);
    }

    private void OnDestroy()
    {
        if (bossStats != null)
        {
            bossStats.OnPhaseChanged.RemoveListener(OnBossPhaseChanged);
        }
        NemesisAPIManager.OnNemesisResponse -= HandleNemesisResponse;
    }

    private void Update()
    {
        // Update Player Bars
        if (playerStats != null)
        {
            if (playerHealthFill != null)
            {
                float hpPercent = Mathf.Clamp01(playerStats.HPPercent) * 100f;
                playerHealthFill.style.width = Length.Percent(hpPercent);
            }

            if (playerStaminaFill != null)
            {
                float staminaPercent = Mathf.Clamp01(playerStats.StaminaPercent) * 100f;
                playerStaminaFill.style.width = Length.Percent(staminaPercent);
            }
        }

        // Update Boss Health Bar
        if (bossStats != null && bossHealthFill != null)
        {
            float hpPercent = Mathf.Clamp01(bossStats.HPPercent) * 100f;
            bossHealthFill.style.width = Length.Percent(hpPercent);
        }
    }

    private void OnBossPhaseChanged(int newPhase)
    {
        UpdateBossStagePips(newPhase);

        if (bossStageLabel != null)
        {
            switch (newPhase)
            {
                case 1:
                    bossStageLabel.text = "THE ETERNAL GUARDIAN";
                    break;
                case 2:
                    bossStageLabel.text = "THE FOREST WRATH";
                    ShowMidFightTaunt("Humbaba's form distorts! The root of the woodland deepens...");
                    break;
                case 3:
                    bossStageLabel.text = "NEMESIS UNLEASHED";
                    ShowMidFightTaunt("You cannot escape the core. Everything adapts. Everything falls.");
                    break;
            }
        }
    }

    private void UpdateBossStagePips(int activePhase)
    {
        Color activeColor = new Color(220f / 255f, 170f / 255f, 80f / 255f, 1f);
        Color inactiveColor = new Color(60f / 255f, 40f / 255f, 20f / 255f, 1f);

        if (pip1 != null) pip1.style.backgroundColor = activePhase >= 1 ? activeColor : inactiveColor;
        if (pip2 != null) pip2.style.backgroundColor = activePhase >= 2 ? activeColor : inactiveColor;
        if (pip3 != null) pip3.style.backgroundColor = activePhase >= 3 ? activeColor : inactiveColor;
    }

    private void HandleNemesisResponse(NemesisResponse response)
    {
        if (response != null && !string.IsNullOrEmpty(response.taunt))
        {
            // Show new dynamic taunt when adaptation happens
            ShowMidFightTaunt(response.taunt);
        }
    }

    public void ShowMidFightTaunt(string message)
    {
        if (tauntLabel == null) return;

        if (tauntCoroutine != null)
        {
            StopCoroutine(tauntCoroutine);
        }

        tauntCoroutine = StartCoroutine(ShowTauntSequence(message));
    }

    private IEnumerator ShowTauntSequence(string message)
    {
        tauntLabel.text = $"\"{message}\"";
        tauntLabel.style.opacity = 1f;

        // Display for 4 seconds
        yield return new WaitForSeconds(4f);

        // Fade out
        tauntLabel.style.opacity = 0f;
    }
}
