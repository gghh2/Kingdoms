using UnityEngine;
using System.Collections.Generic;
using Kingdoms.Managers;

namespace Kingdoms.World
{
    /// <summary>
    /// Controls boat movement from spawn point to landing point
    /// Carries NPCs and player on first wave
    /// </summary>
    public class BoatController : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Movement Settings")]
        [Tooltip("Speed of the boat")]
        [SerializeField] private float boatSpeed = 5f;
        
        [Tooltip("How close to landing point before NPCs disembark")]
        [SerializeField] private float landingDistance = 2f;
        
        [Header("Spawn Points")]
        [Tooltip("Where the boat spawns (far from shore)")]
        [SerializeField] private Transform spawnPoint;
        
        [Tooltip("Where the boat lands (on shore)")]
        [SerializeField] private Transform landingPoint;
        
        [Header("NPC Settings")]
        [Tooltip("Parent transform where NPCs are placed on boat")]
        [SerializeField] private Transform npcContainer;
        
        [Tooltip("Offset for NPC disembark position")]
        [SerializeField] private Vector3 disembarkOffset = new Vector3(0f, 0f, -3f);
        
        [Header("Deck Settings")]
        [Tooltip("Box collider representing the boat deck (auto-created if null)")]
        [SerializeField] private BoxCollider deckCollider;
        
        #endregion
        
        #region Private Fields
        
        private List<GameObject> _passengers = new List<GameObject>();
        private bool _isMoving = false;
        private bool _hasLanded = false;
        private bool _isPlayerBoat = false;
        private Vector3 _lastPosition;
        
        #endregion
        
        #region Properties
        
        public bool HasLanded => _hasLanded;
        public bool IsPlayerBoat => _isPlayerBoat;
        public int PassengerCount => _passengers.Count;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            // Initialize last position
            _lastPosition = transform.position;
            
            // Ensure boat has a Rigidbody for physics-based platform movement
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            // Make it kinematic so it moves via script but affects physics
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        
        private void Update()
        {
            if (_isMoving && !_hasLanded)
            {
                MoveBoat();
            }
        }
        
        private void LateUpdate()
        {
            // Move passengers with the boat AFTER all other movements
            MovePassengersWithBoat();
        }
        
        #endregion
        
        #region Passenger Movement
        
        /// <summary>
        /// Move all passengers with the boat to prevent sliding
        /// </summary>
        private void MovePassengersWithBoat()
        {
            // Calculate how much the boat moved this frame
            Vector3 boatMovement = transform.position - _lastPosition;
            
            // Skip if boat didn't move
            if (boatMovement.magnitude < 0.0001f)
            {
                _lastPosition = transform.position;
                return;
            }
            
            // Apply same movement to all passengers
            foreach (GameObject passenger in _passengers)
            {
                if (passenger != null)
                {
                    // Try to use CharacterController.Move if available
                    CharacterController charController = passenger.GetComponent<CharacterController>();
                    if (charController != null)
                    {
                        // Use CharacterController.Move to respect collisions
                        charController.Move(boatMovement);
                    }
                    else
                    {
                        // Fallback to direct position change
                        passenger.transform.position += boatMovement;
                    }
                }
            }
            
            // Update last position for next frame
            _lastPosition = transform.position;
        }
        
        #endregion
        
        #region Boat Setup
        
        /// <summary>
        /// Setup deck collider for walking
        /// </summary>
        private void SetupDeckCollider()
        {
            if (deckCollider == null)
            {
                deckCollider = gameObject.GetComponent<BoxCollider>();
                
                if (deckCollider == null)
                {
                    deckCollider = gameObject.AddComponent<BoxCollider>();
                    // Default size for a simple boat (adjust based on your boat model)
                    deckCollider.size = new Vector3(4f, 1f, 8f);
                    deckCollider.center = new Vector3(0f, 0.5f, 0f);
                    Debug.Log("BoatController: Created deck collider");
                }
            }
        }
        
        #endregion
        
        #region Boat Control
        
        /// <summary>
        /// Initialize boat at spawn point
        /// </summary>
        public void Initialize(Transform spawn, Transform landing, bool isPlayerBoat = false)
        {
            spawnPoint = spawn;
            landingPoint = landing;
            _isPlayerBoat = isPlayerBoat;
            
            // Position boat at spawn point
            if (spawnPoint != null)
            {
                transform.position = spawnPoint.position;
                transform.rotation = spawnPoint.rotation;
            }
            
            // Setup boat deck collider
            SetupDeckCollider();
            
            Debug.Log($"BoatController: Initialized boat (Player boat: {_isPlayerBoat})");
        }
        
        /// <summary>
        /// Start moving the boat towards landing point
        /// </summary>
        public void StartJourney()
        {
            if (landingPoint == null)
            {
                Debug.LogError("BoatController: No landing point assigned!");
                return;
            }
            
            _isMoving = true;
            Debug.Log("BoatController: Journey started");
        }
        
        /// <summary>
        /// Move boat towards landing point
        /// </summary>
        private void MoveBoat()
        {
            // Use TimeManager's DeltaTime instead of Time.deltaTime to respect wait speed
            float deltaTime = TimeManager.Instance != null ? TimeManager.Instance.DeltaTime : Time.deltaTime;
            
            // Calculate direction to landing point
            Vector3 direction = (landingPoint.position - transform.position).normalized;
            
            // Move boat
            transform.position += direction * boatSpeed * deltaTime;
            
            // Rotate towards landing point
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, deltaTime * 2f);
            }
            
            // Check if arrived
            float distanceToLanding = Vector3.Distance(transform.position, landingPoint.position);
            if (distanceToLanding <= landingDistance)
            {
                OnLanded();
            }
        }
        
        /// <summary>
        /// Called when boat reaches landing point
        /// </summary>
        private void OnLanded()
        {
            _isMoving = false;
            _hasLanded = true;
            
            Debug.Log("BoatController: Boat has landed!");
            
            // Disembark player if this is the player boat
            if (_isPlayerBoat)
            {
                // Find player in passengers list
                GameObject player = _passengers.Find(p => p != null && p.CompareTag("Player"));
                if (player != null)
                {
                    DisembarkPlayer(player);
                }
            }
            
            // Disembark all other passengers (NPCs)
            DisembarkPassengers();
        }
        
        #endregion
        
        #region Passenger Management
        
        /// <summary>
        /// Add NPC as passenger on boat
        /// </summary>
        public void AddPassenger(GameObject npc)
        {
            if (npc == null) return;
            
            _passengers.Add(npc);
            
            // Place NPC on boat deck (not parented, so they can move freely)
            Vector3 deckPosition = transform.position + transform.TransformDirection(GetPassengerPosition(_passengers.Count - 1));
            npc.transform.position = deckPosition;
            npc.transform.rotation = transform.rotation;
            
            // NPCs can wander on the boat - let the controller handle it
            var npcController = npc.GetComponent<NPC.NPCController>();
            if (npcController != null)
            {
                // Keep controller enabled so NPCs can move on boat
                npcController.enabled = true;
            }
            
            Debug.Log($"BoatController: Added passenger {npc.name} (Total: {_passengers.Count})");
        }
        
        /// <summary>
        /// Get position for passenger on boat
        /// Arranges passengers in rows at center of deck
        /// </summary>
        private Vector3 GetPassengerPosition(int index)
        {
            // Arrange in a tighter grid to stay within railings
            int row = index / 3;
            int col = index % 3;
            
            return new Vector3(
                (col - 1) * 1.0f,  // -1, 0, 1 (stays in center)
                1f,                // Above deck
                row * 1.0f         // Rows going forward, tighter spacing
            );
        }
        
        /// <summary>
        /// Disembark all passengers
        /// </summary>
        private void DisembarkPassengers()
        {
            // Calculate base disembark position (in front of boat)
            Vector3 disembarkBasePos = landingPoint.position + transform.TransformDirection(disembarkOffset);

            // Get Ground layer for raycast
            int groundLayer = LayerMask.GetMask("Ground");
            if (groundLayer == 0)
            {
                Debug.LogWarning("BoatController: 'Ground' layer not found, using all layers for raycast");
                groundLayer = -1; // All layers
            }

            for (int i = 0; i < _passengers.Count; i++)
            {
                GameObject passenger = _passengers[i];
                if (passenger == null) continue;

                // Calculate random horizontal offset
                Vector3 horizontalOffset = new Vector3(
                    Random.Range(-3f, 3f),
                    0f,
                    Random.Range(-3f, 3f)
                );

                // Target position (horizontal only)
                Vector3 targetHorizontalPos = disembarkBasePos + horizontalOffset;

                // Raycast from above to find ground
                Vector3 rayStart = new Vector3(targetHorizontalPos.x, targetHorizontalPos.y + 50f, targetHorizontalPos.z);

                Vector3 finalPosition;
                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 100f, groundLayer))
                {
                    // Found ground, place NPC on terrain
                    finalPosition = hit.point + Vector3.up * 0.5f; // Small offset above ground
                    Debug.Log($"BoatController: {passenger.name} disembarked on terrain at {finalPosition}");
                }
                else
                {
                    // Fallback: use landing point position if raycast fails
                    finalPosition = landingPoint.position + horizontalOffset;
                    Debug.LogWarning($"BoatController: Raycast failed for {passenger.name}, using fallback position");
                }

                // Set passenger position
                passenger.transform.position = finalPosition;

                // Get NPC controller
                var npcController = passenger.GetComponent<NPC.NPCController>();
                if (npcController != null)
                {
                    // Update spawn position so wander radius centers on new position (not boat)
                    npcController.UpdateSpawnPosition(finalPosition);

                    // Enable profession now that NPC has disembarked
                    if (npcController.HasProfession)
                    {
                        var professionComponent = passenger.GetComponent<NPC.NPCProfession>();
                        if (professionComponent != null)
                        {
                            professionComponent.enabled = true;
                            Debug.Log($"BoatController: {passenger.name} profession {npcController.Profession} ENABLED after disembark");
                        }
                    }
                }

                Debug.Log($"BoatController: {passenger.name} disembarked successfully");
            }

            // Clear passenger list
            _passengers.Clear();

            // Destroy boat after a delay
            Destroy(gameObject, 5f);
        }
        
        /// <summary>
        /// Add player as passenger (for first wave)
        /// </summary>
        public void AddPlayer(GameObject player)
        {
            if (player == null) return;
            
            // Add player to passengers list so they move with the boat
            _passengers.Add(player);
            
            // Place player at center of boat deck
            Vector3 playerPosition = transform.position + transform.TransformDirection(new Vector3(0f, 1f, 0f));
            player.transform.position = playerPosition;
            player.transform.rotation = transform.rotation;
            
            // Keep player controls active so they can walk on boat
            Debug.Log("BoatController: Player added to boat (controls remain active)");
        }
        
        /// <summary>
        /// Disembark player at landing
        /// </summary>
        public void DisembarkPlayer(GameObject player)
        {
            if (player == null) return;
            
            // Remove from passengers list
            _passengers.Remove(player);
            
            // Move player to landing position
            player.transform.position = landingPoint.position + transform.TransformDirection(disembarkOffset);
            
            Debug.Log("BoatController: Player disembarked");
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmos()
        {
            if (spawnPoint != null && landingPoint != null)
            {
                // Draw path from spawn to landing
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(spawnPoint.position, landingPoint.position);
                
                // Draw spawn point
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(spawnPoint.position, 2f);
                
                // Draw landing point
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(landingPoint.position, landingDistance);
            }
        }
        
        #endregion
    }
}