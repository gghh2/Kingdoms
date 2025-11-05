using UnityEngine;
using Kingdoms.World;

namespace Kingdoms.Managers
{
    /// <summary>
    /// Manages the game world, terrain generation, and world state
    /// Coordinates terrain generation and world objects
    /// </summary>
    public class WorldManager : MonoBehaviour
    {
        #region Singleton
        
        private static WorldManager _instance;
        
        public static WorldManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("WorldManager not found in scene!");
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Serialized Fields
        
        [Header("World Settings")]
        [SerializeField] private int worldSeed = 12345;
        [SerializeField] private bool useRandomSeed = false;
        
        [Header("Terrain")]
        [SerializeField] private Transform terrainParent;
        [SerializeField] private TerrainGenerator terrainGenerator;
        
        #endregion
        
        #region Private Fields
        
        private bool _isWorldGenerated = false;
        
        #endregion
        
        #region Properties
        
        public int WorldSeed => worldSeed;
        public bool IsWorldGenerated => _isWorldGenerated;
        
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
            
            // Generate random seed if needed
            if (useRandomSeed)
            {
                worldSeed = Random.Range(0, 999999);
                Debug.Log($"WorldManager: Generated random seed: {worldSeed}");
            }
        }
        
        private void Start()
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize world systems
        /// </summary>
        private void Initialize()
        {
            Debug.Log($"WorldManager: Initializing world with seed {worldSeed}...");
            
            // Create terrain parent if not assigned
            if (terrainParent == null)
            {
                GameObject parentObj = new GameObject("World");
                terrainParent = parentObj.transform;
                terrainParent.position = Vector3.zero;
            }
            
            // Generate the world
            GenerateWorld();
        }
        
        #endregion
        
        #region World Generation
        
        /// <summary>
        /// Generate the game world
        /// </summary>
        public void GenerateWorld()
        {
            if (_isWorldGenerated)
            {
                Debug.LogWarning("WorldManager: World already generated!");
                return;
            }
            
            Debug.Log("WorldManager: Starting world generation...");
            
            // Create terrain generator if not assigned
            if (terrainGenerator == null)
            {
                GameObject terrainObj = new GameObject("Terrain");
                terrainObj.transform.parent = terrainParent;
                terrainObj.transform.position = Vector3.zero;
                terrainGenerator = terrainObj.AddComponent<TerrainGenerator>();
            }
            
            // Generate terrain
            terrainGenerator.GenerateTerrain(worldSeed);
            
            _isWorldGenerated = true;
            Debug.Log("WorldManager: World generation complete!");
        }
        
        /// <summary>
        /// Regenerate the world (for testing)
        /// </summary>
        public void RegenerateWorld()
        {
            Debug.Log("WorldManager: Regenerating world...");
            
            // Clear existing terrain
            ClearWorld();
            
            // Generate new world
            _isWorldGenerated = false;
            GenerateWorld();
        }
        
        /// <summary>
        /// Clear the current world
        /// </summary>
        private void ClearWorld()
        {
            if (terrainParent == null) return;
            
            // Destroy all children
            for (int i = terrainParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(terrainParent.GetChild(i).gameObject);
            }
            
            Debug.Log("WorldManager: World cleared");
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Get the terrain parent transform
        /// </summary>
        public Transform GetTerrainParent()
        {
            return terrainParent;
        }
        
        #endregion
    }
}
