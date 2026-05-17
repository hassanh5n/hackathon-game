using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStatsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private Slider postureSlider;
    [SerializeField] private TMP_Text deathCountText;

    private PlayerStats playerStats;

    private void Start()
    {
        playerStats = FindObjectOfType<PlayerStats>();
    }

    private void Update()
    {
        if (playerStats != null)
        {
            if (hpSlider != null) hpSlider.value = playerStats.HPPercent;
            if (staminaSlider != null) staminaSlider.value = playerStats.StaminaPercent;
            if (postureSlider != null) postureSlider.value = playerStats.PosturePercent;
        }

        // Pull death count from GameManager directly in Update or via event
        if (GameManager.Instance != null && deathCountText != null)
        {
            deathCountText.text = $"Deaths: {GameManager.Instance.DeathCount}";
        }
    }
}
