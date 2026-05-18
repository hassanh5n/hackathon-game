using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips - BGM")]
    public AudioClip mainMenuMusic;
    public AudioClip bossPhase1Music;
    public AudioClip bossPhase2Music;
    public AudioClip bossPhase3Music;

    [Header("Audio Clips - SFX")]
    public AudioClip playerAttackLight;
    public AudioClip playerAttackHeavy;
    public AudioClip playerDodge;
    public AudioClip playerTakeDamage;
    public AudioClip bossAttackWindup;
    public AudioClip bossAttackSlam;
    public AudioClip bossPhaseTransition;
    public AudioClip uiClick;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Ensure AudioSources exist if not manually assigned
            if (musicSource == null) 
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
            }
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Plays background music. Automatically fades or switches depending on implementation.
    /// </summary>
    public void PlayMusic(AudioClip clip, float volume = 0.5f)
    {
        if (clip == null || musicSource == null) return;
        
        // Don't restart if it's already playing the same track
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.volume = volume;
        musicSource.Play();
    }

    /// <summary>
    /// Plays a sound effect once.
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    // --- Convenience Methods for quick calling from other scripts --- //

    public void PlayUIClick() => PlaySFX(uiClick);
    public void PlayPlayerDodge() => PlaySFX(playerDodge);
    public void PlayPlayerHit() => PlaySFX(playerTakeDamage);
    public void PlayBossTransition() => PlaySFX(bossPhaseTransition);
    
    // Call this from the GameManager when the scene changes or Phase changes
    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }
}
