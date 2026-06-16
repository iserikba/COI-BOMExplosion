using Mafi.Localization;

namespace ProductionCalculator
{
    /// <summary>
    /// Localization Dictionary.
    /// Centralized storage for all translatable strings in the mod.
    /// This allows for easy language support and keeps the UI code tidy.
    /// </summary>
    internal static class Tr
    {
        // --- Main Window ---
        public static readonly LocStr WindowTitle = Loc.Str("ProductionCalculator__WindowTitle", "Bill of Materials Explosion", "mod window title");
        public static readonly LocStr TargetsTitle = Loc.Str("ProductionCalculator__TargetsTitle", "Products", "section title for user-defined production rows");
        public static readonly LocStr BuildingsTitle = Loc.Str("ProductionCalculator__BuildingsTitle", "Buildings occupation", "section title for machine counts per recipe");
        public static readonly LocStr InputsTitle = Loc.Str("ProductionCalculator__InputsTitle", "Required inputs per month", "section title for products that must be imported into the chain");
        public static readonly LocStr OutputsTitle = Loc.Str("ProductionCalculator__OutputsTitle", "Target recipe outputs per month", "section title for outputs of the selected target recipe only");
        public static readonly LocStr NoTargets = Loc.Str("ProductionCalculator__NoTargets", "Add at least one product with a production rate.", "hint when no valid production rows are configured");
        public static readonly LocStr NoResults = Loc.Str("ProductionCalculator__NoResults", "No production chain could be calculated for the selected products.", "hint when calculation returns empty result");

        // --- Buttons & Inputs ---
        public static readonly LocStr AddTarget = Loc.Str("ProductionCalculator__AddTarget", "Add product", "button to add another production row");
        public static readonly LocStr SyncChainRates = Loc.Str("ProductionCalculator__SyncChainRates", "Sync chain rates", "button to update row rates from the current chain calculation");
        public static readonly LocStr Cancel = Loc.Str("ProductionCalculator__Cancel", "Cancel", "cancel dialog button");
        public static readonly LocStr Close = Loc.Str("ProductionCalculator__Close", "Close", "close dialog button");
        public static readonly LocStr FixRate = Loc.Str("ProductionCalculator__FixRate", "Fixed", "toggle to keep the row rate when syncing the chain");
        public static readonly LocStr FixRateTooltip = Loc.Str("ProductionCalculator__FixRateTooltip", "Keep rate and machine count when using Sync chain rates", "tooltip for the fixed-rate toggle");
        public static readonly LocStr RatePerMinute = Loc.Str("ProductionCalculator__RatePerMinute", "/min", "suffix label for production rate input");
        public static readonly LocStr MachineCount = Loc.Str("ProductionCalculator__MachineCount", "machines", "label suffix for required machine count");
        public static readonly LocStr SelectRecipe = Loc.Str("ProductionCalculator__SelectRecipe", "Select recipe", "placeholder when recipe must be chosen");
        public static readonly LocStr FlowOutput = Loc.Str("ProductionCalculator__FlowOutput", "Output", "product row: anchor rate on recipe output");
        public static readonly LocStr FlowInput = Loc.Str("ProductionCalculator__FlowInput", "Input", "product row: anchor rate on recipe input (consumption)");

        // --- Save/Load System ---
        public static readonly LocStr SaveCalculation = Loc.Str("ProductionCalculator__SaveCalculation", "Save", "button to save the current calculation preset");
        public static readonly LocStr LoadCalculations = Loc.Str("ProductionCalculator__LoadCalculations", "Load", "button to open saved calculation presets");
        public static readonly LocStr SaveCalculationTitle = Loc.Str("ProductionCalculator__SaveCalculationTitle", "Save calculation", "title for save calculation dialog");
        public static readonly LocStr LoadCalculationsTitle = Loc.Str("ProductionCalculator__LoadCalculationsTitle", "Saved calculations", "title for saved calculations browser");
        public static readonly LocStr SaveCalculationName = Loc.Str("ProductionCalculator__SaveCalculationName", "Name", "label for saved calculation name");
        public static readonly LocStr SaveCalculationIcon = Loc.Str("ProductionCalculator__SaveCalculationIcon", "Icon", "label for saved calculation icon product");
        public static readonly LocStr SaveCalculationConfirm = Loc.Str("ProductionCalculator__SaveCalculationConfirm", "Save", "confirm save calculation button");
        public static readonly LocStr LoadCalculation = Loc.Str("ProductionCalculator__LoadCalculation", "Load", "load saved calculation button");
        public static readonly LocStr DeleteCalculation = Loc.Str("ProductionCalculator__DeleteCalculation", "Delete", "delete saved calculation button");
        public static readonly LocStr DefaultCalculationName = Loc.Str("ProductionCalculator__DefaultCalculationName", "New calculation", "default name for a saved calculation");
        public static readonly LocStr ProductRowsCount = Loc.Str("ProductionCalculator__ProductRowsCount", "product rows", "suffix for number of rows in a saved calculation");
        public static readonly LocStr LoadCalculationsEmpty = Loc.Str("ProductionCalculator__LoadCalculationsEmpty", "No saved calculations yet.", "hint when saved calculations folder is empty");

        // --- Validation Errors ---
        public static readonly LocStr SaveCalculationNameRequired = Loc.Str("ProductionCalculator__SaveCalculationNameRequired", "Enter a name for this calculation.", "validation error when save name is empty");
        public static readonly LocStr SaveCalculationIconRequired = Loc.Str("ProductionCalculator__SaveCalculationIconRequired", "Choose an icon product.", "validation error when save icon is missing");
        public static readonly LocStr SaveCalculationEmptyChain = Loc.Str("ProductionCalculator__SaveCalculationEmptyChain", "Add at least one valid product row before saving.", "validation error when there is nothing to save");
        public static readonly LocStr SaveCalculationFailed = Loc.Str("ProductionCalculator__SaveCalculationFailed", "Could not save the calculation.", "generic save failure message");
    }
}