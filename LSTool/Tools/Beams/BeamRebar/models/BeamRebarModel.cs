using LSTool.MVVM.model.Structures;

namespace LSTool.Tools.Beams.BeamRebar.models
{
    public class BeamRebarModel : ConcreteModel
    {
        public static double COVER = 40;
        public BeamRebarSectionModel SectionStart { get; set; }
        public BeamRebarSectionModel SectionMid { get; set; }
        public BeamRebarSectionModel SectionEnd { get; set; }
        public BeamBearingModel BeamBearingStart { get; set; }
        public BeamBearingModel BeamBearingEnd { get; set; }
        public BeamFaceModel FaceTop { get; set; }
        public BeamFaceModel FaceRight { get; set; }
        public BeamFaceModel FaceBot { get; set; }
        public BeamFaceModel FaceLeft { get; set; }

        /// <summary>Số vị trí trên lưới chia thép chủ (chung cho cả dãy dầm).</summary>
        public int GridQty { get; set; }
        /// <summary>Vị trí các thanh thép chủ đã dựng (dùng cho đai móc phụ).</summary>
        public List<BeamRebarPositionModel> RebarMainPositions { get; set; } = new List<BeamRebarPositionModel>();
        /// <summary>Đai phụ (đứng / ngang) – hình dạng chung cho 3 mặt cắt của dầm.</summary>
        public List<BeamTieModel> Ties { get; set; } = new List<BeamTieModel>();
    }
}
