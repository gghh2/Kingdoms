using UnityEngine;
using Kingdoms.Managers;
using System.Collections;

namespace Kingdoms.NPC
{
    /// <summary>
    /// Spawns NPCs in the world
    /// Simple spawner for Phase 1 - Waits for terrain generation
    /// </summary>
    public class NPCSpawner : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("NPC Prefab")]
        [Tooltip("NPC prefab to spawn (must have NPCController)")]
        [SerializeField] private GameObject npcPrefab;
        
        [Header("Spawn Settings")]
        [Tooltip("Number of NPCs to spawn")]
        [SerializeField] private int spawnCount = 5;
        
        [Tooltip("Radius around spawner to place NPCs")]
        [SerializeField] private float spawnRadius = 20f;
        
        [Tooltip("Spawn NPCs on Start")]
        [SerializeField] private bool spawnOnStart = true;
        
        [Header("Terrain Detection")]
        [Tooltip("Height to start raycast from (above highest possible terrain)")]
        [SerializeField] private float raycastStartHeight = 100f;
        
        [Tooltip("Maximum raycast distance downward")]
        [SerializeField] private float raycastDistance = 200f;
        
        [Tooltip("Height offset above ground")]
        [SerializeField] private float groundOffset = 0.5f;
        
        [Tooltip("Layer mask for terrain detection")]
        [SerializeField] private LayerMask terrainLayer = -1; // Default: everything
        
        #endregion
        
        #region Private Fields
        
        private Transform _npcParent;
        private bool _hasSpawned = false;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            if (spawnOnStart)
            {
                StartCoroutine(WaitForTerrainAndSpawn());
            }
        }
        
        #endregion
        
        #region Spawning
        
        /// <summary>
        /// Wait for terrain generation before spawning NPCs
        /// </summary>
        private IEnumerator WaitForTerrainAndSpawn()
        {
            Debug.Log("NPCSpawner: Waiting for terrain generation...");
            
            // Wait for WorldManager to exist and terrain to be generated
            while (WorldManager.Instance == null || !WorldManager.Instance.IsWorldGenerated)
            {
                yield return new WaitForSeconds(0.5f);
            }
            
            // Extra frame wait to ensure terrain is fully ready
            yield return new WaitForEndOfFrame();
            
            Debug.Log("NPCSpawner: Terrain ready, spawning NPCs...");
            SpawnNPCs();
        }
        
        /// <summary>
        /// Spawn all NPCs
        /// </summary>
        public void SpawnNPCs()
        {
            if (_hasSpawned)
            {
                Debug.LogWarning("NPCSpawner: NPCs already spawned!");
                return;
            }
            
            if (npcPrefab == null)
            {
                Debug.LogError("NPCSpawner: No NPC prefab assigned!");
                return;
            }
            
            // Create parent for organization
            if (_npcParent == null)
            {
                GameObject parentObj = new GameObject("NPCs");
                _npcParent = parentObj.transform;
            }
            
            // Spawn NPCs
            int successfulSpawns = 0;
            for (int i = 0; i < spawnCount; i++)
            {
                if (SpawnSingleNPC(i))
                {
                    successfulSpawns++;
                }
            }
            
            _hasSpawned = true;
            Debug.Log($"NPCSpawner: Successfully spawned {successfulSpawns}/{spawnCount} NPCs");
        }
        
        /// <summary>
        /// Spawn a single NPC at random position on terrain
        /// </summary>
        /// <returns>True if spawn was successful</returns>
        private bool SpawnSingleNPC(int index)
        {
            // Try to find valid spawn position (with retries)
            Vector3 spawnPosition;
            bool foundValidPosition = false;
            int maxRetries = 10;
            
            for (int retry = 0; retry < maxRetries; retry++)
            {
                if (TryGetTerrainSpawnPosition(out spawnPosition))
                {
                    // Spawn NPC
                    GameObject npc = Instantiate(npcPrefab, spawnPosition, Quaternion.identity, _npcParent);
                    npc.name = $"NPC_{index:00}";
                    
                    // Verify it has NPCController
                    if (npc.GetComponent<NPCController>() == null)
                    {
                        Debug.LogWarning($"NPCSpawner: Spawned NPC {npc.name} doesn't have NPCController!");
                    }
                    
                    foundValidPosition = true;
                    break;
                }
            }
            
            if (!foundValidPosition)
            {
                Debug.LogWarning($"NPCSpawner: Failed to find valid spawn position for NPC_{index:00} after {maxRetries} retries");
            }
            
            return foundValidPosition;
        }
        
        /// <summary>
        /// Try to get a valid spawn position on terrain using raycast
        /// </summary>
        private bool TryGetTerrainSpawnPosition(out Vector3 position)
        {
            // Get random horizontal position
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 horizontalPos = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
            
            // Start raycast from high above
            Vector3 rayStart = new Vector3(horizontalPos.x, transform.position.y + raycastStartHeight, horizontalPos.z);
            
            // Raycast downward to find terrain
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastDistance, terrainLayer))
            {
                // Found terrain, place NPC slightly above ground
                position = hit.point + Vector3.up * groundOffset;
                return true;
            }
            
            // No terrain found
            position = Vector3.zero;
            return false;
        }
        
        /// <summary>
        /// Clear all spawned NPCs
        /// </summary>
        public void ClearNPCs()
        {
            if (_npcParent != null)
            {
                Destroy(_npcParent.gameObject);
                _npcParent = null;
                _hasSpawned = false;
                Debug.Log("NPCSpawner: Cleared all NPCs");
            }
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmosSelected()
        {
            // Show spawn radius
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
            
            // Show raycast start height
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * raycastStartHeight, 2f);
        }
        
        #endregion
    }
}
