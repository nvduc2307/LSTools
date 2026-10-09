using LSTool.Tools.Beams.BeamRebar.types;

namespace LSTool.Tools.Beams.BeamRebar.action
{
    public partial class BeamRebarAction
    {
        /// <summary>
        /// Dựng thép lớp dưới thứ 2 (Bot2) cho toàn bộ dãy dầm.
        /// Theo mặt cắt: Start = 0→L/4, Mid = L/8→7L/8, End = 3L/4→L. Thép gối Start/End liên tục qua cột sang nhịp kế bên hoặc neo vào cột.
        /// Vị trí chia theo lưới chung (xem BeamRebarAction.MainBar.cs).
        /// </summary>
        private void CreateBot2()
        {
            _createMainBarLayer(BeamRebarLayerType.Bot2);
        }
    }
}
