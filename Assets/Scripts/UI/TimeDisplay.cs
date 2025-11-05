using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Kingdoms.Managers;

namespace Kingdoms.UI
{
    /// <summary>
    /// Simple UI display for current game time
    /// Shows day and time in HH:MM format
    /// </summary>
    public class TimeDisplay : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI timeText;
        
        [Header("Settings")]
        [SerializeField] private bool showDayNumber = true;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Update()
        {
            UpdateTimeDisplay();
        }
        
        #endregion
        
        #region Display Logic
        
        /// <summary>
        /// Update the time display text
        /// </summary>
        private void UpdateTimeDisplay()
        {
            if (timeText == null || TimeManager.Instance == null) return;
            
            if (showDayNumber)
            {
                timeText.text = TimeManager.Instance.GetFullTimeString();
            }
            else
            {
                timeText.text = TimeManager.Instance.GetTimeString();
            }
        }
        
        #endregion
    }
}
