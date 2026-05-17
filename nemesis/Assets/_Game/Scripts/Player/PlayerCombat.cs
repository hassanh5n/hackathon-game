using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerStats stats;
    [SerializeField] private PlayerController controller;
    [SerializeField] private GameObject hitboxObject;

    [Header("Events")]
    public UnityEvent<float> OnAttackLanded;
    public UnityEvent OnParrySuccess;
    
    // We use a public event action so CombatLogger can listen
    public event System.Action<string, string> OnActionRecorded; // (actionType, detail)

    // States
    private bool isStaggered = false;
    private bool isAttacking = false;
    public bool isParrying { get; private set; } = false;

    private float currentAttackDamage = 0f;

    private void Start()
    {
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (controller == null) controller = GetComponent<PlayerController>();

        if (stats != null)
        {
            stats.OnPostureBroken.AddListener(HandlePostureBroken);
        }

        if (hitboxObject != null)
        {
            hitboxObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (stats != null)
        {
            stats.OnPostureBroken.RemoveListener(HandlePostureBroken);
        }
    }

    private void HandlePostureBroken()
    {
        isStaggered = true;
        
        // Interrupt attacks/parries
        StopAllCoroutines();
        CancelInvoke(nameof(RecoverFromStagger)); // Prevent stacking
        if (hitboxObject != null) hitboxObject.SetActive(false);
        isAttacking = false;
        isParrying = false;
        
        // Recover after an arbitrary stagger time (e.g., 2 seconds)
        Invoke(nameof(RecoverFromStagger), 2f);
    }

    private void RecoverFromStagger()
    {
        isStaggered = false;
    }

    public void LightAttack()
    {
        if (isStaggered || isAttacking) return;
        if (controller != null && controller.IsDodging) return;

        if (stats.UseStamina(15f))
        {
            StartCoroutine(LightAttackRoutine());
            LogAction("player_attack", "light");
        }
    }

    private IEnumerator LightAttackRoutine()
    {
        isAttacking = true;
        currentAttackDamage = 12f;
        
        if (hitboxObject != null) hitboxObject.SetActive(true);
        yield return new WaitForSeconds(0.3f);
        if (hitboxObject != null) hitboxObject.SetActive(false);
        
        isAttacking = false;
    }

    public void HeavyAttack()
    {
        if (isStaggered || isAttacking) return;
        if (controller != null && controller.IsDodging) return;

        if (stats.UseStamina(30f))
        {
            StartCoroutine(HeavyAttackRoutine());
            LogAction("player_attack", "heavy");
        }
    }

    private IEnumerator HeavyAttackRoutine()
    {
        isAttacking = true;
        currentAttackDamage = 28f;
        
        // 0.8s windup
        yield return new WaitForSeconds(0.8f);
        
        if (hitboxObject != null) hitboxObject.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        if (hitboxObject != null) hitboxObject.SetActive(false);
        
        isAttacking = false;
    }

    public void Parry()
    {
        if (isStaggered || isAttacking || isParrying) return;
        if (controller != null && controller.IsDodging) return;

        if (stats.UseStamina(20f))
        {
            StartCoroutine(ParryRoutine());
            LogAction("player_parry", "");
        }
    }

    private IEnumerator ParryRoutine()
    {
        isParrying = true;
        yield return new WaitForSeconds(0.4f);
        isParrying = false;
    }

    public void Dodge()
    {
        if (isStaggered || isAttacking || isParrying) return;
        if (controller == null || controller.IsDodging) return;

        string direction = controller.GetDodgeDirection();
        controller.Dodge();
        LogAction("player_dodge", direction);
    }

    // Called if the boss successfully hits the player. Let Boss check this or call it.
    public void HandleIncomingHit()
    {
        if (isParrying)
        {
            OnParrySuccess?.Invoke();
            LogAction("player_parry_success", "");
        }
    }

    private void LogAction(string actionType, string detail)
    {
        OnActionRecorded?.Invoke(actionType, detail);
    }

    // Handles the Hitbox trigger. Requires the Hitbox to have a BoxCollider (isTrigger) 
    // and this parent GameObject to have a Rigidbody so collisions are sent here.
    private void OnTriggerEnter(Collider other)
    {
        if (hitboxObject != null && hitboxObject.activeSelf)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Boss"))
            {
                OnAttackLanded?.Invoke(currentAttackDamage);
                
                // Prevent multi-hits per swing by disabling hitbox early
                hitboxObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Force-disables the hitbox. Call on death to prevent ghost hits.
    /// </summary>
    public void ForceDisableHitbox()
    {
        StopAllCoroutines();
        if (hitboxObject != null) hitboxObject.SetActive(false);
        isAttacking = false;
        isParrying = false;
    }
}
