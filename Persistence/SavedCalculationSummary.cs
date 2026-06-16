namespace ProductionCalculator.Core.Persistence
{
    /// <summary>
    /// A lightweight summary of a saved calculation file. 
    /// Used by the UI to list files without loading the entire calculation into memory.
    /// </summary>
    public sealed class SavedCalculationSummary
    {
        public string FileName { get; }
        public string Name { get; }
        public string IconProductId { get; }
        public int RowCount { get; }

        public SavedCalculationSummary(string fileName, string name, string iconProductId, int rowCount)
        {
            this.FileName = fileName;
            this.Name = name;
            this.IconProductId = iconProductId;
            this.RowCount = rowCount;
        }
    }
}