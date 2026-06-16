using System;
using Mafi;
using Mafi.Core.Products;

namespace ProductionCalculator.Core.Calculation
{
    // Token: 0x02000015 RID: 21
    public readonly struct ProductFlowTotals
    {
        // Token: 0x1700001A RID: 26
        // (get) Token: 0x060000B5 RID: 181 RVA: 0x0000578B File Offset: 0x0000398B
        public ProductProto Product { get; }

        // Token: 0x1700001B RID: 27
        // (get) Token: 0x060000B6 RID: 182 RVA: 0x00005793 File Offset: 0x00003993
        public Fix32 PerMinute { get; }

        // Token: 0x060000B7 RID: 183 RVA: 0x0000579B File Offset: 0x0000399B
        public ProductFlowTotals(ProductProto product, Fix32 perMinute)
        {
            this.Product = product;
            this.PerMinute = perMinute;
        }
    }
}
