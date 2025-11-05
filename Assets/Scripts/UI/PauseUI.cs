using UnityEngine;
using TMPro;

namespace Kingdoms.UI
{
    /// <summary>
    /// Manages the pause UI display
    /// Shows "PAUSE" text when game is paused
    /// </summary>
    public class PauseUI : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("UI References")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private TextMeshProUGUI pauseText;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            // Hide pause UI at start
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }
        }
        
        private void Update()
        {
            UpdatePauseUI();
        }
        
        #endregion
        
        #region UI Logic
        
        /// <summary>
        /// Update pause UI visibility based on game state
        /// </summary>
        private void UpdatePauseUI()
        {
            if (pausePanel == null) return;
            
            bool isPaused = Managers.GameManager.Instance != null && 
                           Managers.GameManager.Instance.CurrentState == Managers.GameManager.GameState.Paused;
            
            pausePanel.SetActive(isPaused);
        }
        
        #endregion
    }
}