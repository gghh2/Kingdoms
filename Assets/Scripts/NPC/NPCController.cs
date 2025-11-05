using UnityEngine;
using Kingdoms.Managers;

namespace Kingdoms.NPC
{
    /// <summary>
    /// Controls NPC behavior with simple state machine
    /// Phase 1: Idle and Wander only
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class NPCController : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Movement Settings")]
        [Tooltip("Speed when wandering")]
        [SerializeField] private float wanderSpeed = 2f;
        
        [Tooltip("How far the NPC can wander from spawn point")]
        [SerializeField] private float wanderRadius = 10f;
        
        [Header("State Timings")]
        [Tooltip("Min/Max time to stay idle (seconds)")]
        [SerializeField] private Vector2 idleTimeRange = new Vector2(2f, 5f);
        
        [Tooltip("Min/Max time to wander (seconds)")]
        [SerializeField] private Vector2 wanderTimeRange = new Vector2(3f, 8f);
        
        [Header("Rotation Settings")]
        [Tooltip("How fast the NPC rotates towards movement direction")]
        [SerializeField] private float rotationSpeed = 5f;
        
        [Header("Profession")]
        [Tooltip("NPC's profession - determines behavior")]
        [SerializeField] private ProfessionType professionType = ProfessionType.None;
        
        #endregion
        
        #region Private Fields
        
        private CharacterController _controller;
        private NPCState _currentState;
        private Vector3 _spawnPosition;
        private Vector3 _targetPosition;
        private float _stateTimer;
        private float _gravity = -9.81f;
        private Vector3 _velocity;
        private NPCProfession _profession;
        
        #endregion
        
        #region Properties
        
        public NPCState CurrentState => _currentState;
        public ProfessionType Profession => professionType;
        public bool HasProfession => _profession != null;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _spawnPosition = transform.position;
        }
        
        private void Start()
        {
            // Assign profession if specified
            AssignProfession(professionType);
            
            // Start in idle state (will be overridden by profession if exists)
            ChangeState(NPCState.Idle);
        }
        
        private void Update()
        {
            ApplyGravity();
            
            // If NPC has profession, let profession control behavior
            // Otherwise use basic state machine
            if (_profession == null)
            {
                UpdateState();
            }
        }
        
        #endregion
        
        #region State Machine
        
        private void ChangeState(NPCState newState)
        {
            _currentState = newState;
            
            switch (newState)
            {
                case NPCState.Idle:
                    OnEnterIdle();
                    break;
                    
                case NPCState.Wander:
                    OnEnterWander();
                    break;
            }
        }
        
        private void UpdateState()
        {
            switch (_currentState)
            {
                case NPCState.Idle:
                    UpdateIdle();
                    break;
                    
                case NPCState.Wander:
                    UpdateWander();
                    break;
            }
        }
        
        #endregion
        
        #region Idle State
        
        private void OnEnterIdle()
        {
            // Set random idle duration
            _stateTimer = Random.Range(idleTimeRange.x, idleTimeRange.y);
        }
        
        private void UpdateIdle()
        {
            // Use TimeManager's DeltaTime instead of Time.deltaTime to respect wait speed
            if (TimeManager.Instance != null)
            {
                _stateTimer -= TimeManager.Instance.DeltaTime;
            }
            
            // When idle time is up, start wandering
            if (_stateTimer <= 0f)
            {
                ChangeState(NPCState.Wander);
            }
        }
        
        #endregion
        
        #region Wander State
        
        private void OnEnterWander()
        {
            // Pick a random point within wander radius
            _targetPosition = GetRandomPointInRadius(_spawnPosition, wanderRadius);
            
            // Set random wander duration
            _stateTimer = Random.Range(wanderTimeRange.x, wanderTimeRange.y);
        }
        
        private void UpdateWander()
        {
            // Use TimeManager's DeltaTime instead of Time.deltaTime to respect wait speed
            float deltaTime = TimeManager.Instance != null ? TimeManager.Instance.DeltaTime : Time.deltaTime;
            
            _stateTimer -= deltaTime;
            
            // Calculate direction to target
            Vector3 direction = (_targetPosition - transform.position).normalized;
            direction.y = 0f; // Keep movement horizontal
            
            // Check if reached target or time is up
            float distanceToTarget = Vector3.Distance(transform.position, _targetPosition);
            
            if (distanceToTarget < 0.5f || _stateTimer <= 0f)
            {
                // Reached target or time is up, go back to idle
                ChangeState(NPCState.Idle);
                return;
            }
            
            // Move towards target
            _controller.Move(direction * wanderSpeed * deltaTime);
            
            // Rotate towards movement direction
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * deltaTime);
            }
        }
        
        /// <summary>
        /// Get a random point within radius from origin
        /// </summary>
        private Vector3 GetRandomPointInRadius(Vector3 origin, float radius)
        {
            // Random point in circle
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 randomPoint = origin + new Vector3(randomCircle.x, 0f, randomCircle.y);
            
            // Keep at ground level
            randomPoint.y = origin.y;
            
            return randomPoint;
        }
        
        #endregion
        
        #region Gravity
        
        private void ApplyGravity()
        {
            // Use TimeManager's DeltaTime instead of Time.deltaTime to respect wait speed
            float deltaTime = TimeManager.Instance != null ? TimeManager.Instance.DeltaTime : Time.deltaTime;
            
            // Apply gravity
            _velocity.y += _gravity * deltaTime;
            
            // Apply velocity
            _controller.Move(_velocity * deltaTime);
            
            // Reset velocity when grounded
            if (_controller.isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }
        }
        
        #endregion
        
        #region Profession Management
        
        /// <summary>
        /// Assign a profession to this NPC
        /// </summary>
        public void AssignProfession(ProfessionType type)
        {
            if (type == ProfessionType.None)
            {
                return;
            }
            
            professionType = type;
            
            // Remove old profession if exists
            if (_profession != null)
            {
                Destroy(_profession);
                _profession = null;
            }
            
            // Add new profession component
            _profession = type switch
            {
                ProfessionType.ColonyLeader => gameObject.AddComponent<ColonyLeaderProfession>(),
                ProfessionType.Hunter => gameObject.AddComponent<HunterProfession>(),
                // TODO: Add other professions as they are implemented
                // ProfessionType.Blacksmith => gameObject.AddComponent<BlacksmithProfession>(),
                // ProfessionType.Miner => gameObject.AddComponent<MinerProfession>(),
                // etc...
                _ => null
            };

            if (_profession != null)
            {
                // CRITICAL: Disable profession until NPC disembarks from boat
                // This prevents NPCs from acting while still on the boat
                _profession.enabled = false;
                Debug.Log($"{gameObject.name}: Assigned profession {type} (DISABLED until disembark)");
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: Profession {type} not yet implemented!");
            }
        }
        
        /// <summary>
        /// Get the CharacterController component (for profession use)
        /// </summary>
        public CharacterController GetCharacterController()
        {
            return _controller;
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmosSelected()
        {
            // Show wander radius
            Gizmos.color = Color.yellow;
            Vector3 spawnPos = Application.isPlaying ? _spawnPosition : transform.position;
            Gizmos.DrawWireSphere(spawnPos, wanderRadius);
            
            // Show target position when wandering
            if (Application.isPlaying && _currentState == NPCState.Wander)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(_targetPosition, 0.3f);
                Gizmos.DrawLine(transform.position, _targetPosition);
            }
        }
        
        #endregion
    }
}
