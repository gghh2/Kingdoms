using UnityEngine;

namespace Kingdoms.World
{
    /// <summary>
    /// Generates procedural terrain using Perlin noise
    /// Handles heightmap generation and terrain texturing
    /// </summary>
    public class TerrainGenerator : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Terrain Dimensions")]
        [SerializeField] private int terrainWidth = 512;
        [SerializeField] private int terrainLength = 512;
        [SerializeField] private int terrainHeight = 100;
        
        [Header("Heightmap Settings")]
        [SerializeField] private float scale = 50f;
        [SerializeField] private int octaves = 4;
        [SerializeField] private float persistence = 0.5f;
        [SerializeField] private float lacunarity = 2f;
        [SerializeField] private Vector2 offset = Vector2.zero;
        
        [Header("Terrain Shape")]
        [Tooltip("Controls how mountainous the terrain is. Lower = more flat plains, Higher = more mountains")]
        [Range(0f, 1f)]
        [SerializeField] private float mountainDensity = 0.5f;
        [Tooltip("Scale for mountain distribution. Higher = larger mountain clusters")]
        [SerializeField] private float mountainScale = 100f;
        
        [Header("Terrain Features")]
        [SerializeField] private AnimationCurve heightCurve;
        
        [Header("Water & Edges")]
        [Tooltip("Water level height in world units")]
        [SerializeField] private float waterLevel = 32f;
        
        [Tooltip("Distance from edge where terrain starts flattening towards water level")]
        [SerializeField] private float edgeFalloffDistance = 50f;
        
        [Tooltip("Curve controlling how terrain flattens to water level (0 = edge, 1 = interior)")]
        [SerializeField] private AnimationCurve edgeFalloffCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Terrain Layers - Assign from TerrainSampleAssets")]
        [SerializeField] private TerrainLayer grassLayer;
        [SerializeField] private TerrainLayer stoneLayer;
        [SerializeField] private TerrainLayer sandLayer;
        [SerializeField] private TerrainLayer snowLayer;
        
        [Header("Vegetation - Optional")]
        [Tooltip("Generate grass/vegetation details")]
        [SerializeField] private bool generateVegetation = true;
        
        [Tooltip("Grass texture for detail system (optional - will use default if not set)")]
        [SerializeField] private Texture2D grassTexture;
        
        #endregion
        
        #region Edge Falloff
        
        /// <summary>
        /// Calculate edge falloff factor (0 at edges, 1 in center)
        /// </summary>
        private float CalculateEdgeFalloff(int x, int y, int width, int height)
        {
            // Calculate distance from each edge (in terrain units)
            float distFromLeft = (x / (float)width) * terrainWidth;
            float distFromRight = ((width - x) / (float)width) * terrainWidth;
            float distFromTop = (y / (float)height) * terrainLength;
            float distFromBottom = ((height - y) / (float)height) * terrainLength;
            
            // Get minimum distance to any edge
            float minDistToEdge = Mathf.Min(distFromLeft, distFromRight, distFromTop, distFromBottom);
            
            // If within falloff distance, calculate falloff factor
            if (minDistToEdge < edgeFalloffDistance)
            {
                // Normalize to 0-1 (0 at edge, 1 at falloff distance)
                float normalizedDist = minDistToEdge / edgeFalloffDistance;
                
                // Apply falloff curve
                return edgeFalloffCurve.Evaluate(normalizedDist);
            }
            
            // Beyond falloff distance, no effect (return 1 = full terrain height)
            return 1f;
        }
        
        #endregion
        
        #region Private Fields
        
        private Terrain _terrain;
        private TerrainData _terrainData;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Generate terrain with the current settings
        /// </summary>
        public void GenerateTerrain(int seed)
        {
            Debug.Log("TerrainGenerator: Starting terrain generation...");
            
            // Create or get terrain component
            InitializeTerrain();
            
            // Generate heightmap
            GenerateHeightmap(seed);
            
            // Apply textures
            ApplyTextures();
            
            // Generate vegetation
            if (generateVegetation)
            {
                ApplyVegetation();
            }
            
            Debug.Log("TerrainGenerator: Terrain generation complete!");
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize or create terrain and terrain data
        /// </summary>
        private void InitializeTerrain()
        {
            _terrain = GetComponent<Terrain>();
            
            if (_terrain == null)
            {
                _terrain = gameObject.AddComponent<Terrain>();
            }
            
            // Create new terrain data
            _terrainData = new TerrainData();
            _terrainData.heightmapResolution = terrainWidth + 1;
            _terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);
            
            _terrain.terrainData = _terrainData;
            
            // Add terrain collider
            TerrainCollider collider = GetComponent<TerrainCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<TerrainCollider>();
            }
            collider.terrainData = _terrainData;
            
            Debug.Log("TerrainGenerator: Terrain initialized");
        }
        
        #endregion
        
        #region Heightmap Generation
        
        /// <summary>
        /// Generate heightmap using multi-octave Perlin noise
        /// </summary>
        private void GenerateHeightmap(int seed)
        {
            int width = _terrainData.heightmapResolution;
            int height = _terrainData.heightmapResolution;
            
            float[,] heights = new float[width, height];
            
            // Use seed for random offset
            System.Random prng = new System.Random(seed);
            Vector2[] octaveOffsets = new Vector2[octaves];
            
            for (int i = 0; i < octaves; i++)
            {
                float offsetX = prng.Next(-100000, 100000) + offset.x;
                float offsetY = prng.Next(-100000, 100000) + offset.y;
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
            }
            
            // Generate mountain mask offset
            Vector2 mountainMaskOffset = new Vector2(
                prng.Next(-100000, 100000),
                prng.Next(-100000, 100000)
            );
            
            // Track min/max for normalization
            float maxNoiseHeight = float.MinValue;
            float minNoiseHeight = float.MaxValue;
            
            // Calculate noise for each point
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float amplitude = 1f;
                    float frequency = 1f;
                    float noiseHeight = 0f;
                    
                    // Add multiple octaves for detail
                    for (int i = 0; i < octaves; i++)
                    {
                        float sampleX = (x / scale) * frequency + octaveOffsets[i].x;
                        float sampleY = (y / scale) * frequency + octaveOffsets[i].y;
                        
                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;
                        
                        amplitude *= persistence;
                        frequency *= lacunarity;
                    }
                    
                    // Generate mountain mask (large scale noise)
                    float maskX = (x / mountainScale) + mountainMaskOffset.x;
                    float maskY = (y / mountainScale) + mountainMaskOffset.y;
                    float mountainMask = Mathf.PerlinNoise(maskX, maskY);
                    
                    // Apply mountain density
                    // If mountainMask < threshold, flatten the terrain
                    float threshold = 1f - mountainDensity;
                    if (mountainMask < threshold)
                    {
                        // Flatten this area
                        noiseHeight *= Mathf.Lerp(0.1f, 1f, mountainMask / threshold);
                    }
                    
                    if (noiseHeight > maxNoiseHeight)
                    {
                        maxNoiseHeight = noiseHeight;
                    }
                    if (noiseHeight < minNoiseHeight)
                    {
                        minNoiseHeight = noiseHeight;
                    }
                    
                    heights[x, y] = noiseHeight;
                }
            }
            
            // Normalize heights to 0-1 range and apply edge falloff
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float normalizedHeight = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, heights[x, y]);
                    
                    // Apply height curve if assigned
                    if (heightCurve != null && heightCurve.keys.Length > 0)
                    {
                        normalizedHeight = heightCurve.Evaluate(normalizedHeight);
                    }
                    
                    // Calculate edge falloff
                    float edgeFactor = CalculateEdgeFalloff(x, y, width, height);
                    
                    // Lerp between water level and terrain height based on edge factor
                    float waterLevelNormalized = waterLevel / terrainHeight; // Convert water level to 0-1 range
                    normalizedHeight = Mathf.Lerp(waterLevelNormalized, normalizedHeight, edgeFactor);
                    
                    heights[x, y] = normalizedHeight;
                }
            }
            
            // Apply heights to terrain
            _terrainData.SetHeights(0, 0, heights);
            
            Debug.Log($"TerrainGenerator: Heightmap generated ({width}x{height}) with mountain density {mountainDensity}");
        }
        
        #endregion
        
        #region Texturing
        
        /// <summary>
        /// Apply textures to the terrain
        /// </summary>
        private void ApplyTextures()
        {
            TerrainTexturer texturer = GetComponent<TerrainTexturer>();
            if (texturer == null)
            {
                texturer = gameObject.AddComponent<TerrainTexturer>();
            }
            
            // Pass the terrain layers to the texturer
            texturer.SetTerrainLayers(sandLayer, grassLayer, stoneLayer, snowLayer);
            texturer.ApplyTextures();
        }
        
        #endregion
        
        #region Vegetation
        
        /// <summary>
        /// Apply vegetation details to the terrain
        /// </summary>
        private void ApplyVegetation()
        {
            // FORCE: Remove any existing TerrainDetailGenerator to start fresh
            TerrainDetailGenerator existingGen = GetComponent<TerrainDetailGenerator>();
            if (existingGen != null)
            {
                DestroyImmediate(existingGen);
                Debug.Log("TerrainGenerator: Removed old TerrainDetailGenerator");
            }
            
            // Create new one
            TerrainDetailGenerator detailGen = gameObject.AddComponent<TerrainDetailGenerator>();
            
            // Pass grass texture if assigned
            if (grassTexture != null)
            {
                detailGen.SetGrassTexture(grassTexture);
            }
            
            // Generate grass
            detailGen.GenerateGrass();
        }
        
        #endregion
        
        #region Gizmos
        
        private void OnDrawGizmosSelected()
        {
            // Draw terrain bounds
            Gizmos.color = Color.green;
            Vector3 center = transform.position + new Vector3(terrainWidth / 2f, 0, terrainLength / 2f);
            Vector3 size = new Vector3(terrainWidth, 0, terrainLength);
            Gizmos.DrawWireCube(center, size);
        }
        
        #endregion
    }
}
