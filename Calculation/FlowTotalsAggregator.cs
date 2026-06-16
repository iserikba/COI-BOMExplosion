using System;
using System.Collections.Generic;
using Mafi;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core.Products;

namespace ProductionCalculator.Core.Calculation
{
    /// <summary>
    /// A utility class for combining and filtering production rates.
    /// It handles floating-point/fixed-point stabilization, ensuring that 
    /// microscopic rounding errors don't show up in the UI.
    /// </summary>
    internal static class FlowTotalsAggregator
    {
        // By discarding any net rates under 0.05/min, we prevent "ghost" items
        // from appearing in the UI due to tiny math rounding errors in the engine.
        private static readonly Fix32 s_balanceTolerance = Fix32.FromFloat(0.05f);
        private static readonly Fix32 s_absoluteMinNetRate = Fix32.FromFloat(0.05f);
        private static readonly Fix32 s_displayRateStep = Fix32.FromFloat(0.01f);

        /// <summary>
        /// Calculates the net production rate (primary - subtract).
        /// Contains strict tolerance checks to safely balance out inputs and outputs.
        /// </summary>
        public static Fix32 ComputeNetRate(Fix32 primary, Fix32 subtract)
        {
            if (primary <= Fix32.Zero) return Fix32.Zero;
            if (subtract <= Fix32.Zero) return SanitizePositiveNet(primary);

            // Calculate the absolute difference between what is produced and what is consumed
            Fix32 absoluteDifference = (primary > subtract) ? (primary - subtract) : (subtract - primary);

            // CRITICAL LOGIC: The "Balance Tolerance"
            // If a factory produces 10.0001 Water and consumes 10.0 Water, we don't want 
            // the UI demanding an input of exactly 0.0001 Water. We treat it as perfectly balanced.
            if (absoluteDifference <= s_balanceTolerance) return Fix32.Zero;

            // If we are producing more than we are consuming, calculate the true net output
            Fix32 net = primary - subtract;
            if (net <= Fix32.Zero) return Fix32.Zero;

            return SanitizePositiveNet(net);
        }

        private static Fix32 SanitizePositiveNet(Fix32 net)
        {
            Fix32 roundedNet = RoundToDisplayStep(net);

            // Final safety net: if the rounding pushed it below our minimum threshold, discard it.
            if (roundedNet <= Fix32.Zero || roundedNet < s_absoluteMinNetRate)
            {
                return Fix32.Zero;
            }

            return roundedNet;
        }

        private static Fix32 RoundToDisplayStep(Fix32 value)
        {
            if (value <= Fix32.Zero) return Fix32.Zero;

            // Note: Converting Mafi's deterministic Fix32 into standard float for display rounding.
            // This is safe because this mod is "IsUiOnly = true" and doesn't affect the physical game state.
            double valFloat = value.ToFloat();
            double stepFloat = s_displayRateStep.ToFloat();

            return Fix32.FromFloat((float)(Math.Round(valFloat / stepFloat) * stepFloat));
        }

        /// <summary>
        /// Accumulates the rate of a specific product in a dictionary.
        /// </summary>
        public static void AddRate(Dictionary<ProductProto, Fix32> rates, ProductProto product, Fix32 perMinute)
        {
            if (product == null || perMinute == Fix32.Zero) return;

            // Modern C# dictionary lookup pattern
            if (rates.TryGetValue(product, out Fix32 existingRate))
            {
                rates[product] = existingRate + perMinute;
            }
            else
            {
                rates.Add(product, perMinute);
            }
        }

        /// <summary>
        /// Converts the raw dictionary into a clean, sorted, immutable list ready for the UI to render.
        /// </summary>
        public static ImmutableArray<ProductFlowTotals> ToSortedTotals(Dictionary<ProductProto, Fix32> rates)
        {
            if (rates.Count == 0) return ImmutableArray<ProductFlowTotals>.Empty;

            var validTotals = new List<ProductFlowTotals>(rates.Count);

            foreach (var kvp in rates)
            {
                if (kvp.Value > Fix32.Zero)
                {
                    validTotals.Add(new ProductFlowTotals(kvp.Key, kvp.Value));
                }
            }

            if (validTotals.Count == 0) return ImmutableArray<ProductFlowTotals>.Empty;

            // Sort alphabetically by the product's internal string ID so the UI list never randomly shuffles.
            validTotals.Sort((left, right) => string.Compare(left.Product.Id.Value, right.Product.Id.Value, StringComparison.Ordinal));

            return validTotals.ToImmutableArray();
        }
    }
}