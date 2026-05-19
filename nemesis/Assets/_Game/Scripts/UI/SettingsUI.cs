using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settings panel with volume sliders and quality toggle.
/// Works in both MainMenu and pause overlay.
/// Uses legacy Canvas UI (matching the rest of the project).
/// </summary>
public class SettingsUI : MonoBehaviour
{
    [Header("Audio Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Graphics")]
    [SerializeField] private Dropdown qualityDropdown;

    [Header("Controls")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Toggle hapticToggle;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button resetButton;

    [Header("Panel")]
    [SerializeField] private GameObject settingsPanel;

    // PlayerPrefs keys
    private const string KEY_MASTER_VOL = "MasterVolume";
    private const string KEY_MUSIC_VOL = "MusicVolume";
    private const string KEY_SFX_VOL = "SFXVolume";
    private const string KEY_QUALITY = "QualityLevel";
    private const string KEY_SENSITIVITY = "Sensitivity";
    private const string KEY_HAPTIC = "HapticEnabled";

    private void Start()
    {
        LoadSettings();

        // Hook up listeners
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        if (qualityDropdown != null) qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        if (hapticToggle != null) hapticToggle.onValueChanged.AddListener(OnHapticChanged);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (resetButton != null) resetButton.onClick.AddListener(ResetToDefaults);
    }

    public void Show()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void Hide()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        SaveSettings();
    }

    private void LoadSettings()
    {
        float masterVol = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
        float musicVol = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 0.5f);
        float sfxVol = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);
        int quality = PlayerPrefs.GetInt(KEY_QUALITY, QualitySettings.GetQualityLevel());
        float sensitivity = PlayerPrefs.GetFloat(KEY_SENSITIVITY, 1f);
        bool haptic = PlayerPrefs.GetInt(KEY_HAPTIC, 1) == 1;

        if (masterVolumeSlider != null) masterVolumeSlider.value = masterVol;
        if (musicVolumeSlider != null) musicVolumeSlider.value = musicVol;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVol;
        if (sensitivitySlider != null) sensitivitySlider.value = sensitivity;
        if (hapticToggle != null) hapticToggle.isOn = haptic;

        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
            qualityDropdown.value = quality;
        }

        // Apply
        AudioListener.volume = masterVol;
        QualitySettings.SetQualityLevel(quality, true);
    }

    private void SaveSettings()
    {
        if (masterVolumeSlider != null) PlayerPrefs.SetFloat(KEY_MASTER_VOL, masterVolumeSlider.value);
        if (musicVolumeSlider != null) PlayerPrefs.SetFloat(KEY_MUSIC_VOL, musicVolumeSlider.value);
        if (sfxVolumeSlider != null) PlayerPrefs.SetFloat(KEY_SFX_VOL, sfxVolumeSlider.value);
        if (qualityDropdown != null) PlayerPrefs.SetInt(KEY_QUALITY, qualityDropdown.value);
        if (sensitivitySlider != null) PlayerPrefs.SetFloat(KEY_SENSITIVITY, sensitivitySlider.value);
        if (hapticToggle != null) PlayerPrefs.SetInt(KEY_HAPTIC, hapticToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnMasterVolumeChanged(float value)
    {
        AudioListener.volume = value;
    }

    private void OnMusicVolumeChanged(float value)
    {
        // If AudioManager has separate music source, control it here
        // For now, handled via AudioListener.volume as master
    }

    private void OnSFXVolumeChanged(float value)
    {
        // Would control sfx source volume separately if needed
    }

    private void OnQualityChanged(int index)
    {
        QualitySettings.SetQualityLevel(index, true);
        Debug.Log($"[SettingsUI] Quality set to: {QualitySettings.names[index]}");
    }

    private void OnSensitivityChanged(float value)
    {
        Debug.Log($"[SettingsUI] Sensitivity set to: {value}");
    }

    private void OnHapticChanged(bool enabled)
    {
        Debug.Log($"[SettingsUI] Haptic feedback: {(enabled ? "ON" : "OFF")}");
    }

    private void ResetToDefaults()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.value = 1f;
        if (musicVolumeSlider != null) musicVolumeSlider.value = 0.5f;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = 1f;
        if (sensitivitySlider != null) sensitivitySlider.value = 1f;
        if (hapticToggle != null) hapticToggle.isOn = true;
        if (qualityDropdown != null) qualityDropdown.value = QualitySettings.GetQualityLevel();

        SaveSettings();
        Debug.Log("[SettingsUI] Reset to defaults.");
    }
}
