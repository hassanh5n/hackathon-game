using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Listens to OnNemesisResponse and applies adaptations (strings) by mutating
/// the BossController's runtime attack lists (altering weights, cooldowns, etc).
/// </summary>
[RequireComponent(typeof(BossController))]
public class NemesisWeightReceiver : MonoBehaviour
{
    private BossController bossController;

    private void Awake()
    {
        bossController = GetComponent<BossController>();
    }

    private void OnEnable()
    {
        CombatLogger.OnNemesisResponse += HandleNemesisResponse;
    }

    private void OnDisable()
    {
        CombatLogger.OnNemesisResponse -= HandleNemesisResponse;
    }

    private void HandleNemesisResponse(NemesisResponse response)
    {
        if (response == null || response.adaptations_applied == null) return;

        Debug.Log($"[NemesisWeightReceiver] Applying {response.adaptations_applied.Count} adaptations.");

        // First, reset all runtime weights back to defaults before applying new ones
        // (Assuming you want adaptations to overwrite, or you'd just stack them. We'll stack here by adjusting.)
        
        foreach (string adaptation in response.adaptations_applied)
        {
            ApplyAdaptation(adaptation);
        }

        if (response.mercy_applied)
        {
            Debug.Log("[NemesisWeightReceiver] MERCY MODE APPLIED: Boss speed/damage slightly reduced.");
            ReduceGlobalDifficulty();
        }
    }

    private void ApplyAdaptation(string adaptationId)
    {
        // Search all phases and modify relevant attacks
        List<List<BossAttackData>> allPhases = new List<List<BossAttackData>> 
        { 
            bossController.RuntimePhase1, 
            bossController.RuntimePhase2, 
            bossController.RuntimePhase3 
        };

        foreach (var phase in allPhases)
        {
            foreach (var attack in phase)
            {
                // Translate Python string IDs into actual weight adjustments
                // These are examples based on adaptation_selector.py
                switch (adaptationId)
                {
                    case "PUNISH_RIGHT_DODGE":
                    case "PUNISH_LEFT_DODGE":
                        if (attack.attackId == "HeavySwing" || attack.attackId == "Grapple")
                            attack.weight *= 1.5f; // Increase chance
                        break;
                    
                    case "CHAIN_PUNISH_LIGHT":
                        if (attack.attackId == "Stomp")
                            attack.cooldown = Mathf.Max(0.5f, attack.cooldown - 0.5f); // Faster recovery
                        break;
                    
                    case "APPROACH_PUNISH":
                        if (attack.attackId == "AoESlam")
                            attack.weight *= 2.0f;
                        break;
                }
            }
        }
    }

    private void ReduceGlobalDifficulty()
    {
        List<List<BossAttackData>> allPhases = new List<List<BossAttackData>> 
        { 
            bossController.RuntimePhase1, 
            bossController.RuntimePhase2, 
            bossController.RuntimePhase3 
        };

        foreach (var phase in allPhases)
        {
            foreach (var attack in phase)
            {
                attack.baseDamage *= 0.8f;
                attack.postureDamage *= 0.8f;
                attack.windupTime *= 1.2f; // Slower windup
            }
        }
    }
}
