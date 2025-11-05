using UnityEngine;
using Kingdoms.Managers;

namespace Kingdoms.Systems
{
    /// <summary>
    /// Manages the wait/rest system
    /// Allows player to fast-forward time while keeping the world active
    /// Similar to Skyrim's wait system
    /// </summary>
    public class WaitSystem : MonoBehaviour
    {
        #region Singleton
        
        private static WaitSystem _instance;
        
        public static WaitSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("WaitSystem not found in scene!");
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Wait Settings")]
        [Tooltip("Time multiplier during wait (how fast time passes)")]
        [SerializeField] private float waitTimeMultiplier = 300f; // Default: 1 real second = 5 game minutes
        
        [Tooltip("Minimum wait duration in hours")]
        [SerializeField] private float minWaitHours = 1f;
        
        [Tooltip("Maximum wait duration in hours")]
        [SerializeField] private float maxWaitHours = 24f;
        
        [Header("Speed Presets (optional)")]
        [Tooltip("Slow: 60x = 1 real second = 1 game minute")]
        [SerializeField] private float slowSpeed = 60f;
        
        [Tooltip("Normal: 300x = 1 real second = 5 game minutes")]
        [SerializeField] private float normalSpeed = 300f;
        
        [Tooltip("Fast: 600x = 1 real second = 10 game minutes")]
        [SerializeField] private float fastSpeed = 600f;
        
        [Tooltip("Very Fast: 1200x = 1 real second = 20 game minutes")]
        [SerializeField] private float veryFastSpeed = 1200f;
        
        #endregion
        
        #region Private Fields
        
        private bool _isWaiting = false;
        private float _targetEndTime; // Time to wait until (in game hours)
        private float _startTime; // Time when we started waiting
        private float _waitDuration; // How long to wait in hours
        private float _normalTimeScale; // Original time scale to restore
        
        #endregion
        
        #region Properties
        
        public bool IsWaiting => _isWaiting;
        public float MinWaitHours => minWaitHours;
        public float MaxWaitHours => maxWaitHours;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
        }
        
        private void Update()
        {
            if (_isWaiting)
            {
                UpdateWait();
            }
        }
        
        #endregion
        
        #region Wait Logic
        
        /// <summary>
        /// Start waiting for specified duration
        /// </summary>
        /// <param name="hours">Duration in game hours</param>
        public void StartWait(float hours)
        {
            if (_isWaiting)
            {
                Debug.LogWarning("WaitSystem: Already waiting!");
                return;
            }
            
            if (TimeManager.Instance == null)
            {
                Debug.LogError("WaitSystem: TimeManager not found!");
                return;
            }
            
            // Clamp hours
            hours = Mathf.Clamp(hours, minWaitHours, maxWaitHours);
            
            // Save start time and duration
            _startTime = TimeManager.Instance.CurrentTime;
            _waitDuration = hours;
            _targetEndTime = _startTime + hours;
            
            // Handle day overflow (past 24 hours)
            if (_targetEndTime >= 24f)
            {
                _targetEndTime -= 24f;
            }
            
            // Save normal time scale and apply wait multiplier
            _normalTimeScale = TimeManager.Instance.GetTimeScale();
            TimeManager.Instance.SetTimeScale(_normalTimeScale * waitTimeMultiplier);
            
            // Also accelerate physical movements (NPCs, boats)
            TimeManager.Instance.SetMovementMultiplier(waitTimeMultiplier);
            
            _isWaiting = true;
            
            Debug.Log($"WaitSystem: Started waiting from {_startTime:F1}h for {hours}h (target: {_targetEndTime:F1}h)");
            
            // Notify GameManager to change state
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetWaitingState(true);
                Debug.Log($"WaitSystem: GameManager state is now {GameManager.Instance.CurrentState}");
            }
        }
        
        /// <summary>
        /// Update wait progress
        /// </summary>
        private void UpdateWait()
        {
            if (TimeManager.Instance == null)
            {
                StopWait();
                return;
            }
            
            float currentTime = TimeManager.Instance.CurrentTime;
            
            // Calculate elapsed time since start
            float elapsed;
            
            // Handle day transition
            if (_targetEndTime < _startTime)
            {
                // We're waiting past midnight (e.g., start at 23h, target at 2h)
                if (currentTime >= _startTime)
                {
                    // Still same day
                    elapsed = currentTime - _startTime;
                }
                else
                {
                    // Passed midnight
                    elapsed = (24f - _startTime) + currentTime;
                }
            }
            else
            {
                // Normal case: target is later same day
                elapsed = currentTime - _startTime;
            }
            
            // Check if we've waited long enough
            if (elapsed >= _waitDuration)
            {
                Debug.Log($"WaitSystem: Wait complete (elapsed: {elapsed:F1}h / duration: {_waitDuration:F1}h)");
                StopWait();
            }
        }
        
        /// <summary>
        /// Stop waiting (called when finished or cancelled)
        /// </summary>
        public void StopWait()
        {
            if (!_isWaiting) return;
            
            // Restore normal time scale
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.SetTimeScale(_normalTimeScale);
                // Restore normal movement speed
                TimeManager.Instance.SetMovementMultiplier(1f);
            }
            
            _isWaiting = false;
            
            Debug.Log("WaitSystem: Wait finished");
            
            // Notify GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetWaitingState(false);
            }
        }
        
        /// <summary>
        /// Cancel ongoing wait (user pressed ESC)
        /// </summary>
        public void CancelWait()
        {
            if (!_isWaiting) return;
            
            Debug.Log("WaitSystem: Wait cancelled by user");
            StopWait();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Check if player can start waiting
        /// </summary>
        public bool CanWait()
        {
            // Can't wait if already waiting
            if (_isWaiting) return false;
            
            // Can't wait if game is paused
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Paused)
            {
                return false;
            }
            
            // Add more conditions here (combat, etc.)
            
            return true;
        }
        
        /// <summary>
        /// Set wait speed to slow (60x)
        /// </summary>
        public void SetSpeedSlow()
        {
            waitTimeMultiplier = slowSpeed;
            Debug.Log($"WaitSystem: Speed set to Slow ({slowSpeed}x)");
        }
        
        /// <summary>
        /// Set wait speed to normal (300x)
        /// </summary>
        public void SetSpeedNormal()
        {
            waitTimeMultiplier = normalSpeed;
            Debug.Log($"WaitSystem: Speed set to Normal ({normalSpeed}x)");
        }
        
        /// <summary>
        /// Set wait speed to fast (600x)
        /// </summary>
        public void SetSpeedFast()
        {
            waitTimeMultiplier = fastSpeed;
            Debug.Log($"WaitSystem: Speed set to Fast ({fastSpeed}x)");
        }
        
        /// <summary>
        /// Set wait speed to very fast (1200x)
        /// </summary>
        public void SetSpeedVeryFast()
        {
            waitTimeMultiplier = veryFastSpeed;
            Debug.Log($"WaitSystem: Speed set to Very Fast ({veryFastSpeed}x)");
        }
        
        /// <summary>
        /// Set custom wait speed
        /// </summary>
        public void SetCustomSpeed(float multiplier)
        {
            waitTimeMultiplier = Mathf.Max(1f, multiplier);
            Debug.Log($"WaitSystem: Speed set to Custom ({waitTimeMultiplier}x)");
        }
        
        /// <summary>
        /// Get current wait speed multiplier
        /// </summary>
        public float GetCurrentSpeed()
        {
            return waitTimeMultiplier;
        }
        
        /// <summary>
        /// Get current wait progress (0-1)
        /// </summary>
        public float GetWaitProgress()
        {
            if (!_isWaiting || TimeManager.Instance == null) return 0f;
            
            float currentTime = TimeManager.Instance.CurrentTime;
            float elapsed;
            
            // Handle day transition
            if (_targetEndTime < _startTime)
            {
                // Waiting past midnight
                if (currentTime >= _startTime)
                {
                    elapsed = currentTime - _startTime;
                }
                else
                {
                    elapsed = (24f - _startTime) + currentTime;
                }
            }
            else
            {
                // Normal case
                elapsed = currentTime - _startTime;
            }
            
            return Mathf.Clamp01(elapsed / _waitDuration);
        }
        
        #endregion
    }
}