using System;
using Mafi;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Products;
using Mafi.Localization;
using Mafi.Unity.Ui;
using Mafi.Unity.UiToolkit.Component;
using Mafi.Unity.UiToolkit.Library;
using ProductionCalculator.Core.Calculation;
using ProductionCalculator.Core.Catalog;

namespace ProductionCalculator.Ui
{
    internal static class RecipePreviewUi
    {
        private static readonly Px ProductIconSize = 28.px();
        private static readonly Px MachineIconSize = 36.px();
        private static readonly Px ArrowIconSize = 34.px();
        private static readonly Px PlusIconSize = 14.px();

        public static UiComponent Create(RecipeCatalog catalog, RecipeProto recipe)
        {
            Row row = new Row(2.pt()).AlignItemsCenterMiddle();

            MachineProto machineForRecipe = catalog.GetMachineForRecipe(recipe);
            if (machineForRecipe != null)
            {
                row.Add(new Icon(machineForRecipe, false, false)
                    .Size(MachineIconSize)
                    .Tooltip(machineForRecipe.Strings.Name, true, false, false));
            }

            row.Add(createInputsRow(recipe));

            // The Arrow Icon
            row.Add(new Icon("Assets/Unity/UserInterface/General/Transform-v2.svg")
                .Size(ArrowIconSize, Px.Auto)
                .MarginLeftRight(1.pt())
                .AlignSelfCenter());

            row.Add(createOutputsRow(recipe));

            return row;
        }

        private static Row createInputsRow(RecipeProto recipe)
        {
            Row row = new Row(1.pt()).AlignItemsCenterMiddle();
            bool needsPlus = false;

            foreach (RecipeInput input in recipe.AllUserVisibleInputs)
            {
                if (shouldIncludeProduct(input.Product))
                {
                    if (needsPlus)
                    {
                        row.Add(createPlusIcon());
                    }
                    needsPlus = true;
                    row.Add(createProductTile(recipe, input.Product, input.Quantity.Value));
                }
            }
            return row;
        }

        private static Row createOutputsRow(RecipeProto recipe)
        {
            Row row = new Row(1.pt()).AlignItemsCenterMiddle();
            bool needsPlus = false;

            foreach (RecipeOutput output in recipe.AllUserVisibleOutputs)
            {
                if (!output.HideInUi && shouldIncludeProduct(output.Product))
                {
                    if (needsPlus)
                    {
                        row.Add(createPlusIcon());
                    }
                    needsPlus = true;
                    row.Add(createProductTile(recipe, output.Product, output.Quantity.Value));
                }
            }
            return row;
        }

        private static Icon createPlusIcon()
        {
            return new Icon("Assets/Unity/UserInterface/General/PlusThin.svg")
                .Size(PlusIconSize)
                .MarginLeftRight(1.pt())
                .AlignSelfCenter();
        }

        private static UiComponent createProductTile(RecipeProto recipe, ProductProto product, int quantityPerCycle)
        {
            LocStrFormatted rateText = RecipeRateCalculator.ToPerMinute(recipe, quantityPerCycle).ToStringRoundedAdaptive(2).AsLoc();

            Column column = new Column(1.pt());

            column.Add(new Icon()
                .Value(product, false)
                .Size(ProductIconSize)
                .OpenCodexOnRightClick(() => product));

            column.Add(new Label(rateText)
                .FontBold()
                .TextCenterMiddle());

            // We safely chain the return here because these methods return the component, not void!
            return column
                .AlignItemsCenterMiddle()
                .MarginRight(1.pt())
                .Tooltip(product.Strings.Name, true, false, false);
        }

        private static bool shouldIncludeProduct(ProductProto product)
        {
            return product != null && !product.IsObsolete && !(product is VirtualProductProto);
        }
    }
}