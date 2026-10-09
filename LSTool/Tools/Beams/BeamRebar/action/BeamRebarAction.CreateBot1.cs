using LSTool.Tools.Beams.BeamRebar.types;

namespace LSTool.Tools.Beams.BeamRebar.action
{
    public partial class BeamRebarAction
    {
        /// <summary>
        /// Dựng thép lớp dưới ngoài cùng (Bot1) cho toàn bộ dãy dầm.
        /// Chạy suốt nhịp, tối thiểu 2 thanh (2 thanh góc). Liên tục qua gối nếu cùng đường kính và chênh cao độ lớp &lt; E0, ngược lại neo vào cột.
        /// Vị trí chia theo lưới chung (xem BeamRebarAction.MainBar.cs).
        /// </summary>
        private void CreateBot1()
        {
            _createMainBarLayer(BeamRebarLayerType.Bot1);
        }
    }
}
