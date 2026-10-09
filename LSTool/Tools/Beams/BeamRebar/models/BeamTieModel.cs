namespace LSTool.Tools.Beams.BeamRebar.models
{
    /// <summary>
    /// 1 đai phụ của dầm – hình dạng áp dụng cho cả 3 mặt cắt (Start / Mid / End).
    /// Vertical  : Index = vị trí trên lưới chia; nối Top1[Index] với Bot1[Index].
    /// Horizontal: Layer = lớp thép (BeamRebarLayerType: Top2, Top3, Bot2, Bot3);
    ///             nối 2 thanh ngoài cùng của lớp đó (mặt cắt nào lớp có &lt; 2 thanh thì bỏ qua).
    /// Lưu JSON vào Extensible Storage của dầm nên chỉ dùng kiểu nguyên thủy.
    /// </summary>
    public class BeamTieModel
    {
        public int Type { get; set; }   // BeamTieType
        public int Layer { get; set; }  // BeamRebarLayerType (Horizontal)
        public int Index { get; set; }  // Vertical
    }
}
