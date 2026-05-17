using UnityEngine;
using UnityEngine.Events;

public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float maxPosture = 100f;

    [Header("Regeneration")]
    [SerializeField] private float staminaRegenRate = 20f;
    [SerializeField] private float postureRegenRate = 5f;
    [SerializeField] private float staminaRegenDelay = 1.5f;
    [SerializeField] private float postureRegenDelay = 2f;

    [Header("Configuration")]
    [SerializeField] private GameConfig config;

    [Header("Events")]
    public UnityEvent OnPlayerDeath;
    public UnityEvent OnPostureBroken;

    // Read-only properties
    public float CurrentHP { get; private set; }
    public float CurrentStamina { get; private set; }
    public float CurrentPosture { get; private set; }

    public float HPPercent => maxHP > 0 ? CurrentHP / maxHP : 0f;
    public float StaminaPercent => maxStamina > 0 ? CurrentStamina / maxStamina : 0f;
    public float PosturePercent => maxPosture > 0 ? CurrentPosture / maxPosture : 0f;
    public bool IsDead => isDead;

    private float staminaRegenTimer = 0f;
    private float postureRegenTimer = 0f;
    private bool isDead = false;

    private void Start()
    {
        if (config != null)
        {
            maxHP = config.playerMaxHp;
            maxStamina = config.playerStamina;
            maxPosture = config.playerMaxPosture;
            staminaRegenRate = config.staminaRegenRate;
        }

        CurrentHP = maxHP;
        CurrentStamina = maxStamina;
        CurrentPosture = 0f;
    }

    private void Update()
    {
        if (isDead) return;

        // Handle Stamina Regeneration
        if (staminaRegenTimer > 0)
        {
            staminaRegenTimer -= Time.deltaTime;
        }
        else if (CurrentStamina < maxStamina)
        {
            CurrentStamina += staminaRegenRate * Time.deltaTime;
            if (CurrentStamina > maxStamina)
            {
                CurrentStamina = maxStamina;
            }
        }

        // Handle Posture Regeneration (decay over time back to 0)
        if (postureRegenTimer > 0)
        {
            postureRegenTimer -= Time.deltaTime;
        }
        else if (CurrentPosture > 0)
        {
            CurrentPosture -= postureRegenRate * Time.deltaTime;
            if (CurrentPosture < 0)
            {
                CurrentPosture = 0;
            }
        }
    }

    /// <summary>
    /// Reduces the player's HP by the specified damage amount.
    /// Resets posture regeneration delay.
    /// </summary>
    /// <param name="dmg">The amount of damage to apply.</param>
    public void TakeDamage(float dmg)
    {
        if (isDead) return;

        CurrentHP -= dmg;
        postureRegenTimer = postureRegenDelay; // Reset regen when hit

        if (CurrentHP <= 0)
        {
            CurrentHP = 0;
            Die();
        }
    }

    /// <summary>
    /// Increases the player's posture damage by the specified amount.
    /// Resets posture regeneration delay. Breaks posture if max is reached.
    /// </summary>
    /// <param name="amt">The amount of posture damage to apply.</param>
    public void TakePostureDamage(float amt)
    {
        if (isDead) return;

        CurrentPosture += amt;
        postureRegenTimer = postureRegenDelay;

        if (CurrentPosture >= maxPosture)
        {
            CurrentPosture = maxPosture;
            OnPostureBroken?.Invoke();
        }
    }

    /// <summary>
    /// Restores the player's HP by the specified amount up to the maximum.
    /// </summary>
    /// <param name="amt">The amount of HP to heal.</param>
    public void HealHP(float amt)
    {
        if (isDead) return;

        CurrentHP += amt;
        if (CurrentHP > maxHP)
        {
            CurrentHP = maxHP;
        }
    }

    /// <summary>
    /// Attempts to consume the specified amount of stamina.
    /// </summary>
    /// <param name="amt">The amount of stamina required.</param>
    /// <returns>True if sufficient stamina was available and consumed; otherwise, false.</returns>
    public bool UseStamina(float amt)
    {
        if (isDead) return false;

        if (CurrentStamina >= amt)
        {
            CurrentStamina -= amt;
            staminaRegenTimer = staminaRegenDelay;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Resets all stats for a new run. Call when respawning.
    /// </summary>
    public void ResetStats()
    {
        isDead = false;
        CurrentHP = maxHP;
        CurrentStamina = maxStamina;
        CurrentPosture = 0f;
        staminaRegenTimer = 0f;
        postureRegenTimer = 0f;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Register the death with the GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayerDeath();
        }

        // Fire death event for other listeners (UI, Animation, etc.)
        OnPlayerDeath?.Invoke();
    }
}
