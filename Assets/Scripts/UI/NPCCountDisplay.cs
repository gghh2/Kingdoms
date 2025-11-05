using UnityEngine;
using TMPro;
using Kingdoms.NPC;

namespace Kingdoms.UI
{
    /// <summary>
    /// Displays the number of living NPCs in the world
    /// Updates every frame to show current count
    /// </summary>
    public class NPCCountDisplay : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("UI References")]
        [Tooltip("TextMeshPro component to display NPC count")]
        [SerializeField] private TextMeshProUGUI npcCountText;
        
        [Header("Update Settings")]
        [Tooltip("How often to update the count (in seconds). Set to 0 to update every frame.")]
        [SerializeField] private float updateInterval = 0.5f;
        
        [Header("Display Format")]
        [Tooltip("Text format. Use {0} for the count.")]
        [SerializeField] private string displayFormat = "NPCs: {0}";
        
        #endregion
        
        #region Private Fields
        
        private float _updateTimer = 0f;
        private int _lastCount = -1;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            // Auto-find TextMeshPro if not assigned
            if (npcCountText == null)
            {
                npcCountText = GetComponent<TextMeshProUGUI>();
                
                if (npcCountText == null)
                {
                    Debug.LogError("NPCCountDisplay: No TextMeshProUGUI component found!");
                    enabled = false;
                    return;
                }
            }
            
            // Initial update
            UpdateNPCCount();
        }
        
        private void Update()
        {
            // Update timer
            _updateTimer += Time.deltaTime;
            
            // Update count when timer expires (or every frame if interval is 0)
            if (updateInterval <= 0f || _updateTimer >= updateInterval)
            {
                UpdateNPCCount();
                _updateTimer = 0f;
            }
        }
        
        #endregion
        
        #region NPC Counting
        
        /// <summary>
        /// Count and display living NPCs
        /// </summary>
        private void UpdateNPCCount()
        {
            if (npcCountText == null) return;
            
            // Count all active NPCController components in the scene
            int count = CountLivingNPCs();
            
            // Only update text if count changed (optimization)
            if (count != _lastCount)
            {
                npcCountText.text = string.Format(displayFormat, count);
                _lastCount = count;
            }
        }
        
        /// <summary>
        /// Count living NPCs in the scene
        /// </summary>
        private int CountLivingNPCs()
        {
            // Find all NPCController components
            NPCController[] npcs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
            
            // Count only active ones
            int count = 0;
            foreach (NPCController npc in npcs)
            {
                if (npc != null && npc.gameObject.activeInHierarchy && npc.enabled)
                {
                    count++;
                }
            }
            
            return count;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Force an immediate update of the display
        /// </summary>
        public void ForceUpdate()
        {
            UpdateNPCCount();
        }
        
        /// <summary>
        /// Set custom display format
        /// </summary>
        public void SetDisplayFormat(string format)
        {
            displayFormat = format;
            _lastCount = -1; // Force update on next frame
        }
        
        #endregion
    }
}