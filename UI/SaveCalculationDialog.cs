using Mafi;
using Mafi.Core.Products;
using Mafi.Localization;
using Mafi.Unity.Ui;
using Mafi.Unity.Ui.Library;
using Mafi.Unity.UiToolkit;
using Mafi.Unity.UiToolkit.Component;
using Mafi.Unity.UiToolkit.Library;
using ProductionCalculator.Core.Persistence;
using ProductionCalculator.Core.Services;
using System;
using System.Collections.Generic;

namespace ProductionCalculator.Ui
{
    internal sealed class SaveCalculationDialog : PanelWithHeader
    {
        private const string DefaultNameText = "New calculation";

        private readonly SavedCalculationRepository m_repository;
        private readonly ProductionCalculatorService m_service;
        private readonly Action m_close;
        private readonly TextField m_nameField;
        private readonly Label m_errorLabel;

        private ProductProto m_iconProduct;
        private Func<IReadOnlyList<SavedTargetRowData>> m_getRows;
        private Action<string, ProductProto> m_onSaved;

        public SaveCalculationDialog(SavedCalculationRepository repository, ProductionCalculatorService service, Action close)
            : base(Tr.SaveCalculationTitle)
        {
            this.m_repository = repository;
            this.m_service = service;
            this.m_close = close;

            // 1. CLEANUP: Fluent picker construction
            var iconPicker = new SingleProductPickerUi(
                this.getIconProducts,
                this.onIconSelected,
                () => this.m_iconProduct == null ? Option<ProductProto>.None : Option<ProductProto>.Some(this.m_iconProduct),
                this.onIconCleared,
                null, true, true);

            this.m_nameField = new TextField()
                .Text(DefaultNameText.AsLoc())
                .OnEditEnd(this.onNameEdited);

            // 2. CLEANUP: Inline styles
            // The decompiler had 4 static class calls here. We can just use the fluent helper!
            this.m_errorLabel = new Label("".AsLoc())
                .FontBold()
                .Class(Cls.danger)
                .Visible(false);

            // Assemble the UI
            Column column = new Column(2.pt());

            column.Add(new Label(Tr.SaveCalculationName).FontBold());
            column.Add(this.m_nameField);

            column.Add(new Label(Tr.SaveCalculationIcon).FontBold().MarginTop(1.pt()));
            column.Add(iconPicker);
            column.Add(this.m_errorLabel);

            Row buttons = new Row(1.pt()).MarginTop(2.pt());
            buttons.Add(new ButtonText(Button.General, Tr.SaveCalculationConfirm, this.onSaveClicked));
            buttons.Add(new ButtonText(Button.General, Tr.Cancel, this.onCancelClicked));
                

            column.Add(buttons);
            base.Body.Add(column.AlignItemsStretch());
        }

        public void Prepare(Func<IReadOnlyList<SavedTargetRowData>> getRows, string defaultName, ProductProto defaultIconProduct, Action<string, ProductProto> onSaved = null)
        {
            this.m_getRows = getRows;
            this.m_onSaved = onSaved;
            this.m_iconProduct = defaultIconProduct;

            string name = string.IsNullOrWhiteSpace(defaultName) ? DefaultNameText : defaultName.Trim();
            this.m_nameField.Text(name.AsLoc());
            this.m_errorLabel.Visible(false);
        }

        private void onNameEdited(string text) => this.m_errorLabel.Visible(false);
        private void onIconSelected(ProductProto product) { this.m_iconProduct = product; this.m_errorLabel.Visible(false); }
        private void onIconCleared() => this.m_iconProduct = null;
        private IEnumerable<ProductProto> getIconProducts() => SavedCalculationLoader.GetIconProductCandidates(this.m_service.ProtosDb);

        private void onSaveClicked()
        {
            string name = this.m_nameField.GetText()?.Trim();

            // Validation Logic
            if (string.IsNullOrWhiteSpace(name)) { this.showError(Tr.SaveCalculationNameRequired); return; }
            if (this.m_iconProduct == null) { this.showError(Tr.SaveCalculationIconRequired); return; }

            var rows = this.m_getRows?.Invoke();
            if (rows == null || rows.Count == 0) { this.showError(Tr.SaveCalculationEmptyChain); return; }

            // Build Document
            var document = new SavedCalculationDocument
            {
                Name = name,
                IconProductId = this.m_iconProduct.Id.Value,
                Rows = new List<SavedTargetRowData>(rows)
            };

            try
            {
                this.m_repository.Save(document);
                this.m_onSaved?.Invoke(name, this.m_iconProduct);
                this.m_close();
            }
            catch (Exception ex)
            {
                Log.Error($"ProductionCalculator: failed to save calculation: {ex}");
                this.showError(Tr.SaveCalculationFailed);
            }
        }

        private void onCancelClicked() => this.m_close();

        private void showError(LocStr message)
        {
            this.m_errorLabel.Value(message).Visible(true);
        }
    }
}