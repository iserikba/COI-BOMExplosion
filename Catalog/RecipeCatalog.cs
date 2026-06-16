using System;
using System.Collections.Generic;
using System.Linq;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using ProductionCalculator.Core.Calculation;

namespace ProductionCalculator.Core.Catalog
{
    /// <summary>
    /// This catalog maps products to the recipes that consume or produce them.
    /// It effectively turns the game's flat database into a relational web.
    /// </summary>
    public sealed class RecipeCatalog
    {
        private readonly ProtosDb m_protosDb;

        private ImmutableArray<RecipeProto> m_allRecipes = ImmutableArray<RecipeProto>.Empty;
        private Dictionary<ProductProto, ImmutableArray<RecipeProto>> m_recipesByOutput;
        private Dictionary<ProductProto, ImmutableArray<RecipeProto>> m_recipesByInput;
        private Dictionary<RecipeProto, MachineProto> m_machineByRecipe = new Dictionary<RecipeProto, MachineProto>();

        public RecipeCatalog(ProtosDb protosDb)
        {
            this.m_protosDb = protosDb;
        }

        public ImmutableArray<RecipeProto> AllRecipes => this.m_allRecipes;

        /// <summary>
        /// Scans the entire game database and builds lookup tables for instant recipe searching.
        /// </summary>
        public void Refresh()
        {
            var recipeList = new List<RecipeProto>();
            var byOutput = new Dictionary<ProductProto, List<RecipeProto>>();
            var byInput = new Dictionary<ProductProto, List<RecipeProto>>();

            this.m_machineByRecipe = new Dictionary<RecipeProto, MachineProto>();

            // 1. Index Machines: Map every recipe to the machine that runs it.
            foreach (MachineProto machine in this.m_protosDb.All<MachineProto>())
            {
                foreach (RecipeProto recipe in machine.Recipes)
                {
                    if (recipe != null && !this.m_machineByRecipe.ContainsKey(recipe))
                        this.m_machineByRecipe[recipe] = machine;
                }
            }

            // 2. Index Recipes: Build the Input and Output dictionary maps.
            foreach (RecipeProto recipe in this.m_protosDb.All<RecipeProto>())
            {
                if (recipe != null && recipe.Duration.Ticks > 0)
                {
                    recipeList.Add(recipe);
                    this.indexRecipesByOutput(byOutput, recipe);
                    this.indexRecipesByInput(byInput, recipe);
                }
            }

            this.m_allRecipes = recipeList.ToImmutableArray();
            this.m_recipesByOutput = buildLookup(byOutput);
            this.m_recipesByInput = buildLookup(byInput);
        }

        public MachineProto GetMachineForRecipe(RecipeProto recipe) =>
            recipe != null && this.m_machineByRecipe.TryGetValue(recipe, out var machine) ? machine : null;

        public bool IsCraftable(ProductProto product) => this.GetRecipesProducing(product).IsNotEmpty;

        // --- LOOKUP METHODS ---

        public bool RecipeMatchesProductFlow(RecipeProto recipe, ProductProto product, ProductionRowFlow flow) =>
            flow == ProductionRowFlow.Output
                ? this.RecipeProducesProduct(recipe, product)
                : this.RecipeConsumesProduct(recipe, product);

        public ImmutableArray<RecipeProto> GetRecipesForProductFlow(ProductProto product, ProductionRowFlow flow) =>
            flow == ProductionRowFlow.Output
                ? this.GetRecipesProducing(product)
                : this.GetRecipesConsuming(product);

        public ImmutableArray<RecipeProto> GetRecipesProducing(ProductProto product) =>
            product != null && this.m_recipesByOutput.TryGetValue(product, out var result) ? result : ImmutableArray<RecipeProto>.Empty;

        public ImmutableArray<RecipeProto> GetRecipesConsuming(ProductProto product) =>
            product != null && this.m_recipesByInput.TryGetValue(product, out var result) ? result : ImmutableArray<RecipeProto>.Empty;

        public ImmutableArray<ProductProto> GetProductsForFlow(ProductionRowFlow flow)
        {
            var source = (flow == ProductionRowFlow.Output) ? this.m_recipesByOutput : this.m_recipesByInput;
            if (source == null) return ImmutableArray<ProductProto>.Empty;

            return source.Keys
                .Where(shouldIncludeProduct)
                .OrderBy(p => p.Id.Value)
                .ToImmutableArray();
        }

        // --- INDEXING HELPERS ---

        private void indexRecipesByOutput(Dictionary<ProductProto, List<RecipeProto>> index, RecipeProto recipe)
        {
            foreach (var output in recipe.AllUserVisibleOutputs)
                if (!output.HideInUi && shouldIncludeProduct(output.Product))
                    addRecipeToIndex(index, output.Product, recipe);
        }

        private void indexRecipesByInput(Dictionary<ProductProto, List<RecipeProto>> index, RecipeProto recipe)
        {
            foreach (var input in recipe.AllUserVisibleInputs)
                if (!input.HideInUi && shouldIncludeProduct(input.Product))
                    addRecipeToIndex(index, input.Product, recipe);
        }

        private static void addRecipeToIndex(Dictionary<ProductProto, List<RecipeProto>> index, ProductProto product, RecipeProto recipe)
        {
            if (!index.TryGetValue(product, out var list))
            {
                list = new List<RecipeProto>();
                index.Add(product, list);
            }
            if (!list.Contains(recipe)) list.Add(recipe);
        }

        private static Dictionary<ProductProto, ImmutableArray<RecipeProto>> buildLookup(Dictionary<ProductProto, List<RecipeProto>> source)
        {
            return source.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableArray());
        }

        private static bool shouldIncludeProduct(ProductProto product) =>
            product != null && !product.IsObsolete && !(product is VirtualProductProto);

        private bool RecipeProducesProduct(RecipeProto recipe, ProductProto product) =>
            recipe?.AllUserVisibleOutputs.Any(o => !o.HideInUi && o.Product == product) ?? false;

        private bool RecipeConsumesProduct(RecipeProto recipe, ProductProto product) =>
            recipe?.AllUserVisibleInputs.Any(i => !i.HideInUi && i.Product == product) ?? false;
    }
}