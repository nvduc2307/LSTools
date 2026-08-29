namespace LSTool.Tools.Beams.InstallRebarBeamV2.Revit.Grouping
{
    /// <summary>
    /// Mô tả một nhóm thép đã được chuyển sang layout Fixed Number.
    /// </summary>
    public sealed class RebarLayoutGroupSummary
    {
        /// <summary>Id của thanh đại diện, thanh giữ lại và mang cả nhóm.</summary>
        public long RepresentativeRebarId { get; set; }

        /// <summary>Id của các thanh đã bị xoá vì đã nằm trong nhóm.</summary>
        public List<long> AbsorbedRebarIds { get; set; } = new List<long>();

        public long HostId { get; set; }
        public int BarCount { get; set; }
        public double SpacingMm { get; set; }
        public double ArrayLengthMm { get; set; }
        public bool BarsOnNormalSide { get; set; }
    }

    public sealed class RebarLayoutGroupingResult
    {
        public List<RebarLayoutGroupSummary> Groups { get; set; } =
            new List<RebarLayoutGroupSummary>();

        /// <summary>Các thanh đã bị xoá sau khi gom nhóm.</summary>
        public HashSet<long> RemovedRebarIds { get; set; } = new HashSet<long>();

        /// <summary>Số thanh đầu vào không đủ điều kiện gom, giữ nguyên Single.</summary>
        public int UngroupedBarCount { get; set; }

        /// <summary>Các nhóm đã thử gom nhưng Revit không dựng đúng, đã hoàn về Single.</summary>
        public List<string> RejectedGroups { get; set; } = new List<string>();

        public int GroupCount => Groups.Count;
    }
}
