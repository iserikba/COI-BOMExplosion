using System;
using Mafi;
using Mafi.Unity.InputControl;
using Mafi.Unity.Ui.Hud;
using Mafi.Unity.UiStatic.Toolbar;
using UnityEngine;

namespace ProductionCalculator.Ui
{
    // RegistrationMode.AsEverything is the enum value for '3'
    [GlobalDependency(RegistrationMode.AsEverything, false, false)]
    public sealed class ProductionCalculatorUiController : WindowController<ProductionCalculatorWindow>, IToolbarItemController, IUnityInputController
    {
        private const string ToolbarIconPath = "Assets/Unity/UserInterface/Toolbar/Stats.svg";
        private const float ToolbarOrder = 905f;

        // Modern expression-bodied properties for cleaner syntax
        public bool IsVisible => true;
        public bool DeactivateShortcutsIfNotVisible => true;

        // Required by the IToolbarItemController interface
        public event Action<IToolbarItemController> VisibilityChanged;

        public ProductionCalculatorUiController(ControllerContext controllerContext, ToolbarHud toolbar)
            : base(controllerContext, null)
        {
            // Utilizing the constants and proper Enum names instead of magic numbers
            toolbar.AddMainMenuButton(
                Tr.WindowTitle,
                this,
                ToolbarIconPath,
                ToolbarOrder,
                (ShortcutsManager sm) => KeyBindings.FromKey(KbCategory.Tools, ShortcutMode.Game, KeyCode.F10)
            );

            Log.Info("ProductionCalculator: Toolbar button registered (F10)");
        }

        protected override void OnActivate()
        {
            base.Window.RefreshPreview();
        }
    }
}