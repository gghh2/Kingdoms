using UnityEngine;
using System.Collections;
using Kingdoms.Managers;

namespace Kingdoms.World
{
    /// <summary>
    /// Manages NPC arrival waves
    /// Spawns boats with NPCs every hour (game time)
    /// First wave includes the player
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Wave Settings")]
        [Tooltip("Minimum NPCs per wave")]
        [SerializeField] private int minNPCsPerWave = 10;
        
        [Tooltip("Maximum NPCs per wave")]
        [SerializeField] private int maxNPCsPerWave = 15;
        
        [Tooltip("Time between waves (game hours)")]
        [SerializeField] private float waveCooldownHours = 1f;
        
        [Header("Prefabs")]
        [Tooltip("Boat prefab")]
        [SerializeField] private GameObject boatPrefab;
        
        [Tooltip("NPC prefab")]
        [SerializeField] private GameObject npcPrefab;
        
        [Header("Spawn/Landing Points")]
        [Tooltip("Where boats spawn (far from shore)")]
        [SerializeField] private Transform boatSpawnPoint;
        
        [Tooltip("Where boats land (on shore)")]
        [SerializeField] private Transform boatLandingPoint;
        
        [Header("Player Settings")]
        [Tooltip("Player GameObject")]
        [SerializeField] private GameObject player;
        
        [Tooltip("Enable player on first wave")]
        [SerializeField] private bool playerArrivesOnFirstWave = true;
        
        #endregion
        
        #region Private Fields
        
        private int _currentWave = 0;
        private float _nextWaveTime = 0f;
        private bool _firstWaveSpawned = false;
        private Transform _boatParent;
        
        #endregion
        
        #region Properties
        
        public int CurrentWave => _currentWave;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            // Create parent for boats
            GameObject boatParentObj = new GameObject("Boats");
            _boatParent = boatParentObj.transform;
            
            // Auto-find player if not assigned
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    Debug.Log("WaveManager: Auto-found player");
                }
            }
            
            // Disable player at start if they arrive on first wave
            if (playerArrivesOnFirstWave && player != null)
            {
                player.SetActive(false);
                Debug.Log("WaveManager: Player disabled, waiting for first boat");
            }
            
            // Wait for terrain generation before first wave
            StartCoroutine(WaitForTerrainAndStartWaves());
        }
        
        private void Update()
        {
            if (!_firstWaveSpawned) return;
            
            // Check if it's time for next wave
            if (TimeManager.Instance != null && TimeManager.Instance.CurrentTime >= _nextWaveTime)
            {
                SpawnWave();
            }
        }
        
        #endregion
        
        #region Wave Management
        
        /// <summary>
        /// Wait for terrain generation before spawning first wave
        /// </summary>
        private IEnumerator WaitForTerrainAndStartWaves()
        {
            Debug.Log("WaveManager: Waiting for terrain generation...");
            
            // Wait for WorldManager and terrain
            while (WorldManager.Instance == null || !WorldManager.Instance.IsWorldGenerated)
            {
                yield return new WaitForSeconds(0.5f);
            }
            
            // Extra wait to ensure everything is ready
            yield return new WaitForSeconds(1f);
            
            Debug.Log("WaveManager: Terrain ready, spawning first wave...");
            
            // Spawn first wave (with player)
            SpawnWave(isFirstWave: true);
        }
        
        /// <summary>
        /// Spawn a wave of NPCs on a boat
        /// </summary>
        private void SpawnWave(bool isFirstWave = false)
        {
            if (boatPrefab == null)
            {
                Debug.LogError("WaveManager: No boat prefab assigned!");
                return;
            }
            
            if (npcPrefab == null)
            {
                Debug.LogError("WaveManager: No NPC prefab assigned!");
                return;
            }
            
            if (boatSpawnPoint == null || boatLandingPoint == null)
            {
                Debug.LogError("WaveManager: Boat spawn or landing point not assigned!");
                return;
            }
            
            _currentWave++;
            _firstWaveSpawned = true;
            
            Debug.Log($"WaveManager: Spawning Wave {_currentWave}...");
            
            // Create boat
            GameObject boat = Instantiate(boatPrefab, boatSpawnPoint.position, boatSpawnPoint.rotation, _boatParent);
            boat.name = $"Boat_Wave{_currentWave:00}";
            
            BoatController boatController = boat.GetComponent<BoatController>();
            if (boatController == null)
            {
                boatController = boat.AddComponent<BoatController>();
            }
            
            // Initialize boat
            boatController.Initialize(boatSpawnPoint, boatLandingPoint, isFirstWave);
            
            // Add player to first boat
            if (isFirstWave && playerArrivesOnFirstWave && player != null)
            {
                // Activate player before adding to boat
                player.SetActive(true);
                
                boatController.AddPlayer(player);
                
                // Player controls remain active - they can walk on boat
                Debug.Log("WaveManager: Player can move on boat");
            }
            
            // Determine number of NPCs for this wave
            int npcCount = Random.Range(minNPCsPerWave, maxNPCsPerWave + 1);

            // Get profession assignments for this wave
            // First wave gets guaranteed Colony Leader
            bool guaranteeLeader = (_currentWave == 1);
            var professions = NPC.ProfessionSelector.SelectGroupProfessions(npcCount, forceColonyLeader: guaranteeLeader);

            Debug.Log($"WaveManager: Wave {_currentWave} professions assigned (Colony Leader guaranteed: {guaranteeLeader})");

            // Spawn NPCs and add them to boat
            for (int i = 0; i < npcCount; i++)
            {
                GameObject npc = Instantiate(npcPrefab);
                npc.name = $"NPC_W{_currentWave:00}_{i:00}_{professions[i]}";

                // Assign profession to NPC
                var npcController = npc.GetComponent<NPC.NPCController>();
                if (npcController != null)
                {
                    npcController.AssignProfession(professions[i]);
                    Debug.Log($"WaveManager: {npc.name} assigned profession {professions[i]}");
                }
                else
                {
                    Debug.LogError($"WaveManager: {npc.name} has no NPCController!");
                }

                boatController.AddPassenger(npc);
            }

            Debug.Log($"WaveManager: Wave {_currentWave} created with {npcCount} NPCs");
            
            // Start boat journey
            boatController.StartJourney();
            
            // Schedule next wave
            if (TimeManager.Instance != null)
            {
                _nextWaveTime = TimeManager.Instance.CurrentTime + waveCooldownHours;
                Debug.Log($"WaveManager: Next wave at {_nextWaveTime:F1} hours");
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Force spawn a wave (for testing)
        /// </summary>
        public void ForceSpawnWave()
        {
            SpawnWave();
        }
        
        /// <summary>
        /// Set wave cooldown time
        /// </summary>
        public void SetWaveCooldown(float hours)
        {
            waveCooldownHours = Mathf.Max(0.1f, hours);
        }
        
        #endregion
    }
}