using System;
using System.Collections.Generic;
using Mafi;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using Mafi.Localization;
using Mafi.Unity.UiToolkit.Component;
using Mafi.Unity.UiToolkit.Library;
using ProductionCalculator.Core.Persistence;
using ProductionCalculator.Core.Services;

namespace ProductionCalculator.Ui
{
    internal sealed class LoadCalculationsDialog : PanelWithHeader
    {
        // 1. CLEANUP: Extension methods for pixels/points. 
        // PxExtensions.px(36) becomes 36.px()
        private static readonly Px IconSize = 36.px();

        private readonly SavedCalculationRepository m_repository;
        private readonly ProductionCalculatorService m_service;
        private readonly Action m_close;
        private readonly Column m_listBody;
        private readonly Label m_emptyHint;

        private Action<SavedCalculationDocument> m_onLoad;

        public LoadCalculationsDialog(SavedCalculationRepository repository, ProductionCalculatorService service, Action close)
            : base(new LocStrFormatted?(Tr.LoadCalculationsTitle))
        {
            this.m_repository = repository;
            this.m_service = service;
            this.m_close = close;

            this.m_listBody = new Column(1.pt());

            // 2. CLEANUP: Fluent Method Chaining
            // Unrolled the ugly nested static UI calls into a clean, top-to-bottom chain.
            ScrollColumn scrollColumn = new ScrollColumn()
                .AlignItemsStretch()
                .FlexGrow(1f);

            scrollColumn.Add(this.m_listBody);

            this.m_emptyHint = new Label(Tr.LoadCalculationsEmpty)
                .MarginBottom(1.pt());

            // 3. CLEANUP: Delegate Stripping
            // new Action(this.onCloseClicked) becomes just this.onCloseClicked
            Column column = new Column(1.pt());
            column.Add(this.m_emptyHint);
            column.Add(scrollColumn);
            column.Add(new ButtonText(Button.General, Tr.Close, this.onCloseClicked)
                .MarginTop(1.pt())
                .AlignSelfEnd());

            base.Body.Add(column.AlignItemsStretch().FlexGrow(1f));
        }

        public void Prepare(Action<SavedCalculationDocument> onLoad)
        {
            this.m_onLoad = onLoad;
            this.refreshList();
        }

        private void refreshList()
        {
            this.m_listBody.Clear();
            IReadOnlyList<SavedCalculationSummary> readOnlyList = this.m_repository.ListSummaries();

            // 4. CLEANUP: Static Extension to Native Extension
            // UiComponentExtensions.Visible<Label>(this.m_emptyHint, ...) -> this.m_emptyHint.Visible(...)
            this.m_emptyHint.Visible(readOnlyList.Count == 0);

            // 5. CLEANUP: Foreach loop
            // The decompiler often converts simple foreach loops into clunky for(int i=0) loops.
            foreach (var summary in readOnlyList)
            {
                this.m_listBody.Add(this.createSummaryRow(summary));
            }
        }

        private UiComponent createSummaryRow(SavedCalculationSummary summary)
        {
            ProductProto productProto = this.tryResolveIcon(summary.IconProductId);

            Row row = new Row(2.pt())
                .AlignItemsCenterMiddle()
                .MarginBottom(1.pt());

            if (productProto != null)
            {
                // Unrolling the icon construction and tooltip setup
                row.Add(new Icon()
                    .Value(productProto, false)
                    .Size(IconSize)
                    .Tooltip(productProto.Strings.Name, true, false, false));
            }

            Column column = new Column(0.pt());

            // 6. CLEANUP: LocString casting
            // TranslationExtensions.AsLoc(...) becomes "...".AsLoc()
            column.Add(new Label("".AsLoc())
                .Value((summary.Name ?? summary.FileName).AsLoc())
                .FontBold());

            Row row2 = new Row(1.pt());
            row2.Add(new Label("".AsLoc())
                .Value(summary.RowCount.ToString().AsLoc()));

            row2.Add(new Label(Tr.ProductRowsCount)
                .Opacity(0.85f));

            column.Add(row2);

            row.Add(column.FlexGrow(1f));

            // 7. CLEANUP: Lambda Simplification
            // delegate() { this.onLoadClicked(summary); } becomes a clean arrow lambda
            row.Add(new ButtonText(Button.General, Tr.LoadCalculation, () => this.onLoadClicked(summary))
                .MarginRight(1.pt()));

            row.Add(new ButtonText(Button.General, Tr.DeleteCalculation, () => this.onDeleteClicked(summary)));

            return row;
        }

        private ProductProto tryResolveIcon(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId)) return null;

            // 8. CLEANUP: Inline Out Variables (C# 7.0 Feature)
            // Instead of declaring 'ProductProto result;' on a separate line, 
            // you can declare it directly inside the method call!
            if (this.m_service.ProtosDb.TryGetProto(new Proto.ID(productId), out ProductProto result))
            {
                return result;
            }
            return null;
        }

        private void onLoadClicked(SavedCalculationSummary summary)
        {
            try
            {
                SavedCalculationDocument obj = this.m_repository.Load(summary.FileName);

                // 9. CLEANUP: The Null-Conditional Operator (?.)
                // The decompiler wrote an ugly 4-line null check. 
                // The ?. operator safely checks if m_onLoad is null, and only runs Invoke() if it isn't.
                this.m_onLoad?.Invoke(obj);

                this.m_close();
            }
            catch (Exception arg)
            {
                // 10. CLEANUP: String Interpolation ($"")
                // string.Format() is old syntax. Modern C# lets you put variables directly into strings using {}
                Log.Error($"ProductionCalculator: failed to load calculation '{summary.FileName}': {arg}");
            }
        }

        private void onDeleteClicked(SavedCalculationSummary summary)
        {
            try
            {
                this.m_repository.Delete(summary.FileName);
                this.refreshList();
            }
            catch (Exception arg)
            {
                Log.Error($"ProductionCalculator: failed to delete calculation '{summary.FileName}': {arg}");
            }
        }

        private void onCloseClicked()
        {
            this.m_close();
        }
    }
}