using UnityEngine;

namespace Kingdoms.NPC
{
    /// <summary>
    /// Colony Leader profession
    /// Searches for ideal location to establish a new colony
    /// Does not require existing colony
    /// </summary>
    public class ColonyLeaderProfession : NPCProfession
    {
        #region Properties
        
        public override ProfessionType Type => ProfessionType.ColonyLeader;
        public override string DisplayName => "Colony Leader";
        public override bool RequiresColony => false; // Leaders create colonies
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Colony Leader Settings")]
        [Tooltip("Minimum distance from other colonies")]
        [SerializeField] private float minColonyDistance = 50f;
        
        [Tooltip("How long to search for ideal spot (seconds)")]
        [SerializeField] private float searchDuration = 30f;
        
        [Tooltip("Movement speed while searching")]
        [SerializeField] private float searchSpeed = 3f;
        
        #endregion
        
        #region Private Fields
        
        private enum LeaderState
        {
            Searching,      // Looking for ideal spot
            Evaluating,     // Checking if location is good
            Founding        // Creating colony at chosen spot
        }
        
        private LeaderState _currentState = LeaderState.Searching;
        private Vector3 _evaluationPoint;
        private float _searchTimer = 0f;
        
        #endregion
        
        #region Profession Implementation
        
        protected override void OnProfessionStart()
        {
            base.OnProfessionStart();
            
            _currentState = LeaderState.Searching;
            _searchTimer = 0f;
            
            Debug.Log($"{gameObject.name}: Colony Leader started searching for settlement location");
        }
        
        protected override void OnProfessionUpdate()
        {
            switch (_currentState)
            {
                case LeaderState.Searching:
                    UpdateSearching();
                    break;
                    
                case LeaderState.Evaluating:
                    UpdateEvaluating();
                    break;
                    
                case LeaderState.Founding:
                    UpdateFounding();
                    break;
            }
        }
        
        #endregion
        
        #region State Logic
        
        /// <summary>
        /// Search for ideal colony location
        /// </summary>
        private void UpdateSearching()
        {
            _searchTimer += Time.deltaTime;
            
            // Pick random evaluation point nearby
            if (_searchTimer >= 5f) // Evaluate every 5 seconds
            {
                _evaluationPoint = GetRandomPointNearby(20f);
                _currentState = LeaderState.Evaluating;
                _searchTimer = 0f;
            }
            else
            {
                // Wander while searching
                Vector3 wanderTarget = GetRandomPointNearby(10f);
                MoveTowards(wanderTarget, searchSpeed);
            }
        }
        
        /// <summary>
        /// Evaluate if current location is good for colony
        /// </summary>
        private void UpdateEvaluating()
        {
            // Move to evaluation point
            bool reached = MoveTowards(_evaluationPoint, searchSpeed);
            
            if (reached)
            {
                // Check if location is suitable
                if (IsLocationSuitable(_evaluationPoint))
                {
                    Debug.Log($"{gameObject.name}: Found suitable colony location!");
                    _currentState = LeaderState.Founding;
                }
                else
                {
                    // Not suitable, keep searching
                    _currentState = LeaderState.Searching;
                }
            }
        }
        
        /// <summary>
        /// Found colony at current location
        /// </summary>
        private void UpdateFounding()
        {
            // TODO: Create colony here when colony system exists
            // For now, just log and stay idle
            
            Debug.Log($"{gameObject.name}: Founding colony at {transform.position}");
            
            // Mark this location as colony center
            // TODO: Spawn colony marker/structure
            
            enabled = false; // Stop updating, colony founded
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Check if location is suitable for colony
        /// </summary>
        private bool IsLocationSuitable(Vector3 position)
        {
            // TODO: Add more sophisticated checks:
            // - Terrain flatness
            // - Distance from water
            // - Resource availability nearby
            // - Not too close to other colonies
            
            // For now, simple random chance to simulate evaluation
            return Random.value > 0.7f; // 30% chance to find good spot
        }
        
        /// <summary>
        /// Get random point nearby
        /// </summary>
        private Vector3 GetRandomPointNearby(float radius)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 point = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
            
            // Use raycast to ensure point is on terrain
            if (Physics.Raycast(point + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f))
            {
                return hit.point;
            }
            
            return point;
        }
        
        #endregion
        
        #region Debug
        
        protected override void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            
            // Show current evaluation point
            if (_currentState == LeaderState.Evaluating)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_evaluationPoint, 2f);
                Gizmos.DrawLine(transform.position, _evaluationPoint);
            }
            
            // Show search radius
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, minColonyDistance);
        }
        
        #endregion
    }
}