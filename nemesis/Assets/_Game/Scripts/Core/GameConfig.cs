using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Nemesis/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Player Stats")]
    public float playerMaxHp = 100f;
    public float playerStamina = 100f;
    public float staminaRegenRate = 15f;
    public float playerMoveSpeed = 5f;
    public float playerDodgeForce = 8f;
    public float dodgeCooldown = 0.8f;

    [Header("Items")]
    public int flaskCount = 3;
    public int tabletCount = 2;
    public int spearCount = 3;

    [Header("Boss Adaptations")]
    public float bossStage2Threshold = 0.6f;
    public float bossStage3Threshold = 0.3f;
    public int mercyTriggerDeathCount = 8;
    public int maxActiveAdaptations = 4;

    [Header("Backend")]
    public string backendBaseUrl = "http://localhost:8000";
}
