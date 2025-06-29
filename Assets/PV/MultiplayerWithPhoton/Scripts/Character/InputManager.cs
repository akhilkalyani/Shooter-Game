using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PV.Multiplayer
{
    public class InputManager : MonoBehaviour
    {
        // Singleton instance to provide a single global access point for InputManager.
        public static InputManager Instance;
        public FixedJoystick joystick;
        public Button jumpBtn;
        public Button shootBtn;
        // Public variables to hold player input data, accessible to other scripts.
        public Vector2 move; // Movement vector (uses WASD and Arrow keys).
        public Vector2 look; // Look input (mouse / touch pointer).
        public bool jump; // True when the jump button is pressed.
        public bool attack; // True when the attack button is pressed.
        public bool isAiming; // True when the aim button is held.

        private InputActions _inputActions;

        private void Awake()
        {
            // Initialize the singleton instance and create a new InputActions object.
            Instance = this;
            _inputActions = new();
            jumpBtn.onClick.AddListener(OnJump);
            shootBtn.onClick.AddListener(OnShoot);
        }

        private void OnShoot()
        {
            attack = true;
        }

        private void OnJump()
        {
            jump = true;
        }

        private void OnEnable()
        {
            _inputActions.Player.Enable();
        }

        private void Start()
        {
            // // Handle Look (Mouse/Touch) input
            _inputActions.Player.Look.performed += OnLook;
            _inputActions.Player.Look.canceled += OnLook;

            // Handle aim input: true when performed, false when canceled.
            _inputActions.Player.Aim.performed += c => isAiming = true;
            _inputActions.Player.Aim.canceled += c => isAiming = false;
        }
        void Update()
        {
            move = new Vector2(joystick.Horizontal, joystick.Vertical);
        }
        private void OnLook(InputAction.CallbackContext context)
        {
            // Read the look input
            look = context.ReadValue<Vector2>();
        }

        private void OnDisable()
        {
            _inputActions.Player.Disable();
        }
    }
}
