using UnityEngine;
using UnityEngine.InputSystem;
using Kingdoms.Managers;

namespace Kingdoms.Player
{
    /// <summary>
    /// Handles player movement and jumping
    /// Uses CharacterController for ground-based movement
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        // TODO: Implement sprint functionality
        // [SerializeField] private float sprintMultiplier = 1.5f;
        [SerializeField] private float gravity = -9.81f;
        
        [Tooltip("Air control multiplier (0 = no air control, 1 = full control)")]
        [Range(0f, 1f)]
        [SerializeField] private float airControlMultiplier = 0.2f;
        
        [Header("Jump Settings")]
        [SerializeField] private float jumpHeight = 1.5f;
        
        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundDistance = 0.4f;
        [SerializeField] private LayerMask groundMask;
        
        #endregion
        
        #region Private Fields
        
        private CharacterController _controller;
        private PlayerInputActions _inputActions;
        
        private Vector2 _moveInput;
        private Vector3 _velocity;
        private bool _isGrounded;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _inputActions = new PlayerInputActions();
        }
        
        private void OnEnable()
        {
            _inputActions.Player.Enable();
            _inputActions.Player.Move.performed += OnMove;
            _inputActions.Player.Move.canceled += OnMove;
            _inputActions.Player.Jump.performed += OnJump;
        }
        
        private void OnDisable()
        {
            _inputActions.Player.Move.performed -= OnMove;
            _inputActions.Player.Move.canceled -= OnMove;
            _inputActions.Player.Jump.performed -= OnJump;
            _inputActions.Player.Disable();
        }
        
        private void Update()
        {
            CheckGround();
            ApplyGravity();
            HandleMovement();
        }
        
        #endregion
        
        #region Input Callbacks
        
        private void OnMove(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }
        
        private void OnJump(InputAction.CallbackContext context)
        {
            // Don't jump if game is paused or waiting
            if (GameManager.Instance != null)
            {
                var state = GameManager.Instance.CurrentState;
                if (state == GameManager.GameState.Paused || state == GameManager.GameState.Waiting)
                {
                    return;
                }
            }
            
            if (_isGrounded)
            {
                Jump();
            }
        }
        
        #endregion
        
        #region Movement Logic
        
        /// <summary>
        /// Check if player is on the ground
        /// </summary>
        private void CheckGround()
        {
            if (groundCheck == null)
            {
                // Fallback if no ground check transform is assigned
                _isGrounded = _controller.isGrounded;
                return;
            }
            
            _isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            
            // Reset velocity when grounded to prevent accumulation
            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Small negative value to keep grounded
            }
        }
        
        /// <summary>
        /// Apply gravity to the player
        /// </summary>
        private void ApplyGravity()
        {
            _velocity.y += gravity * Time.deltaTime;
        }
        
        /// <summary>
        /// Handle player movement based on input
        /// </summary>
        private void HandleMovement()
        {
            // Don't move if game is paused or waiting
            if (GameManager.Instance != null)
            {
                var state = GameManager.Instance.CurrentState;
                if (state == GameManager.GameState.Paused || state == GameManager.GameState.Waiting)
                {
                    return;
                }
            }
            
            // Convert input to world space direction
            Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;
            
            // Apply air control multiplier when not grounded
            float currentMoveSpeed = moveSpeed;
            if (!_isGrounded)
            {
                currentMoveSpeed *= airControlMultiplier;
            }
            
            // Apply movement
            _controller.Move(move * currentMoveSpeed * Time.deltaTime);
            
            // Apply velocity (gravity + jump)
            _controller.Move(_velocity * Time.deltaTime);
        }
        
        /// <summary>
        /// Make the player jump
        /// </summary>
        private void Jump()
        {
            // Calculate jump velocity using physics formula: v = sqrt(h * -2 * g)
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmosSelected()
        {
            // Visualize ground check sphere in editor
            if (groundCheck != null)
            {
                Gizmos.color = _isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
            }
        }
        
        #endregion
    }
}
