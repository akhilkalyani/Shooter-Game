using UnityEngine;

namespace PV.Multiplayer
{
    public class Movement : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Speed of player when moving.")]
        public float moveSpeed = 10;

        [Tooltip("How fast the player turns to face move direction.")]
        public float rotationSmoothRate = 15;

        [Tooltip("How fast the player changes speed.")]
        public float speedChangeRate = 10;

        [Header("Jump")]
        [Tooltip("If the player is jumping right now.")]
        public bool isJumping;

        [Tooltip("The height that player can jump.")]
        public float jumpHeight = 1.5f;

        [Tooltip("Custom gravity used by player for jump calculation. Actual gravity is -9.81f.")]
        public float customGravity = -15f;

        [Header("Grounded")]
        [Tooltip("If the player is grounded or not.")]
        public bool isGrounded;

        [Tooltip("Radius of sphere which is used in ground detection. Make it slightly lower than capsule collider's radius.")]
        public float groundCheckRadius = 0.48f;

        [Tooltip("Offset of ground check, useful when ground is rough.")]
        public float groundOffset = 0.65f;

        [Tooltip("Layer to check ground.")]
        public LayerMask groundLayer;

        protected InputManager Input => InputManager.Instance;

        private Rigidbody _rigid;
        private Transform _cameraTransform;

        private float _moveSpeed;
        private float _currentMoveSpeed;
        private float _vertVel; // Vertical velocity (for jumping).

        private Vector3 _currentMoveVelocity;
        private Vector3 _moveDirection;
        private Vector3 _targetDirection;
        private Vector3 _verticalVelocity;
        private Vector3 _spherePosition;
        private Quaternion _targetRotation;
        private Quaternion _playerRotation;

        protected virtual void Awake()
        {
            // References to the Rigidbody and camera transform.
            _rigid = GetComponent<Rigidbody>();
            _cameraTransform = Camera.main.transform;
        }

        /// <summary>
        /// Central method to update all movement-related functionality.
        /// </summary>
        public void UpdateMovement()
        {
            CheckGrounded();
            HandleMovement();
            HandleRotation();
            HandleJump();
        }

        private void CheckGrounded()
        {
            // Determine if the player is grounded using a sphere check.
            _spherePosition = transform.position;
            _spherePosition.y -= groundOffset;
            isGrounded = Physics.CheckSphere(_spherePosition, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
        }

        private void HandleMovement()
        {
            // Determine the player's move speed based on input.
            _moveSpeed = moveSpeed;

            if (Input.move == Vector2.zero)
            {
                _moveSpeed = 0; // Stop moving if there's no input.
            }

            // Calculate the move direction based on camera orientation and input.
            _moveDirection = _cameraTransform.forward * Input.move.y;
            _moveDirection += _cameraTransform.right * Input.move.x;
            _moveDirection.y = 0; // Ensure movement stays on the ground plane.
            _moveDirection.Normalize();

            // Smoothly adjust the player's speed toward the target speed.
            _currentMoveVelocity = _rigid.velocity;
            _currentMoveVelocity.y = 0;
            _currentMoveSpeed = _currentMoveVelocity.magnitude;

            // Smoothly interpolate the movement speed for smooth acceleration/deceleration.
            if (_currentMoveSpeed < _moveSpeed - 0.1f || _currentMoveSpeed > _moveSpeed + 0.1f)
            {
                _moveSpeed = Mathf.Lerp(_currentMoveSpeed, _moveSpeed, speedChangeRate * Time.deltaTime);
            }

            _moveDirection *= _moveSpeed;

            // Apply the calculated movement velocity to the Rigidbody.
            _rigid.velocity = _moveDirection;
        }

        private void HandleRotation()
        {
            // Calculate the player's facing direction based on the camera.
            _targetDirection = _cameraTransform.forward;
            _targetDirection.Normalize();

            if (_targetDirection == Vector3.zero)
            {
                _targetDirection = transform.forward; // Maintain forward direction if no input.
            }

            // Generate a target rotation toward the direction and smooth the transition.
            _targetRotation = Quaternion.LookRotation(_targetDirection);
            // Lock x and z axis rotation.
            _targetRotation.x = 0;
            _targetRotation.z = 0;

            // Smoothly transition to the target rotation.
            _playerRotation = Quaternion.Slerp(transform.rotation, _targetRotation, rotationSmoothRate * Time.fixedDeltaTime);
            transform.rotation = _playerRotation;
        }

        private void HandleJump()
        {
            if (isGrounded)
            {
                isJumping = false;

                // Reset vertical velocity
                if (_vertVel < 0)
                {
                    _vertVel = -3;
                }

                // Check if jump input is pressed.
                if (Input.jump)
                {
                    isJumping = true;

                    // Calculate initial vertical velocity for the jump.
                    _verticalVelocity = _moveDirection;
                    _vertVel = Mathf.Sqrt(-2 * jumpHeight * customGravity);
                    _verticalVelocity.y = _vertVel;

                    // Apply jump velocity to the Rigidbody.
                    _rigid.velocity = _verticalVelocity;
                }
            }
            else
            {
                // Set current velocity.
                _verticalVelocity = _rigid.velocity;

                // Gradually increase gravity up to a limit.
                if (_vertVel > -50)
                {
                    _vertVel += customGravity * Time.deltaTime;
                }

                // Apply vertical velocity with gravity.
                _verticalVelocity.y = _vertVel;
                _rigid.velocity = _verticalVelocity;
            }
        }
    }
}
