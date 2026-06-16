using System;
using System.Collections.Generic;
using System.IO;
using Mafi;
using Mafi.Core.Mods;

namespace ProductionCalculator.Core.Persistence
{
    /// <summary>
    /// Manages the persistence of production chains on the local disk.
    /// Acts as the interface between the game's folder structure and the JSON parser.
    /// </summary>
    public sealed class SavedCalculationRepository
    {
        public const string StorageFolderName = "SavedCalculations";
        private static string s_directory;
        private readonly string m_directory;

        // Static configuration: Called once when the mod initializes
        public static void ConfigureStorage(ModManifest manifest)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));

            s_directory = Path.Combine(manifest.RootDirectoryPath, StorageFolderName);
            Directory.CreateDirectory(s_directory);
        }

        public SavedCalculationRepository()
        {
            if (string.IsNullOrEmpty(s_directory))
                throw new InvalidOperationException("SavedCalculationRepository storage was not configured.");

            this.m_directory = s_directory;
        }

        public string StorageDirectory => this.m_directory;

        /// <summary>
        /// Reads the folder, parses every JSON file, and generates a summary list for the UI.
        /// </summary>
        public IReadOnlyList<SavedCalculationSummary> ListSummaries()
        {
            var summaries = new List<SavedCalculationSummary>();
            if (!Directory.Exists(this.m_directory)) return summaries;

            string[] files = Directory.GetFiles(this.m_directory, "*.json");
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            foreach (string file in files)
            {
                try
                {
                    // Peek at the document without loading the full chain into memory
                    var doc = SavedCalculationJson.ReadFromFile(file);
                    string fileName = Path.GetFileName(file);
                    string name = string.IsNullOrWhiteSpace(doc.Name) ? Path.GetFileNameWithoutExtension(file) : doc.Name;

                    summaries.Add(new SavedCalculationSummary(fileName, name, doc.IconProductId, doc.Rows?.Count ?? 0));
                }
                catch (Exception ex)
                {
                    Log.Warning($"ProductionCalculator: skipped invalid calculation file '{file}': {ex.Message}");
                }
            }
            return summaries;
        }

        public SavedCalculationDocument Load(string fileName)
        {
            var doc = SavedCalculationJson.ReadFromFile(this.GetExistingPath(fileName));
            if (doc.Version <= 0) doc.Version = 1; // Basic migration for legacy files
            return doc;
        }

        public string Save(SavedCalculationDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            document.Version = 1;
            string fileName = SavedCalculationJson.SanitizeFileName(document.Name) + ".json";
            string path = Path.Combine(this.m_directory, fileName);

            SavedCalculationJson.WriteToFile(path, document);
            return fileName;
        }

        public void Delete(string fileName) => File.Delete(this.GetExistingPath(fileName));

        // Ensures we only touch files that exist within our specific directory (Prevents path traversal bugs)
        private string GetExistingPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name required.", nameof(fileName));

            string fullPath = Path.Combine(this.m_directory, Path.GetFileName(fileName));
            if (!File.Exists(fullPath)) throw new FileNotFoundException("Saved calculation not found.", fullPath);

            return fullPath;
        }
    }
}