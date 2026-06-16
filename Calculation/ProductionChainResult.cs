using System;
using Mafi;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core.Products;

namespace ProductionCalculator.Core.Calculation
{
    // Token: 0x02000016 RID: 22
    public sealed class ProductionChainResult
    {
        // Token: 0x1700001C RID: 28
        // (get) Token: 0x060000B8 RID: 184 RVA: 0x000057AB File Offset: 0x000039AB
        public ImmutableArray<ProductionTarget> Targets { get; }

        // Token: 0x1700001D RID: 29
        // (get) Token: 0x060000B9 RID: 185 RVA: 0x000057B3 File Offset: 0x000039B3
        public ImmutableArray<RecipeBuildingTotals> Buildings { get; }

        // Token: 0x1700001E RID: 30
        // (get) Token: 0x060000BA RID: 186 RVA: 0x000057BB File Offset: 0x000039BB
        public ImmutableArray<ProductFlowTotals> RawInputs { get; }

        // Token: 0x1700001F RID: 31
        // (get) Token: 0x060000BB RID: 187 RVA: 0x000057C3 File Offset: 0x000039C3
        public ImmutableArray<ProductFlowTotals> TotalOutputs { get; }

        // Token: 0x17000020 RID: 32
        // (get) Token: 0x060000BC RID: 188 RVA: 0x000057CB File Offset: 0x000039CB
        public ImmutableArray<ProductFlowTotals> GrossProduction { get; }

        // Token: 0x17000021 RID: 33
        // (get) Token: 0x060000BD RID: 189 RVA: 0x000057D3 File Offset: 0x000039D3
        public ImmutableArray<ProductFlowTotals> GrossConsumption { get; }

        // Token: 0x17000022 RID: 34
        // (get) Token: 0x060000BE RID: 190 RVA: 0x000057DB File Offset: 0x000039DB
        public static ProductionChainResult Empty { get; } = new ProductionChainResult();

        // Token: 0x060000BF RID: 191 RVA: 0x000057E4 File Offset: 0x000039E4
        private ProductionChainResult()
        {
            this.Targets = ImmutableArray<ProductionTarget>.Empty;
            this.Buildings = ImmutableArray<RecipeBuildingTotals>.Empty;
            this.RawInputs = ImmutableArray<ProductFlowTotals>.Empty;
            this.TotalOutputs = ImmutableArray<ProductFlowTotals>.Empty;
            this.GrossProduction = ImmutableArray<ProductFlowTotals>.Empty;
            this.GrossConsumption = ImmutableArray<ProductFlowTotals>.Empty;
        }

        // Token: 0x060000C0 RID: 192 RVA: 0x00005839 File Offset: 0x00003A39
        public ProductionChainResult(ImmutableArray<ProductionTarget> targets, ImmutableArray<RecipeBuildingTotals> buildings, ImmutableArray<ProductFlowTotals> rawInputs, ImmutableArray<ProductFlowTotals> totalOutputs, ImmutableArray<ProductFlowTotals> grossProduction, ImmutableArray<ProductFlowTotals> grossConsumption)
        {
            this.Targets = targets;
            this.Buildings = buildings;
            this.RawInputs = rawInputs;
            this.TotalOutputs = totalOutputs;
            this.GrossProduction = grossProduction;
            this.GrossConsumption = grossConsumption;
        }

        // Token: 0x060000C1 RID: 193 RVA: 0x0000586E File Offset: 0x00003A6E
        public bool TryGetSuggestedRateForFlow(ProductProto product, ProductionRowFlow flow, out Fix32 rate)
        {
            if (flow != ProductionRowFlow.Input)
            {
                return ProductionChainResult.tryFindRate(this.GrossConsumption, product, out rate);
            }
            return ProductionChainResult.tryFindRate(this.GrossProduction, product, out rate);
        }

        // Token: 0x060000C2 RID: 194 RVA: 0x00005890 File Offset: 0x00003A90
        private static bool tryFindRate(ImmutableArray<ProductFlowTotals> flows, ProductProto product, out Fix32 rate)
        {
            foreach (ProductFlowTotals productFlowTotals in flows)
            {
                if (productFlowTotals.Product == product)
                {
                    rate = productFlowTotals.PerMinute;
                    return true;
                }
            }
            rate = Fix32.Zero;
            return false;
        }
    }
}
