using Mafi.Collections.ImmutableCollections;
using Mafi.Core.Prototypes;
using ProductionCalculator.Core.Calculation;
using ProductionCalculator.Core.Catalog;

namespace ProductionCalculator.Core.Services
{
    /// <summary>
    /// This service acts as the central hub for the calculator mod.
    /// It provides unified access to the game's prototype database and 
    /// the mod's custom recipe catalog.
    /// </summary>
    public sealed class ProductionCalculatorService
    {
        // We keep these private to ensure they are managed only through this service.
        private readonly ProtosDb m_protosDb;
        private readonly RecipeCatalog m_catalog;

        public ProductionCalculatorService(ProtosDb protosDb)
        {
            this.m_protosDb = protosDb;

            // The catalog is the "BOM Explosion" brain. 
            // We initialize it here once so the UI doesn't have to rebuild it.
            this.m_catalog = new RecipeCatalog(protosDb);
            this.m_catalog.Refresh();
        }

        // C# Expression-Bodied Properties (=>)
        // These are cleaner and more readable than the verbose get { return ... } blocks.
        public ProtosDb ProtosDb => this.m_protosDb;
        public RecipeCatalog Catalog => this.m_catalog;

        /// <summary>
        /// This is the entry point for the "BOM Explosion" calculation.
        /// It takes a list of targets (what you want to build) and 
        /// uses the solver to return the required buildings and raw inputs.
        /// </summary>
        public ProductionChainResult Calculate(ImmutableArray<ProductionTarget> targets)
        {
            return ProductionChainSolver.Solve(this.m_catalog, targets);
        }
    }
}