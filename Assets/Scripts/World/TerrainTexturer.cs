using UnityEngine;

namespace Kingdoms.World
{
    /// <summary>
    /// Handles terrain texturing based on height and slope
    /// Automatically applies terrain layers
    /// </summary>
    public class TerrainTexturer : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Terrain Layers - Assign from Project")]
        [Tooltip("Assign terrain layers from TerrainSampleAssets/TerrainLayers folder")]
        [SerializeField] private TerrainLayer grassLayer;
        [SerializeField] private TerrainLayer stoneLayer;
        [SerializeField] private TerrainLayer sandLayer;
        [SerializeField] private TerrainLayer snowLayer;
        
        [Header("Height Thresholds")]
        [SerializeField] private float sandHeight = 0.2f;
        [SerializeField] private float grassHeight = 0.4f;
        [SerializeField] private float stoneHeight = 0.7f;
        [SerializeField] private float snowHeight = 0.85f;
        
        [Header("Slope Settings")]
        [SerializeField] private float slopeSteepness = 0.5f; // Above this slope, use stone
        
        #endregion
        
        #region Private Fields
        
        private Terrain _terrain;
        private TerrainData _terrainData;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Set terrain layers from external source (e.g. TerrainGenerator)
        /// </summary>
        public void SetTerrainLayers(TerrainLayer sand, TerrainLayer grass, TerrainLayer stone, TerrainLayer snow)
        {
            sandLayer = sand;
            grassLayer = grass;
            stoneLayer = stone;
            snowLayer = snow;
        }
        
        /// <summary>
        /// Apply textures to terrain based on height and slope
        /// </summary>
        public void ApplyTextures()
        {
            _terrain = GetComponent<Terrain>();
            if (_terrain == null)
            {
                Debug.LogError("TerrainTexturer: No Terrain component found!");
                return;
            }
            
            _terrainData = _terrain.terrainData;
            
            Debug.Log("TerrainTexturer: Applying terrain layers...");
            
            // Check if layers are assigned
            if (!ValidateLayers())
            {
                Debug.LogWarning("TerrainTexturer: Some terrain layers are not assigned! Terrain will appear pink/magenta.");
                return;
            }
            
            // Apply layers to terrain
            ApplyTerrainLayers();
            
            // Paint the terrain
            PaintTerrain();
            
            Debug.Log("TerrainTexturer: Texturing complete!");
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Check if all terrain layers are assigned
        /// </summary>
        private bool ValidateLayers()
        {
            bool allAssigned = true;
            
            if (grassLayer == null)
            {
                Debug.LogWarning("TerrainTexturer: Grass layer is not assigned!");
                allAssigned = false;
            }
            
            if (stoneLayer == null)
            {
                Debug.LogWarning("TerrainTexturer: Stone layer is not assigned!");
                allAssigned = false;
            }
            
            if (sandLayer == null)
            {
                Debug.LogWarning("TerrainTexturer: Sand layer is not assigned!");
                allAssigned = false;
            }
            
            if (snowLayer == null)
            {
                Debug.LogWarning("TerrainTexturer: Snow layer is not assigned!");
                allAssigned = false;
            }
            
            // If no layers assigned, create default ones
            if (!allAssigned)
            {
                Debug.Log("TerrainTexturer: Creating default terrain layers...");
                CreateDefaultLayers();
                allAssigned = true;
            }
            
            return allAssigned;
        }
        
        /// <summary>
        /// Create default terrain layers with solid colors (HDRP compatible)
        /// </summary>
        private void CreateDefaultLayers()
        {
            // Create sand layer
            if (sandLayer == null)
            {
                sandLayer = new TerrainLayer();
                sandLayer.diffuseTexture = CreateSolidColorTexture(256, 256, new Color(0.76f, 0.70f, 0.50f)); // Beige/sand
                sandLayer.tileSize = new Vector2(15f, 15f);
                sandLayer.name = "Sand_Default";
                Debug.Log("TerrainTexturer: Created default sand layer");
            }
            
            // Create grass layer
            if (grassLayer == null)
            {
                grassLayer = new TerrainLayer();
                grassLayer.diffuseTexture = CreateSolidColorTexture(256, 256, new Color(0.4f, 0.6f, 0.2f)); // Green
                grassLayer.tileSize = new Vector2(15f, 15f);
                grassLayer.name = "Grass_Default";
                Debug.Log("TerrainTexturer: Created default grass layer");
            }
            
            // Create stone layer
            if (stoneLayer == null)
            {
                stoneLayer = new TerrainLayer();
                stoneLayer.diffuseTexture = CreateSolidColorTexture(256, 256, new Color(0.5f, 0.5f, 0.5f)); // Gray
                stoneLayer.tileSize = new Vector2(15f, 15f);
                stoneLayer.name = "Stone_Default";
                Debug.Log("TerrainTexturer: Created default stone layer");
            }
            
            // Create snow layer
            if (snowLayer == null)
            {
                snowLayer = new TerrainLayer();
                snowLayer.diffuseTexture = CreateSolidColorTexture(256, 256, new Color(0.95f, 0.95f, 0.95f)); // White
                snowLayer.tileSize = new Vector2(15f, 15f);
                snowLayer.name = "Snow_Default";
                Debug.Log("TerrainTexturer: Created default snow layer");
            }
        }
        
        /// <summary>
        /// Create a solid color texture
        /// </summary>
        private Texture2D CreateSolidColorTexture(int width, int height, Color color)
        {
            Texture2D texture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];
            
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return texture;
        }
        
        #endregion
        
        #region Terrain Painting
        
        /// <summary>
        /// Apply terrain layers to terrain data
        /// </summary>
        private void ApplyTerrainLayers()
        {
            TerrainLayer[] layers = new TerrainLayer[4];
            layers[0] = sandLayer;
            layers[1] = grassLayer;
            layers[2] = stoneLayer;
            layers[3] = snowLayer;
            
            _terrainData.terrainLayers = layers;
            
            Debug.Log($"TerrainTexturer: Applied {layers.Length} terrain layers");
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] != null)
                {
                    Debug.Log($"  Layer {i}: {layers[i].name}");
                }
            }
        }
        
        /// <summary>
        /// Paint terrain based on height and slope
        /// </summary>
        private void PaintTerrain()
        {
            int alphamapWidth = _terrainData.alphamapWidth;
            int alphamapHeight = _terrainData.alphamapHeight;
            
            float[,,] splatmapData = new float[alphamapWidth, alphamapHeight, 4];
            
            for (int y = 0; y < alphamapHeight; y++)
            {
                for (int x = 0; x < alphamapWidth; x++)
                {
                    // Normalize coordinates
                    float normX = (float)x / alphamapWidth;
                    float normY = (float)y / alphamapHeight;
                    
                    // Get height at this position (0-1)
                    float height = _terrainData.GetHeight(
                        Mathf.RoundToInt(normY * _terrainData.heightmapResolution),
                        Mathf.RoundToInt(normX * _terrainData.heightmapResolution)
                    ) / _terrainData.size.y;
                    
                    // Get steepness (slope)
                    float steepness = _terrainData.GetSteepness(normY, normX);
                    
                    // Calculate texture weights
                    float[] weights = new float[4];
                    
                    // Steep slopes always get stone
                    if (steepness > slopeSteepness)
                    {
                        weights[2] = 1f; // Stone
                    }
                    else
                    {
                        // Texture based on height
                        if (height < sandHeight)
                        {
                            weights[0] = 1f; // Sand
                        }
                        else if (height < grassHeight)
                        {
                            // Transition from sand to grass
                            float blend = (height - sandHeight) / (grassHeight - sandHeight);
                            weights[0] = 1f - blend; // Sand
                            weights[1] = blend;      // Grass
                        }
                        else if (height < stoneHeight)
                        {
                            weights[1] = 1f; // Grass
                        }
                        else if (height < snowHeight)
                        {
                            // Transition from grass to stone
                            float blend = (height - stoneHeight) / (snowHeight - stoneHeight);
                            weights[1] = 1f - blend; // Grass
                            weights[2] = blend;      // Stone
                        }
                        else
                        {
                            // Transition from stone to snow
                            float blend = (height - snowHeight) / (1f - snowHeight);
                            weights[2] = 1f - blend; // Stone
                            weights[3] = blend;      // Snow
                        }
                    }
                    
                    // Normalize weights
                    float sum = weights[0] + weights[1] + weights[2] + weights[3];
                    for (int i = 0; i < 4; i++)
                    {
                        splatmapData[x, y, i] = weights[i] / sum;
                    }
                }
            }
            
            _terrainData.SetAlphamaps(0, 0, splatmapData);
        }
        
        #endregion
    }
}
