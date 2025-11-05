using UnityEngine;

namespace Kingdoms.Managers
{
    /// <summary>
    /// Manages game time and day/night cycle
    /// Controls sun rotation and lighting based on time of day
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        #region Singleton
        
        private static TimeManager _instance;
        
        public static TimeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("TimeManager not found in scene!");
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Time Settings")]
        [SerializeField] private float timeScale = 60f; // 1 real second = 60 game seconds (1 minute)
        [SerializeField] private float startHour = 6f; // Start at 6 AM
        
        [Header("Day/Night Cycle")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Gradient lightColorGradient;
        [SerializeField] private AnimationCurve lightIntensityCurve;
        [SerializeField] private float maxLightIntensity = 150000f; // HDRP uses Lux (real sun at noon)
        [SerializeField] private float minNightIntensity = 5000f; // Minimum light at night (moon/ambient)
        
        [Header("Moon Settings")]
        [SerializeField] private Light moonLight;
        [SerializeField] private Color moonColor = new Color(0.5f, 0.6f, 0.8f, 1f); // Subtle blue tint
        [SerializeField] private float moonIntensity = 50f; // Very subtle moonlight (was 10000!)
        
        [Header("Sky Settings")]
        [SerializeField] private Material skyboxMaterial;
        [SerializeField] private Gradient skyColorGradient;
        
        #endregion
        
        #region Private Fields
        
        private float _currentTime; // Time in hours (0-24)
        private int _currentDay = 1;
        private float _movementMultiplier = 1f; // Multiplier for physical movements (NPCs, boats)
        
        #endregion
        
        #region Properties
        
        public float CurrentTime => _currentTime;
        public int CurrentDay => _currentDay;
        public float TimeOfDayNormalized => _currentTime / 24f; // 0 to 1
        
        // Readable time
        public int Hours => Mathf.FloorToInt(_currentTime);
        public int Minutes => Mathf.FloorToInt((_currentTime - Hours) * 60f);
        
        // Delta time affected by movement multiplier (for NPCs, boats, etc.)
        public float DeltaTime => Time.deltaTime * _movementMultiplier;
        
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
            
            // Initialize time
            _currentTime = startHour;
        }
        
        private void Start()
        {
            // Auto-find sun if not assigned
            if (sunLight == null)
            {
                sunLight = FindMainDirectionalLight();
            }
            
            // Create or find moon light
            if (moonLight == null)
            {
                moonLight = CreateMoonLight();
            }
            
            // Initialize gradients if not set
            InitializeDefaultGradients();
            
            Debug.Log($"TimeManager: Initialized at Day {_currentDay}, {Hours:00}:{Minutes:00}");
        }
        
        private void Update()
        {
            // Update time if game is playing or waiting
            if (GameManager.Instance != null)
            {
                var state = GameManager.Instance.CurrentState;
                if (state != GameManager.GameState.Playing && state != GameManager.GameState.Waiting)
                {
                    return; // Don't update time if paused or in menu
                }
            }
            
            UpdateTime();
            UpdateLighting();
        }
        
        #endregion
        
        #region Time Logic
        
        /// <summary>
        /// Update the current time
        /// </summary>
        private void UpdateTime()
        {
            // Advance time based on timeScale
            _currentTime += Time.deltaTime * (timeScale / 3600f); // Convert to hours
            
            // Handle day transition
            if (_currentTime >= 24f)
            {
                _currentTime = 0f;
                _currentDay++;
                OnNewDay();
            }
        }
        
        /// <summary>
        /// Called when a new day starts
        /// </summary>
        private void OnNewDay()
        {
            Debug.Log($"TimeManager: New day started - Day {_currentDay}");
            // TODO: Trigger events for new day (NPC routines, quests, etc.)
        }
        
        /// <summary>
        /// Set time manually (for testing or game events)
        /// </summary>
        public void SetTime(float hour)
        {
            _currentTime = Mathf.Clamp(hour, 0f, 23.99f);
            UpdateLighting();
        }
        
        /// <summary>
        /// Set time scale (speed of time passage)
        /// </summary>
        public void SetTimeScale(float scale)
        {
            timeScale = Mathf.Max(0f, scale);
        }
        
        /// <summary>
        /// Get current time scale
        /// </summary>
        public float GetTimeScale()
        {
            return timeScale;
        }
        
        /// <summary>
        /// Set movement multiplier (for wait system acceleration)
        /// </summary>
        public void SetMovementMultiplier(float multiplier)
        {
            _movementMultiplier = Mathf.Max(1f, multiplier);
        }
        
        /// <summary>
        /// Get current movement multiplier
        /// </summary>
        public float GetMovementMultiplier()
        {
            return _movementMultiplier;
        }
        
        #endregion
        
        #region Lighting Logic
        
        /// <summary>
        /// Update sun rotation and lighting based on time
        /// </summary>
        private void UpdateLighting()
        {
            if (sunLight == null) return;
            
            // Ensure light is enabled
            if (!sunLight.enabled)
            {
                sunLight.enabled = true;
            }
            
            // Calculate sun rotation (0 = midnight, 12 = noon)
            // Sun rises at 6 AM (90 degrees) and sets at 6 PM (270 degrees)
            float sunRotation = (_currentTime / 24f) * 360f - 90f;
            sunLight.transform.rotation = Quaternion.Euler(sunRotation, 170f, 0f);
            
            // Update light color and intensity based on time
            float timeNormalized = TimeOfDayNormalized;
            
            if (lightColorGradient != null)
            {
                sunLight.color = lightColorGradient.Evaluate(timeNormalized);
            }
            
            if (lightIntensityCurve != null)
            {
                // HDRP uses Lux values (0-150000), multiply curve value by maxLightIntensity
                float curveValue = lightIntensityCurve.Evaluate(timeNormalized);
                float targetIntensity = curveValue * maxLightIntensity;
                
                // Ensure minimum intensity at night
                targetIntensity = Mathf.Max(targetIntensity, minNightIntensity);
                
                sunLight.intensity = targetIntensity;
            }
            
            // Update moon
            UpdateMoon(timeNormalized);
            
            // Update skybox if material is assigned
            UpdateSkybox(timeNormalized);
        }
        
        /// <summary>
        /// Update skybox color based on time
        /// </summary>
        private void UpdateSkybox(float timeNormalized)
        {
            if (skyboxMaterial == null || skyColorGradient == null) return;
            
            Color skyColor = skyColorGradient.Evaluate(timeNormalized);
            skyboxMaterial.SetColor("_Tint", skyColor);
        }
        
        #endregion
        
        #region Initialization Helpers
        
        /// <summary>
        /// Find the main directional light in the scene
        /// </summary>
        private Light FindMainDirectionalLight()
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    Debug.Log("TimeManager: Auto-assigned Directional Light as sun");
                    return light;
                }
            }
            
            Debug.LogWarning("TimeManager: No Directional Light found in scene!");
            return null;
        }
        
        /// <summary>
        /// Create moon light for nighttime
        /// </summary>
        private Light CreateMoonLight()
        {
            // Check if moon already exists
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light light in lights)
            {
                if (light.name == "MoonLight")
                {
                    Debug.Log("TimeManager: Found existing MoonLight");
                    return light;
                }
            }
            
            // Create new moon light
            GameObject moonObj = new GameObject("MoonLight");
            moonObj.transform.SetParent(transform);
            
            Light moon = moonObj.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = moonColor;
            moon.intensity = moonIntensity;
            moon.shadows = LightShadows.None; // Moon should not cast strong shadows
            
            // Position moon opposite to sun initially
            moonObj.transform.rotation = Quaternion.Euler(90f, 170f, 0f);
            
            Debug.Log("TimeManager: Created MoonLight");
            return moon;
        }
        
        /// <summary>
        /// Update moon visibility and intensity based on time
        /// </summary>
        private void UpdateMoon(float timeNormalized)
        {
            if (moonLight == null) return;
            
            // Moon is visible at night (18:00 - 6:00)
            // timeNormalized: 0 = midnight, 0.25 = 6am, 0.5 = noon, 0.75 = 18:00
            
            bool isNight = _currentTime < 6f || _currentTime >= 18f;
            
            if (isNight)
            {
                moonLight.enabled = true;
                
                // Calculate moon rotation (opposite to sun, shifted by 12 hours)
                float moonRotation = ((_currentTime + 12f) / 24f) * 360f - 90f;
                moonLight.transform.rotation = Quaternion.Euler(moonRotation, 170f, 0f);
                
                // Moon intensity based on time (brightest at midnight)
                float nightProgress;
                if (_currentTime < 6f)
                {
                    // After midnight (0-6)
                    nightProgress = 1f - (_currentTime / 6f); // 1 at midnight, 0 at 6am
                }
                else
                {
                    // Before midnight (18-24)
                    nightProgress = (_currentTime - 18f) / 6f; // 0 at 18:00, 1 at midnight
                }
                
                moonLight.intensity = Mathf.Lerp(moonIntensity * 0.2f, moonIntensity, nightProgress);
            }
            else
            {
                moonLight.enabled = false;
            }
        }
        
        /// <summary>
        /// Initialize default gradients if not set in inspector
        /// </summary>
        private void InitializeDefaultGradients()
        {
            // Default light color gradient (dawn > day > dusk > night)
            if (lightColorGradient == null || lightColorGradient.colorKeys.Length == 0)
            {
                lightColorGradient = new Gradient();
                GradientColorKey[] colorKeys = new GradientColorKey[5];
                colorKeys[0] = new GradientColorKey(new Color(0.3f, 0.3f, 0.5f), 0f);        // Midnight - dark blue
                colorKeys[1] = new GradientColorKey(new Color(1f, 0.6f, 0.4f), 0.25f);       // Dawn - orange
                colorKeys[2] = new GradientColorKey(new Color(1f, 0.96f, 0.9f), 0.5f);       // Noon - warm white (better for HDRP)
                colorKeys[3] = new GradientColorKey(new Color(1f, 0.5f, 0.3f), 0.75f);       // Dusk - orange/red
                colorKeys[4] = new GradientColorKey(new Color(0.3f, 0.3f, 0.5f), 1f);        // Back to midnight
                
                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0] = new GradientAlphaKey(1f, 0f);
                alphaKeys[1] = new GradientAlphaKey(1f, 1f);
                
                lightColorGradient.SetKeys(colorKeys, alphaKeys);
            }
            
            // Default light intensity curve
            if (lightIntensityCurve == null || lightIntensityCurve.length == 0)
            {
                lightIntensityCurve = new AnimationCurve();
                lightIntensityCurve.AddKey(0f, 0.05f);   // Midnight - very dim
                lightIntensityCurve.AddKey(0.25f, 0.6f); // Dawn - getting brighter
                lightIntensityCurve.AddKey(0.5f, 1.0f);  // Noon - bright but not overwhelming (HDRP adjusted)
                lightIntensityCurve.AddKey(0.75f, 0.6f); // Dusk - dimming
                lightIntensityCurve.AddKey(1f, 0.05f);   // Back to midnight
            }
            
            // Default sky color gradient
            if (skyColorGradient == null || skyColorGradient.colorKeys.Length == 0)
            {
                skyColorGradient = new Gradient();
                GradientColorKey[] colorKeys = new GradientColorKey[4];
                colorKeys[0] = new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0f);    // Night sky
                colorKeys[1] = new GradientColorKey(new Color(0.8f, 0.5f, 0.3f), 0.25f); // Dawn
                colorKeys[2] = new GradientColorKey(new Color(0.5f, 0.7f, 1f), 0.5f);    // Day sky
                colorKeys[3] = new GradientColorKey(new Color(0.9f, 0.4f, 0.2f), 0.75f); // Dusk
                
                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0] = new GradientAlphaKey(1f, 0f);
                alphaKeys[1] = new GradientAlphaKey(1f, 1f);
                
                skyColorGradient.SetKeys(colorKeys, alphaKeys);
            }
        }
        
        #endregion
        
        #region Public Utility Methods
        
        /// <summary>
        /// Check if it's currently daytime
        /// </summary>
        public bool IsDaytime()
        {
            return _currentTime >= 6f && _currentTime < 18f;
        }
        
        /// <summary>
        /// Check if it's currently nighttime
        /// </summary>
        public bool IsNighttime()
        {
            return !IsDaytime();
        }
        
        /// <summary>
        /// Get formatted time string
        /// </summary>
        public string GetTimeString()
        {
            return $"{Hours:00}:{Minutes:00}";
        }
        
        /// <summary>
        /// Get formatted day and time string
        /// </summary>
        public string GetFullTimeString()
        {
            return $"Day {_currentDay}, {Hours:00}:{Minutes:00}";
        }
        
        #endregion
    }
}
