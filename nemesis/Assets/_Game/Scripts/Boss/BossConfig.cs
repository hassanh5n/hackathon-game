using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BossConfig", menuName = "Nemesis/Boss Config")]
public class BossConfig : ScriptableObject
{
    [Header("Phase 1 Attacks (Base)")]
    public List<BossAttackData> phase1Attacks = new List<BossAttackData>
    {
        new BossAttackData { attackId = "HeavySwing", baseDamage = 20f, postureDamage = 15f, windupTime = 0.8f, cooldown = 2.0f, weight = 0.5f },
        new BossAttackData { attackId = "Stomp", baseDamage = 15f, postureDamage = 25f, windupTime = 0.6f, cooldown = 1.5f, weight = 0.3f },
        new BossAttackData { attackId = "Roar", baseDamage = 10f, postureDamage = 30f, windupTime = 1.0f, cooldown = 3.0f, weight = 0.2f }
    };

    [Header("Phase 2 Attacks (Base)")]
    public List<BossAttackData> phase2Attacks = new List<BossAttackData>
    {
        new BossAttackData { attackId = "HeavySwing", baseDamage = 25f, postureDamage = 20f, windupTime = 0.7f, cooldown = 1.8f, weight = 0.3f },
        new BossAttackData { attackId = "Stomp", baseDamage = 20f, postureDamage = 30f, windupTime = 0.5f, cooldown = 1.2f, weight = 0.2f },
        new BossAttackData { attackId = "AoESlam", baseDamage = 35f, postureDamage = 40f, windupTime = 1.2f, cooldown = 4.0f, weight = 0.3f },
        new BossAttackData { attackId = "Grapple", baseDamage = 40f, postureDamage = 10f, windupTime = 0.9f, cooldown = 3.5f, weight = 0.2f }
    };

    [Header("Phase 3 Attacks (Base)")]
    public List<BossAttackData> phase3Attacks = new List<BossAttackData>
    {
        new BossAttackData { attackId = "AoESlam", baseDamage = 40f, postureDamage = 45f, windupTime = 1.0f, cooldown = 3.5f, weight = 0.3f },
        new BossAttackData { attackId = "Grapple", baseDamage = 50f, postureDamage = 15f, windupTime = 0.8f, cooldown = 3.0f, weight = 0.2f },
        new BossAttackData { attackId = "CedarFireball", baseDamage = 30f, postureDamage = 20f, windupTime = 1.5f, cooldown = 2.5f, weight = 0.3f },
        new BossAttackData { attackId = "HeavySwing", baseDamage = 30f, postureDamage = 25f, windupTime = 0.6f, cooldown = 1.5f, weight = 0.2f }
    };

    // We no longer mutate the ScriptableObject directly because that bleeds across play sessions in Editor
    // and fails in build. BossController will clone these lists.
}
