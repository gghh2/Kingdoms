using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Kingdoms.NPC
{
    /// <summary>
    /// Handles profession selection with rarity weights
    /// Higher weight = more common profession
    /// </summary>
    public static class ProfessionSelector
    {
        #region Rarity Weights
        
        /// <summary>
        /// Rarity weights for each profession
        /// Higher value = more common
        /// </summary>
        private static readonly Dictionary<ProfessionType, int> ProfessionWeights = new Dictionary<ProfessionType, int>
        {
            { ProfessionType.None, 0 },           // Never selected
            { ProfessionType.ColonyLeader, 5 },   // Very Rare (5% base)
            { ProfessionType.Hunter, 20 },        // Common (20% base)
            { ProfessionType.Blacksmith, 10 },    // Uncommon (10% base)
            { ProfessionType.Miner, 20 },         // Common (20% base)
            { ProfessionType.Baker, 15 },         // Uncommon (15% base)
            { ProfessionType.Lumberjack, 20 },    // Common (20% base)
            { ProfessionType.Builder, 10 }        // Uncommon (10% base)
        };
        
        #endregion
        
        #region Selection Methods
        
        /// <summary>
        /// Select a random profession based on rarity weights
        /// </summary>
        public static ProfessionType SelectRandomProfession()
        {
            // Calculate total weight
            int totalWeight = ProfessionWeights.Values.Sum();
            
            // Get random value
            int randomValue = Random.Range(0, totalWeight);
            
            // Select profession based on weight
            int currentWeight = 0;
            foreach (var kvp in ProfessionWeights)
            {
                currentWeight += kvp.Value;
                if (randomValue < currentWeight)
                {
                    return kvp.Key;
                }
            }
            
            // Fallback (should never happen)
            return ProfessionType.Hunter;
        }
        
        /// <summary>
        /// Select a random profession, excluding certain types
        /// </summary>
        public static ProfessionType SelectRandomProfessionExcluding(params ProfessionType[] excludedTypes)
        {
            // Create filtered weights
            var filteredWeights = ProfessionWeights
                .Where(kvp => !excludedTypes.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            
            // Calculate total weight
            int totalWeight = filteredWeights.Values.Sum();
            
            if (totalWeight == 0)
            {
                Debug.LogWarning("ProfessionSelector: No valid professions after filtering!");
                return ProfessionType.Hunter;
            }
            
            // Get random value
            int randomValue = Random.Range(0, totalWeight);
            
            // Select profession based on weight
            int currentWeight = 0;
            foreach (var kvp in filteredWeights)
            {
                currentWeight += kvp.Value;
                if (randomValue < currentWeight)
                {
                    return kvp.Key;
                }
            }
            
            // Fallback
            return ProfessionType.Hunter;
        }
        
        /// <summary>
        /// Get the weight (rarity) of a profession
        /// </summary>
        public static int GetProfessionWeight(ProfessionType type)
        {
            if (ProfessionWeights.TryGetValue(type, out int weight))
            {
                return weight;
            }
            return 0;
        }
        
        /// <summary>
        /// Get rarity description for a profession
        /// </summary>
        public static string GetRarityDescription(ProfessionType type)
        {
            int weight = GetProfessionWeight(type);
            
            return weight switch
            {
                <= 5 => "Very Rare",
                <= 10 => "Rare",
                <= 15 => "Uncommon",
                <= 20 => "Common",
                _ => "Very Common"
            };
        }
        
        /// <summary>
        /// Select professions for a group, ensuring diversity
        /// Guarantees at least one Colony Leader if forceColonyLeader is true
        /// </summary>
        public static List<ProfessionType> SelectGroupProfessions(int count, bool forceColonyLeader = true)
        {
            List<ProfessionType> professions = new List<ProfessionType>();
            
            // Ensure at least one Colony Leader if forced or spawning multiple NPCs
            if (forceColonyLeader && count > 0)
            {
                professions.Add(ProfessionType.ColonyLeader);
                count--;
            }
            
            // Fill remaining slots with random professions
            for (int i = 0; i < count; i++)
            {
                // Avoid spawning too many Colony Leaders
                ProfessionType profession = SelectRandomProfessionExcluding(ProfessionType.None);
                
                // If we already have 1 Colony Leader, prevent more
                if (professions.Contains(ProfessionType.ColonyLeader) && profession == ProfessionType.ColonyLeader)
                {
                    profession = SelectRandomProfessionExcluding(ProfessionType.None, ProfessionType.ColonyLeader);
                }
                
                professions.Add(profession);
            }
            
            // Shuffle for randomness (except first if we forced Colony Leader)
            if (forceColonyLeader && professions.Count > 1)
            {
                // Keep first as Colony Leader, shuffle the rest
                var firstLeader = professions[0];
                professions.RemoveAt(0);
                professions = professions.OrderBy(x => Random.value).ToList();
                professions.Insert(0, firstLeader);
            }
            else
            {
                professions = professions.OrderBy(x => Random.value).ToList();
            }
            
            return professions;
        }
        
        #endregion
        
        #region Debug Utilities
        
        /// <summary>
        /// Log profession distribution for testing
        /// </summary>
        public static void LogProfessionDistribution(int sampleSize = 1000)
        {
            Dictionary<ProfessionType, int> counts = new Dictionary<ProfessionType, int>();
            
            // Initialize counts
            foreach (var type in ProfessionWeights.Keys)
            {
                counts[type] = 0;
            }
            
            // Generate samples
            for (int i = 0; i < sampleSize; i++)
            {
                ProfessionType profession = SelectRandomProfession();
                counts[profession]++;
            }
            
            // Log results
            Debug.Log($"=== Profession Distribution (Sample: {sampleSize}) ===");
            foreach (var kvp in counts.OrderByDescending(x => x.Value))
            {
                float percentage = (kvp.Value / (float)sampleSize) * 100f;
                string rarity = GetRarityDescription(kvp.Key);
                Debug.Log($"{kvp.Key}: {kvp.Value} ({percentage:F1}%) - {rarity}");
            }
        }
        
        #endregion
    }
}