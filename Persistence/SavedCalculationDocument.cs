using System;
using System.Collections.Generic;

namespace ProductionCalculator.Core.Persistence
{
    // Token: 0x0200000D RID: 13
    public sealed class SavedCalculationDocument
    {
        // Token: 0x1700000A RID: 10
        // (get) Token: 0x0600005F RID: 95 RVA: 0x0000406F File Offset: 0x0000226F
        // (set) Token: 0x06000060 RID: 96 RVA: 0x00004077 File Offset: 0x00002277
        public int Version { get; set; } = 1;

        // Token: 0x1700000B RID: 11
        // (get) Token: 0x06000061 RID: 97 RVA: 0x00004080 File Offset: 0x00002280
        // (set) Token: 0x06000062 RID: 98 RVA: 0x00004088 File Offset: 0x00002288
        public string Name { get; set; }

        // Token: 0x1700000C RID: 12
        // (get) Token: 0x06000063 RID: 99 RVA: 0x00004091 File Offset: 0x00002291
        // (set) Token: 0x06000064 RID: 100 RVA: 0x00004099 File Offset: 0x00002299
        public string IconProductId { get; set; }

        // Token: 0x1700000D RID: 13
        // (get) Token: 0x06000065 RID: 101 RVA: 0x000040A2 File Offset: 0x000022A2
        // (set) Token: 0x06000066 RID: 102 RVA: 0x000040AA File Offset: 0x000022AA
        public List<SavedTargetRowData> Rows { get; set; } = new List<SavedTargetRowData>();

        // Token: 0x04000057 RID: 87
        public const int CurrentVersion = 1;
    }
}
