using Mafi;
using Mafi.Core.Products;

namespace ProductionCalculator.Core.Calculation
{
    /// <summary>
    /// An immutable snapshot representing the total flow of a specific product.
    /// Used to pass data from the Calculation Engine to the UI.
    /// </summary>
    public readonly struct ProductFlowTotals
    {
        public ProductProto Product { get; }
        public Fix32 PerMinute { get; }

        public ProductFlowTotals(ProductProto product, Fix32 perMinute)
        {
            this.Product = product;
            this.PerMinute = perMinute;
        }
    }
}