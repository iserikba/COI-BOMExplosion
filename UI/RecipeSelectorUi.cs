using System;
using Mafi;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Products;
using Mafi.Unity.UiToolkit.Component;
using Mafi.Unity.UiToolkit.Library;
using ProductionCalculator.Core.Calculation;
using ProductionCalculator.Core.Catalog;
using ProductionCalculator.Core.Services;

namespace ProductionCalculator.Ui
{
    internal sealed class RecipeSelectorUi : Column
    {
        private static readonly Px MinRecipeWidth = 480.px();

        private readonly ProductionCalculatorService m_service;
        private readonly Action m_onChanged;

        private ProductProto m_product;
        private ProductionRowFlow m_flow;
        private RecipeProto m_selectedRecipe;

        // 1. CLEANUP: Inheriting the base constructor cleanly and chaining the layout
        public RecipeSelectorUi(ProductionCalculatorService service, Action onChanged) : base()
        {
            this.m_service = service;
            this.m_onChanged = onChanged;

            this.MinWidth(MinRecipeWidth).AlignItemsStart();
        }

        public void SetProduct(ProductProto product, ProductionRowFlow flow)
        {
            this.m_product = product;
            this.m_flow = flow;
            this.refresh();
        }

        public void SetProductAndRecipe(ProductProto product, ProductionRowFlow flow, RecipeProto recipe)
        {
            this.m_product = product;
            this.m_flow = flow;
            this.m_selectedRecipe = recipe;
            this.refresh();
        }

        // 2. CLEANUP: Expression-Bodied Property
        // The decompiler wrote an ugly 4-line Get block. C# lets us do this in one line!
        public RecipeProto SelectedRecipe => this.m_selectedRecipe;

        public void ClearProduct()
        {
            this.m_product = null;
            this.m_selectedRecipe = null;
            this.Clear();
            this.Visible(false);
        }

        public RecipeProto ResolveSelectedRecipe()
        {
            if (this.m_product == null) return null;

            RecipeCatalog catalog = this.m_service.Catalog;

            if (this.m_selectedRecipe != null && catalog.RecipeMatchesProductFlow(this.m_selectedRecipe, this.m_product, this.m_flow))
            {
                return this.m_selectedRecipe;
            }

            ImmutableArray<RecipeProto> recipesForProductFlow = catalog.GetRecipesForProductFlow(this.m_product, this.m_flow);
            if (recipesForProductFlow.IsEmpty)
            {
                this.m_selectedRecipe = null;
                return null;
            }

            this.m_selectedRecipe = recipesForProductFlow[0];
            return this.m_selectedRecipe;
        }

        private void refresh()
        {
            this.Clear();

            if (this.m_product == null)
            {
                this.Visible(false);
                return;
            }

            ImmutableArray<RecipeProto> recipesForProductFlow = this.m_service.Catalog.GetRecipesForProductFlow(this.m_product, this.m_flow);
            if (recipesForProductFlow.IsEmpty)
            {
                this.m_selectedRecipe = null;
                this.Visible(false);
                return;
            }

            this.ResolveSelectedRecipe();
            this.Visible(true);

            // If there's only one way to make it, just draw it (no dropdown needed)
            if (recipesForProductFlow.Length == 1)
            {
                this.Add(RecipePreviewUi.Create(this.m_service.Catalog, recipesForProductFlow[0]));
                return;
            }

            // 3. CLEANUP: Fluent Dropdown Assembly
            // We unrolled the massive decompiler onion into a beautiful, readable chain.
            RecipeProto[] options = recipesForProductFlow.ToArray();

            this.Add(new Dropdown<RecipeProto>(this.createRecipeOption, null, null, false)
                .SetOptions(options)
                .MinWidth(MinRecipeWidth)
                .ObserveValueDropdown(() => this.m_selectedRecipe)
                .OnValueChanged(this.onRecipeChanged));
        }

        private void onRecipeChanged(RecipeProto recipe, int index)
        {
            if (this.m_product == null || recipe == null) return;

            this.m_selectedRecipe = recipe;

            // 4. CLEANUP: Null-Conditional Invocation
            // Replaces the 5-line null check the decompiler wrote.
            this.m_onChanged?.Invoke();
        }

        private UiComponent createRecipeOption(RecipeProto recipe, int index, bool isInDropdown)
        {
            // 5. CLEANUP: Ternary Operator
            // A clean, one-line if/else statement.
            return recipe == null
                ? new Label(Tr.SelectRecipe)
                : RecipePreviewUi.Create(this.m_service.Catalog, recipe);
        }
    }
}