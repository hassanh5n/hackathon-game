using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class DeathScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private TMP_Text tauntText;
    [SerializeField] private Button riseAgainButton;
    [SerializeField] private Button returnToSanctumButton;

    private PlayerStats playerStats;
    private PlayerController playerController;
    private PlayerCombat playerCombat;

    private void Start()
    {
        if (deathPanel != null) deathPanel.SetActive(false);
        
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

        if (riseAgainButton != null) riseAgainButton.onClick.AddListener(OnRiseAgainClicked);
        if (returnToSanctumButton != null) returnToSanctumButton.onClick.AddListener(OnReturnToSanctumClicked);
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnPlayerDeath.RemoveListener(ShowDeathScreen);
        }
        NemesisAPIManager.OnNemesisResponse -= HandleNemesisResponse;
    }

    private void ShowDeathScreen()
    {
        if (deathPanel != null) deathPanel.SetActive(true);
        if (tauntText != null) tauntText.text = "The forest is analyzing your failure...";

        // Block player input while dead
        if (playerController != null) playerController.enabled = false;
        if (playerCombat != null) playerCombat.enabled = false;

        // Optionally, do a slow motion effect on death
        StartCoroutine(SlowMotionDeath());
    }

    private IEnumerator SlowMotionDeath()
    {
        Time.timeScale = 0.3f;
        yield return new WaitForSecondsRealtime(2f);
        Time.timeScale = 0f; // Completely freeze action behind the UI
    }

    private void HandleNemesisResponse(NemesisResponse response)
    {
        if (tauntText == null) return;

        if (response != null && !string.IsNullOrEmpty(response.taunt))
        {
            tauntText.text = "\"" + response.taunt + "\"";
        }
        else
        {
            tauntText.text = "\"The forest claims another...\""; // Fallback
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
}
