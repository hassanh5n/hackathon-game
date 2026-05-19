using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Bridges the Nemesis backend AI data with the BossController, applying adaptations at runtime.
/// </summary>
[RequireComponent(typeof(BossController))]
public class BossAdaptation : MonoBehaviour
{
    [Header("Base Parameters")]
    [SerializeField] private float baseAttackCooldown = 3.0f;
    [SerializeField] private float baseMovementSpeed = 4.0f;
    [SerializeField] private float minCooldownClamp = 0.8f;
    [SerializeField] private float maxCooldownClamp = 6.0f;

    private BossController bossController;
    private NemesisWeightReceiver weightReceiver;

    private Dictionary<string, int> playerDodgeCounters = new Dictionary<string, int>();
    private string lastKillingBlow = string.Empty;
    private NemesisAdaptationData currentAdaptation;

    private void Awake()
    {
        bossController = GetComponent<BossController>();
        
        // Find NemesisWeightReceiver either on this object or globally
        weightReceiver = GetComponent<NemesisWeightReceiver>();
        if (weightReceiver == null)
        {
            weightReceiver = FindObjectOfType<NemesisWeightReceiver>();
        }

        if (weightReceiver == null)
        {
            Debug.LogWarning("[BossAdaptation] NemesisWeightReceiver not found. Boss will use default unadapted behavior.");
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRunStarted += HandleRunStarted;
        }
        else
        {
            Debug.LogWarning("[BossAdaptation] GameManager.Instance is null. Cannot listen for run start.");
        }

        bossController.OnStageChanged += HandleStageChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRunStarted -= HandleRunStarted;
        }

        if (bossController != null)
        {
            bossController.OnStageChanged -= HandleStageChanged;
        }
    }

    /// <summary>
    /// Records a successful dodge against a specific attack by the player.
    /// Called externally by the CombatLogger.
    /// </summary>
    /// <param name="attackName">The name of the dodged attack.</param>
    public void RecordPlayerDodge(string attackName)
    {
        if (string.IsNullOrEmpty(attackName)) return;

        if (playerDodgeCounters.ContainsKey(attackName))
        {
            playerDodgeCounters[attackName]++;
        }
        else
        {
            playerDodgeCounters[attackName] = 1;
        }
    }

    /// <summary>
    /// Records the attack that killed the player to punish this pattern on the next run.
    /// Called externally by the CombatLogger on player death.
    /// </summary>
    /// <param name="attackName">The name of the attack that caused the death.</param>
    public void RecordKillingBlow(string attackName)
    {
        lastKillingBlow = attackName;
    }

    /// <summary>
    /// Returns the currently active Nemesis adaptation data.
    /// </summary>
    public NemesisAdaptationData GetCurrentAdaptation()
    {
        return currentAdaptation;
    }

    private void HandleRunStarted()
    {
        if (weightReceiver != null && weightReceiver.CurrentAdaptation != null)
        {
            currentAdaptation = weightReceiver.CurrentAdaptation;
        }
        else
        {
            currentAdaptation = GetDefaultAdaptation();
        }

        ApplyAdaptation();
    }

    private void HandleStageChanged(BossStage stage)
    {
        ApplyPatternShiftOverrides(stage);

        if (currentAdaptation != null && currentAdaptation.defenseWeight > 1.0f)
        {
            float immunityDuration = currentAdaptation.defenseWeight * 0.5f;
            StartCoroutine(ApplyDefensiveImmunity(immunityDuration));
        }
    }

    private void ApplyAdaptation()
    {
        if (currentAdaptation == null) return;

        int runNumber = GameManager.Instance != null ? GameManager.Instance.CurrentRunNumber : 1;

        if (currentAdaptation.adaptationConfidence < 0.3f)
        {
            Debug.LogWarning($"[BossAdaptation] Nemesis adaptation confidence too low ({currentAdaptation.adaptationConfidence:F2} < 0.3). Reverting to default behavior. Need more run data.");
            currentAdaptation = GetDefaultAdaptation();
        }

        Debug.Log($"[BossAdaptation] Applying Adaptation for Run {runNumber} | " +
                  $"Aggression: {currentAdaptation.aggressionWeight:F2} | " +
                  $"Pattern Shift: {currentAdaptation.patternShiftWeight:F2} | " +
                  $"Punish Counter: {currentAdaptation.punishCounterWeight:F2} | " +
                  $"Defense: {currentAdaptation.defenseWeight:F2} | " +
                  $"Confidence: {currentAdaptation.adaptationConfidence:F2}");

        // 1. Apply Aggression Weight (Inversely scale cooldown)
        float aggressionClamped = Mathf.Max(0.1f, currentAdaptation.aggressionWeight);
        float newCooldown = baseAttackCooldown / aggressionClamped;
        bossController.AttackCooldown = Mathf.Clamp(newCooldown, minCooldownClamp, maxCooldownClamp);

        // 2. Movement Speed scaling based on aggression
        bossController.MovementSpeed = baseMovementSpeed * aggressionClamped;

        // 3. Apply Pattern Shifts & Punishments based on current stage
        ApplyPatternShiftOverrides(bossController.CurrentStage);
    }

    private void ApplyPatternShiftOverrides(BossStage stage)
    {
        if (currentAdaptation == null) return;

        List<string> stageAttacks = GetAttacksForStage(stage);

        // Pattern Shift Logic
        if (currentAdaptation.patternShiftWeight > 0.5f)
        {
            // Randomize attack selection weights to break player predictability
            foreach (string attack in stageAttacks)
            {
                bossController.SetAttackWeightOverride(attack, UnityEngine.Random.Range(0.5f, 1.5f));
            }
        }
        else
        {
            // Bias toward the last killing blow
            if (!string.IsNullOrEmpty(lastKillingBlow) && stageAttacks.Contains(lastKillingBlow))
            {
                foreach (string attack in stageAttacks)
                {
                    float weight = (attack == lastKillingBlow) ? 2.0f : 1.0f;
                    bossController.SetAttackWeightOverride(attack, weight);
                }
            }
            else
            {
                // Reset to default weights if the killing blow isn't in this stage
                foreach (string attack in stageAttacks)
                {
                    bossController.SetAttackWeightOverride(attack, 1.0f);
                }
            }
        }

        // Punish Counter Logic
        if (currentAdaptation.punishCounterWeight > 1.0f)
        {
            string mostDodgedAttack = GetMostDodgedAttack();
            if (!string.IsNullOrEmpty(mostDodgedAttack) && stageAttacks.Contains(mostDodgedAttack))
            {
                float punishingWeight = currentAdaptation.punishCounterWeight * 1.5f;
                bossController.SetAttackWeightOverride(mostDodgedAttack, punishingWeight);
            }
        }
    }

    private IEnumerator ApplyDefensiveImmunity(float duration)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        yield return new WaitForSeconds(duration);

        foreach (var col in colliders)
        {
            col.enabled = true;
        }
    }

    private string GetMostDodgedAttack()
    {
        string mostDodged = string.Empty;
        int maxDodges = 0;

        foreach (var kvp in playerDodgeCounters)
        {
            if (kvp.Value > maxDodges)
            {
                maxDodges = kvp.Value;
                mostDodged = kvp.Key;
            }
        }

        return mostDodged;
    }

    private List<string> GetAttacksForStage(BossStage stage)
    {
        switch (stage)
        {
            case BossStage.STAGE_ONE:
                return new List<string> { "GROUND_SLAM", "CEDAR_SWEEP", "ROAR" };
            case BossStage.STAGE_TWO:
                return new List<string> { "ROOT_BURST", "SAPLING_SUMMON", "VINE_WHIP" };
            case BossStage.STAGE_THREE:
                return new List<string> { "DIVINE_FIRE", "PHASE_STEP", "TABLET_STORM", "MIRROR_ATTACK" };
            default:
                return new List<string>();
        }
    }

    private NemesisAdaptationData GetDefaultAdaptation()
    {
        return new NemesisAdaptationData
        {
            aggressionWeight = 1.0f,
            patternShiftWeight = 0.5f,
            punishCounterWeight = 1.0f,
            defenseWeight = 1.0f,
            adaptationConfidence = 1.0f
        };
    }
}
