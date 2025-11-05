using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Kingdoms.Managers
{
    /// <summary>
    /// Manages fog color and density based on time of day
    /// Works with TimeManager and HDRP Volume system
    /// </summary>
    public class FogManager : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Volume Reference")]
        [Tooltip("Global Volume containing the fog override")]
        [SerializeField] private Volume globalVolume;
        
        [Header("Fog Color Gradient")]
        [Tooltip("Fog tint color throughout the day (0 = midnight, 0.5 = noon)")]
        [SerializeField] private Gradient fogColorGradient;
        
        [Header("Fog Density")]
        [Tooltip("Fog density curve throughout the day")]
        [SerializeField] private AnimationCurve fogDensityCurve;
        
        [Tooltip("Base fog distance (higher = less fog overall)")]
        [Range(10f, 500f)]
        [SerializeField] private float baseFogDensity = 100f;
        
        #endregion
        
        #region Private Fields
        
        private Fog _fog;
        private bool _initialized = false;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            InitializeFog();
        }
        
        private void Update()
        {
            if (!_initialized) return;
            
            // Update fog based on time
            if (TimeManager.Instance != null)
            {
                UpdateFog();
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeFog()
        {
            // Auto-find global volume if not assigned
            if (globalVolume == null)
            {
                globalVolume = FindFirstObjectByType<Volume>();
            }
            
            if (globalVolume == null)
            {
                Debug.LogError("FogManager: No Global Volume found in scene!");
                return;
            }
            
            // Get fog override from volume
            if (!globalVolume.profile.TryGet(out _fog))
            {
                Debug.LogError("FogManager: No Fog override found in Global Volume!");
                return;
            }
            
            // Initialize default gradients if not set
            InitializeDefaultGradients();
            
            _initialized = true;
            Debug.Log("FogManager: Initialized successfully");
        }
        
        private void InitializeDefaultGradients()
        {
            // Default fog color gradient (changes with time of day)
            if (fogColorGradient == null || fogColorGradient.colorKeys.Length == 0)
            {
                fogColorGradient = new Gradient();
                GradientColorKey[] colorKeys = new GradientColorKey[5];
                
                // Midnight - dark blue
                colorKeys[0] = new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0f);
                // Dawn - orange/pink
                colorKeys[1] = new GradientColorKey(new Color(1f, 0.7f, 0.5f), 0.25f);
                // Noon - light blue/white
                colorKeys[2] = new GradientColorKey(new Color(0.9f, 0.95f, 1f), 0.5f);
                // Dusk - orange/red
                colorKeys[3] = new GradientColorKey(new Color(1f, 0.6f, 0.4f), 0.75f);
                // Back to midnight
                colorKeys[4] = new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 1f);
                
                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0] = new GradientAlphaKey(1f, 0f);
                alphaKeys[1] = new GradientAlphaKey(1f, 1f);
                
                fogColorGradient.SetKeys(colorKeys, alphaKeys);
            }
            
            // Default fog density curve (thicker at dawn/dusk/night, thinner at noon)
            if (fogDensityCurve == null || fogDensityCurve.length == 0)
            {
                fogDensityCurve = new AnimationCurve();
                fogDensityCurve.AddKey(0f, 3.0f);    // Midnight - THICK fog
                fogDensityCurve.AddKey(0.25f, 5.0f); // Dawn - VERY THICK fog
                fogDensityCurve.AddKey(0.5f, 0.15f); // Noon - EXTREMELY clear
                fogDensityCurve.AddKey(0.75f, 5.0f); // Dusk - VERY THICK fog
                fogDensityCurve.AddKey(1f, 3.0f);    // Back to midnight - THICK fog
            }
        }
        
        #endregion
        
        #region Fog Update
        
        private void UpdateFog()
        {
            float timeNormalized = TimeManager.Instance.TimeOfDayNormalized;
            
            // Update fog tint color
            Color fogColor = fogColorGradient.Evaluate(timeNormalized);
            _fog.tint.value = fogColor;
            
            // Update fog density
            // CRITICAL: meanFreePath works INVERSELY in HDRP
            // HIGH value = LESS fog (light travels further)
            // LOW value = MORE fog (light absorbed quickly)
            float densityMultiplier = fogDensityCurve.Evaluate(timeNormalized);
            
            // Invert: when we want thick fog (multiplier high), use LOW meanFreePath
            float meanFreePathValue = baseFogDensity / Mathf.Max(densityMultiplier, 0.1f);
            _fog.meanFreePath.value = meanFreePathValue;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Set base fog density
        /// </summary>
        public void SetBaseFogDensity(float density)
        {
            baseFogDensity = Mathf.Clamp(density, 10f, 500f);
        }
        
        /// <summary>
        /// Enable or disable fog
        /// </summary>
        public void SetFogEnabled(bool enabled)
        {
            if (_fog != null)
            {
                _fog.enabled.value = enabled;
            }
        }
        
        #endregion
    }
}
