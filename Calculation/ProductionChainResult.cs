using Mafi;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core.Products;

namespace ProductionCalculator.Core.Calculation
{
    /// <summary>
    /// The final result of a production chain calculation.
    /// This holds the entire breakdown of inputs, outputs, and building requirements.
    /// </summary>
    public sealed class ProductionChainResult
    {
        public ImmutableArray<ProductionTarget> Targets { get; }
        public ImmutableArray<RecipeBuildingTotals> Buildings { get; }
        public ImmutableArray<ProductFlowTotals> RawInputs { get; }
        public ImmutableArray<ProductFlowTotals> TotalOutputs { get; }
        public ImmutableArray<ProductFlowTotals> GrossProduction { get; }
        public ImmutableArray<ProductFlowTotals> GrossConsumption { get; }

        // Static factory to represent a cleared calculation
        public static ProductionChainResult Empty { get; } = new ProductionChainResult();

        private ProductionChainResult()
        {
            this.Targets = ImmutableArray<ProductionTarget>.Empty;
            this.Buildings = ImmutableArray<RecipeBuildingTotals>.Empty;
            this.RawInputs = ImmutableArray<ProductFlowTotals>.Empty;
            this.TotalOutputs = ImmutableArray<ProductFlowTotals>.Empty;
            this.GrossProduction = ImmutableArray<ProductFlowTotals>.Empty;
            this.GrossConsumption = ImmutableArray<ProductFlowTotals>.Empty;
        }

        public ProductionChainResult(
            ImmutableArray<ProductionTarget> targets,
            ImmutableArray<RecipeBuildingTotals> buildings,
            ImmutableArray<ProductFlowTotals> rawInputs,
            ImmutableArray<ProductFlowTotals> totalOutputs,
            ImmutableArray<ProductFlowTotals> grossProduction,
            ImmutableArray<ProductFlowTotals> grossConsumption)
        {
            this.Targets = targets;
            this.Buildings = buildings;
            this.RawInputs = rawInputs;
            this.TotalOutputs = totalOutputs;
            this.GrossProduction = grossProduction;
            this.GrossConsumption = grossConsumption;
        }

        /// <summary>
        /// Used by the UI to help users sync their production rates by 
        /// looking up what the current calculation suggests for a product.
        /// </summary>
        public bool TryGetSuggestedRateForFlow(ProductProto product, ProductionRowFlow flow, out Fix32 rate)
        {
            // If the user is defining an Input, we suggest the rate based on Gross Production
            // If the user is defining an Output, we suggest the rate based on Gross Consumption
            var source = (flow == ProductionRowFlow.Input) ? this.GrossProduction : this.GrossConsumption;
            return TryFindRate(source, product, out rate);
        }

        private static bool TryFindRate(ImmutableArray<ProductFlowTotals> flows, ProductProto product, out Fix32 rate)
        {
            foreach (var flow in flows)
            {
                if (flow.Product == product)
                {
                    rate = flow.PerMinute;
                    return true;
                }
            }
            rate = Fix32.Zero;
            return false;
        }
    }
}