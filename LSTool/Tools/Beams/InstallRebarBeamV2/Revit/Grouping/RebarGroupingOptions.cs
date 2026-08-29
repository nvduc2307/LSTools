using LSTool.Compatibility;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Revit.Grouping
{
    /// <summary>
    /// Cấu hình gom các thanh thép rời thành rebar set bố trí theo
    /// Fixed Number.
    /// </summary>
    public sealed class RebarGroupingOptions
    {
        /// <summary>
        /// Bật gom nhóm. Tắt thì mỗi thanh giữ nguyên layout Single như
        /// phương án cũ.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Sai số dùng khi so khớp hình học và khoảng cách giữa các thanh.
        /// </summary>
        public double ToleranceFt { get; set; } = 0.5d.MmToFoot();

        /// <summary>
        /// Số thanh tối thiểu để tạo thành một nhóm. Chuỗi ngắn hơn giá trị
        /// này được giữ nguyên dạng thanh rời.
        /// </summary>
        public int MinimumBarsPerGroup { get; set; } = 2;

        public static RebarGroupingOptions Default => new RebarGroupingOptions();
    }
}
