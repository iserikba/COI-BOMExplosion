using System;
using System.Collections.Generic;
using Mafi;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using ProductionCalculator.Core.Calculation;
using ProductionCalculator.Core.Catalog;

namespace ProductionCalculator.Core.Persistence
{
    public static class SavedCalculationLoader
    {
        /// <summary>
        /// Converts the raw JSON saved data back into live game objects.
        /// </summary>
        public static bool TryResolveRow(
            SavedTargetRowData row,
            RecipeCatalog catalog,
            ProtosDb protosDb,
            out ProductionRowFlow flow,
            out ProductProto product,
            out RecipeProto recipe)
        {
            flow = ProductionRowFlow.Output;
            product = null;
            recipe = null;

            if (row == null || catalog == null || protosDb == null) return false;

            // Step 1: Parse the flow (Input vs Output)
            if (!TryParseFlow(row.Flow, out flow)) return false;

            // Step 2: Resolve the Product and Recipe using the ProtosDb
            if (!TryGetProto(protosDb, row.ProductId, out product)) return false;
            if (!TryGetProto(protosDb, row.RecipeId, out recipe)) return false;

            // Step 3: Verify the recipe actually matches the product and flow
            return catalog.RecipeMatchesProductFlow(recipe, product, flow);
        }

        public static IEnumerable<ProductProto> GetIconProductCandidates(ProtosDb protosDb)
        {
            // Simplified the filtering logic using modern LINQ style
            return protosDb.Filter<ProductProto>(p => p.IsStorable);
        }

        private static bool TryParseFlow(string value, out ProductionRowFlow flow)
        {
            if (Enum.TryParse(value, out flow)) return true;

            // Fallback
            flow = ProductionRowFlow.Output;
            return false;
        }

        private static bool TryGetProto<T>(ProtosDb protosDb, string id, out T proto) where T : class, IProto
        {
            proto = default;
            if (string.IsNullOrWhiteSpace(id)) return false;

            if (!protosDb.TryGetProto(new Proto.ID(id), out proto))
            {
                Log.Warning($"ProductionCalculator: proto '{id}' was not found while loading a saved calculation.");
                return false;
            }
            return true;
        }
    }
}