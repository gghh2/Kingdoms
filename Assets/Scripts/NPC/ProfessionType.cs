namespace Kingdoms.NPC
{
    /// <summary>
    /// Available NPC professions
    /// Each profession has unique behaviors and goals
    /// </summary>
    public enum ProfessionType
    {
        None,           // No profession assigned yet
        ColonyLeader,   // Finds ideal location and creates colony
        Hunter,         // Hunts animals for food
        Blacksmith,     // Crafts tools and weapons
        Miner,          // Mines resources
        Baker,          // Produces bread and food
        Lumberjack,     // Cuts trees for wood
        Builder         // Constructs buildings
    }
}