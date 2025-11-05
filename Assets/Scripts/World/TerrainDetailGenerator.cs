using UnityEngine;

namespace Kingdoms.World
{
    /// <summary>
    /// Simple terrain detail generator - Version 1
    /// Generates basic grass on terrain
    /// </summary>
    public class TerrainDetailGenerator : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Grass Texture")]
        [Tooltip("Main grass texture for billboard rendering")]
        [SerializeField] private Texture2D grassTexture;
        
        [Header("Density")]
        [Tooltip("How dense the grass is (0-16 per cell)")]
        [Range(1, 16)]
        [SerializeField] private int grassDensity = 6; // Reduced for performance
        
        [Header("Size")]
        [Tooltip("Grass width range")]
        [SerializeField] private float minWidth = 0.3f;
        [SerializeField] private float maxWidth = 0.6f;
        [Tooltip("Grass height range")]
        [SerializeField] private float minHeight = 0.5f;
        [SerializeField] private float maxHeight = 1.2f;
        
        [Header("Placement Rules")]
        [Tooltip("Max slope angle for grass placement")]
        [Range(0f, 90f)]
        [SerializeField] private float maxSlopeAngle = 45f;
        
        [Tooltip("Min terrain height for grass (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float minTerrainHeight = 0.1f;
        
        [Tooltip("Max terrain height for grass (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float maxTerrainHeight = 0.8f;
        
        #endregion
        
        #region Private Fields
        
        private Terrain _terrain;
        private TerrainData _terrainData;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Set grass texture (optional - will use default if not set)
        /// </summary>
        public void SetGrassTexture(Texture2D texture)
        {
            grassTexture = texture;
        }
        
        /// <summary>
        /// Generate grass on the terrain
        /// </summary>
        public void GenerateGrass()
        {
            _terrain = GetComponent<Terrain>();
            if (_terrain == null)
            {
                Debug.LogError("TerrainDetailGenerator: No Terrain component found!");
                return;
            }
            
            _terrainData = _terrain.terrainData;
            
            // CRITICAL: Clear all existing detail layers first
            _terrainData.detailPrototypes = new DetailPrototype[0];
            _terrainData.SetDetailResolution(16, 8); // Reset to minimum
            Debug.Log("TerrainDetailGenerator: Cleared existing detail layers");
            
            Debug.Log("TerrainDetailGenerator: Starting grass generation...");
            
            // Step 1: Create grass prototype
            CreateGrassPrototype();
            
            // Step 2: Paint grass on terrain
            PaintGrass();
            
            Debug.Log("TerrainDetailGenerator: Grass generation complete!");
        }
        
        #endregion
        
        #region Grass Prototype
        
        private void CreateGrassPrototype()
        {
            // HDRP ISSUE: Billboard grass doesn't render properly in HDRP
            // We need to use mesh-based grass instead
            
            // For now, create a simple quad mesh for grass
            GameObject grassMeshObj = CreateGrassMesh();
            
            DetailPrototype prototype = new DetailPrototype();
            prototype.prototype = grassMeshObj;
            prototype.renderMode = DetailRenderMode.Grass;
            prototype.usePrototypeMesh = true;
            
            // Size
            prototype.minWidth = minWidth;
            prototype.maxWidth = maxWidth;
            prototype.minHeight = minHeight;
            prototype.maxHeight = maxHeight;
            
            Debug.Log($"TerrainDetailGenerator: Grass size - Width: {minWidth}-{maxWidth}, Height: {minHeight}-{maxHeight}");
            
            // Colors - use white to preserve material colors
            prototype.healthyColor = Color.white;
            prototype.dryColor = Color.white;
            
            // Noise spread for variation
            prototype.noiseSpread = 0.1f;
            
            // Apply to terrain
            _terrainData.detailPrototypes = new DetailPrototype[] { prototype };
            
            // Set detail resolution (lower = better performance)
            // For HDRP with mesh grass, use lower resolution to avoid too many instances
            int resolution = 256; // Much lower than terrain resolution for performance
            _terrainData.SetDetailResolution(resolution, 8);
            
            Debug.Log($"TerrainDetailGenerator: Grass prototype created (MESH mode for HDRP) with resolution {resolution}");
        }
        
        private GameObject CreateGrassMesh()
        {
            GameObject grassObj = new GameObject("GrassMesh");
            MeshFilter meshFilter = grassObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = grassObj.AddComponent<MeshRenderer>();
            
            // Create simple quad mesh
            Mesh mesh = new Mesh();
            
            // Vertices for a vertical quad
            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-0.25f, 0f, 0f),    // Bottom left
                new Vector3(0.25f, 0f, 0f),     // Bottom right
                new Vector3(-0.25f, 1f, 0f),    // Top left
                new Vector3(0.25f, 1f, 0f)      // Top right
            };
            
            // UVs
            Vector2[] uvs = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            
            // Triangles (two triangles to form a quad)
            int[] triangles = new int[6]
            {
                0, 2, 1,    // First triangle
                2, 3, 1     // Second triangle
            };
            
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            meshFilter.mesh = mesh;
            
            // Try HDRP Unlit shader first (simpler, more reliable)
            Material grassMaterial = null;
            
            // Try Unlit first
            Shader unlitShader = Shader.Find("HDRP/Unlit");
            if (unlitShader != null)
            {
                grassMaterial = new Material(unlitShader);
                Debug.Log("TerrainDetailGenerator: Using HDRP/Unlit shader");
                
                // Set grass color
                Color grassColor = new Color(0.3f, 0.6f, 0.2f, 1f);
                grassMaterial.SetColor("_UnlitColor", grassColor);
                grassMaterial.SetColor("_EmissiveColor", grassColor);
            }
            else
            {
                // Fallback to Lit
                grassMaterial = new Material(Shader.Find("HDRP/Lit"));
                Debug.Log("TerrainDetailGenerator: Using HDRP/Lit shader (fallback)");
                
                // CRITICAL: Must enable keywords for HDRP to render properly
                grassMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                grassMaterial.EnableKeyword("_ENABLE_FOG_ON_TRANSPARENT");
                
                // Set to transparent mode for proper rendering
                grassMaterial.SetFloat("_SurfaceType", 1); // 1 = Transparent
                grassMaterial.SetFloat("_BlendMode", 0); // Alpha
                grassMaterial.SetFloat("_AlphaCutoffEnable", 1);
                grassMaterial.SetFloat("_AlphaCutoff", 0.3f);
                grassMaterial.SetFloat("_ZWrite", 1);
                
                // Set grass color (vibrant green)
                Color grassColor = new Color(0.3f, 0.6f, 0.2f, 1f);
                grassMaterial.SetColor("_BaseColor", grassColor);
                grassMaterial.SetColor("_EmissiveColor", grassColor * 0.5f); // Slight self-illumination
                grassMaterial.SetFloat("_EmissiveIntensity", 0.3f);
                
                Debug.Log($"TerrainDetailGenerator: Set grass color to R:{grassColor.r} G:{grassColor.g} B:{grassColor.b}");
                Debug.Log($"TerrainDetailGenerator: Material shader: {grassMaterial.shader.name}");
            }
            
            // Enable double-sided rendering
            grassMaterial.SetFloat("_DoubleSidedEnable", 1);
            grassMaterial.SetFloat("_CullMode", 0); // 0 = Off (double-sided)
            
            // Set render queue for transparent geometry
            grassMaterial.renderQueue = 3000;
            
            // Apply texture if available
            if (grassTexture != null)
            {
                grassMaterial.SetTexture("_BaseColorMap", grassTexture);
                Debug.Log($"TerrainDetailGenerator: Applied grass texture: {grassTexture.name}");
            }
            
            meshRenderer.material = grassMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            
            Debug.Log("TerrainDetailGenerator: Created grass mesh with HDRP material");
            
            return grassObj;
        }
        
        private Texture2D CreateDefaultGrassTexture()
        {
            int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            
            Color transparent = new Color(0, 0, 0, 0);
            Color grassColor = new Color(0.4f, 0.7f, 0.3f, 1f);
            
            // Create simple grass blade shape
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float centerX = Mathf.Abs((x - size / 2f) / (size / 2f));
                    float centerY = (y / (float)size);
                    
                    // Grass blade gets thinner toward top
                    float width = 0.2f * (1f - centerY * 0.7f);
                    
                    if (centerX < width)
                    {
                        float alpha = 1f - (centerX / width) * 0.2f;
                        Color col = Color.Lerp(grassColor * 0.6f, grassColor, centerY);
                        texture.SetPixel(x, y, new Color(col.r, col.g, col.b, alpha));
                    }
                    else
                    {
                        texture.SetPixel(x, y, transparent);
                    }
                }
            }
            
            texture.Apply();
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            
            return texture;
        }
        
        #endregion
        
        #region Grass Painting
        
        private void PaintGrass()
        {
            int detailWidth = _terrainData.detailWidth;
            int detailHeight = _terrainData.detailHeight;
            
            Debug.Log($"TerrainDetailGenerator: Detail map size: {detailWidth}x{detailHeight}");
            
            // Create detail layer (2D array)
            int[,] detailLayer = new int[detailWidth, detailHeight];
            
            int grassCount = 0;
            
            // Loop through each cell
            for (int y = 0; y < detailHeight; y++)
            {
                for (int x = 0; x < detailWidth; x++)
                {
                    // Convert detail coordinates to terrain coordinates
                    float normX = (float)x / (detailWidth - 1);
                    float normY = (float)y / (detailHeight - 1);
                    
                    // Get terrain height at this position (normalized 0-1)
                    int heightX = Mathf.RoundToInt(normX * (_terrainData.heightmapResolution - 1));
                    int heightY = Mathf.RoundToInt(normY * (_terrainData.heightmapResolution - 1));
                    float terrainHeight = _terrainData.GetHeight(heightY, heightX) / _terrainData.size.y;
                    
                    // Get slope steepness
                    float steepness = _terrainData.GetSteepness(normY, normX);
                    
                    // Check if this position is valid for grass
                    bool isValidForGrass = 
                        terrainHeight >= minTerrainHeight && 
                        terrainHeight <= maxTerrainHeight && 
                        steepness <= maxSlopeAngle;
                    
                    if (isValidForGrass)
                    {
                        detailLayer[x, y] = grassDensity;
                        grassCount++;
                    }
                }
            }
            
            // Apply detail layer to terrain
            _terrainData.SetDetailLayer(0, 0, 0, detailLayer);
            
            // Force terrain to refresh detail rendering
            _terrain.Flush();
            
            float coverage = (grassCount / (float)(detailWidth * detailHeight)) * 100f;
            Debug.Log($"TerrainDetailGenerator: Placed grass on {grassCount} cells ({coverage:F1}% coverage)");
            Debug.Log($"TerrainDetailGenerator: Detail layer applied and terrain flushed");
        }
        
        #endregion
    }
}
