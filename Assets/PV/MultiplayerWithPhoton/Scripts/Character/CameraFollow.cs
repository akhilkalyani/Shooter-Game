using UnityEngine;
using Cinemachine;

namespace PV.Multiplayer
{
    public class CameraFollow : MonoBehaviour
    {
        public static CameraFollow Instance;

        // Reference to the InputManager to handle player input.
        public InputManager input;

        // Cinemachine virtual cameras for normal following and aiming modes.
        public CinemachineVirtualCameraBase followCamera;
        public CinemachineVirtualCameraBase aimCamera;

        public float angleUpDown = 20; // Vertical angle for non-aiming mode.
        public float lookMultiplier = 1; // Look multiplier for horizontal sensitivity.
        public float rotationSmooth = 0.1f; // Smoothing factor for rotation interpolation.

        // Private variables for managing the follow target and rotation.
        private Transform _followTarget;
        private Vector3 _targetAngle;
        private Quaternion _targetRotation;

        void Awake()
        {
            Instance = this;
            if (input == null)
            {
                input = InputManager.Instance;
            }
        }

        public void Init(PlayerController playerController)
        {
            if (followCamera == null || aimCamera == null)
            {
                Debug.LogError("Follow and Aim cameras are missing.");
                return;
            }

            // Assign the player's follow target to the follow camera.
            if (followCamera != null)
            {
                followCamera.Follow = playerController.followTarget;
                _followTarget = playerController.followTarget;
            }

            // Configure the aim camera with the follow target and aim reticle.
            if (aimCamera != null)
            {
                aimCamera.Follow = playerController.followTarget;
            }
        }

        void Update()
        {
            if (_followTarget == null)
            {
                return;
            }

            // Toggle between follow and aim cameras based on the player's aiming input.
            if (input.isAiming && followCamera.gameObject.activeSelf)
            {
                followCamera.gameObject.SetActive(false);
                aimCamera.gameObject.SetActive(true);
            }
            else if (!input.isAiming && aimCamera.gameObject.activeSelf)
            {
                aimCamera.gameObject.SetActive(false);
                followCamera.gameObject.SetActive(true);
            }

            _targetAngle.y = input.look.x * lookMultiplier; // Horizontal rotation.
            _targetAngle.x = input.isAiming ? 0 : angleUpDown; // Vertical angle based on aiming input.
            _targetAngle.z = 0; // Insure it will not have a roll rotation.

            if (input.look.x != 0)
            {
                // Directly rotate the target horizontally when there is input.
                _followTarget.Rotate(Vector3.up, _targetAngle.y, Space.World);
            }
            else
            {
                // Smoothly interpolate to the target rotation when there is no input.
                _targetRotation = Quaternion.Euler(_targetAngle);
                _followTarget.localRotation = Quaternion.Slerp(_followTarget.localRotation, _targetRotation, rotationSmooth * Time.deltaTime);
            }
        }
    }
}
