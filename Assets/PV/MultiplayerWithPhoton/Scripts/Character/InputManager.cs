using UnityEngine;
using UnityEngine.InputSystem;

namespace PV.Multiplayer
{
    public class InputManager : MonoBehaviour
    {
        // Singleton instance to provide a single global access point for InputManager.
        public static InputManager Instance;

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
        }

        private void OnEnable()
        {
            _inputActions.Player.Enable();
        }

        private void Start()
        {
            // Subscribing to input action events for different player controls.
            // Handle Movement input
            _inputActions.Player.Move.performed += OnMove;
            _inputActions.Player.Move.canceled += OnMove;

            // Handle Look (Mouse/Touch) input
            _inputActions.Player.Look.performed += OnLook;
            _inputActions.Player.Look.canceled += OnLook;

            // Handle jump input: true when performed, false when canceled.
            _inputActions.Player.Jump.performed += c => jump = true;
            _inputActions.Player.Jump.canceled += c => jump = false;

            // Handle aim input: true when performed, false when canceled.
            _inputActions.Player.Aim.performed += c => isAiming = true;
            _inputActions.Player.Aim.canceled += c => isAiming = false;

            // Handle attack input: true when performed, false when canceled.
            _inputActions.Player.Attack.performed += c => attack = true;
            _inputActions.Player.Attack.canceled += c => attack = false;
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            // Read the look input
            look = context.ReadValue<Vector2>();
        }

        private void OnMove(InputAction.CallbackContext ctx)
        {
            // Read the movement input
            move = ctx.ReadValue<Vector2>();
        }

        private void OnDisable()
        {
            _inputActions.Player.Disable();
        }
    }
}
