using UnityEngine;

namespace Kingdoms.NPC
{
    /// <summary>
    /// Hunter profession
    /// Seeks to join colony, hunts animals for food
    /// Requires colony to function
    /// </summary>
    public class HunterProfession : NPCProfession
    {
        #region Properties
        
        public override ProfessionType Type => ProfessionType.Hunter;
        public override string DisplayName => "Hunter";
        public override bool RequiresColony => true;
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Hunter Settings")]
        [Tooltip("Movement speed while traveling to colony")]
        [SerializeField] private float travelSpeed = 3.5f;
        
        [Tooltip("How often to search for colony (seconds)")]
        [SerializeField] private float colonySearchInterval = 5f;
        
        #endregion
        
        #region Private Fields
        
        private enum HunterState
        {
            SeekingColony,  // Looking for colony to join
            TravelingToColony, // Moving towards colony
            InColony,       // Arrived at colony
            Hunting         // Hunting for food (future)
        }
        
        private HunterState _currentState = HunterState.SeekingColony;
        private Transform _targetColony;
        private float _searchTimer = 0f;
        
        #endregion
        
        #region Profession Implementation
        
        protected override void OnProfessionStart()
        {
            base.OnProfessionStart();
            
            _currentState = HunterState.SeekingColony;
            _searchTimer = 0f;
            
            Debug.Log($"{gameObject.name}: Hunter seeking colony to join");
        }
        
        protected override void OnProfessionUpdate()
        {
            switch (_currentState)
            {
                case HunterState.SeekingColony:
                    UpdateSeekingColony();
                    break;
                    
                case HunterState.TravelingToColony:
                    UpdateTravelingToColony();
                    break;
                    
                case HunterState.InColony:
                    UpdateInColony();
                    break;
                    
                case HunterState.Hunting:
                    UpdateHunting();
                    break;
            }
        }
        
        #endregion
        
        #region State Logic
        
        /// <summary>
        /// Search for nearby colony
        /// </summary>
        private void UpdateSeekingColony()
        {
            _searchTimer += Time.deltaTime;
            
            if (_searchTimer >= colonySearchInterval)
            {
                _searchTimer = 0f;
                
                // Try to find colony
                _targetColony = FindNearestColony();
                
                if (_targetColony != null)
                {
                    Debug.Log($"{gameObject.name}: Found colony! Traveling...");
                    _currentState = HunterState.TravelingToColony;
                }
                else
                {
                    // No colony yet, wander while waiting
                    // Use basic wander behavior from NPCController
                }
            }
        }
        
        /// <summary>
        /// Travel towards target colony
        /// </summary>
        private void UpdateTravelingToColony()
        {
            if (_targetColony == null)
            {
                // Colony disappeared, search again
                _currentState = HunterState.SeekingColony;
                return;
            }
            
            // Move towards colony
            bool reached = MoveTowards(_targetColony.position, travelSpeed);
            
            if (reached)
            {
                Debug.Log($"{gameObject.name}: Arrived at colony!");
                _currentState = HunterState.InColony;
                OnArrivedAtColony();
            }
        }
        
        /// <summary>
        /// Behavior when in colony
        /// </summary>
        private void UpdateInColony()
        {
            // TODO: Implement colony-based behavior
            // For now, just wander near colony
            
            if (_targetColony == null)
            {
                // Colony was destroyed, search for new one
                _currentState = HunterState.SeekingColony;
                return;
            }
            
            // Stay near colony center
            float distanceFromColony = Vector3.Distance(transform.position, _targetColony.position);
            if (distanceFromColony > 20f)
            {
                // Too far from colony, return
                MoveTowards(_targetColony.position, travelSpeed);
            }
        }
        
        /// <summary>
        /// Hunting behavior (future implementation)
        /// </summary>
        private void UpdateHunting()
        {
            // TODO: Implement hunting behavior
            // - Find animals
            // - Chase and hunt
            // - Return to colony with food
        }
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Called when NPC arrives at colony
        /// </summary>
        private void OnArrivedAtColony()
        {
            // TODO: Register with colony
            // TODO: Get house assignment
            // TODO: Start work schedule
        }
        
        #endregion
        
        #region Debug
        
        protected override void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            
            // Show target colony
            if (_targetColony != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _targetColony.position);
                Gizmos.DrawWireSphere(_targetColony.position, 2f);
            }
            
            // Show state
            Gizmos.color = _currentState switch
            {
                HunterState.SeekingColony => Color.yellow,
                HunterState.TravelingToColony => Color.cyan,
                HunterState.InColony => Color.green,
                HunterState.Hunting => Color.red,
                _ => Color.white
            };
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
        
        #endregion
    }
}