using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameConfig config;
    [SerializeField] private Transform lockOnTarget;

    [SerializeField] private Animator animator;

    private Rigidbody _rb;
    private Vector2 _moveInput;

    public bool IsMoving => _moveInput.sqrMagnitude > 0.01f;
    public bool IsDodging { get; private set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (_rb == null) return;

        if (lockOnTarget != null)
        {
            Vector3 direction = lockOnTarget.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            }
        }

        // Direct keyboard polling fallback for movement blend speed
        Vector2 inputDir = _moveInput;
        if (inputDir.sqrMagnitude < 0.001f)
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                float x = 0f;
                float y = 0f;
                if (keyboard.wKey.isPressed) y += 1f;
                if (keyboard.sKey.isPressed) y -= 1f;
                if (keyboard.aKey.isPressed) x -= 1f;
                if (keyboard.dKey.isPressed) x += 1f;
                inputDir = new Vector2(x, y);
            }
        }

        // Update animation parameters
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            // Speed between 0 and 1 for movement blend trees
            float speedValue = inputDir.magnitude;
            animator.SetFloat("Speed", speedValue);
            animator.SetBool("IsDodging", IsDodging);
        }
    }

    private void FixedUpdate()
    {
        if (_rb == null || IsDodging) return;

        // Direct keyboard polling fallback for WASD movement force
        Vector2 inputDir = _moveInput;
        if (inputDir.sqrMagnitude < 0.001f)
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                float x = 0f;
                float y = 0f;
                if (keyboard.wKey.isPressed) y += 1f;
                if (keyboard.sKey.isPressed) y -= 1f;
                if (keyboard.aKey.isPressed) x -= 1f;
                if (keyboard.dKey.isPressed) x += 1f;
                inputDir = new Vector2(x, y);
            }
        }

        float speed = config != null ? config.playerMoveSpeed : 5f;
        Vector3 movement = new Vector3(inputDir.x, 0f, inputDir.y).normalized * speed * Time.fixedDeltaTime;
        _rb.MovePosition(_rb.position + movement);
    }

    public void Dodge()
    {
        if (IsDodging) return;
        IsDodging = true;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger("Dodge");
            animator.SetBool("IsDodging", true);
        }

        if (_rb != null)
        {
            Vector3 dodgeDir = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
            if (dodgeDir == Vector3.zero) dodgeDir = transform.forward;
            float force = config != null ? config.playerDodgeForce : 8f;
            _rb.AddForce(dodgeDir * force, ForceMode.Impulse);
        }

        float cooldown = config != null ? config.dodgeCooldown : 0.8f;
        Invoke(nameof(EndDodge), cooldown);
    }

    private void EndDodge()
    {
        IsDodging = false;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetBool("IsDodging", false);
        }
    }

    public void OnMove(InputValue value)
    {
        _moveInput = value.Get<Vector2>();
    }

    public Vector2 GetMovementInput()
    {
        return _moveInput;
    }

    /// <summary>
    /// Returns the current dodge direction as a string ("right","left","forward","back")
    /// based on the movement input at the time this is called.
    /// </summary>
    public string GetDodgeDirection()
    {
        if (Mathf.Abs(_moveInput.x) > Mathf.Abs(_moveInput.y))
        {
            return _moveInput.x > 0 ? "right" : "left";
        }
        else if (_moveInput.sqrMagnitude > 0.01f)
        {
            return _moveInput.y > 0 ? "forward" : "back";
        }
        return "back"; // default retreat
    }

    public void SetLockOnTarget(Transform target)
    {
        lockOnTarget = target;
    }

    public void ClearLockOnTarget()
    {
        lockOnTarget = null;
    }
}
