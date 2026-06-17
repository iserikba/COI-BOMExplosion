using Mafi;
using Mafi.Base;
using Mafi.Collections;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core;
using Mafi.Core.Buildings.Farms;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using ProductionCalculator.Core.Calculation;
using System;
using System.Collections.Generic;
using System.Linq;
using static Mafi.Core.Factory.NuclearReactors.NuclearReactor;

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
        private Dictionary<RecipeProto, FarmProto> m_farmByRecipe = new Dictionary<RecipeProto, FarmProto>();

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
            this.m_farmByRecipe = new Dictionary<RecipeProto, FarmProto>();

            // 1. Index Machines: Map every recipe to the machine that runs it.
            foreach (MachineProto machine in this.m_protosDb.All<MachineProto>())
            {
                foreach (RecipeProto recipe in machine.Recipes)
                {
                    if (recipe != null && !this.m_machineByRecipe.ContainsKey(recipe))
                        this.m_machineByRecipe[recipe] = machine;
                }
            }

            // 2. Index Normal Recipes: Build the Input and Output dictionary maps.
            foreach (RecipeProto recipe in this.m_protosDb.All<RecipeProto>())
            {
                if (recipe != null && recipe.Duration.Ticks > 0)
                {
                    recipeList.Add(recipe);
                    this.indexRecipesByOutput(byOutput, recipe);
                    this.indexRecipesByInput(byInput, recipe);
                }
            }

            // 3. Generate our custom farm recipes
            GenerateVirtualFarmRecipes();

            // 4. THE MISSING LINK: Index the newly generated farm recipes
            // We loop through the keys we just created and add them to the main search lists.
            foreach (RecipeProto virtualRecipe in this.m_farmByRecipe.Keys)
            {
                recipeList.Add(virtualRecipe);
                this.indexRecipesByOutput(byOutput, virtualRecipe);
                this.indexRecipesByInput(byInput, virtualRecipe);
            }

            // 5. Finalize the immutable lookup tables
            this.m_allRecipes = recipeList.ToImmutableArray();
            this.m_recipesByOutput = buildLookup(byOutput);
            this.m_recipesByInput = buildLookup(byInput);

            Log.Info($"RecipeCatalog initialized. Total recipes indexed: {this.m_allRecipes.Length}");
        }

        public MachineProto GetMachineForRecipe(RecipeProto recipe) =>
            recipe != null && this.m_machineByRecipe.TryGetValue(recipe, out var machine) ? machine : null;
        public FarmProto GetFarmForRecipe(RecipeProto recipe) =>
            recipe != null && this.m_farmByRecipe.TryGetValue(recipe, out var farm) ? farm : null;

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

    private void GenerateVirtualFarmRecipes()
    {
        // 1. Retrieve the base product templates from the game's database
        var water = this.m_protosDb.GetOrThrow<ProductProto>(IdsCore.Products.CleanWater);
        var fertChem1 = this.m_protosDb.GetOrThrow<ProductProto>(Ids.Products.FertilizerChemical);

        // Fertilizer I restores 2% fertility per 1 unit of quantity.
        // We use Mafi's Fix32 for safe deterministic math.
        Fix32 fert1RestorationPerUnit = 2;

        foreach (CropProto crop in this.m_protosDb.All<CropProto>()) 
        {

                Log.Info($"Crop {crop.ProductProduced.Product.Id} q:{crop.ProductProduced.Quantity} ConsumedWaterPerDay: {crop.ConsumedWaterPerDay.Value.ToString()}" +
                   $" ConsumedFertilityPerDay: {crop.ConsumedFertilityPerDay.ToFloat()}, DaysToGrow {crop.DaysToGrow}");

        }
        foreach (FarmProto farm in this.m_protosDb.All<FarmProto>())
        {
                foreach (CropProto crop in this.m_protosDb.All<CropProto>())
                {
                 
                    // 1. Validate: Skip if the crop produces nothing, or if it requires a greenhouse and this farm isn't one
                    if (crop.ProductProduced.IsEmpty || (!farm.IsGreenhouse && crop.RequiresGreenhouse))
                    {
                        continue;
                    }

                    // 2. Create the unique ID 
                    RecipeProto.ID recipeId = new RecipeProto.ID($"VirtualRecipe_{farm.Id.Value}_{crop.Id.Value}");
                    string sdebug = $"VirtualRecipe_{farm.Id.Value}_{crop.Id.Value}";

                    // SCALE FACTOR: We multiply everything by 100 to save the decimal places!
                    //int scale = 100;
                    //Fix32 fScale = Fix32.FromInt(scale);

                    var inputs = new Lyst<RecipeInput>();

                    // 3. Calculate Water 
                    if (crop.ConsumedWaterPerDay.IsPositive)
                    {
                        Fix32 waterPerDay = crop.GetConsumedWaterPerDay(farm).Value;
                        Fix32 exactWater = waterPerDay * Fix32.FromInt(crop.DaysToGrow);

                        // Scale by 100 and cast to Integer
                        Quantity scaledWater = new Quantity(exactWater.IntegerPart);
                        inputs.Add(new RecipeInput(water, scaledWater));
                        sdebug += $" waterPerDay = {waterPerDay.ToString()} Recipe: {scaledWater.Value}";
                    }

                    // 4. Calculate Fertilizer
                    Percent dailyFertility = crop.GetConsumedFertilityPerDay(farm);
                    if (dailyFertility.IsPositive)
                    {
                        // .ToFix32() already converts 2% to 0.02.
                        Fix32 totalFertDrained = dailyFertility.ToFix32() * Fix32.FromInt(crop.DaysToGrow);

                        // Fertilizer I restores 2% per unit (0.02 in Fix32 math)
                        Fix32 fert1Restoration = Fix32.FromInt(2) / Fix32.FromInt(100);

                        Fix32 exactFert = totalFertDrained / fert1Restoration;

                        // Scale by 100 and cast to Integer
                        Quantity scaledFert = new Quantity(exactFert.IntegerPart);
                        inputs.Add(new RecipeInput(fertChem1, scaledFert));
                        sdebug += $" dailyFertility = {dailyFertility.ToString()} Recipe: {scaledFert.Value}";
                    }

                    // 5. Calculate Output
                    var outputs = new Lyst<RecipeOutput>();
                    ProductQuantity exactYield = crop.GetProductProduced(farm);

                    //int nMonth= crop.DaysToGrow / 30;
                    outputs.Add(new RecipeOutput(exactYield.Product, exactYield.Quantity));
                    sdebug += $" Prod: {exactYield.Quantity } ";
                    // 6. Assemble the Virtual Recipe
                    var virtualRecipe = new RecipeProto(
                        id: recipeId,
                        strings: crop.Strings,
                        //duration: (crop.DaysToGrow * 100).Days(), // CRITICAL: Scale time by 100 so per-minute rates match!
                        duration: crop.DaysToGrow.Days(), 
                        allInputs: inputs.ToImmutableArray(),
                        allOutputs: outputs.ToImmutableArray(),
                        minUtilization: Percent.Hundred
                    );

                    // 7. Inject it into your Catalog dictionary
                    this.m_farmByRecipe[virtualRecipe] = farm;
                    Log.Info(sdebug);
                }
            }
    }

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