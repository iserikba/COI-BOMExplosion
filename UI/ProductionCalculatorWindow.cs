using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Mafi;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using Mafi.Localization;
using Mafi.Unity.Ui;
using Mafi.Unity.Ui.Library;
using Mafi.Unity.UiToolkit.Component;
using Mafi.Unity.UiToolkit.Library;
using ProductionCalculator.Core.Calculation;
using ProductionCalculator.Core.Persistence;
using ProductionCalculator.Core.Services;

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
                this.renderResult();
                return;
            }

            this.m_targetsHint.Visible(false);
            this.m_lastResult = this.m_service.Calculate(ImmutableArray.ToImmutableArray(validTargets));
            this.renderResult();
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

            if (entry.Machine != null)
            {
                row.Add(new Icon(entry.Machine, false, false)
                    .Size(BuildingIconSize)
                    .Tooltip(entry.Machine.Strings.Name, true, false, false));
            }

            Column column = new Column(0.pt());
            column.Add(new Label(entry.Recipe.Strings.Name).FontBold());

            Row countRow = new Row(1.pt());
            countRow.Add(new Label(formatMachineCount(entry.MachineCount).ToString().AsLoc()).FontBold());
            countRow.Add(new Label(Tr.MachineCount).Opacity(0.8f));

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
        }
    }
}
/*
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Mafi;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using Mafi.Localization;
using Mafi.Unity.Ui;
using Mafi.Unity.Ui.Library;
using Mafi.Unity.UiToolkit.Component;
using Mafi.Unity.UiToolkit.Library;
using ProductionCalculator.Core.Calculation;
using ProductionCalculator.Core.Persistence;
using ProductionCalculator.Core.Services;

namespace ProductionCalculator.Ui
{
	// Token: 0x02000008 RID: 8
	public sealed class ProductionCalculatorWindow : Window
	{
		// Token: 0x06000021 RID: 33 RVA: 0x00002978 File Offset: 0x00000B78
		public ProductionCalculatorWindow(ProductionCalculatorService service, SavedCalculationRepository repository) : base(Tr.WindowTitle, false)
		{
			this.m_service = service;
			this.m_saveDialog = new SaveCalculationDialog(repository, service, this.closeDialogs);
			this.m_loadDialog = new LoadCalculationsDialog(repository, service,this.closeDialogs);
			base.WindowSize(PxExtensions.px(1200), PxExtensions.px(780));
			base.MakeMovable();
			base.EnablePinning();
			PanelWithHeader panelWithHeader = new PanelWithHeader(new LocStrFormatted?(Tr.TargetsTitle));
			UiComponent body = panelWithHeader.Body;
			UiComponent[] array = new UiComponent[3];
			array[0] = (this.m_targetsHint = UiComponentLayoutExtensions.MarginBottom<Label>(new Label(Tr.NoTargets), PxExtensions.pt(1)));
			array[1] = (this.m_targetsBody = new Column(PxExtensions.pt(1)));
			int num = 2;
			Row row = new Row(PxExtensions.pt(1));
			row.Add(new ButtonText(Button.General, Tr.AddTarget, new Action(this.addTargetRow)));
			row.Add(new ButtonText(Button.General, Tr.SyncChainRates, new Action(this.syncChainRates)));
			array[num] = UiComponentLayoutExtensions.MarginTop<Row>(row, PxExtensions.pt(1));
			body.Add(array);
			PanelWithHeader panelWithHeader2 = new PanelWithHeader(new LocStrFormatted?(Tr.BuildingsTitle));
			panelWithHeader2.Body.Add(this.m_buildingsBody = new Column(PxExtensions.pt(1)));
			PanelWithHeader panelWithHeader3 = new PanelWithHeader(new LocStrFormatted?(Tr.InputsTitle));
			panelWithHeader3.Body.Add(this.m_inputsBody = new Column(PxExtensions.pt(1)));
			PanelWithHeader panelWithHeader4 = new PanelWithHeader(new LocStrFormatted?(Tr.OutputsTitle));
			panelWithHeader4.Body.Add(this.m_outputsBody = new Column(PxExtensions.pt(1)));
			ScrollColumn scrollColumn = UiComponentLayoutExtensions.FlexGrow<ScrollColumn>(UiComponentFlexExt.AlignItemsStretch<ScrollColumn>(new ScrollColumn()), 1f);
			scrollColumn.Add(panelWithHeader);
			this.m_resultsScroll = UiComponentLayoutExtensions.FlexGrow<ScrollColumn>(UiComponentFlexExt.AlignItemsStretch<ScrollColumn>(new ScrollColumn()), 1f);
			this.m_resultsScroll.Add(this.m_resultsHint = UiComponentLayoutExtensions.MarginBottom<Label>(new Label(Tr.NoResults), PxExtensions.pt(1)));
			this.m_resultsScroll.Add(UiComponentLayoutExtensions.MarginBottom<PanelWithHeader>(panelWithHeader2, PxExtensions.pt(2)));
			this.m_resultsScroll.Add(UiComponentLayoutExtensions.MarginBottom<PanelWithHeader>(panelWithHeader3, PxExtensions.pt(2)));
			this.m_resultsScroll.Add(panelWithHeader4);
			Column column = UiComponentFlexExt.AlignItemsStretch<Column>(UiComponentLayoutExtensions.FlexShrink<Column>(UiComponentLayoutExtensions.Width<Column>(new Column(PxExtensions.pt(1)), PxExtensions.px(420)), 0f));
			column.Add(this.m_resultsScroll);
			column.Add(UiComponentLayoutExtensions.FlexGrow<SaveCalculationDialog>(Mafi.Unity.UiToolkit.Component.UiComponentExtensions.Visible<SaveCalculationDialog>(this.m_saveDialog, false), 1f));
			column.Add(UiComponentLayoutExtensions.FlexGrow<LoadCalculationsDialog>(Mafi.Unity.UiToolkit.Component.UiComponentExtensions.Visible<LoadCalculationsDialog>(this.m_loadDialog, false), 1f));
			UiComponent uiComponent = column;
			Row row2 = new Row(PxExtensions.pt(1));
			row2.Add(new ButtonText(Button.General, Tr.SaveCalculation, new Action(this.openSaveDialog)));
			row2.Add(new ButtonText(Button.General, Tr.LoadCalculations, new Action(this.openLoadDialog)));
			uiComponent.Add(UiComponentLayoutExtensions.AlignSelfStretch<Row>(UiComponentLayoutExtensions.MarginTop<Row>(row2, PxExtensions.pt(1))));
			UiComponent body2 = this.Body;
			Row row3 = new Row(PxExtensions.pt(2));
			row3.Add(scrollColumn);
			row3.Add(column);
			body2.Add(UiComponentLayoutExtensions.FlexGrow<Row>(UiComponentFlexExt.AlignItemsStretch<Row>(row3), 1f));
			this.addTargetRow();
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002D38 File Offset: 0x00000F38
		public void RefreshPreview()
		{
			this.runCalculation();
		}

        private void openSaveDialog()
        {
            this.m_loadDialog.SetVisible(false);
            this.m_resultsScroll.SetVisible(false);
            this.m_saveDialog.Prepare(this.captureSavedRows, this.getDefaultSaveName(), this.getDefaultSaveIcon(), this.onCalculationSaved);
            this.m_saveDialog.SetVisible(true);
        }

        private void openLoadDialog()
        {
            this.m_saveDialog.SetVisible(false);
            this.m_resultsScroll.SetVisible(false);
            this.m_loadDialog.Prepare(this.applySavedDocument);
            this.m_loadDialog.SetVisible(true);
        }

		// Token: 0x06000025 RID: 37 RVA: 0x00002DE3 File Offset: 0x00000FE3
		private void closeDialogs()
		{
			Mafi.Unity.UiToolkit.Component.UiComponentExtensions.Visible<SaveCalculationDialog>(this.m_saveDialog, false);
			Mafi.Unity.UiToolkit.Component.UiComponentExtensions.Visible<LoadCalculationsDialog>(this.m_loadDialog, false);
			Mafi.Unity.UiToolkit.Component.UiComponentExtensions.Visible<ScrollColumn>(this.m_resultsScroll, true);
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002E0C File Offset: 0x0000100C
		private IReadOnlyList<SavedTargetRowData> captureSavedRows()
		{
			List<SavedTargetRowData> list = new List<SavedTargetRowData>(this.m_targetEntries.Count);
			for (int i = 0; i < this.m_targetEntries.Count; i++)
			{
				SavedTargetRowData savedTargetRowData = this.m_targetEntries[i].CaptureRow();
				if (savedTargetRowData != null)
				{
					list.Add(savedTargetRowData);
				}
			}
			return list;
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00002E60 File Offset: 0x00001060
		private ProductProto getDefaultIconProduct()
		{
			for (int i = 0; i < this.m_targetEntries.Count; i++)
			{
				ProductProto product = this.m_targetEntries[i].GetProduct();
				if (product != null)
				{
					return product;
				}
			}
			return null;
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00002EA1 File Offset: 0x000010A1
		private string getDefaultSaveName()
		{
			if (!string.IsNullOrWhiteSpace(this.m_loadedPresetName))
			{
				return this.m_loadedPresetName;
			}
			return "New calculation";
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00002EBC File Offset: 0x000010BC
		private ProductProto getDefaultSaveIcon()
		{
			ProductProto productProto = this.tryResolveProduct(this.m_loadedPresetIconProductId);
			if (productProto != null)
			{
				return productProto;
			}
			return this.getDefaultIconProduct();
		}

		
		private ProductProto tryResolveProduct(string productId)
		{
			if (string.IsNullOrWhiteSpace(productId))
			{
				return null;
			}

			ProductProto result;
			if (this.m_service.ProtosDb.TryGetProto<ProductProto>(new Proto.ID(productId), out result))
			{
				return result;
			}
			return null;
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002F1C File Offset: 0x0000111C
		private void onCalculationSaved(string name, ProductProto iconProduct)
		{
			this.m_loadedPresetName = name;
			this.m_loadedPresetIconProductId = ((iconProduct != null) ? iconProduct.Id.Value : null);
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00002F3C File Offset: 0x0000113C
		private void applySavedDocument(SavedCalculationDocument document)
		{
			if (((document != null) ? document.Rows : null) == null || document.Rows.Count == 0)
			{
				return;
			}
			this.m_loadedPresetName = document.Name;
			this.m_loadedPresetIconProductId = document.IconProductId;
			this.clearAllTargetRows();
			for (int i = 0; i < document.Rows.Count; i++)
			{
				ProductionCalculatorWindow.TargetEntry targetEntry = this.createTargetEntry();
				if (!targetEntry.ApplyFromSaved(document.Rows[i]))
				{
					targetEntry.RemoveFromHierarchy();
					Log.Warning("ProductionCalculator: skipped invalid row while loading '" + document.Name + "'.");
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
			Mafi.Unity.UiToolkit.Component.UiComponentExtensions.Visible<Label>(this.m_targetsHint, false);
			this.scheduleCalculation();
		}

		// Token: 0x0600002D RID: 45 RVA: 0x0000300C File Offset: 0x0000120C
		private void clearAllTargetRows()
		{
			for (int i = this.m_targetEntries.Count - 1; i >= 0; i--)
			{
				this.m_targetEntries[i].RemoveFromHierarchy();
			}
			this.m_targetEntries.Clear();
		}

		// Token: 0x0600002E RID: 46 RVA: 0x0000304D File Offset: 0x0000124D
		private ProductionCalculatorWindow.TargetEntry createTargetEntry()
		{
			return new ProductionCalculatorWindow.TargetEntry(this.m_service, this.m_targetsBody, new Action<ProductionCalculatorWindow.TargetEntry>(this.removeTargetRow), new Action(this.scheduleCalculation), new Func<ProductionCalculatorWindow.TargetEntry, ProductProto, ProductionRowFlow, Fix32?>(this.tryGetSuggestedRate));
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00003084 File Offset: 0x00001284
		private void addTargetRow()
		{
			ProductionCalculatorWindow.TargetEntry item = this.createTargetEntry();
			this.m_targetEntries.Add(item);
			Mafi.Unity.UiToolkit.Component.UiComponentExtensions.Visible<Label>(this.m_targetsHint, false);
			this.scheduleCalculation();
		}

		// Token: 0x06000030 RID: 48 RVA: 0x000030B8 File Offset: 0x000012B8
		private Fix32? tryGetSuggestedRate(ProductionCalculatorWindow.TargetEntry skipEntry, ProductProto product, ProductionRowFlow flow)
		{
			List<ProductionTarget> list = new List<ProductionTarget>(this.m_targetEntries.Count);
			for (int i = 0; i < this.m_targetEntries.Count; i++)
			{
				ProductionCalculatorWindow.TargetEntry targetEntry = this.m_targetEntries[i];
				if (targetEntry != skipEntry)
				{
					ProductionTarget? productionTarget = targetEntry.TryCreateTarget();
					if (productionTarget != null)
					{
						list.Add(productionTarget.Value);
					}
				}
			}
			if (list.Count == 0)
			{
				return null;
			}
			ProductionChainResult productionChainResult = this.m_service.Calculate(ImmutableArray.ToImmutableArray<ProductionTarget>(list));
			Fix32 value;
			if (productionChainResult.TryGetSuggestedRateForFlow(product, flow, out value))
			{
				return new Fix32?(value);
			}
			return null;
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00003160 File Offset: 0x00001360
		private void syncChainRates()
		{
			if (this.m_targetEntries.Count == 0)
			{
				return;
			}
			bool flag = true;
			int num = 0;
			while (num < this.m_targetEntries.Count && flag)
			{
				flag = false;
				for (int i = 0; i < this.m_targetEntries.Count; i++)
				{
					if (this.m_targetEntries[i].TryApplySuggestedRate(true))
					{
						flag = true;
					}
				}
				num++;
			}
			this.scheduleCalculation();
		}

		// Token: 0x06000032 RID: 50 RVA: 0x000031CB File Offset: 0x000013CB
		private void removeTargetRow(ProductionCalculatorWindow.TargetEntry entry)
		{
			entry.RemoveFromHierarchy();
			this.m_targetEntries.Remove(entry);
			if (this.m_targetEntries.Count == 0)
			{
				Mafi.Unity.UiToolkit.Component.UiComponentExtensions.Visible<Label>(this.m_targetsHint, true);
			}
			this.scheduleCalculation();
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00003200 File Offset: 0x00001400
		private void scheduleCalculation()
		{
			this.runCalculation();
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00003208 File Offset: 0x00001408
		private void runCalculation()
		{
			List<ProductionTarget> list = new List<ProductionTarget>(this.m_targetEntries.Count);
			for (int i = 0; i < this.m_targetEntries.Count; i++)
			{
				ProductionTarget? productionTarget = this.m_targetEntries[i].TryCreateTarget();
				if (productionTarget != null)
				{
					list.Add(productionTarget.Value);
				}
			}
			if (list.Count == 0)
			{
				this.m_lastResult = ProductionChainResult.Empty;
				Mafi.Unity.UiToolkit.Component.UiComponentExtensions.Visible<Label>(this.m_targetsHint, this.m_targetEntries.Count == 0);
				this.renderResult();
				return;
			}
			Mafi.Unity.UiToolkit.Component.UiComponentExtensions.Visible<Label>(this.m_targetsHint, false);
			this.m_lastResult = this.m_service.Calculate(ImmutableArray.ToImmutableArray<ProductionTarget>(list));
			this.renderResult();
		}

		// Token: 0x06000035 RID: 53 RVA: 0x000032C4 File Offset: 0x000014C4
		private void renderResult()
		{
			bool flag = this.m_lastResult.Buildings.IsNotEmpty || this.m_lastResult.RawInputs.IsNotEmpty || this.m_lastResult.TotalOutputs.IsNotEmpty;
			Mafi.Unity.UiToolkit.Component.UiComponentExtensions.Visible<Label>(this.m_resultsHint, !flag);
			this.renderBuildings(this.m_lastResult.Buildings);
			this.renderFlows(this.m_inputsBody, this.m_lastResult.RawInputs);
			this.renderFlows(this.m_outputsBody, this.m_lastResult.TotalOutputs);
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00003364 File Offset: 0x00001564
		private void renderBuildings(ImmutableArray<RecipeBuildingTotals> buildings)
		{
			this.m_buildingsBody.Clear();
			if (buildings.IsEmpty)
			{
				return;
			}
			foreach (RecipeBuildingTotals entry in buildings)
			{
				this.m_buildingsBody.Add(ProductionCalculatorWindow.createBuildingRow(entry));
			}
		}

		// Token: 0x06000037 RID: 55 RVA: 0x000033B4 File Offset: 0x000015B4
		private static UiComponent createBuildingRow(RecipeBuildingTotals entry)
		{
			Row row = UiComponentLayoutExtensions.MarginBottom<Row>(UiComponentFlexExt.AlignItemsCenterMiddle<Row>(new Row(PxExtensions.pt(2))), PxExtensions.pt(1));
			if (entry.Machine != null)
			{
				row.Add(TooltipExtensions.Tooltip<Icon>(UiComponentLayoutExtensions.Size<Icon>(new Icon(entry.Machine, false, false), ProductionCalculatorWindow.BuildingIconSize), new LocStrFormatted?(entry.Machine.Strings.Name), true, false, false));
			}
			UiComponent uiComponent = row;
			Column column = new Column(PxExtensions.pt(0));
			column.Add(UiComponentFontExtensions.FontBold<Label>(UiComponentWithTextExtensions.Value<Label>(new Label(default(LocStrFormatted)), entry.Recipe.Strings.Name)));
			Row row2 = new Row(PxExtensions.pt(1));
			row2.Add(UiComponentFontExtensions.FontBold<Label>(UiComponentWithTextExtensions.Value<Label>(new Label(default(LocStrFormatted)), ProductionCalculatorWindow.formatMachineCount(entry.MachineCount))));
			row2.Add(UiComponentLayoutExtensions.Opacity<Label>(new Label(Tr.MachineCount), new float?(0.8f)));
			column.Add(row2);
			uiComponent.Add(UiComponentLayoutExtensions.FlexGrow<Column>(column, 1f));
			return row;
		}

		// Token: 0x06000038 RID: 56 RVA: 0x000034DC File Offset: 0x000016DC
		private static LocStrFormatted formatMachineCount(Fix32 count)
		{
			return TranslationExtensions.AsLoc(count.ToStringRoundedAdaptive(2));
		}

		// Token: 0x06000039 RID: 57 RVA: 0x000034EC File Offset: 0x000016EC
		private void renderFlows(Column body, ImmutableArray<ProductFlowTotals> flows)
		{
			body.Clear();
			if (flows.IsEmpty)
			{
				return;
			}
			Row row = UiComponentPaddingExtensions.PaddingTop<Row>(UiComponentFlexExt.Wrap<Row>(new Row(null, null, null), true), PxExtensions.pt(1));
			foreach (ProductFlowTotals entry in flows)
			{
				row.Add(ProductionCalculatorWindow.createProductTile(entry));
			}
			body.Add(row);
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00003558 File Offset: 0x00001758
		private static UiComponent createProductTile(ProductFlowTotals entry)
		{
			Column column = new Column(PxExtensions.pt(1));
			column.Add(CodexExtensions.OpenCodexOnRightClick<Icon>(UiComponentLayoutExtensions.Size<Icon>(new Icon().Value(entry.Product, false), ProductionCalculatorWindow.IconSize), () => entry.Product));
			column.Add(UiComponentFontExtensions.TextCenterMiddle<Label>(UiComponentFontExtensions.FontBold<Label>(UiComponentWithTextExtensions.Value<Label>(new Label(default(LocStrFormatted)), TranslationExtensions.AsLoc(entry.PerMinute.ToStringRoundedAdaptive(2))))));
			return TooltipExtensions.Tooltip<Column>(UiComponentLayoutExtensions.MarginBottom<Column>(UiComponentLayoutExtensions.MarginRight<Column>(UiComponentFlexExt.AlignItemsCenterMiddle<Column>(column), PxExtensions.pt(2)), PxExtensions.pt(1)), new LocStrFormatted?(entry.Product.Strings.Name), true, false, false);
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00003638 File Offset: 0x00001838
		private static Fix32 parseRate(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return Fix32.Zero;
			}
			float num;
			if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out num) && !float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out num))
			{
				return Fix32.Zero;
			}
			if (num <= 0f)
			{
				return Fix32.Zero;
			}
			return Fix32.FromFloat(num);
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00003698 File Offset: 0x00001898
		private static string formatQuantity(Fix32 value)
		{
			return value.ToFloat().ToString("0.##", CultureInfo.InvariantCulture);
		}

		// Token: 0x0600003D RID: 61 RVA: 0x000036BE File Offset: 0x000018BE
		private static Fix32 parseQuantity(string text)
		{
			return ProductionCalculatorWindow.parseRate(text);
		}

		// Token: 0x04000031 RID: 49
		private const string DefaultRateText = "60";

		// Token: 0x04000032 RID: 50
		private static readonly Px IconSize = PxExtensions.px(36);

		// Token: 0x04000033 RID: 51
		private static readonly Px BuildingIconSize = PxExtensions.px(42);

		// Token: 0x04000034 RID: 52
		private readonly ProductionCalculatorService m_service;

		// Token: 0x04000035 RID: 53
		private readonly SaveCalculationDialog m_saveDialog;

		// Token: 0x04000036 RID: 54
		private readonly LoadCalculationsDialog m_loadDialog;

		// Token: 0x04000037 RID: 55
		private readonly ScrollColumn m_resultsScroll;

		// Token: 0x04000038 RID: 56
		private readonly Column m_targetsBody;

		// Token: 0x04000039 RID: 57
		private readonly Column m_buildingsBody;

		// Token: 0x0400003A RID: 58
		private readonly Column m_inputsBody;

		// Token: 0x0400003B RID: 59
		private readonly Column m_outputsBody;

		// Token: 0x0400003C RID: 60
		private readonly Label m_targetsHint;

		// Token: 0x0400003D RID: 61
		private readonly Label m_resultsHint;

		// Token: 0x0400003E RID: 62
		private readonly List<ProductionCalculatorWindow.TargetEntry> m_targetEntries = new List<ProductionCalculatorWindow.TargetEntry>();

		// Token: 0x0400003F RID: 63
		private ProductionChainResult m_lastResult = ProductionChainResult.Empty;

		// Token: 0x04000040 RID: 64
		private string m_loadedPresetName;

		// Token: 0x04000041 RID: 65
		private string m_loadedPresetIconProductId;

		// Token: 0x0200001F RID: 31
		private sealed class TargetEntry
		{
			// Token: 0x060000E2 RID: 226 RVA: 0x00005F90 File Offset: 0x00004190
			public TargetEntry(ProductionCalculatorService service, Column parent, Action<ProductionCalculatorWindow.TargetEntry> onRemove, Action onChanged, Func<ProductionCalculatorWindow.TargetEntry, ProductProto, ProductionRowFlow, Fix32?> getSuggestedRate)
			{
				this.m_service = service;
				this.m_onRemove = onRemove;
				this.m_onChanged = onChanged;
				this.m_getSuggestedRate = getSuggestedRate;
				SingleProductPickerUi singleProductPickerUi = new SingleProductPickerUi(this.getCraftableProducts, this.onProductSelected, () => this.m_product == null ? Option<ProductProto>.None : this.m_product, this.onProductCleared, null, true, true);
				TextField textField = UiComponentLayoutExtensions.Width<TextField>(new TextField().Text(TranslationExtensions.AsLoc("60")), PxExtensions.px(90)).OnEditEnd(new Action<string>(this.onRateEdited));
				this.m_rateField = textField;
				TextField textField2 = UiComponentLayoutExtensions.Width<TextField>(new TextField().Text(TranslationExtensions.AsLoc("1")), PxExtensions.px(70)).OnEditEnd(new Action<string>(this.onMachinesEdited));
				this.m_machinesField = textField2;
				this.m_recipeSelector = new RecipeSelectorUi(this.m_service, new Action(this.onRecipeOrProductChanged));
				Mafi.Unity.UiToolkit.Component.UiComponentExtensions.Visible<RecipeSelectorUi>(this.m_recipeSelector, false);
				this.m_rateFixedToggle = TooltipExtensions.Tooltip<Toggle>(UiComponentWithTextExtensions.Label<Toggle>(new Toggle(true), Tr.FixRate), new LocStrFormatted?(Tr.FixRateTooltip), true, false, false);
				this.m_rateFixedToggle.OnValueChanged(this.onRateFixedChanged);

                this.m_flowDropdown = new Dropdown<ProductionRowFlow>(createFlowOption, null, null, false)
					.SetOptions(new ProductionRowFlow[] { ProductionRowFlow.Output, ProductionRowFlow.Input })
					.Width(100.px())
					.ObserveValueDropdown(() => this.m_flow)
					.OnValueChanged(this.onFlowChanged);

                Column column = new Column(PxExtensions.pt(1));
				Row row = new Row(PxExtensions.pt(2));
				row.Add(this.m_flowDropdown);
				row.Add(singleProductPickerUi);
				row.Add(textField);
				row.Add(new Label(Tr.RatePerMinute));
				row.Add(textField2);
				row.Add(new Label(Tr.MachineCount));
				row.Add(this.m_rateFixedToggle);
				row.Add(UiComponentEventExtensions.OnClick<ButtonIcon>(singleProductPickerUi.AddTrashUnityButton(), delegate()
				{
					this.m_onRemove(this);
				}, false));
				column.Add(UiComponentFlexExt.AlignItemsCenterMiddle<Row>(row));
				column.Add(this.m_recipeSelector);
				this.m_row = UiComponentLayoutExtensions.MarginBottom<Column>(column, PxExtensions.pt(1));
				parent.Add(this.m_row);
			}

			// Token: 0x060000E3 RID: 227 RVA: 0x0000620D File Offset: 0x0000440D
			private static UiComponent createFlowOption(ProductionRowFlow flow, int index, bool isInDropdown)
			{
				return new Label((flow == ProductionRowFlow.Output) ? Tr.FlowOutput : Tr.FlowInput);
			}

			// Token: 0x060000E4 RID: 228 RVA: 0x00006228 File Offset: 0x00004428
			private void onFlowChanged(ProductionRowFlow flow, int index)
			{
				if (this.m_flow == flow)
				{
					return;
				}
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

			// Token: 0x060000E5 RID: 229 RVA: 0x000062B8 File Offset: 0x000044B8
			private void onProductSelected(ProductProto product)
			{
				this.m_product = product;
				this.clearRateFixed();
				this.m_recipeSelector.SetProduct(product, this.m_flow);
				this.TryApplySuggestedRate(false);
				this.syncMachinesFromRate();
				this.m_onChanged();
			}

			// Token: 0x060000E6 RID: 230 RVA: 0x000062F2 File Offset: 0x000044F2
			private void onRateFixedChanged(bool isFixed)
			{
				this.m_rateFixed = isFixed;
			}

			// Token: 0x060000E7 RID: 231 RVA: 0x000062FB File Offset: 0x000044FB
			private void clearRateFixed()
			{
				this.m_rateFixed = false;
				this.m_rateFixedToggle.Value(false);
			}

			// Token: 0x060000E8 RID: 232 RVA: 0x00006314 File Offset: 0x00004514
			public bool TryApplySuggestedRate(bool fromSync = false)
			{
				if (fromSync && this.m_rateFixed)
				{
					return false;
				}
				if (this.m_product == null)
				{
					return false;
				}
				Fix32? fix = this.m_getSuggestedRate(this, this.m_product, this.m_flow);
				if (fix == null)
				{
					return false;
				}
				string text = ProductionCalculatorWindow.formatQuantity(fix.Value);
				bool flag = this.m_rateField.GetText() != text;
				if (flag)
				{
					this.setRateFieldText(text);
				}
				string text2 = this.m_machinesField.GetText();
				this.syncMachinesFromRate();
				return flag || this.m_machinesField.GetText() != text2;
			}

			// Token: 0x060000E9 RID: 233 RVA: 0x000063B3 File Offset: 0x000045B3
			private void onProductCleared()
			{
				this.m_product = null;
				this.clearRateFixed();
				this.m_recipeSelector.ClearProduct();
				this.m_onChanged();
			}

			// Token: 0x060000EA RID: 234 RVA: 0x000063D8 File Offset: 0x000045D8
			private void onRateEdited(string text)
			{
				this.syncMachinesFromRate();
				this.m_onChanged();
			}

			// Token: 0x060000EB RID: 235 RVA: 0x000063EB File Offset: 0x000045EB
			private void onMachinesEdited(string text)
			{
				this.syncRateFromMachines();
				this.m_onChanged();
			}

			// Token: 0x060000EC RID: 236 RVA: 0x000063FE File Offset: 0x000045FE
			private void onRecipeOrProductChanged()
			{
				this.syncMachinesFromRate();
				this.m_onChanged();
			}

			// Token: 0x060000ED RID: 237 RVA: 0x00006414 File Offset: 0x00004614
			private void syncMachinesFromRate()
			{
				if (this.m_suppressFieldLink || this.m_product == null)
				{
					return;
				}
				RecipeProto recipeProto = this.m_recipeSelector.ResolveSelectedRecipe();
				if (recipeProto == null)
				{
					return;
				}
				Fix32 fix = ProductionCalculatorWindow.parseQuantity(this.m_rateField.GetText());
				if (fix <= Fix32.Zero)
				{
					return;
				}
				Fix32 machinesForRate = RecipeAnchorCalculator.GetMachinesForRate(recipeProto, this.m_product, this.m_flow, fix);
				if (machinesForRate <= Fix32.Zero)
				{
					return;
				}
				this.setMachinesFieldText(ProductionCalculatorWindow.formatQuantity(machinesForRate));
			}

			// Token: 0x060000EE RID: 238 RVA: 0x0000649C File Offset: 0x0000469C
			private void syncRateFromMachines()
			{
				if (this.m_suppressFieldLink || this.m_product == null)
				{
					return;
				}
				RecipeProto recipeProto = this.m_recipeSelector.ResolveSelectedRecipe();
				if (recipeProto == null)
				{
					return;
				}
				Fix32 fix = ProductionCalculatorWindow.parseQuantity(this.m_machinesField.GetText());
				if (fix <= Fix32.Zero)
				{
					return;
				}
				Fix32 rateForMachines = RecipeAnchorCalculator.GetRateForMachines(recipeProto, this.m_product, this.m_flow, fix);
				if (rateForMachines <= Fix32.Zero)
				{
					return;
				}
				this.setRateFieldText(ProductionCalculatorWindow.formatQuantity(rateForMachines));
			}

			// Token: 0x060000EF RID: 239 RVA: 0x00006523 File Offset: 0x00004723
			private void setRateFieldText(string text)
			{
				this.m_suppressFieldLink = true;
				this.m_rateField.Text(TranslationExtensions.AsLoc(text));
				this.m_suppressFieldLink = false;
			}

			// Token: 0x060000F0 RID: 240 RVA: 0x00006545 File Offset: 0x00004745
			private void setMachinesFieldText(string text)
			{
				this.m_suppressFieldLink = true;
				this.m_machinesField.Text(TranslationExtensions.AsLoc(text));
				this.m_suppressFieldLink = false;
			}

			// Token: 0x060000F1 RID: 241 RVA: 0x00006568 File Offset: 0x00004768
			private IEnumerable<ProductProto> getCraftableProducts()
			{
				return this.m_service.Catalog.GetProductsForFlow(this.m_flow).ToArray();
			}

			// Token: 0x060000F2 RID: 242 RVA: 0x00006593 File Offset: 0x00004793
			public void RemoveFromHierarchy()
			{
				this.m_row.RemoveFromHierarchy();
			}

			// Token: 0x060000F3 RID: 243 RVA: 0x000065A0 File Offset: 0x000047A0
			public ProductionTarget? TryCreateTarget()
			{
				if (this.m_product == null)
				{
					return null;
				}
				Fix32 fix = ProductionCalculatorWindow.parseQuantity(this.m_rateField.GetText());
				if (fix <= Fix32.Zero)
				{
					return null;
				}
				RecipeProto recipeProto = this.m_recipeSelector.ResolveSelectedRecipe();
				if (recipeProto == null)
				{
					return null;
				}
				return new ProductionTarget?(new ProductionTarget(this.m_product, fix, this.m_flow, recipeProto));
			}

			// Token: 0x060000F4 RID: 244 RVA: 0x00006624 File Offset: 0x00004824
			public ProductProto GetProduct()
			{
				return this.m_product;
			}

			// Token: 0x060000F5 RID: 245 RVA: 0x0000662C File Offset: 0x0000482C
			public SavedTargetRowData CaptureRow()
			{
				if (this.m_product == null)
				{
					return null;
				}
				RecipeProto recipeProto = this.m_recipeSelector.ResolveSelectedRecipe();
				if (recipeProto == null)
				{
					return null;
				}
				Fix32 fix = ProductionCalculatorWindow.parseQuantity(this.m_rateField.GetText());
				Fix32 fix2 = ProductionCalculatorWindow.parseQuantity(this.m_machinesField.GetText());
				if (fix <= Fix32.Zero || fix2 <= Fix32.Zero)
				{
					return null;
				}
				return new SavedTargetRowData
				{
					Flow = this.m_flow.ToString(),
					ProductId = this.m_product.Id.Value,
					RecipeId = recipeProto.Id.Value,
					Rate = fix.ToFloat(),
					Machines = fix2.ToFloat(),
					IsFixed = this.m_rateFixed
				};
			}

			// Token: 0x060000F6 RID: 246 RVA: 0x00006708 File Offset: 0x00004908
			public bool ApplyFromSaved(SavedTargetRowData rowData)
			{
				if (rowData == null)
				{
					return false;
				}
				ProductionRowFlow flow;
				ProductProto product;
				RecipeProto recipe;
				if (!SavedCalculationLoader.TryResolveRow(rowData, this.m_service.Catalog, this.m_service.ProtosDb, out flow, out product, out recipe))
				{
					return false;
				}
				this.m_flow = flow;
				this.m_product = product;
				this.m_rateFixed = rowData.IsFixed;
				this.m_rateFixedToggle.Value(rowData.IsFixed);
				this.setRateFieldText(ProductionCalculatorWindow.formatQuantity(Fix32.FromFloat(rowData.Rate)));
				this.setMachinesFieldText(ProductionCalculatorWindow.formatQuantity(Fix32.FromFloat(rowData.Machines)));
				this.m_recipeSelector.SetProductAndRecipe(product, flow, recipe);
				return true;
			}


			// Token: 0x0400008A RID: 138
			private readonly ProductionCalculatorService m_service;

			// Token: 0x0400008B RID: 139
			private readonly Action<ProductionCalculatorWindow.TargetEntry> m_onRemove;

			// Token: 0x0400008C RID: 140
			private readonly Action m_onChanged;

			// Token: 0x0400008D RID: 141
			private readonly Func<ProductionCalculatorWindow.TargetEntry, ProductProto, ProductionRowFlow, Fix32?> m_getSuggestedRate;

			// Token: 0x0400008E RID: 142
			private readonly Column m_row;

			// Token: 0x0400008F RID: 143
			private readonly TextField m_rateField;

			// Token: 0x04000090 RID: 144
			private readonly TextField m_machinesField;

			// Token: 0x04000091 RID: 145
			private readonly Toggle m_rateFixedToggle;

			// Token: 0x04000092 RID: 146
			private readonly RecipeSelectorUi m_recipeSelector;

			// Token: 0x04000093 RID: 147
			private ProductProto m_product;

			// Token: 0x04000094 RID: 148
			private ProductionRowFlow m_flow;

			// Token: 0x04000095 RID: 149
			private Dropdown<ProductionRowFlow> m_flowDropdown;

			// Token: 0x04000096 RID: 150
			private bool m_rateFixed;

			// Token: 0x04000097 RID: 151
			private bool m_suppressFieldLink;

		}
	}
}
*/