using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Kingdoms.Managers
{
    /// <summary>
    /// Manages volumetric cloud movement and animation
    /// Works with HDRP Volumetric Clouds system
    /// </summary>
    public class CloudManager : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Volume Reference")]
        [Tooltip("Global Volume containing the Volumetric Clouds override")]
        [SerializeField] private Volume globalVolume;
        
        [Header("Wind Settings")]
        [Tooltip("Speed multiplier for cloud scrolling")]
        [Range(0f, 5f)]
        [SerializeField] private float scrollSpeed = 1f;
        
        #endregion
        
        #region Private Fields
        
        private VolumetricClouds _volumetricClouds;
        private bool _initialized = false;
        private float _currentScrollValue = 0f;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            InitializeClouds();
        }
        
        private void Update()
        {
            if (!_initialized) return;
            
            UpdateCloudScroll();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeClouds()
        {
            // Auto-find global volume if not assigned
            if (globalVolume == null)
            {
                globalVolume = FindFirstObjectByType<Volume>();
            }
            
            if (globalVolume == null)
            {
                Debug.LogWarning("CloudManager: No Global Volume found in scene!");
                return;
            }
            
            // Get volumetric clouds override from volume
            if (!globalVolume.profile.TryGet(out _volumetricClouds))
            {
                Debug.LogWarning("CloudManager: No Volumetric Clouds override found in Global Volume - clouds will not animate");
                return;
            }
            
            // Try to get initial scroll value
            if (_volumetricClouds.cloudMapSpeedMultiplier.overrideState)
            {
                _currentScrollValue = _volumetricClouds.cloudMapSpeedMultiplier.value;
            }
            
            _initialized = true;
            Debug.Log($"CloudManager: Initialized successfully - Scroll speed: {scrollSpeed}");
            
            // Debug: Log available properties
            Debug.Log("CloudManager: Available properties check:");
            Debug.Log($"- cloudMapSpeedMultiplier exists: {_volumetricClouds.cloudMapSpeedMultiplier != null}");
        }
        
        #endregion
        
        #region Cloud Animation
        
        private void UpdateCloudScroll()
        {
            // Animate the cloud map speed multiplier over time
            _currentScrollValue += scrollSpeed * Time.deltaTime * 0.1f;
            
            // Wrap value to prevent overflow
            if (_currentScrollValue > 100f)
            {
                _currentScrollValue -= 100f;
            }
            
            // Apply to clouds
            _volumetricClouds.cloudMapSpeedMultiplier.value = _currentScrollValue;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Set scroll speed
        /// </summary>
        public void SetScrollSpeed(float speed)
        {
            scrollSpeed = Mathf.Clamp(speed, 0f, 5f);
        }
        
        #endregion
    }
}
