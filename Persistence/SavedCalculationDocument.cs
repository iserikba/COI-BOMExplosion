using System.Collections.Generic;

namespace ProductionCalculator.Core.Persistence
{
    /// <summary>
    /// The root object that gets serialized to disk when a user saves their production setup.
    /// </summary>
    public sealed class SavedCalculationDocument
    {
        // Increment this version if you ever change the data structure in a way 
        // that breaks backward compatibility for old saved files.
        public const int CurrentVersion = 1;

        public int Version { get; set; } = CurrentVersion;

        /// <summary>
        /// A friendly name the user gave to this calculation (e.g., "Steel Smelting Setup").
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The ID of the primary product to show in the UI icon for this saved file.
        /// </summary>
        public string IconProductId { get; set; }

        /// <summary>
        /// The list of target rows that reconstruct the production chain.
        /// </summary>
        public List<SavedTargetRowData> Rows { get; set; } = new List<SavedTargetRowData>();
    }
}