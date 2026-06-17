using Mafi;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Products;

namespace ProductionCalculator.Core.Calculation
{
    /// <summary>
    /// Represents the total machine requirement for a specific recipe.
    /// Used by the UI to list required buildings.
    /// </summary>
    /*public readonly struct RecipeBuildingTotals
    {
        public RecipeProto Recipe { get; }
        public MachineProto Machine { get; }
        public Fix32 MachineCount { get; }

        public RecipeBuildingTotals(RecipeProto recipe, MachineProto machine, Fix32 machineCount)
        {
            this.Recipe = recipe;
            this.Machine = machine;
            this.MachineCount = machineCount;
        }
    }


    */
    /// <summary>
    /// Determines the "Anchor Rate": The exact speed at which one machine 
    /// produces or consumes a specific product for a given recipe.
    /// </summary>
    public static class RecipeAnchorCalculator
    {
        public static Fix32 GetAnchorRatePerMachine(RecipeProto recipe, ProductProto product, ProductionRowFlow flow)
        {
            return (flow == ProductionRowFlow.Output)
                ? GetOutputRatePerMachine(recipe, product)
                : GetInputRatePerMachine(recipe, product);
        }

        public static Fix32 GetRateForMachines(RecipeProto recipe, ProductProto product, ProductionRowFlow flow, Fix32 machines)
        {
            return GetAnchorRatePerMachine(recipe, product, flow) * machines;
        }

        public static Fix32 GetMachinesForRate(RecipeProto recipe, ProductProto product, ProductionRowFlow flow, Fix32 ratePerMinute)
        {
            Fix32 anchor = GetAnchorRatePerMachine(recipe, product, flow);
            return (anchor > Fix32.Zero) ? (ratePerMinute / anchor) : Fix32.Zero;
        }

        private static Fix32 GetOutputRatePerMachine(RecipeProto recipe, ProductProto product)
        {
            foreach (var output in recipe.AllUserVisibleOutputs)
            {
                if (!output.HideInUi && output.Product == product)
                    return RecipeRateCalculator.ToPerMinute(recipe, output.Quantity.Value);
            }
            return Fix32.Zero;
        }

        private static Fix32 GetInputRatePerMachine(RecipeProto recipe, ProductProto product)
        {
            foreach (var input in recipe.AllUserVisibleInputs)
            {
                if (!input.HideInUi && input.Product == product)
                    return RecipeRateCalculator.ToPerMinute(recipe, input.Quantity.Value);
            }
            return Fix32.Zero;
        }
    }
}