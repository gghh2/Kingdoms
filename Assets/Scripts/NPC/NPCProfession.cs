using UnityEngine;

namespace Kingdoms.NPC
{
    /// <summary>
    /// Base class for all NPC professions
    /// Defines common behavior and properties for different jobs
    /// </summary>
    public abstract class NPCProfession : MonoBehaviour
    {
        #region Properties
        
        /// <summary>
        /// Type of this profession
        /// </summary>
        public abstract ProfessionType Type { get; }
        
        /// <summary>
        /// Display name of the profession
        /// </summary>
        public abstract string DisplayName { get; }
        
        /// <summary>
        /// Whether this profession requires a colony to function
        /// </summary>
        public virtual bool RequiresColony => true;
        
        #endregion
        
        #region Protected Fields
        
        protected NPCController _controller;
        
        #endregion
        
        #region Unity Lifecycle
        
        protected virtual void Awake()
        {
            _controller = GetComponent<NPCController>();
            
            if (_controller == null)
            {
                Debug.LogError($"NPCProfession: {DisplayName} requires NPCController component!");
            }
        }
        
        protected virtual void Start()
        {
            OnProfessionStart();
        }
        
        protected virtual void Update()
        {
            OnProfessionUpdate();
        }
        
        #endregion
        
        #region Abstract Methods
        
        /// <summary>
        /// Called when the profession is initialized
        /// Override to setup profession-specific data
        /// </summary>
        protected virtual void OnProfessionStart()
        {
            Debug.Log($"NPCProfession: {gameObject.name} started as {DisplayName}");
        }
        
        /// <summary>
        /// Called every frame to update profession behavior
        /// Override to implement profession-specific logic
        /// </summary>
        protected virtual void OnProfessionUpdate()
        {
            // Base implementation: do nothing
            // Professions override this for custom behavior
        }
        
        #endregion
        
        #region Colony Methods
        
        /// <summary>
        /// Find nearest colony in the world
        /// Returns null if no colony exists
        /// </summary>
        protected virtual Transform FindNearestColony()
        {
            // TODO: Implement colony finding logic when colony system exists
            // For now, return null
            return null;
        }
        
        /// <summary>
        /// Move towards a target position
        /// Returns true if reached target
        /// </summary>
        protected virtual bool MoveTowards(Vector3 target, float speed)
        {
            if (_controller == null) return false;
            
            // Calculate direction
            Vector3 direction = (target - transform.position).normalized;
            direction.y = 0f; // Keep horizontal
            
            // Check if reached
            float distance = Vector3.Distance(transform.position, target);
            if (distance < 1f)
            {
                return true;
            }
            
            // Move using controller
            CharacterController charController = _controller.GetComponent<CharacterController>();
            if (charController != null)
            {
                float deltaTime = Managers.TimeManager.Instance != null 
                    ? Managers.TimeManager.Instance.DeltaTime 
                    : Time.deltaTime;
                    
                charController.Move(direction * speed * deltaTime);
                
                // Rotate towards direction
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * deltaTime);
                }
            }
            
            return false;
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Draw profession-specific gizmos
        /// Override for custom debug visualization
        /// </summary>
        protected virtual void OnDrawGizmosSelected()
        {
            // Base implementation: show profession name above NPC
            // Professions can override for custom gizmos
        }
        
        #endregion
    }
}