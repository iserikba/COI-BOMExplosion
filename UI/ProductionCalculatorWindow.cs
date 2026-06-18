using Mafi;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using Mafi.Localization;
using Mafi.Unity.Ui;
using Mafi.Unity.Ui.Library;
using Mafi.Unity.UiToolkit;
using Mafi.Unity.UiToolkit.Component;
using Mafi.Unity.UiToolkit.Library;
using ProductionCalculator.Core.Calculation;
using ProductionCalculator.Core.Persistence;
using ProductionCalculator.Core.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ProductionCalculator.Ui
{
    public sealed class ProductionCalculatorWindow : Window
    {
        private const string DefaultRateText = "60";
        private static readonly Px IconSize = 36.px();
        private static readonly Px BuildingIconSize = 42.px();

        private readonly ProductionCalculatorService m_service;
        private readonly SaveCalculationDialog m_saveDialog;
        private readonly LoadCalculationsDialog m_loadDialog;
        private readonly ScrollColumn m_resultsScroll;

        private readonly Column m_targetsBody;
        private readonly Column m_buildingsBody;
        private readonly Column m_inputsBody;
        private readonly Column m_outputsBody;

        private readonly Label m_targetsHint;
        private readonly Label m_resultsHint;

        private readonly List<TargetEntry> m_targetEntries = new List<TargetEntry>();
        private ProductionChainResult m_lastResult = ProductionChainResult.Empty;

        private string m_loadedPresetName;
        private string m_loadedPresetIconProductId;

        public ProductionCalculatorWindow(ProductionCalculatorService service, SavedCalculationRepository repository)
            : base(Tr.WindowTitle, false)
        {
            this.m_service = service;

            this.m_saveDialog = new SaveCalculationDialog(repository, service, this.closeDialogs);
            this.m_loadDialog = new LoadCalculationsDialog(repository, service, this.closeDialogs);

            base.WindowSize(1200.px(), 780.px());
            base.MakeMovable();
            base.EnablePinning();

            // --- LEFT SIDE: TARGETS PANEL ---
            PanelWithHeader targetsPanel = new PanelWithHeader(Tr.TargetsTitle);
            this.m_targetsHint = new Label(Tr.NoTargets).MarginBottom(1.pt());
            this.m_targetsBody = new Column(1.pt());

			Row targetButtons = new Row(1.pt())
				.MarginTop(1.pt());
			targetButtons.Add(new ButtonText(Button.General, Tr.AddTarget, this.addTargetRow));
			targetButtons.Add(new ButtonText(Button.General, Tr.SyncChainRates, this.syncChainRates));
 

            targetsPanel.Body.Add(this.m_targetsHint, this.m_targetsBody, targetButtons);

            // --- RIGHT SIDE: RESULTS PANELS ---
            PanelWithHeader buildingsPanel = new PanelWithHeader(Tr.BuildingsTitle);
            buildingsPanel.Body.Add(this.m_buildingsBody = new Column(1.pt()));

            PanelWithHeader inputsPanel = new PanelWithHeader(Tr.InputsTitle);
            inputsPanel.Body.Add(this.m_inputsBody = new Column(1.pt()));

            PanelWithHeader outputsPanel = new PanelWithHeader(Tr.OutputsTitle);
            outputsPanel.Body.Add(this.m_outputsBody = new Column(1.pt()));

            // --- LAYOUT ASSEMBLY ---
            ScrollColumn leftScroll = new ScrollColumn().AlignItemsStretch().FlexGrow(1f);
            leftScroll.Add(targetsPanel);

            this.m_resultsScroll = new ScrollColumn().AlignItemsStretch().FlexGrow(1f);
            this.m_resultsHint = new Label(Tr.NoResults).MarginBottom(1.pt());

            this.m_resultsScroll.Add(this.m_resultsHint);
            this.m_resultsScroll.Add(buildingsPanel.MarginBottom(2.pt()));
            this.m_resultsScroll.Add(inputsPanel.MarginBottom(2.pt()));
            this.m_resultsScroll.Add(outputsPanel);

            Column rightSideWrapper = new Column(1.pt()).Width(420.px()).FlexShrink(0f).AlignItemsStretch();
            rightSideWrapper.Add(this.m_resultsScroll);
            rightSideWrapper.Add(this.m_saveDialog.Visible(false).FlexGrow(1f));
            rightSideWrapper.Add(this.m_loadDialog.Visible(false).FlexGrow(1f));

            Row bottomButtons = new Row(1.pt())
                .MarginTop(1.pt())
                .AlignSelfStretch();
			bottomButtons.Add(new ButtonText(Button.General, Tr.SaveCalculation, this.openSaveDialog));
			bottomButtons.Add(new ButtonText(Button.General, Tr.LoadCalculations, this.openLoadDialog));
            rightSideWrapper.Add(bottomButtons);

            Row mainLayout = new Row(2.pt()).AlignItemsStretch().FlexGrow(1f);
            mainLayout.Add(leftScroll);
            mainLayout.Add(rightSideWrapper);

            base.Body.Add(mainLayout);
            this.addTargetRow();
        }

        public void RefreshPreview() => this.runCalculation();

        private void openSaveDialog()
        {
            this.m_loadDialog.Visible(false);
            this.m_resultsScroll.Visible(false);
            this.m_saveDialog.Prepare(this.captureSavedRows, this.getDefaultSaveName(), this.getDefaultSaveIcon(), this.onCalculationSaved);
            this.m_saveDialog.Visible(true);
        }

        private void openLoadDialog()
        {
            this.m_saveDialog.Visible(false);
            this.m_resultsScroll.Visible(false);
            this.m_loadDialog.Prepare(this.applySavedDocument);
            this.m_loadDialog.Visible(true);
        }

        private void closeDialogs()
        {
            this.m_saveDialog.Visible(false);
            this.m_loadDialog.Visible(false);
            this.m_resultsScroll.Visible(true);
        }

        private IReadOnlyList<SavedTargetRowData> captureSavedRows()
        {
            return this.m_targetEntries
                .Select(entry => entry.CaptureRow())
                .Where(row => row != null)
                .ToList();
        }

        private ProductProto getDefaultIconProduct()
        {
            return this.m_targetEntries
                .Select(e => e.GetProduct())
                .FirstOrDefault(p => p != null);
        }

        private string getDefaultSaveName()
        {
            return !string.IsNullOrWhiteSpace(this.m_loadedPresetName) ? this.m_loadedPresetName : "New calculation";
        }

        private ProductProto getDefaultSaveIcon()
        {
            return this.tryResolveProduct(this.m_loadedPresetIconProductId) ?? this.getDefaultIconProduct();
        }

        private ProductProto tryResolveProduct(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId)) return null;
            return this.m_service.ProtosDb.TryGetProto(new Proto.ID(productId), out ProductProto result) ? result : null;
        }

        private void onCalculationSaved(string name, ProductProto iconProduct)
        {
            this.m_loadedPresetName = name;
            this.m_loadedPresetIconProductId = iconProduct?.Id.Value;
        }

        private void applySavedDocument(SavedCalculationDocument document)
        {
            if (document?.Rows == null || document.Rows.Count == 0) return;

            this.m_loadedPresetName = document.Name;
            this.m_loadedPresetIconProductId = document.IconProductId;
            this.clearAllTargetRows();

            foreach (var row in document.Rows)
            {
                TargetEntry targetEntry = this.createTargetEntry();
                if (!targetEntry.ApplyFromSaved(row))
                {
                    targetEntry.RemoveFromHierarchy();
                    Log.Warning($"ProductionCalculator: skipped invalid row while loading '{document.Name}'.");
                }
                else
                {
                    this.m_targetEntries.Add(targetEntry);
                }
            }

            if (this.m_targetEntries.Count == 0)
            {
                this.addTargetRow();
                return;
            }

            this.m_targetsHint.Visible(false);
            this.scheduleCalculation();
        }

        private void clearAllTargetRows()
        {
            foreach (var entry in this.m_targetEntries)
            {
                entry.RemoveFromHierarchy();
            }
            this.m_targetEntries.Clear();
        }

        private TargetEntry createTargetEntry()
        {
            return new TargetEntry(
                this.m_service,
                this.m_targetsBody,
                this.removeTargetRow,
                this.scheduleCalculation,
                this.tryGetSuggestedRate);
        }

        private void addTargetRow()
        {
            this.m_targetEntries.Add(this.createTargetEntry());
            this.m_targetsHint.Visible(false);
            this.scheduleCalculation();
        }

        private Fix32? tryGetSuggestedRate(TargetEntry skipEntry, ProductProto product, ProductionRowFlow flow)
        {
            var validTargets = this.m_targetEntries
                .Where(e => e != skipEntry)
                .Select(e => e.TryCreateTarget())
                .Where(t => t.HasValue)
                .Select(t => t.Value)
                .ToList();

            if (validTargets.Count == 0) return null;

            ProductionChainResult result = this.m_service.Calculate(ImmutableArray.ToImmutableArray(validTargets));

            if (result.TryGetSuggestedRateForFlow(product, flow, out Fix32 value))
            {
                return value;
            }
            return null;
        }

        private void syncChainRates()
        {
            if (this.m_targetEntries.Count == 0) return;

            bool changed = true;
            int passes = 0;

            while (passes < this.m_targetEntries.Count && changed)
            {
                changed = false;
                foreach (var entry in this.m_targetEntries)
                {
                    if (entry.TryApplySuggestedRate(true)) changed = true;
                }
                passes++;
            }
            this.scheduleCalculation();
        }

        private void removeTargetRow(TargetEntry entry)
        {
            entry.RemoveFromHierarchy();
            this.m_targetEntries.Remove(entry);
            this.m_targetsHint.Visible(this.m_targetEntries.Count == 0);
            this.scheduleCalculation();
        }

        private void scheduleCalculation() => this.runCalculation();

        private void runCalculation()
        {
            var validTargets = this.m_targetEntries
                .Select(e => e.TryCreateTarget())
                .Where(t => t.HasValue)
                .Select(t => t.Value)
                .ToList();

            if (validTargets.Count == 0)
            {
                this.m_lastResult = ProductionChainResult.Empty;
                this.m_targetsHint.Visible(this.m_targetEntries.Count == 0);
                //this.renderResult();
                this.renderInteractiveResult(); // <-- Updated call
                return;
            }

            this.m_targetsHint.Visible(false);
            this.m_lastResult = this.m_service.Calculate(ImmutableArray.ToImmutableArray(validTargets));
            //this.renderResult();
            this.renderInteractiveResult(); // <-- Updated call
        }

        private void renderResult()
        {
            bool hasData = this.m_lastResult.Buildings.IsNotEmpty || this.m_lastResult.RawInputs.IsNotEmpty || this.m_lastResult.TotalOutputs.IsNotEmpty;
            this.m_resultsHint.Visible(!hasData);

            this.renderBuildings(this.m_lastResult.Buildings);
            this.renderFlows(this.m_inputsBody, this.m_lastResult.RawInputs);
            this.renderFlows(this.m_outputsBody, this.m_lastResult.TotalOutputs);
        }

        private void renderBuildings(ImmutableArray<RecipeBuildingTotals> buildings)
        {
            this.m_buildingsBody.Clear();
            if (buildings.IsEmpty) return;

            foreach (RecipeBuildingTotals entry in buildings)
            {
                this.m_buildingsBody.Add(createBuildingRow(entry));
            }
        }

        private static UiComponent createBuildingRow(RecipeBuildingTotals entry)
        {
            Row row = new Row(2.pt()).AlignItemsCenterMiddle().MarginBottom(1.pt());

            // 1. Draw Machine Icon if it is a machine
            if (entry.Machine != null)
            {
                row.Add(new Icon(entry.Machine, false, false)
                    .Size(BuildingIconSize)
                    .Tooltip(entry.Machine.Strings.Name, true, false, false));
            }
            // 2. Draw Farm Icon if it is a farm
            else if (entry.Farm != null)
            {
                row.Add(new Icon(entry.Farm, false, false)
                    .Size(BuildingIconSize)
                    .Tooltip(entry.Farm.Strings.Name, true, false, false));
            }

            Column column = new Column(0.pt());
            column.Add(new Label(entry.Recipe.Strings.Name).FontBold());

            Row countRow = new Row(1.pt());
            countRow.Add(new Label(formatMachineCount(entry.MachineCount).ToString().AsLoc()).FontBold());

            // Optional: You can customize the label text so it doesn't say "1.8 machines" for farms
            string labelText = entry.Farm != null ? " farms" : Tr.MachineCount.TranslatedString;
            countRow.Add(new Label(labelText.AsLoc()).Opacity(0.8f));

            column.Add(countRow);
            row.Add(column.FlexGrow(1f));

            return row;
        }

        private void renderFlows(Column body, ImmutableArray<ProductFlowTotals> flows)
        {
            body.Clear();
            if (flows.IsEmpty) return;

            Row row = new Row().Wrap(true).PaddingTop(1.pt());
            foreach (ProductFlowTotals entry in flows)
            {
                row.Add(createProductTile(entry));
            }
            body.Add(row);
        }

        private static UiComponent createProductTile(ProductFlowTotals entry)
        {
            Column column = new Column(1.pt());

            column.Add(new Icon()
                .Value(entry.Product, false)
                .Size(IconSize)
                .OpenCodexOnRightClick(() => entry.Product));

            column.Add(new Label(entry.PerMinute.ToStringRoundedAdaptive(2).AsLoc())
                .FontBold()
                .TextCenterMiddle());

            return column
                .AlignItemsCenterMiddle()
                .MarginRight(2.pt())
                .MarginBottom(1.pt())
                .Tooltip(entry.Product.Strings.Name, true, false, false);
        }

        private static Fix32 parseRate(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Fix32.Zero;

            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float num) &&
                !float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out num))
            {
                return Fix32.Zero;
            }

            return num <= 0f ? Fix32.Zero : Fix32.FromFloat(num);
        }

        private static string formatQuantity(Fix32 value) => value.ToFloat().ToString("0.##", CultureInfo.InvariantCulture);
        private static Fix32 parseQuantity(string text) => parseRate(text);
        private static LocStrFormatted formatMachineCount(Fix32 count) => count.ToStringRoundedAdaptive(2).AsLoc();

        // -------------------------------------------------------------------------
        // TARGET ENTRY NESTED CLASS
        // -------------------------------------------------------------------------
        private sealed class TargetEntry
        {
            private readonly ProductionCalculatorService m_service;
            private readonly Action<TargetEntry> m_onRemove;
            private readonly Action m_onChanged;
            private readonly Func<TargetEntry, ProductProto, ProductionRowFlow, Fix32?> m_getSuggestedRate;

            private readonly Column m_row;
            private readonly TextField m_rateField;
            private readonly TextField m_machinesField;
            private readonly Toggle m_rateFixedToggle;
            private readonly RecipeSelectorUi m_recipeSelector;
            private readonly Dropdown<ProductionRowFlow> m_flowDropdown;

            private ProductProto m_product;
            private ProductionRowFlow m_flow;
            private bool m_rateFixed;
            private bool m_suppressFieldLink;

            public TargetEntry(ProductionCalculatorService service, Column parent, Action<TargetEntry> onRemove, Action onChanged, Func<TargetEntry, ProductProto, ProductionRowFlow, Fix32?> getSuggestedRate)
            {
                this.m_service = service;
                this.m_onRemove = onRemove;
                this.m_onChanged = onChanged;
                this.m_getSuggestedRate = getSuggestedRate;

                SingleProductPickerUi singleProductPickerUi = new SingleProductPickerUi(
                    this.getCraftableProducts,
                    this.onProductSelected,
                    () => this.m_product == null ? Option<ProductProto>.None : this.m_product,
                    this.onProductCleared,
                    null, true, true);

                this.m_rateField = new TextField().Text("60".AsLoc()).Width(90.px()).OnEditEnd(this.onRateEdited);
                this.m_machinesField = new TextField().Text("1".AsLoc()).Width(70.px()).OnEditEnd(this.onMachinesEdited);

                this.m_recipeSelector = new RecipeSelectorUi(this.m_service, this.onRecipeOrProductChanged).Visible(false);

                this.m_rateFixedToggle = new Toggle(true)
                    .Label(Tr.FixRate)
                    .Tooltip(Tr.FixRateTooltip, true, false, false)
                    .OnValueChanged(this.onRateFixedChanged);

                this.m_flowDropdown = new Dropdown<ProductionRowFlow>(createFlowOption, null, null, false)
                    .SetOptions(new ProductionRowFlow[] { ProductionRowFlow.Output, ProductionRowFlow.Input })
                    .Width(100.px())
                    .ObserveValueDropdown(() => this.m_flow)
                    .OnValueChanged(this.onFlowChanged);

                Column column = new Column(1.pt());
                Row row = new Row(2.pt());

                row.Add(this.m_flowDropdown);
                row.Add(singleProductPickerUi);
                row.Add(this.m_rateField);
                row.Add(new Label(Tr.RatePerMinute));
                row.Add(this.m_machinesField);
                row.Add(new Label(Tr.MachineCount));
                row.Add(this.m_rateFixedToggle);

                row.Add(singleProductPickerUi.AddTrashUnityButton().OnClick(() => this.m_onRemove(this), false));

                column.Add(row.AlignItemsCenterMiddle());
                column.Add(this.m_recipeSelector);

                this.m_row = column.MarginBottom(1.pt());
                parent.Add(this.m_row);
            }

            private static UiComponent createFlowOption(ProductionRowFlow flow, int index, bool isInDropdown)
            {
                return new Label(flow == ProductionRowFlow.Output ? Tr.FlowOutput : Tr.FlowInput);
            }

            private void onFlowChanged(ProductionRowFlow flow, int index)
            {
                if (this.m_flow == flow) return;

                this.m_flow = flow;
                this.clearRateFixed();

                if (this.m_product != null)
                {
                    if (this.m_service.Catalog.GetRecipesForProductFlow(this.m_product, this.m_flow).IsEmpty)
                    {
                        this.onProductCleared();
                    }
                    else
                    {
                        this.m_recipeSelector.SetProduct(this.m_product, this.m_flow);
                        this.TryApplySuggestedRate(false);
                        this.syncMachinesFromRate();
                    }
                }
                this.m_onChanged();
            }

            private void onProductSelected(ProductProto product)
            {
                this.m_product = product;
                this.clearRateFixed();
                this.m_recipeSelector.SetProduct(product, this.m_flow);
                this.TryApplySuggestedRate(false);
                this.syncMachinesFromRate();
                this.m_onChanged();
            }

            private void onRateFixedChanged(bool isFixed) => this.m_rateFixed = isFixed;

            private void clearRateFixed()
            {
                this.m_rateFixed = false;
                this.m_rateFixedToggle.Value(false);
            }

            public bool TryApplySuggestedRate(bool fromSync = false)
            {
                if ((fromSync && this.m_rateFixed) || this.m_product == null) return false;

                Fix32? fix = this.m_getSuggestedRate(this, this.m_product, this.m_flow);
                if (fix == null) return false;

                string text = formatQuantity(fix.Value);
                bool changed = this.m_rateField.GetText() != text;

                if (changed) this.setRateFieldText(text);

                string text2 = this.m_machinesField.GetText();
                this.syncMachinesFromRate();

                return changed || this.m_machinesField.GetText() != text2;
            }

            private void onProductCleared()
            {
                this.m_product = null;
                this.clearRateFixed();
                this.m_recipeSelector.ClearProduct();
                this.m_onChanged();
            }

            private void onRateEdited(string text)
            {
                this.syncMachinesFromRate();
                this.m_onChanged();
            }

            private void onMachinesEdited(string text)
            {
                this.syncRateFromMachines();
                this.m_onChanged();
            }

            private void onRecipeOrProductChanged()
            {
                this.syncMachinesFromRate();
                this.m_onChanged();
            }

            private void syncMachinesFromRate()
            {
                if (this.m_suppressFieldLink || this.m_product == null) return;

                RecipeProto recipeProto = this.m_recipeSelector.ResolveSelectedRecipe();
                if (recipeProto == null) return;

                Fix32 rate = parseQuantity(this.m_rateField.GetText());
                if (rate <= Fix32.Zero) return;

                Fix32 machinesForRate = RecipeAnchorCalculator.GetMachinesForRate(recipeProto, this.m_product, this.m_flow, rate);
                if (machinesForRate > Fix32.Zero)
                {
                    this.setMachinesFieldText(formatQuantity(machinesForRate));
                }
            }

            private void syncRateFromMachines()
            {
                if (this.m_suppressFieldLink || this.m_product == null) return;

                RecipeProto recipeProto = this.m_recipeSelector.ResolveSelectedRecipe();
                if (recipeProto == null) return;

                Fix32 machines = parseQuantity(this.m_machinesField.GetText());
                if (machines <= Fix32.Zero) return;

                Fix32 rateForMachines = RecipeAnchorCalculator.GetRateForMachines(recipeProto, this.m_product, this.m_flow, machines);
                if (rateForMachines > Fix32.Zero)
                {
                    this.setRateFieldText(formatQuantity(rateForMachines));
                }
            }

            private void setRateFieldText(string text)
            {
                this.m_suppressFieldLink = true;
                this.m_rateField.Text(text.AsLoc());
                this.m_suppressFieldLink = false;
            }

            private void setMachinesFieldText(string text)
            {
                this.m_suppressFieldLink = true;
                this.m_machinesField.Text(text.AsLoc());
                this.m_suppressFieldLink = false;
            }

            private IEnumerable<ProductProto> getCraftableProducts()
            {
                return this.m_service.Catalog.GetProductsForFlow(this.m_flow).ToArray();
            }

            public void RemoveFromHierarchy() => this.m_row.RemoveFromHierarchy();

            public ProductionTarget? TryCreateTarget()
            {
                if (this.m_product == null) return null;

                Fix32 rate = parseQuantity(this.m_rateField.GetText());
                if (rate <= Fix32.Zero) return null;

                RecipeProto recipeProto = this.m_recipeSelector.ResolveSelectedRecipe();
                if (recipeProto == null) return null;

                return new ProductionTarget(this.m_product, rate, this.m_flow, recipeProto);
            }

            public ProductProto GetProduct() => this.m_product;

            public SavedTargetRowData CaptureRow()
            {
                if (this.m_product == null) return null;

                RecipeProto recipeProto = this.m_recipeSelector.ResolveSelectedRecipe();
                if (recipeProto == null) return null;

                Fix32 rate = parseQuantity(this.m_rateField.GetText());
                Fix32 machines = parseQuantity(this.m_machinesField.GetText());

                if (rate <= Fix32.Zero || machines <= Fix32.Zero) return null;

                return new SavedTargetRowData
                {
                    Flow = this.m_flow.ToString(),
                    ProductId = this.m_product.Id.Value,
                    RecipeId = recipeProto.Id.Value,
                    Rate = rate.ToFloat(),
                    Machines = machines.ToFloat(),
                    IsFixed = this.m_rateFixed
                };
            }

            public bool ApplyFromSaved(SavedTargetRowData rowData)
            {
                if (rowData == null) return false;

                if (!SavedCalculationLoader.TryResolveRow(rowData, this.m_service.Catalog, this.m_service.ProtosDb, out ProductionRowFlow flow, out ProductProto product, out RecipeProto recipe))
                {
                    return false;
                }

                this.m_flow = flow;
                this.m_product = product;
                this.m_rateFixed = rowData.IsFixed;
                this.m_rateFixedToggle.Value(rowData.IsFixed);

                this.setRateFieldText(formatQuantity(Fix32.FromFloat(rowData.Rate)));
                this.setMachinesFieldText(formatQuantity(Fix32.FromFloat(rowData.Machines)));
                this.m_recipeSelector.SetProductAndRecipe(product, flow, recipe);

                return true;
            }

            public void InjectProduct(ProductProto product, ProductionRowFlow flow)
            {
                this.m_product = product;
                this.m_flow = flow;

                // Update the dropdown list of recipes to match the injected product
                if (this.m_service.Catalog.GetRecipesForProductFlow(this.m_product, this.m_flow).IsEmpty)
                {
                    this.onProductCleared();
                }
                else
                {
                    this.m_recipeSelector.SetProduct(this.m_product, this.m_flow);
                    this.TryApplySuggestedRate(false);
                    this.syncMachinesFromRate();
                }

                // Tell the system the data changed so it updates the overall calculation
                this.m_onChanged();
            }
        }

        // Create new production line in the panning window
        private void addTargetRowFor(ProductProto product, ProductionRowFlow flow)
        {
            TargetEntry entry = this.createTargetEntry();

            // We will build this InjectProduct method inside TargetEntry in Step 3!
            entry.InjectProduct(product, flow);

            this.m_targetEntries.Add(entry);
            this.m_targetsHint.Visible(false);
            this.scheduleCalculation();
        }

        private void renderInteractiveResult()
        {
            bool hasData = this.m_lastResult.Buildings.IsNotEmpty || this.m_lastResult.RawInputs.IsNotEmpty || this.m_lastResult.TotalOutputs.IsNotEmpty;
            this.m_resultsHint.Visible(!hasData);

            // Buildings don't have click-throughs (yet!), so we use the standard render
            this.renderBuildings(this.m_lastResult.Buildings);

            // MAGIC LINK 1: Clicking an Input creates a row to produce it (Output)
            this.renderClickableFlows(this.m_inputsBody, this.m_lastResult.RawInputs,
                product => this.addTargetRowFor(product, ProductionRowFlow.Output));

            // MAGIC LINK 2: Clicking an Output creates a row to consume it (Input)
            this.renderClickableFlows(this.m_outputsBody, this.m_lastResult.TotalOutputs,
                product => this.addTargetRowFor(product, ProductionRowFlow.Input));
        }

        private void renderClickableFlows(Column body, ImmutableArray<ProductFlowTotals> flows, Action<ProductProto> onClick)
        {
            body.Clear();
            if (flows.IsEmpty) return;

            Row row = new Row().Wrap(true).PaddingTop(1.pt());
            foreach (ProductFlowTotals entry in flows)
            {
                row.Add(createClickableProductTile(entry, onClick));
            }
            body.Add(row);
        }

        private static UiComponent createClickableProductTile(ProductFlowTotals entry, Action<ProductProto> onClick)
        {
            Column column = new Column(1.pt());

            // 1. Add the Icon and its click actions
            column.Add(new Icon()
                .Value(entry.Product, false)
                .Size(IconSize)
                .OnClick(() => onClick(entry.Product))
                .OpenCodexOnRightClick(() => entry.Product));

            column.Add(new Label(entry.PerMinute.ToStringRoundedAdaptive(2).AsLoc())
                .FontBold()
                .TextCenterMiddle());

            // 2. Build the Multi-Line Tooltip
            // We grab the translated Product Name, add a newline (\n), and add our hint.
            string tooltipText = $"{entry.Product.Strings.Name}\n{Tr.ClickToExplore}";

            // 3. Assemble the tile
            var clickableTile = column
                .AlignItemsCenterMiddle()
                .MarginRight(2.pt())
                .MarginBottom(1.pt())
                .Tooltip(tooltipText.AsLoc(), true, false, false);

            // 4. Force the Pointer/Hand Cursor
            // By adding the standard button class, the Unity engine knows to change 
            // the mouse cursor to a pointer when hovering over this specific column.
            //clickableTile.Class(Cls.Button);

            // (Note: If Cls.Button adds an unwanted background color, use Cls.Clickable 
            // or Cls.Interactable instead, depending on your exact Mafi game version).

            return clickableTile;
        }
    }
}
