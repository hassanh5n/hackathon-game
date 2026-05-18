using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Main Buttons")]
    [SerializeField] private Button beginJourneyButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("Settings Buttons")]
    [SerializeField] private Button closeSettingsButton;

    private void Start()
    {
        // Ensure initial state
        ShowMainPanel();

        // Hook up Main Buttons
        if (beginJourneyButton != null) beginJourneyButton.onClick.AddListener(OnBeginJourneyClicked);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

        // Hook up Settings Buttons
        if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(ShowMainPanel);
    }

    private void OnBeginJourneyClicked()
    {
        // Transition to the game via GameManager if it exists, otherwise use SceneManager directly
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.HeroIntro);
            // If intro cinematics aren't ready, we can just load the BossFight scene directly for now:
            GameManager.Instance.LoadScene("BossFight"); 
        }
        else
        {
            SceneManager.LoadScene("BossFight");
        }
    }

    private void OnSettingsClicked()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    private void ShowMainPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void OnQuitClicked()
    {
        Debug.Log("[MainMenuUI] Quitting game...");
        Application.Quit();
    }
}
