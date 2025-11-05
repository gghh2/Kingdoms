using UnityEngine;
using UnityEngine.InputSystem;
using Kingdoms.Managers;

namespace Kingdoms.Player
{
    /// <summary>
    /// Handles first-person camera rotation
    /// Rotates the player body horizontally and the camera vertically
    /// </summary>
    public class PlayerCamera : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Camera Settings")]
        [SerializeField] private Transform playerBody;
        [SerializeField] private Transform cameraTransform;
        
        [Header("Sensitivity")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float verticalClampAngle = 80f;
        
        #endregion
        
        #region Private Fields
        
        private PlayerInputActions _inputActions;
        private Vector2 _lookInput;
        private float _xRotation = 0f;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            _inputActions = new PlayerInputActions();
            
            // Lock cursor to center of screen
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        private void OnEnable()
        {
            _inputActions.Player.Enable();
            _inputActions.Player.Look.performed += OnLook;
            _inputActions.Player.Look.canceled += OnLook;
        }
        
        private void OnDisable()
        {
            _inputActions.Player.Look.performed -= OnLook;
            _inputActions.Player.Look.canceled -= OnLook;
            _inputActions.Player.Disable();
        }
        
        private void Start()
        {
            // Auto-assign camera if not set
            if (cameraTransform == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    cameraTransform = mainCam.transform;
                    Debug.Log("PlayerCamera: Auto-assigned Main Camera");
                }
                else
                {
                    Debug.LogError("PlayerCamera: No camera assigned and no Main Camera found!");
                }
            }
        }
        
        private void LateUpdate()
        {
            HandleCameraRotation();
        }
        
        #endregion
        
        #region Input Callbacks
        
        private void OnLook(InputAction.CallbackContext context)
        {
            _lookInput = context.ReadValue<Vector2>();
        }
        
        #endregion
        
        #region Camera Logic
        
        /// <summary>
        /// Handle camera rotation based on mouse input
        /// Horizontal rotation: player body (Y axis)
        /// Vertical rotation: camera (X axis) with clamping
        /// </summary>
        private void HandleCameraRotation()
        {
            if (playerBody == null || cameraTransform == null) return;
            
            // Don't rotate camera if game is paused or waiting
            if (GameManager.Instance != null)
            {
                var state = GameManager.Instance.CurrentState;
                if (state == GameManager.GameState.Paused || state == GameManager.GameState.Waiting)
                {
                    return;
                }
            }
            
            // Don't rotate camera if cursor is visible (menu open)
            if (Cursor.visible || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }
            
            // Calculate rotation based on input and sensitivity
            float mouseX = _lookInput.x * mouseSensitivity;
            float mouseY = _lookInput.y * mouseSensitivity;
            
            // Vertical rotation (up/down) - applied to camera only
            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -verticalClampAngle, verticalClampAngle);
            
            // Apply rotations
            cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
            playerBody.Rotate(Vector3.up * mouseX);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Toggle cursor lock state (for menus, etc.)
        /// </summary>
        public void ToggleCursorLock()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        
        #endregion
    }
}
