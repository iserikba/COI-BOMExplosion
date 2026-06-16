namespace ProductionCalculator.Core.Persistence
{
    /// <summary>
    /// Represents a single target row in a production calculation.
    /// This is the exact data structure that gets serialized to JSON 
    /// and entry ot the production window.
    /// </summary>
    public sealed class SavedTargetRowData
    {
        // Indicates if this is an Input (consumable) or Output (product)
        public string Flow { get; set; }

        // Unique ID from the game's ProtosDb
        public string ProductId { get; set; }

        // Unique ID for the production method (recipe)
        public string RecipeId { get; set; }

        // The target production rate (e.g., 60 per minute)
        public float Rate { get; set; }

        // Number of machines required to meet this rate
        public float Machines { get; set; }

        // Whether this row's values are "locked" to prevent automated sync
        public bool IsFixed { get; set; }
    }
}