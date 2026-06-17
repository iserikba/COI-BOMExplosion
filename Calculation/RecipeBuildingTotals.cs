using Mafi;
using Mafi.Core.Buildings.Farms;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Factory.Recipes;

namespace ProductionCalculator.Core.Calculation
{
    /// <summary>
    /// Represents the total building requirement for a specific recipe.
    /// Expanded to support both standard Machines and Farms.
    /// </summary>
    public readonly struct RecipeBuildingTotals
    {
        public RecipeProto Recipe { get; }

        // These can be null depending on what type of building runs the recipe
        public MachineProto Machine { get; }
        public FarmProto Farm { get; }

        public Fix32 MachineCount { get; }

        public RecipeBuildingTotals(RecipeProto recipe, MachineProto machine, FarmProto farm, Fix32 machineCount)
        {
            this.Recipe = recipe;
            this.Machine = machine;
            this.Farm = farm;
            this.MachineCount = machineCount;
        }

        // A helper property to make your UI rendering easier later
        public bool IsFarm => this.Farm != null;
    }

    /// <summary>
    /// Handles the conversion of recipe quantities (per cycle) into 
    /// production rates (per minute).
    /// </summary>
    public static class RecipeRateCalculator
    {
        private static readonly Fix32 s_ticksPerMinute = DurationExtensions.Seconds(60).Ticks;

        public static Fix32 ToPerMinute(IRecipeForUi recipe, int quantityPerCycle)
        {
            if (recipe.Duration.Ticks <= 0) return Fix32.Zero;
            return s_ticksPerMinute * quantityPerCycle / recipe.Duration.Ticks;
        }
    }
}