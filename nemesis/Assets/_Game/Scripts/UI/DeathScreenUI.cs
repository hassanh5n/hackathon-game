using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;

public class DeathScreenUI : MonoBehaviour
{
    // Keeping 'panel' field for compatibility with Editor's BossFightSetup reflection link
    public UIDocument panel;

    private VisualElement rootElement;
    private Label tauntTextLabel;
    private Label deathCountLabel;
    private Label adaptationHintLabel;
    private Button respawnButton;
    private Button quitButton;

    private PlayerStats playerStats;
    private PlayerController playerController;
    private PlayerCombat playerCombat;

    private bool isScreenActive = false;

    private void Start()
    {
        // Try to get UIDocument on this object if not set via reflection
        if (panel == null)
        {
            panel = GetComponent<UIDocument>();
        }

        if (panel != null && panel.rootVisualElement != null)
        {
            var root = panel.rootVisualElement;
            rootElement = root.Q<VisualElement>("DeathScreen-Root");
            tauntTextLabel = root.Q<Label>("TauntText");
            deathCountLabel = root.Q<Label>("DeathCountLabel");
            adaptationHintLabel = root.Q<Label>("AdaptationHint");
            respawnButton = root.Q<Button>("RespawnButton");
            quitButton = root.Q<Button>("QuitButton");

            if (respawnButton != null) respawnButton.clicked += OnRiseAgainClicked;
            if (quitButton != null) quitButton.clicked += OnReturnToSanctumClicked;
        }

        // Hide screen at start
        HideDeathScreen();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerStats = playerObj.GetComponent<PlayerStats>();
            playerController = playerObj.GetComponent<PlayerController>();
            playerCombat = playerObj.GetComponent<PlayerCombat>();
            
            if (playerStats != null)
            {
                playerStats.OnPlayerDeath.AddListener(ShowDeathScreen);
            }
        }

        NemesisAPIManager.OnNemesisResponse += HandleNemesisResponse;
    }

    private void Update()
    {
        // Add keyboard shortcut listeners as shown in KeyHint
        if (isScreenActive)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                OnRiseAgainClicked();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnReturnToSanctumClicked();
            }
        }
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnPlayerDeath.RemoveListener(ShowDeathScreen);
        }
        NemesisAPIManager.OnNemesisResponse -= HandleNemesisResponse;

        if (respawnButton != null) respawnButton.clicked -= OnRiseAgainClicked;
        if (quitButton != null) quitButton.clicked -= OnReturnToSanctumClicked;
    }

    private void ShowDeathScreen()
    {
        isScreenActive = true;
        if (rootElement != null)
        {
            rootElement.style.display = DisplayStyle.Flex;
        }
        
        if (tauntTextLabel != null)
        {
            tauntTextLabel.text = "\"The forest is analyzing your failure...\"";
        }

        if (deathCountLabel != null && GameManager.Instance != null)
        {
            // Convert death count to Roman numerals or standard counter
            deathCountLabel.text = $"DEATH  {GetRomanNumeral(GameManager.Instance.DeathCount + 1)}";
        }

        // Block player input while dead
        if (playerController != null) playerController.enabled = false;
        if (playerCombat != null) playerCombat.enabled = false;

        // Start a slow motion effect on death
        StartCoroutine(SlowMotionDeath());
    }

    private void HideDeathScreen()
    {
        isScreenActive = false;
        if (rootElement != null)
        {
            rootElement.style.display = DisplayStyle.None;
        }
    }

    private IEnumerator SlowMotionDeath()
    {
        Time.timeScale = 0.3f;
        yield return new WaitForSecondsRealtime(2f);
        Time.timeScale = 0f; // Completely freeze action behind the UI
    }

    private void HandleNemesisResponse(NemesisResponse response)
    {
        if (tauntTextLabel == null) return;

        if (response != null && !string.IsNullOrEmpty(response.taunt))
        {
            tauntTextLabel.text = "\"" + response.taunt + "\"";
        }
        else
        {
            tauntTextLabel.text = "\"The forest claims another...\""; // Fallback
        }

        if (adaptationHintLabel != null && response != null && response.adaptations_applied != null && response.adaptations_applied.Count > 0)
        {
            string primaryAdaptation = response.adaptations_applied[0].Replace("_", " ");
            adaptationHintLabel.text = $"▲  {primaryAdaptation.ToUpper()}  |  PATTERN RECORDED";
        }
    }

    private void OnRiseAgainClicked()
    {
        Time.timeScale = 1f; // Restore time scale!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetForNewRun();
            GameManager.Instance.LoadScene("BossFight");
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void OnReturnToSanctumClicked()
    {
        Time.timeScale = 1f; // Restore time scale!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.MainMenu);
            GameManager.Instance.LoadScene("MainMenu");
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    private string GetRomanNumeral(int number)
    {
        if (number <= 0) return "0";
        string[] romans = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
        if (number <= 10) return romans[number - 1];
        return number.ToString(); // Fallback for high numbers
    }
}
