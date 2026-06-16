using System;
using System.Collections.Generic;
using Mafi;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core.Products;

namespace ProductionCalculator.Core.Calculation
{
	// Token: 0x02000014 RID: 20
	internal static class FlowTotalsAggregator
	{
		// Token: 0x060000AF RID: 175 RVA: 0x00005554 File Offset: 0x00003754
		public static Fix32 ComputeNetRate(Fix32 primary, Fix32 subtract)
		{
			if (primary <= Fix32.Zero)
			{
				return Fix32.Zero;
			}
			if (subtract <= Fix32.Zero)
			{
				return FlowTotalsAggregator.sanitizePositiveNet(primary);
			}
			Fix32 fix = (primary > subtract) ? (primary - subtract) : (subtract - primary);
			if (fix <= FlowTotalsAggregator.s_balanceTolerance)
			{
				return Fix32.Zero;
			}
			Fix32 fix2 = primary - subtract;
			if (fix2 <= Fix32.Zero)
			{
				return Fix32.Zero;
			}
			return FlowTotalsAggregator.sanitizePositiveNet(fix2);
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x000055D8 File Offset: 0x000037D8
		private static Fix32 sanitizePositiveNet(Fix32 net)
		{
			Fix32 fix = FlowTotalsAggregator.roundToDisplayStep(net);
			if (fix <= Fix32.Zero || fix < FlowTotalsAggregator.s_absoluteMinNetRate)
			{
				return Fix32.Zero;
			}
			return fix;
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x0000560D File Offset: 0x0000380D
		private static Fix32 roundToDisplayStep(Fix32 value)
		{
			if (value <= Fix32.Zero)
			{
				return Fix32.Zero;
			}
			return Fix32.FromFloat((float)(Math.Round((double)(value.ToFloat() / FlowTotalsAggregator.s_displayRateStep.ToFloat())) * (double)FlowTotalsAggregator.s_displayRateStep.ToFloat()));
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x0000564C File Offset: 0x0000384C
		public static void AddRate(Dictionary<ProductProto, Fix32> rates, ProductProto product, Fix32 perMinute)
		{
			if (product == null || perMinute == Fix32.Zero)
			{
				return;
			}
			Fix32 fix;
			if (rates.TryGetValue(product, out fix))
			{
				rates[product] = fix + perMinute;
				return;
			}
			rates.Add(product, perMinute);
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x00005694 File Offset: 0x00003894
		public static ImmutableArray<ProductFlowTotals> ToSortedTotals(Dictionary<ProductProto, Fix32> rates)
		{
			if (rates.Count == 0)
			{
				return ImmutableArray<ProductFlowTotals>.Empty;
			}
			List<ProductFlowTotals> list = new List<ProductFlowTotals>(rates.Count);
			foreach (KeyValuePair<ProductProto, Fix32> keyValuePair in rates)
			{
				if (!(keyValuePair.Value <= Fix32.Zero))
				{
					list.Add(new ProductFlowTotals(keyValuePair.Key, keyValuePair.Value));
				}
			}
			if (list.Count == 0)
			{
				return ImmutableArray<ProductFlowTotals>.Empty;
			}
			list.Sort((ProductFlowTotals left, ProductFlowTotals right) => string.Compare(left.Product.Id.Value, right.Product.Id.Value, StringComparison.Ordinal));
			return ImmutableArray.ToImmutableArray<ProductFlowTotals>(list);
		}

		// Token: 0x0400006E RID: 110
		private static readonly Fix32 s_balanceTolerance = Fix32.FromFloat(0.05f);

		// Token: 0x0400006F RID: 111
		private static readonly Fix32 s_absoluteMinNetRate = Fix32.FromFloat(0.05f);

		// Token: 0x04000070 RID: 112
		private static readonly Fix32 s_displayRateStep = Fix32.FromFloat(0.01f);
	}
}
