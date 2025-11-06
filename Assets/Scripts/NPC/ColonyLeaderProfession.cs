using UnityEngine;

namespace Kingdoms.NPC
{
    /// <summary>
    /// Colony Leader profession
    /// Searches for ideal location to establish a new colony
    /// Does not require existing colony
    /// </summary>
    public class ColonyLeaderProfession : NPCProfession
    {
        #region Properties
        
        public override ProfessionType Type => ProfessionType.ColonyLeader;
        public override string DisplayName => "Colony Leader";
        public override bool RequiresColony => false; // Leaders create colonies
        
        #endregion
        
        #region Serialized Fields

        [Header("Colony Leader Settings")]
        [Tooltip("Minimum distance from other colonies")]
        [SerializeField] private float minColonyDistance = 50f;

        [Tooltip("How long to explore before searching (seconds)")]
        [SerializeField] private float explorationDuration = 60f;

        [Tooltip("Minimum distance from spawn point (beach) before settling")]
        [SerializeField] private float minDistanceFromSpawn = 80f;

        [Tooltip("Movement speed while exploring/searching")]
        [SerializeField] private float searchSpeed = 3f;

        [Header("Terrain Preferences")]
        [Tooltip("Ideal slope angle (degrees) - gentle hills")]
        [SerializeField] private float idealSlope = 15f;

        [Tooltip("Maximum acceptable slope (degrees)")]
        [SerializeField] private float maxSlope = 35f;

        [Tooltip("Terrain analysis radius (meters)")]
        [SerializeField] private float analysisRadius = 10f;

        #endregion
        
        #region Private Fields

        private enum LeaderState
        {
            Exploring,      // Wandering away from beach to find general area
            Searching,      // Looking for ideal spot within area
            Evaluating,     // Checking if location is good
            Founding        // Creating colony at chosen spot
        }

        private LeaderState _currentState = LeaderState.Exploring;
        private Vector3 _spawnPosition;
        private Vector3 _evaluationPoint;
        private float _stateTimer = 0f;
        private Vector3 _explorationTarget;

        #endregion
        
        #region Profession Implementation
        
        protected override void OnProfessionStart()
        {
            base.OnProfessionStart();

            // Remember spawn position (beach) to avoid settling too close
            _spawnPosition = transform.position;
            _currentState = LeaderState.Exploring;
            _stateTimer = 0f;

            // Pick initial exploration target away from spawn
            _explorationTarget = GetExplorationTarget();

            Debug.Log($"{gameObject.name}: Colony Leader started exploring (will search after {explorationDuration}s, min distance from spawn: {minDistanceFromSpawn}m)");
        }
        
        protected override void OnProfessionUpdate()
        {
            switch (_currentState)
            {
                case LeaderState.Exploring:
                    UpdateExploring();
                    break;

                case LeaderState.Searching:
                    UpdateSearching();
                    break;

                case LeaderState.Evaluating:
                    UpdateEvaluating();
                    break;

                case LeaderState.Founding:
                    UpdateFounding();
                    break;
            }
        }
        
        #endregion
        
        #region State Logic

        /// <summary>
        /// Explore the map by wandering away from spawn point (beach)
        /// </summary>
        private void UpdateExploring()
        {
            _stateTimer += Time.deltaTime;

            // Move towards exploration target
            bool reached = MoveTowards(_explorationTarget, searchSpeed);

            if (reached)
            {
                // Pick new exploration target
                _explorationTarget = GetExplorationTarget();
            }

            // After exploration time, switch to searching
            float distanceFromSpawn = Vector3.Distance(transform.position, _spawnPosition);
            if (_stateTimer >= explorationDuration && distanceFromSpawn >= minDistanceFromSpawn)
            {
                Debug.Log($"{gameObject.name}: Exploration complete ({distanceFromSpawn:F1}m from spawn), now searching for ideal colony site");
                _currentState = LeaderState.Searching;
                _stateTimer = 0f;
            }
            else if (_stateTimer >= explorationDuration)
            {
                Debug.Log($"{gameObject.name}: Exploration time up but only {distanceFromSpawn:F1}m from spawn (need {minDistanceFromSpawn}m), continuing exploration");
            }
        }

        /// <summary>
        /// Search for ideal colony location
        /// </summary>
        private void UpdateSearching()
        {
            _stateTimer += Time.deltaTime;

            // Pick random evaluation point nearby
            if (_stateTimer >= 5f) // Evaluate every 5 seconds
            {
                _evaluationPoint = GetRandomPointNearby(20f);
                _currentState = LeaderState.Evaluating;
                _stateTimer = 0f;
            }
            else
            {
                // Wander while searching
                Vector3 wanderTarget = GetRandomPointNearby(10f);
                MoveTowards(wanderTarget, searchSpeed);
            }
        }
        
        /// <summary>
        /// Evaluate if current location is good for colony
        /// </summary>
        private void UpdateEvaluating()
        {
            // Move to evaluation point
            bool reached = MoveTowards(_evaluationPoint, searchSpeed);
            
            if (reached)
            {
                // Check if location is suitable
                if (IsLocationSuitable(_evaluationPoint))
                {
                    Debug.Log($"{gameObject.name}: Found suitable colony location!");
                    _currentState = LeaderState.Founding;
                }
                else
                {
                    // Not suitable, keep searching
                    _currentState = LeaderState.Searching;
                }
            }
        }
        
        /// <summary>
        /// Found colony at current location
        /// </summary>
        private void UpdateFounding()
        {
            // TODO: Create colony here when colony system exists
            // For now, just log and stay idle
            
            Debug.Log($"{gameObject.name}: Founding colony at {transform.position}");
            
            // Mark this location as colony center
            // TODO: Spawn colony marker/structure
            
            enabled = false; // Stop updating, colony founded
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Check if location is suitable for colony
        /// Analyzes terrain slope, distance from spawn, and other factors
        /// </summary>
        private bool IsLocationSuitable(Vector3 position)
        {
            float score = 0f;
            int checks = 0;

            // 1. Check distance from spawn (beach) - must be minimum distance
            float distanceFromSpawn = Vector3.Distance(position, _spawnPosition);
            if (distanceFromSpawn < minDistanceFromSpawn)
            {
                Debug.Log($"{gameObject.name}: Location too close to spawn ({distanceFromSpawn:F1}m < {minDistanceFromSpawn}m)");
                return false;
            }

            // Bonus for being further from spawn (up to 2x min distance)
            float spawnDistanceScore = Mathf.Clamp01((distanceFromSpawn - minDistanceFromSpawn) / minDistanceFromSpawn);
            score += spawnDistanceScore * 25f; // Worth 25 points
            checks++;

            // 2. Analyze terrain slope - prefer gentle hills
            float averageSlope = AnalyzeTerrainSlope(position);
            float slopeScore = GetSlopeScore(averageSlope);
            score += slopeScore * 50f; // Worth 50 points (most important)
            checks++;

            Debug.Log($"{gameObject.name}: Location score: {score:F1}/100 (distance: {distanceFromSpawn:F1}m, slope: {averageSlope:F1}°, slopeScore: {slopeScore:F2})");

            // Location is suitable if score is high enough
            return score >= 50f; // Need at least 50/100 points
        }

        /// <summary>
        /// Analyze terrain slope around a position
        /// Returns average slope in degrees
        /// </summary>
        private float AnalyzeTerrainSlope(Vector3 center)
        {
            int sampleCount = 8; // 8 directions
            float totalSlope = 0f;
            int validSamples = 0;

            // Sample terrain in circle around center
            for (int i = 0; i < sampleCount; i++)
            {
                float angle = (i / (float)sampleCount) * 360f * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * analysisRadius;
                Vector3 samplePos = center + offset;

                // Raycast to get terrain height at sample point
                if (Physics.Raycast(samplePos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f, LayerMask.GetMask("Ground")))
                {
                    // Calculate slope between center and sample point
                    float heightDiff = hit.point.y - center.y;
                    float horizontalDist = analysisRadius;
                    float slopeAngle = Mathf.Abs(Mathf.Atan2(heightDiff, horizontalDist) * Mathf.Rad2Deg);

                    totalSlope += slopeAngle;
                    validSamples++;
                }
            }

            if (validSamples == 0) return 999f; // Invalid location

            return totalSlope / validSamples;
        }

        /// <summary>
        /// Get score for a given slope (0-1, 1 being ideal)
        /// </summary>
        private float GetSlopeScore(float slope)
        {
            // Too steep = bad
            if (slope > maxSlope)
            {
                return 0f;
            }

            // Calculate how close to ideal slope
            float slopeDiff = Mathf.Abs(slope - idealSlope);
            float slopeRange = maxSlope - idealSlope;

            // Score decreases as we move away from ideal
            float score = 1f - (slopeDiff / slopeRange);
            return Mathf.Clamp01(score);
        }
        
        /// <summary>
        /// Get exploration target that moves away from spawn point
        /// </summary>
        private Vector3 GetExplorationTarget()
        {
            // Direction away from spawn
            Vector3 awayFromSpawn = (transform.position - _spawnPosition).normalized;

            // Add some randomness to the direction (within 60 degrees cone)
            float randomAngle = Random.Range(-60f, 60f);
            Quaternion rotation = Quaternion.Euler(0f, randomAngle, 0f);
            Vector3 direction = rotation * awayFromSpawn;

            // Target at a good distance
            float targetDistance = Random.Range(30f, 50f);
            Vector3 targetPoint = transform.position + direction * targetDistance;

            // Use raycast to ensure point is on terrain
            if (Physics.Raycast(targetPoint + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f, LayerMask.GetMask("Ground")))
            {
                return hit.point;
            }

            // Fallback: just move in that direction
            return targetPoint;
        }

        /// <summary>
        /// Get random point nearby
        /// </summary>
        private Vector3 GetRandomPointNearby(float radius)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 point = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            // Use raycast to ensure point is on terrain
            if (Physics.Raycast(point + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f, LayerMask.GetMask("Ground")))
            {
                return hit.point;
            }

            return point;
        }
        
        #endregion
        
        #region Debug

        protected override void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            // Show spawn position (beach)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_spawnPosition, 5f);

            // Show minimum distance from spawn
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(_spawnPosition, minDistanceFromSpawn);

            // Show exploration target
            if (_currentState == LeaderState.Exploring)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(_explorationTarget, 3f);
                Gizmos.DrawLine(transform.position, _explorationTarget);
            }

            // Show current evaluation point
            if (_currentState == LeaderState.Evaluating)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_evaluationPoint, 2f);
                Gizmos.DrawLine(transform.position, _evaluationPoint);
            }

            // Show colony location when founding
            if (_currentState == LeaderState.Founding)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, minColonyDistance);
            }
        }

        #endregion
    }
}