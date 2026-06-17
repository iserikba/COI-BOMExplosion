using Mafi;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core.Buildings.Farms;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Products;
using ProductionCalculator.Core.Catalog;
using System;
using System.Collections.Generic;

namespace ProductionCalculator.Core.Calculation
{
    public enum ProductionRowFlow { Output, Input }

    /// <summary>
    /// Represents a specific production goal: "I want to produce/consume X of product Y using recipe Z".
    /// </summary>
    public readonly struct ProductionTarget
    {
        public ProductProto Product { get; }
        public Fix32 RatePerMinute { get; }
        public ProductionRowFlow Flow { get; }
        public RecipeProto Recipe { get; }

        public ProductionTarget(ProductProto product, Fix32 ratePerMinute, ProductionRowFlow flow, RecipeProto recipe)
        {
            Product = product;
            RatePerMinute = ratePerMinute;
            Flow = flow;
            Recipe = recipe;
        }

        public bool IsValid => Product != null && Recipe != null && RatePerMinute > Fix32.Zero;
    }

    internal static class ProductionChainSolver
    {
        private static readonly Fix32 s_minRate = Fix32.FromFloat(0.001f);

        public static ProductionChainResult Solve(RecipeCatalog catalog, ImmutableArray<ProductionTarget> targets)
        {
            if (catalog == null || targets.IsEmpty) return ProductionChainResult.Empty;

            // 1. Group targets by Recipe to aggregate demands efficiently
            var recipeAggregates = new Dictionary<RecipeProto, RecipeDemandAggregate>();
            var validTargets = new List<ProductionTarget>();

            foreach (var target in targets)
            {
                if (!target.IsValid) continue;

                validTargets.Add(target);
                if (!recipeAggregates.TryGetValue(target.Recipe, out var aggregate))
                {
                    aggregate = new RecipeDemandAggregate(target.Recipe);
                    recipeAggregates.Add(target.Recipe, aggregate);
                }
                aggregate.AddTarget(target);
            }

            if (validTargets.Count == 0) return ProductionChainResult.Empty;

            // 2. Solve machine requirements and total flow for every recipe
            var recipeMachineCounts = new Dictionary<RecipeProto, Fix32>();
            var totalProduction = new Dictionary<ProductProto, Fix32>();
            var totalConsumption = new Dictionary<ProductProto, Fix32>();

            foreach (var aggregate in recipeAggregates.Values)
            {
                Fix32 machineCount = aggregate.ComputeMachineCount();
                if (machineCount > s_minRate)
                {
                    recipeMachineCounts[aggregate.Recipe] = machineCount;
                    AccumulateRecipeFlows(aggregate.Recipe, machineCount, totalConsumption, totalProduction);
                }
            }

            // 3. Package results into the result bundle
            return new ProductionChainResult(
                validTargets.ToImmutableArray(),
                ToBuildingTotals(catalog, recipeMachineCounts),
                FlowTotalsAggregator.ToSortedTotals(BuildNetInputs(totalConsumption, totalProduction)),
                BuildNetOutputs(totalProduction, totalConsumption),
                FlowTotalsAggregator.ToSortedTotals(totalProduction),
                FlowTotalsAggregator.ToSortedTotals(totalConsumption)
            );
        }

        private static ImmutableArray<ProductFlowTotals> BuildNetOutputs(Dictionary<ProductProto, Fix32> production, Dictionary<ProductProto, Fix32> consumption)
        {
            var net = new Dictionary<ProductProto, Fix32>();
            foreach (var kvp in production)
            {
                consumption.TryGetValue(kvp.Key, out var consumed);
                var netRate = FlowTotalsAggregator.ComputeNetRate(kvp.Value, consumed);
                if (netRate > Fix32.Zero) net[kvp.Key] = netRate;
            }
            return FlowTotalsAggregator.ToSortedTotals(net);
        }

        private static Dictionary<ProductProto, Fix32> BuildNetInputs(Dictionary<ProductProto, Fix32> consumption, Dictionary<ProductProto, Fix32> production)
        {
            var net = new Dictionary<ProductProto, Fix32>();
            foreach (var kvp in consumption)
            {
                production.TryGetValue(kvp.Key, out var produced);
                var netRate = FlowTotalsAggregator.ComputeNetRate(kvp.Value, produced);
                if (netRate > Fix32.Zero) net[kvp.Key] = netRate;
            }
            return net;
        }

        private static void AccumulateRecipeFlows(RecipeProto recipe, Fix32 machines, Dictionary<ProductProto, Fix32> consumption, Dictionary<ProductProto, Fix32> production)
        {
           // Mafi.Core.Factory.Recipes.RecipeOutput
            foreach (var input in recipe.AllUserVisibleInputs)
            {
                if (!input.HideInUi && ShouldIncludeProduct(input.Product))
                {
                    var rate = RecipeRateCalculator.ToPerMinute(recipe, input.Quantity.Value) * machines;
                    FlowTotalsAggregator.AddRate(consumption, input.Product, rate);
                }
            }
            foreach (var output in recipe.AllUserVisibleOutputs)
            {
                if (!output.HideInUi && ShouldIncludeProduct(output.Product))
                {
                    var rate = RecipeRateCalculator.ToPerMinute(recipe, output.Quantity.Value) * machines;
                    FlowTotalsAggregator.AddRate(production, output.Product, rate);
                }
            }
        }

        private static ImmutableArray<RecipeBuildingTotals> ToBuildingTotals(RecipeCatalog catalog, Dictionary<RecipeProto, Fix32> recipeMachines)
        {
            if (recipeMachines.Count == 0) return ImmutableArray<RecipeBuildingTotals>.Empty;

            var totals = new List<RecipeBuildingTotals>(recipeMachines.Count);

            foreach (var kvp in recipeMachines)
            {
                if (kvp.Value > s_minRate)
                {
                    RecipeProto recipe = kvp.Key;
                    Fix32 count = kvp.Value;

                    // Ask the catalog for both. One will be null, the other will have data.
                    MachineProto machine = catalog.GetMachineForRecipe(recipe);
                    FarmProto farm = catalog.GetFarmForRecipe(recipe);

                    totals.Add(new RecipeBuildingTotals(recipe, machine, farm, count));
                }
            }

            // Sort alphabetically by Recipe ID, then by building count
            totals.Sort((left, right) =>
            {
                int cmp = string.Compare(left.Recipe.Id.Value, right.Recipe.Id.Value, StringComparison.Ordinal);
                return (cmp != 0) ? cmp : right.MachineCount.CompareTo(left.MachineCount);
            });

            return totals.ToImmutableArray();
        }

        private static bool ShouldIncludeProduct(ProductProto product)
            => product != null && !product.IsObsolete && !(product is VirtualProductProto);

        /// <summary>
        /// A helper that groups all demands for a specific recipe and finds 
        /// the "bottleneck" (the maximum number of machines required).
        /// </summary>
        private sealed class RecipeDemandAggregate
        {
            public RecipeProto Recipe { get; }
            private readonly Dictionary<ProductProto, Fix32> m_outputRates = new Dictionary<ProductProto, Fix32>();
            private readonly Dictionary<ProductProto, Fix32> m_inputRates = new Dictionary<ProductProto, Fix32>();

            public RecipeDemandAggregate(RecipeProto recipe) => this.Recipe = recipe;

            public void AddTarget(ProductionTarget target)
            {
                var dict = (target.Flow == ProductionRowFlow.Output) ? m_outputRates : m_inputRates;
                FlowTotalsAggregator.AddRate(dict, target.Product, target.RatePerMinute);
            }

            public Fix32 ComputeMachineCount()
            {
                Fix32 maxMachines = Fix32.Zero;

                // Helper to check bottleneck
                void UpdateMax(Dictionary<ProductProto, Fix32> rates, ProductionRowFlow flow)
                {
                    foreach (var kvp in rates)
                    {
                        var perMachine = RecipeAnchorCalculator.GetAnchorRatePerMachine(Recipe, kvp.Key, flow);
                        if (perMachine > Fix32.Zero)
                        {
                            Fix32 neededForThisProduct = kvp.Value / perMachine;
                            maxMachines = maxMachines.Max(neededForThisProduct);
                        }
                    }
                }

                UpdateMax(m_outputRates, ProductionRowFlow.Output);
                UpdateMax(m_inputRates, ProductionRowFlow.Input);

                return maxMachines;
            }
        }
    }
}