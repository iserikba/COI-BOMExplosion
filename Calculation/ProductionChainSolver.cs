using System;
using System.Collections.Generic;
using Mafi;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Products;
using ProductionCalculator.Core.Catalog;

namespace ProductionCalculator.Core.Calculation
{
    // Token: 0x02000018 RID: 24
    public enum ProductionRowFlow
    {
        Output,
        Input
    }

    public readonly struct ProductionTarget
    {
        // Token: 0x17000023 RID: 35
        // (get) Token: 0x060000CB RID: 203 RVA: 0x00005D4A File Offset: 0x00003F4A
        public ProductProto Product { get; }

        // Token: 0x17000024 RID: 36
        // (get) Token: 0x060000CC RID: 204 RVA: 0x00005D52 File Offset: 0x00003F52
        public Fix32 RatePerMinute { get; }

        // Token: 0x17000025 RID: 37
        // (get) Token: 0x060000CD RID: 205 RVA: 0x00005D5A File Offset: 0x00003F5A
        public ProductionRowFlow Flow { get; }

        // Token: 0x17000026 RID: 38
        // (get) Token: 0x060000CE RID: 206 RVA: 0x00005D62 File Offset: 0x00003F62
        public RecipeProto Recipe { get; }

        // Token: 0x060000CF RID: 207 RVA: 0x00005D6A File Offset: 0x00003F6A
        public ProductionTarget(ProductProto product, Fix32 ratePerMinute, ProductionRowFlow flow, RecipeProto recipe)
        {
            this.Product = product;
            this.RatePerMinute = ratePerMinute;
            this.Flow = flow;
            this.Recipe = recipe;
        }

        // Token: 0x17000027 RID: 39
        // (get) Token: 0x060000D0 RID: 208 RVA: 0x00005D89 File Offset: 0x00003F89
        public bool IsValid
        {
            get
            {
                return this.Product != null && this.Recipe != null && this.RatePerMinute > Fix32.Zero;
            }
        }
    }
    // Token: 0x02000017 RID: 23
    public static class ProductionChainSolver
    {
        // Token: 0x060000C4 RID: 196 RVA: 0x000058F0 File Offset: 0x00003AF0
        public static ProductionChainResult Solve(RecipeCatalog catalog, ImmutableArray<ProductionTarget> targets)
        {
            if (catalog == null || targets.IsEmpty)
            {
                return ProductionChainResult.Empty;
            }
            List<ProductionTarget> list = new List<ProductionTarget>();
            Dictionary<RecipeProto, ProductionChainSolver.RecipeDemandAggregate> dictionary = new Dictionary<RecipeProto, ProductionChainSolver.RecipeDemandAggregate>();
            for (int i = 0; i < targets.Length; i++)
            {
                ProductionTarget productionTarget = targets[i];
                if (productionTarget.IsValid)
                {
                    RecipeProto recipe = productionTarget.Recipe;
                    list.Add(productionTarget);
                    ProductionChainSolver.RecipeDemandAggregate recipeDemandAggregate;
                    if (!dictionary.TryGetValue(recipe, out recipeDemandAggregate))
                    {
                        recipeDemandAggregate = new ProductionChainSolver.RecipeDemandAggregate(recipe);
                        dictionary.Add(recipe, recipeDemandAggregate);
                    }
                    recipeDemandAggregate.AddTarget(productionTarget);
                }
            }
            if (list.Count == 0)
            {
                return ProductionChainResult.Empty;
            }
            Dictionary<RecipeProto, Fix32> dictionary2 = new Dictionary<RecipeProto, Fix32>();
            Dictionary<ProductProto, Fix32> dictionary3 = new Dictionary<ProductProto, Fix32>();
            Dictionary<ProductProto, Fix32> dictionary4 = new Dictionary<ProductProto, Fix32>();
            foreach (KeyValuePair<RecipeProto, ProductionChainSolver.RecipeDemandAggregate> keyValuePair in dictionary)
            {
                ProductionChainSolver.RecipeDemandAggregate value = keyValuePair.Value;
                Fix32 fix = value.ComputeMachineCount();
                if (!(fix <= ProductionChainSolver.s_minRate))
                {
                    dictionary2[value.Recipe] = fix;
                    ProductionChainSolver.accumulateRecipeFlows(value.Recipe, fix, dictionary3, dictionary4);
                }
            }
            return new ProductionChainResult(ImmutableArray.ToImmutableArray<ProductionTarget>(list), ProductionChainSolver.toBuildingTotals(catalog, dictionary2), FlowTotalsAggregator.ToSortedTotals(ProductionChainSolver.buildRequiredInputs(dictionary3, dictionary4)), ProductionChainSolver.buildNetOutputs(dictionary4, dictionary3), FlowTotalsAggregator.ToSortedTotals(dictionary4), FlowTotalsAggregator.ToSortedTotals(dictionary3));
        }

        // Token: 0x060000C5 RID: 197 RVA: 0x00005A50 File Offset: 0x00003C50
        private static ImmutableArray<ProductFlowTotals> buildNetOutputs(Dictionary<ProductProto, Fix32> totalProduction, Dictionary<ProductProto, Fix32> totalConsumption)
        {
            Dictionary<ProductProto, Fix32> dictionary = new Dictionary<ProductProto, Fix32>();
            foreach (KeyValuePair<ProductProto, Fix32> keyValuePair in totalProduction)
            {
                Fix32 subtract = Fix32.Zero;
                Fix32 fix;
                if (totalConsumption.TryGetValue(keyValuePair.Key, out fix))
                {
                    subtract = fix;
                }
                Fix32 fix2 = FlowTotalsAggregator.ComputeNetRate(keyValuePair.Value, subtract);
                if (fix2 > Fix32.Zero)
                {
                    dictionary[keyValuePair.Key] = fix2;
                }
            }
            return FlowTotalsAggregator.ToSortedTotals(dictionary);
        }

        // Token: 0x060000C6 RID: 198 RVA: 0x00005AEC File Offset: 0x00003CEC
        private static Dictionary<ProductProto, Fix32> buildRequiredInputs(Dictionary<ProductProto, Fix32> totalConsumption, Dictionary<ProductProto, Fix32> totalProduction)
        {
            Dictionary<ProductProto, Fix32> dictionary = new Dictionary<ProductProto, Fix32>();
            foreach (KeyValuePair<ProductProto, Fix32> keyValuePair in totalConsumption)
            {
                Fix32 subtract = Fix32.Zero;
                Fix32 fix;
                if (totalProduction.TryGetValue(keyValuePair.Key, out fix))
                {
                    subtract = fix;
                }
                Fix32 fix2 = FlowTotalsAggregator.ComputeNetRate(keyValuePair.Value, subtract);
                if (fix2 > Fix32.Zero)
                {
                    dictionary[keyValuePair.Key] = fix2;
                }
            }
            return dictionary;
        }

        // Token: 0x060000C7 RID: 199 RVA: 0x00005B80 File Offset: 0x00003D80
        private static void accumulateRecipeFlows(RecipeProto recipe, Fix32 machines, Dictionary<ProductProto, Fix32> totalConsumption, Dictionary<ProductProto, Fix32> totalProduction)
        {
            foreach (RecipeInput recipeInput in recipe.AllUserVisibleInputs)
            {
                if (!recipeInput.HideInUi && ProductionChainSolver.shouldIncludeProduct(recipeInput.Product))
                {
                    Fix32 perMinute = RecipeRateCalculator.ToPerMinute(recipe, recipeInput.Quantity.Value) * machines;
                    FlowTotalsAggregator.AddRate(totalConsumption, recipeInput.Product, perMinute);
                }
            }
            foreach (RecipeOutput recipeOutput in recipe.AllUserVisibleOutputs)
            {
                if (!recipeOutput.HideInUi && ProductionChainSolver.shouldIncludeProduct(recipeOutput.Product))
                {
                    Fix32 perMinute2 = RecipeRateCalculator.ToPerMinute(recipe, recipeOutput.Quantity.Value) * machines;
                    FlowTotalsAggregator.AddRate(totalProduction, recipeOutput.Product, perMinute2);
                }
            }
        }

        // Token: 0x060000C8 RID: 200 RVA: 0x00005C50 File Offset: 0x00003E50
        private static ImmutableArray<RecipeBuildingTotals> toBuildingTotals(RecipeCatalog catalog, Dictionary<RecipeProto, Fix32> recipeMachines)
        {
            if (recipeMachines.Count == 0)
            {
                return ImmutableArray<RecipeBuildingTotals>.Empty;
            }
            List<RecipeBuildingTotals> list = new List<RecipeBuildingTotals>(recipeMachines.Count);
            foreach (KeyValuePair<RecipeProto, Fix32> keyValuePair in recipeMachines)
            {
                if (!(keyValuePair.Value <= ProductionChainSolver.s_minRate))
                {
                    list.Add(new RecipeBuildingTotals(keyValuePair.Key, catalog.GetMachineForRecipe(keyValuePair.Key), keyValuePair.Value));
                }
            }
            list.Sort(delegate (RecipeBuildingTotals left, RecipeBuildingTotals right)
            {
                int num = string.Compare(left.Recipe.Id.Value, right.Recipe.Id.Value, StringComparison.Ordinal);
                if (num != 0)
                {
                    return num;
                }
                return right.MachineCount.CompareTo(left.MachineCount);
            });
            return ImmutableArray.ToImmutableArray<RecipeBuildingTotals>(list);
        }

        // Token: 0x060000C9 RID: 201 RVA: 0x00005D18 File Offset: 0x00003F18
        private static bool shouldIncludeProduct(ProductProto product)
        {
            return product != null && !product.IsObsolete && !(product is VirtualProductProto);
        }

        // Token: 0x0400007A RID: 122
        private static readonly Fix32 s_minRate = Fix32.FromFloat(0.001f);

        // Token: 0x02000025 RID: 37
        private sealed class RecipeDemandAggregate
        {
            // Token: 0x1700002D RID: 45
            // (get) Token: 0x0600010D RID: 269 RVA: 0x00006A27 File Offset: 0x00004C27
            public RecipeProto Recipe { get; }

            // Token: 0x0600010E RID: 270 RVA: 0x00006A2F File Offset: 0x00004C2F
            public RecipeDemandAggregate(RecipeProto recipe)
            {
                this.Recipe = recipe;
            }

            // Token: 0x0600010F RID: 271 RVA: 0x00006A54 File Offset: 0x00004C54
            public void AddTarget(ProductionTarget target)
            {
                if (target.Flow == ProductionRowFlow.Output)
                {
                    FlowTotalsAggregator.AddRate(this.m_outputRates, target.Product, target.RatePerMinute);
                    return;
                }
                FlowTotalsAggregator.AddRate(this.m_inputRates, target.Product, target.RatePerMinute);
            }

            // Token: 0x06000110 RID: 272 RVA: 0x00006A94 File Offset: 0x00004C94
            public Fix32 ComputeMachineCount()
            {
                Fix32 fix = Fix32.Zero;
                foreach (KeyValuePair<ProductProto, Fix32> keyValuePair in this.m_outputRates)
                {
                    Fix32 anchorRatePerMachine = RecipeAnchorCalculator.GetAnchorRatePerMachine(this.Recipe, keyValuePair.Key, ProductionRowFlow.Output);
                    if (!(anchorRatePerMachine <= Fix32.Zero))
                    {
                        Fix32 fix2 = keyValuePair.Value / anchorRatePerMachine;
                        if (fix2 > fix)
                        {
                            fix = fix2;
                        }
                    }
                }
                foreach (KeyValuePair<ProductProto, Fix32> keyValuePair2 in this.m_inputRates)
                {
                    Fix32 anchorRatePerMachine2 = RecipeAnchorCalculator.GetAnchorRatePerMachine(this.Recipe, keyValuePair2.Key, ProductionRowFlow.Input);
                    if (!(anchorRatePerMachine2 <= Fix32.Zero))
                    {
                        Fix32 fix3 = keyValuePair2.Value / anchorRatePerMachine2;
                        if (fix3 > fix)
                        {
                            fix = fix3;
                        }
                    }
                }
                return fix;
            }

            // Token: 0x040000A4 RID: 164
            private readonly Dictionary<ProductProto, Fix32> m_outputRates = new Dictionary<ProductProto, Fix32>();

            // Token: 0x040000A5 RID: 165
            private readonly Dictionary<ProductProto, Fix32> m_inputRates = new Dictionary<ProductProto, Fix32>();
        }
    }
}
