using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BossStats))]
public class BossController : MonoBehaviour
{
    public enum BossState
    {
        Idle,
        Windup,
        Attacking,
        Cooldown,
        Staggered,
        Dead
    }

    // Adding BossStage enum as required by the BossAdaptation script integration logic
    public enum BossStage
    {
        STAGE_ONE = 1,
        STAGE_TWO = 2,
        STAGE_THREE = 3
    }

    [Header("References")]
    [SerializeField] private BossStats bossStats;
    [SerializeField] private BossConfig bossConfig;
    [SerializeField] private Animator animator;

    [Header("Stage Models (Autowired)")]
    [SerializeField] private GameObject stage1Model;
    [SerializeField] private GameObject stage2Model;
    [SerializeField] private GameObject stage3Model;
    
    [Header("Settings")]
    public bool debugMode = true;

    private Transform player;
    private PlayerStats playerStats;
    
    private BossState currentState = BossState.Idle;
    private Coroutine behaviorCoroutine;
    private BossAttackData currentAttack;

    private Dictionary<string, GameObject> hitboxes = new Dictionary<string, GameObject>();
    private HashSet<Collider> hitTargets = new HashSet<Collider>();

    private CombatLogger _combatLogger;

    // Runtime clones of attack lists so we don't mutate ScriptableObject
    public List<BossAttackData> RuntimePhase1 { get; private set; }
    public List<BossAttackData> RuntimePhase2 { get; private set; }
    public List<BossAttackData> RuntimePhase3 { get; private set; }

    // Public properties required by BossAdaptation
    public float AttackCooldown { get; set; } = 3.0f;
    public float MovementSpeed { get; set; } = 4.0f;
    
    public BossStage CurrentStage
    {
        get
        {
            if (bossStats != null)
            {
                switch(bossStats.CurrentPhase)
                {
                    case 1: return BossStage.STAGE_ONE;
                    case 2: return BossStage.STAGE_TWO;
                    case 3: return BossStage.STAGE_THREE;
                }
            }
            return BossStage.STAGE_ONE;
        }
    }

    public event System.Action<BossStage> OnStageChanged;

    private void Awake()
    {
        if (bossStats == null) bossStats = GetComponent<BossStats>();
        if (animator == null) animator = GetComponent<Animator>();

        // Clone attack configs
        RuntimePhase1 = CloneAttacks(bossConfig.phase1Attacks);
        RuntimePhase2 = CloneAttacks(bossConfig.phase2Attacks);
        RuntimePhase3 = CloneAttacks(bossConfig.phase3Attacks);

        _combatLogger = FindObjectOfType<CombatLogger>();
        if (_combatLogger == null)
        {
            Debug.LogWarning("[BossController] CombatLogger not found in scene!");
        }
    }

    private void Start()
    {
        PlayerStats pStats = FindObjectOfType<PlayerStats>();
        if (pStats != null)
        {
            player = pStats.transform;
            playerStats = pStats;
        }
        else
        {
            Debug.LogError("[BossController] Could not find PlayerStats in the scene! Boss will not move or attack player.");
        }

        // Find stage models if not explicitly assigned
        if (stage1Model == null)
        {
            Transform t = transform.Find("Stage_1");
            if (t != null) stage1Model = t.gameObject;
        }
        if (stage2Model == null)
        {
            Transform t = transform.Find("Stage_2");
            if (t != null) stage2Model = t.gameObject;
        }
        if (stage3Model == null)
        {
            Transform t = transform.Find("Stage_3");
            if (t != null) stage3Model = t.gameObject;
        }

        // Initialize active model based on current stats phase (fallback to phase 1 if 0 due to execution order)
        int initialPhase = bossStats.CurrentPhase <= 0 ? 1 : bossStats.CurrentPhase;
        UpdateActiveModel(initialPhase);

        bossStats.OnPhaseChanged.AddListener(HandlePhaseChanged);
        bossStats.OnBossDeath.AddListener(HandleBossDeath);

        // Find hitboxes
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Hitbox_"))
            {
                string attackId = child.name.Substring("Hitbox_".Length);
                hitboxes[attackId] = child.gameObject;
                child.gameObject.SetActive(false);
            }
        }

        behaviorCoroutine = StartCoroutine(BehaviorLoop());
    }

    private List<BossAttackData> CloneAttacks(List<BossAttackData> original)
    {
        List<BossAttackData> clone = new List<BossAttackData>();
        foreach (var atk in original)
        {
            clone.Add(new BossAttackData 
            { 
                attackId = atk.attackId, 
                baseDamage = atk.baseDamage, 
                postureDamage = atk.postureDamage, 
                windupTime = atk.windupTime, 
                cooldown = atk.cooldown, 
                weight = atk.weight 
            });
        }
        return clone;
    }

    public List<BossAttackData> GetCurrentRuntimeAttacks()
    {
        switch (bossStats.CurrentPhase)
        {
            case 1: return RuntimePhase1;
            case 2: return RuntimePhase2;
            case 3: return RuntimePhase3;
            default: return RuntimePhase1;
        }
    }

    /// <summary>
    /// Adjusts the selection weight of a specific attack within the current phase pool.
    /// Used by BossAdaptation to change AI behavior.
    /// </summary>
    public void SetAttackWeightOverride(string attackName, float weight)
    {
        List<BossAttackData> currentPool = GetCurrentRuntimeAttacks();
        foreach (var attack in currentPool)
        {
            if (attack.attackId == attackName)
            {
                attack.weight = weight;
                if (debugMode) Debug.Log($"[BossController] Set override weight for {attackName} to {weight}");
                return;
            }
        }
    }

    /// <summary>
    /// Forces the boss to immediately execute a specific attack if possible.
    /// Used by specific AI adaptation triggers.
    /// </summary>
    public void ForceAttack(string attackName)
    {
        List<BossAttackData> currentPool = GetCurrentRuntimeAttacks();
        foreach (var attack in currentPool)
        {
            if (attack.attackId == attackName)
            {
                if (currentState == BossState.Idle || currentState == BossState.Cooldown)
                {
                    if (behaviorCoroutine != null) StopCoroutine(behaviorCoroutine);
                    currentAttack = attack;
                    behaviorCoroutine = StartCoroutine(ExecuteAttackRoutine());
                }
                return;
            }
        }
    }

    private void ChangeState(BossState newState)
    {
        if (debugMode && currentState != newState)
        {
            Debug.Log($"[BossController] State changed: {currentState} -> {newState}");
        }
        currentState = newState;
    }

    private IEnumerator BehaviorLoop()
    {
        while (currentState != BossState.Dead)
        {
            if (currentState == BossState.Staggered)
            {
                yield return null;
                continue;
            }

            ChangeState(BossState.Idle);
            
            // Use the public property AttackCooldown to allow the AI to speed up/slow down the loop
            yield return new WaitForSeconds(AttackCooldown);

            if (currentState == BossState.Dead) break;

            List<BossAttackData> attacks = GetCurrentRuntimeAttacks();
            currentAttack = PickAttack(attacks);

            if (currentAttack != null)
            {
                yield return StartCoroutine(ExecuteAttackRoutine());
            }
            else
            {
                yield return null;
            }
        }
    }

    private IEnumerator ExecuteAttackRoutine()
    {
        // Tell CombatLogger which attack we are starting
        _combatLogger?.SetCurrentBossAttack(currentAttack.attackId);

        // Notify CombatLogger that boss started an attack
        if (CombatLogger.Instance != null)
        {
            CombatLogger.Instance.RecordBossAttack(currentAttack.attackId);
        }

        // Windup
        ChangeState(BossState.Windup);
        if (animator != null)
        {
            animator.SetTrigger($"Windup_{currentAttack.attackId}");
        }
        yield return new WaitForSeconds(currentAttack.windupTime);

        if (currentState == BossState.Dead) yield break;

        // Attacking
        ChangeState(BossState.Attacking);
        hitTargets.Clear();
        
        GameObject activeHitbox = null;
        if (hitboxes.TryGetValue(currentAttack.attackId, out activeHitbox))
        {
            activeHitbox.SetActive(true);
        }
        
        yield return new WaitForSeconds(0.4f);

        if (activeHitbox != null)
        {
            activeHitbox.SetActive(false);
        }

        if (currentState == BossState.Dead) yield break;

        // Cooldown
        ChangeState(BossState.Cooldown);
        yield return new WaitForSeconds(currentAttack.cooldown);
    }

    private BossAttackData PickAttack(List<BossAttackData> attacks)
    {
        if (attacks == null || attacks.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var attack in attacks)
        {
            totalWeight += attack.weight;
        }

        // Handle case where all weights are 0
        if (totalWeight <= 0.0001f)
        {
            return attacks[Random.Range(0, attacks.Count)];
        }

        float randomVal = Random.Range(0f, totalWeight);
        float currentSum = 0f;

        foreach (var attack in attacks)
        {
            currentSum += attack.weight;
            if (randomVal <= currentSum)
            {
                return attack;
            }
        }

        return attacks[attacks.Count - 1]; // Fallback
    }

    private void UpdateActiveModel(int phase)
    {
        if (stage1Model != null) stage1Model.SetActive(phase == 1);
        if (stage2Model != null) stage2Model.SetActive(phase == 2);
        if (stage3Model != null) stage3Model.SetActive(phase == 3);

        // Update active animator reference to the active model's animator
        GameObject activeModel = null;
        if (phase == 1) activeModel = stage1Model;
        else if (phase == 2) activeModel = stage2Model;
        else if (phase == 3) activeModel = stage3Model;

        if (activeModel != null)
        {
            animator = activeModel.GetComponent<Animator>();
            if (animator == null)
            {
                animator = activeModel.GetComponentInChildren<Animator>();
            }
        }
    }

    private void HandlePhaseChanged(int newPhase)
    {
        if (debugMode)
        {
            Debug.Log($"[BossController] Phase changed to: {newPhase}");
        }
        
        UpdateActiveModel(newPhase);

        if (animator != null)
        {
            animator.SetTrigger("PhaseTransition");
        }

        // Fire public event for BossAdaptation
        OnStageChanged?.Invoke(CurrentStage);
    }

    private void HandleBossDeath()
    {
        ChangeState(BossState.Dead);
        if (behaviorCoroutine != null)
        {
            StopCoroutine(behaviorCoroutine);
        }
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }
        
        foreach (var hitbox in hitboxes.Values)
        {
            hitbox.SetActive(false);
        }
    }

    // Note: Parent object must have Rigidbody for this to trigger if child hitboxes have colliders
    private void OnTriggerEnter(Collider other)
    {
        if (currentState == BossState.Attacking && currentAttack != null)
        {
            PlayerStats pStats = other.GetComponent<PlayerStats>();
            if (pStats == null) pStats = other.GetComponentInParent<PlayerStats>();

            if (pStats != null && !hitTargets.Contains(other))
            {
                hitTargets.Add(other);
                PlayerCombat pCombat = pStats.GetComponent<PlayerCombat>();
                if (pCombat != null && pCombat.isParrying)
                {
                    pCombat.HandleIncomingHit(); // Parry success
                }
                else
                {
                    pStats.TakeDamage(currentAttack.baseDamage);
                    pStats.TakePostureDamage(currentAttack.postureDamage);
                    
                    if (debugMode)
                    {
                        Debug.Log($"[BossController] Landed {currentAttack.attackId} on Player! Damage: {currentAttack.baseDamage}, Posture: {currentAttack.postureDamage}");
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (bossStats != null)
        {
            bossStats.OnPhaseChanged.RemoveListener(HandlePhaseChanged);
            bossStats.OnBossDeath.RemoveListener(HandleBossDeath);
        }
    }
}
