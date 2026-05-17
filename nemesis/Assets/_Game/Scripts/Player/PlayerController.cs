using UnityEngine;
using UnityEngine.InputSystem;

namespace Nemesis.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private Transform lockOnTarget;

        private Rigidbody _rb;
        private Vector2 _moveInput;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
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
        }

        private void FixedUpdate()
        {
            if (_rb == null) return;

            float speed = config != null ? config.playerMoveSpeed : 5f;
            Vector3 movement = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized * speed * Time.fixedDeltaTime;
            _rb.MovePosition(_rb.position + movement);
        }

        public void OnMove(InputValue value)
        {
            _moveInput = value.Get<Vector2>();
        }

        public Vector2 GetMovementInput()
        {
            return _moveInput;
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
}
