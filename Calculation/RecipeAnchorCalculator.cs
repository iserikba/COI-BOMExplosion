using Mafi;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Products;
using System;

namespace ProductionCalculator.Core.Calculation
{

    // Token: 0x0200001B RID: 27
    public readonly struct RecipeBuildingTotals
    {
        // Token: 0x17000028 RID: 40
        // (get) Token: 0x060000D6 RID: 214 RVA: 0x00005EC8 File Offset: 0x000040C8
        public RecipeProto Recipe { get; }

        // Token: 0x17000029 RID: 41
        // (get) Token: 0x060000D7 RID: 215 RVA: 0x00005ED0 File Offset: 0x000040D0
        public MachineProto Machine { get; }

        // Token: 0x1700002A RID: 42
        // (get) Token: 0x060000D8 RID: 216 RVA: 0x00005ED8 File Offset: 0x000040D8
        public Fix32 MachineCount { get; }

        // Token: 0x060000D9 RID: 217 RVA: 0x00005EE0 File Offset: 0x000040E0
        public RecipeBuildingTotals(RecipeProto recipe, MachineProto machine, Fix32 machineCount)
        {
            this.Recipe = recipe;
            this.Machine = machine;
            this.MachineCount = machineCount;
        }
    }

    // Token: 0x0200001C RID: 28
    public static class RecipeRateCalculator
    {
        // Token: 0x060000DA RID: 218 RVA: 0x00005EF7 File Offset: 0x000040F7
        public static Fix32 ToPerMinute(IRecipeForUi recipe, int quantityPerCycle)
        {
            if (recipe.Duration.Ticks <= 0)
            {
                return Fix32.Zero;
            }
            return RecipeRateCalculator.s_ticksPerMinute * quantityPerCycle / recipe.Duration.Ticks;
        }

        // Token: 0x04000085 RID: 133
        private static readonly Fix32 s_ticksPerMinute = DurationExtensions.Seconds(60).Ticks;
    }

    // Token: 0x0200001A RID: 26
    public static class RecipeAnchorCalculator
    {
        // Token: 0x060000D1 RID: 209 RVA: 0x00005DB9 File Offset: 0x00003FB9
        public static Fix32 GetAnchorRatePerMachine(RecipeProto recipe, ProductProto product, ProductionRowFlow flow)
        {
            if (flow != ProductionRowFlow.Output)
            {
                return RecipeAnchorCalculator.getInputRatePerMachine(recipe, product);
            }
            return RecipeAnchorCalculator.getOutputRatePerMachine(recipe, product);
        }

        // Token: 0x060000D2 RID: 210 RVA: 0x00005DCD File Offset: 0x00003FCD
        public static Fix32 GetRateForMachines(RecipeProto recipe, ProductProto product, ProductionRowFlow flow, Fix32 machines)
        {
            return RecipeAnchorCalculator.GetAnchorRatePerMachine(recipe, product, flow) * machines;
        }

        // Token: 0x060000D3 RID: 211 RVA: 0x00005DE0 File Offset: 0x00003FE0
        public static Fix32 GetMachinesForRate(RecipeProto recipe, ProductProto product, ProductionRowFlow flow, Fix32 ratePerMinute)
        {
            Fix32 anchorRatePerMachine = RecipeAnchorCalculator.GetAnchorRatePerMachine(recipe, product, flow);
            if (anchorRatePerMachine <= Fix32.Zero)
            {
                return Fix32.Zero;
            }
            return ratePerMinute / anchorRatePerMachine;
        }

        // Token: 0x060000D4 RID: 212 RVA: 0x00005E10 File Offset: 0x00004010
        private static Fix32 getOutputRatePerMachine(RecipeProto recipe, ProductProto product)
        {
            foreach (RecipeOutput recipeOutput in recipe.AllUserVisibleOutputs)
            {
                if (!recipeOutput.HideInUi && !(recipeOutput.Product != product))
                {
                    return RecipeRateCalculator.ToPerMinute(recipe, recipeOutput.Quantity.Value);
                }
            }
            return Fix32.Zero;
        }

        // Token: 0x060000D5 RID: 213 RVA: 0x00005E6C File Offset: 0x0000406C
        private static Fix32 getInputRatePerMachine(RecipeProto recipe, ProductProto product)
        {
            foreach (RecipeInput recipeInput in recipe.AllUserVisibleInputs)
            {
                if (!recipeInput.HideInUi && !(recipeInput.Product != product))
                {
                    return RecipeRateCalculator.ToPerMinute(recipe, recipeInput.Quantity.Value);
                }
            }
            return Fix32.Zero;
        }
    }
}
