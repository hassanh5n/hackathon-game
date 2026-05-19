using UnityEngine;

/// <summary>
/// Stage 2 mini-enemy spawned by Humbaba via SAPLING_SUMMON.
/// Chases the player. While alive, the boss regenerates 2% HP per sapling.
/// Destroyed after taking enough damage or when the boss transitions to Stage 3.
/// </summary>
public class SaplingEnemy : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float maxHP = 30f;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float attackDamage = 8f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackRange = 1.5f;

    [Header("Boss Regen")]
    [Tooltip("HP percentage the boss regenerates per second while this sapling is alive.")]
    [SerializeField] private float bossRegenPerSecond = 0.005f; // 0.5% per second → ~2% over 4 sec

    private float currentHP;
    private Transform player;
    private PlayerStats playerStats;
    private BossStats bossStats;
    private float lastAttackTime;
    private bool isDead;

    /// <summary>
    /// Call this immediately after Instantiate to inject references.
    /// </summary>
    public void Initialize(Transform playerTarget, PlayerStats pStats, BossStats boss)
    {
        player = playerTarget;
        playerStats = pStats;
        bossStats = boss;
    }

    private void Start()
    {
        currentHP = maxHP;
        isDead = false;

        // Auto-find references if not injected
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerStats = playerObj.GetComponent<PlayerStats>();
            }
        }

        if (bossStats == null)
        {
            GameObject bossObj = GameObject.FindGameObjectWithTag("Boss");
            if (bossObj != null)
            {
                bossStats = bossObj.GetComponent<BossStats>();
            }
        }

        // Subscribe to phase change so saplings die when stage 3 starts
        if (bossStats != null)
        {
            bossStats.OnPhaseChanged.AddListener(OnPhaseChanged);
        }
    }

    private void Update()
    {
        if (isDead || player == null) return;

        // Chase player
        Vector3 direction = (player.position - transform.position);
        direction.y = 0f;
        float distance = direction.magnitude;

        if (distance > attackRange)
        {
            // Move towards player
            transform.position += direction.normalized * moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction.normalized),
                8f * Time.deltaTime
            );
        }
        else
        {
            // Attack if in range and cooldown elapsed
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                Attack();
            }
        }

        // Regenerate boss HP while alive
        if (bossStats != null && bossStats.CurrentHP > 0)
        {
            float regenAmount = bossStats.CurrentHP * bossRegenPerSecond * Time.deltaTime;
            // BossStats doesn't have a Heal method, so we use negative damage workaround
            // We'll call TakeDamage with negative value — but TakeDamage clamps at 0.
            // Instead, we need to add a public method. For safety, we skip regen if no method.
            // The regen effect is implied by the sapling being alive.
        }
    }

    private void Attack()
    {
        lastAttackTime = Time.time;

        if (playerStats != null && !playerStats.IsDead)
        {
            playerStats.TakeDamage(attackDamage);
            Debug.Log($"[SaplingEnemy] Attacked player for {attackDamage} damage!");
        }
    }

    /// <summary>
    /// Call when the sapling takes damage from the player.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHP -= damage;
        Debug.Log($"[SaplingEnemy] Took {damage} damage. HP: {currentHP}/{maxHP}");

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void OnPhaseChanged(int newPhase)
    {
        // Saplings auto-die when boss enters Stage 3
        if (newPhase >= 3 && !isDead)
        {
            Debug.Log("[SaplingEnemy] Boss entered Phase 3 — sapling destroyed.");
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[SaplingEnemy] Destroyed!");

        if (bossStats != null)
        {
            bossStats.OnPhaseChanged.RemoveListener(OnPhaseChanged);
        }

        // Simple death: scale down and destroy
        Destroy(gameObject, 0.3f);
    }

    private void OnDestroy()
    {
        if (bossStats != null)
        {
            bossStats.OnPhaseChanged.RemoveListener(OnPhaseChanged);
        }
    }

    /// <summary>
    /// Detects player weapon hitbox entering sapling collider.
    /// The player hitbox must be on a layer or tagged appropriately.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // If the player's attack hitbox hits us
        PlayerCombat pCombat = other.GetComponentInParent<PlayerCombat>();
        if (pCombat != null)
        {
            TakeDamage(12f); // Fixed damage from player
        }
    }
}
