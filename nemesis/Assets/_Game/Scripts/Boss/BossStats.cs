using UnityEngine;
using UnityEngine.Events;

public class BossStats : MonoBehaviour
{
    public enum Phase
    {
        Phase1 = 1,
        Phase2 = 2,
        Phase3 = 3
    }

    [Header("Health Settings")]
    [SerializeField] private float maxHP = 500f; // Defaults, overridden by GameConfig
    [SerializeField] private GameConfig config;
    
    [Header("Events")]
    public UnityEvent<int> OnPhaseChanged;
    public UnityEvent OnBossDeath;

    public float CurrentHP { get; private set; }
    public int CurrentPhase { get; private set; } = 1;
    public float HPPercent => maxHP > 0 ? CurrentHP / maxHP : 0f;

    private float stage2Threshold = 0.6f;
    private float stage3Threshold = 0.3f;

    private void Start()
    {
        if (config == null && GameManager.Instance != null)
        {
            config = GameManager.Instance.Config;
        }

        if (config != null)
        {
            maxHP = config.bossMaxHP;
            stage2Threshold = config.bossStage2Threshold;
            stage3Threshold = config.bossStage3Threshold;
        }

        CurrentHP = maxHP;
        CurrentPhase = (int)Phase.Phase1;
    }

    public void TakeDamage(float damage)
    {
        if (CurrentHP <= 0) return;

        CurrentHP -= damage;
        CurrentHP = Mathf.Max(CurrentHP, 0);

        CheckPhaseTransition();

        if (CurrentHP <= 0)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterVictory();
            }
            OnBossDeath?.Invoke();
        }
    }

    private void CheckPhaseTransition()
    {
        int expectedPhase = CurrentPhase;

        if (HPPercent <= stage3Threshold)
        {
            expectedPhase = (int)Phase.Phase3;
        }
        else if (HPPercent <= stage2Threshold)
        {
            expectedPhase = (int)Phase.Phase2;
        }
        else
        {
            expectedPhase = (int)Phase.Phase1;
        }

        if (expectedPhase != CurrentPhase)
        {
            CurrentPhase = expectedPhase;
            OnPhaseChanged?.Invoke(CurrentPhase);
        }
    }
}
