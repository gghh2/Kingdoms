using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Kingdoms.Managers;

namespace Kingdoms.UI
{
    /// <summary>
    /// UI for the wait system
    /// Displays a slider to choose wait duration
    /// </summary>
    public class WaitUI : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("UI References")]
        [SerializeField] private GameObject waitPanel;
        [SerializeField] private Slider durationSlider;
        [SerializeField] private TextMeshProUGUI durationText;
        [SerializeField] private TextMeshProUGUI currentTimeText;
        [SerializeField] private TextMeshProUGUI targetTimeText;
        [SerializeField] private Button waitButton;
        [SerializeField] private Button cancelButton;
        
        [Header("Wait Progress")]
        [SerializeField] private GameObject progressPanel;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TextMeshProUGUI progressText;
        
        #endregion
        
        #region Private Fields
        
        private float _selectedHours = 8f; // Default: 8 hours
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            // Hide panels at start
            if (waitPanel != null)
            {
                waitPanel.SetActive(false);
            }
            
            if (progressPanel != null)
            {
                progressPanel.SetActive(false);
            }
            
            // Setup slider
            if (durationSlider != null && Systems.WaitSystem.Instance != null)
            {
                durationSlider.minValue = Systems.WaitSystem.Instance.MinWaitHours;
                durationSlider.maxValue = Systems.WaitSystem.Instance.MaxWaitHours;
                durationSlider.value = _selectedHours;
                durationSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }
            
            // Setup buttons
            if (waitButton != null)
            {
                waitButton.onClick.AddListener(OnWaitButtonClicked);
            }
            
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelButtonClicked);
            }
            
            // Initial update
            UpdateUI();
        }
        
        private void Update()
        {
            // Toggle wait menu with T key
            if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            {
                ToggleWaitMenu();
            }
            
            // Handle waiting state
            if (Systems.WaitSystem.Instance != null && Systems.WaitSystem.Instance.IsWaiting)
            {
                // Update progress
                UpdateProgress();
            }
            else
            {
                // If we were showing progress panel but wait is finished, hide it
                if (progressPanel != null && progressPanel.activeSelf)
                {
                    HideProgressPanel();
                    
                    // Restore cursor lock for playing
                    if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                    
                    Debug.Log("WaitUI: Wait finished, progress panel closed");
                }
                
                // Update time display when not waiting
                if (waitPanel != null && waitPanel.activeSelf)
                {
                    UpdateUI();
                }
            }
        }
        
        #endregion
        
        #region UI Control
        
        /// <summary>
        /// Toggle wait menu visibility
        /// </summary>
        public void ToggleWaitMenu()
        {
            if (waitPanel == null) return;
            
            // Can't open if already waiting
            if (Systems.WaitSystem.Instance != null && Systems.WaitSystem.Instance.IsWaiting)
            {
                return;
            }
            
            // Can't open if can't wait
            if (Systems.WaitSystem.Instance != null && !Systems.WaitSystem.Instance.CanWait())
            {
                Debug.Log("WaitUI: Cannot wait right now");
                return;
            }
            
            bool isActive = !waitPanel.activeSelf;
            waitPanel.SetActive(isActive);
            
            if (isActive)
            {
                // Show cursor when menu is open
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                
                UpdateUI();
            }
            else
            {
                // Hide cursor when menu closes (if playing)
                if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
        
        /// <summary>
        /// Show wait progress panel
        /// </summary>
        private void ShowProgressPanel()
        {
            if (progressPanel != null)
            {
                progressPanel.SetActive(true);
            }
        }
        
        /// <summary>
        /// Hide wait progress panel
        /// </summary>
        private void HideProgressPanel()
        {
            if (progressPanel != null)
            {
                progressPanel.SetActive(false);
            }
        }
        
        #endregion
        
        #region UI Updates
        
        /// <summary>
        /// Update all UI elements
        /// </summary>
        private void UpdateUI()
        {
            if (TimeManager.Instance == null) return;
            
            // Update duration text
            if (durationText != null)
            {
                durationText.text = $"Wait for: {_selectedHours:F0} hours";
            }
            
            // Update current time
            if (currentTimeText != null)
            {
                currentTimeText.text = $"Current Time: {TimeManager.Instance.GetTimeString()}";
            }
            
            // Calculate and display target time
            if (targetTimeText != null)
            {
                float targetTime = TimeManager.Instance.CurrentTime + _selectedHours;
                if (targetTime >= 24f)
                {
                    targetTime -= 24f;
                }
                
                int targetHours = Mathf.FloorToInt(targetTime);
                int targetMinutes = Mathf.FloorToInt((targetTime - targetHours) * 60f);
                
                targetTimeText.text = $"Until: {targetHours:00}:{targetMinutes:00}";
            }
        }
        
        /// <summary>
        /// Update wait progress display
        /// </summary>
        private void UpdateProgress()
        {
            if (Systems.WaitSystem.Instance == null || TimeManager.Instance == null) return;
            
            if (progressSlider != null)
            {
                progressSlider.value = Systems.WaitSystem.Instance.GetWaitProgress();
            }
            
            if (progressText != null)
            {
                progressText.text = $"Waiting... {TimeManager.Instance.GetTimeString()}";
            }
        }
        
        #endregion
        
        #region Callbacks
        
        /// <summary>
        /// Called when slider value changes
        /// </summary>
        private void OnSliderValueChanged(float value)
        {
            _selectedHours = Mathf.Round(value); // Round to whole hours
            durationSlider.value = _selectedHours; // Snap to integer
            UpdateUI();
        }
        
        /// <summary>
        /// Called when Wait button is clicked
        /// </summary>
        private void OnWaitButtonClicked()
        {
            if (Systems.WaitSystem.Instance == null) return;
            
            // Start waiting
            Systems.WaitSystem.Instance.StartWait(_selectedHours);
            
            // Hide wait menu
            if (waitPanel != null)
            {
                waitPanel.SetActive(false);
            }
            
            // Show progress panel
            ShowProgressPanel();
            
            // Keep cursor visible during wait
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            Debug.Log($"WaitUI: Started waiting for {_selectedHours} hours");
        }
        
        /// <summary>
        /// Called when Cancel button is clicked
        /// </summary>
        private void OnCancelButtonClicked()
        {
            // Just close the menu
            if (waitPanel != null)
            {
                waitPanel.SetActive(false);
            }
            
            // Restore cursor state
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        
        #endregion
    }
}