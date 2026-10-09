namespace LSTool.Tools.Beams.BeamRebar.types
{
    /// <summary>Loại đai phụ của dầm.</summary>
    public enum BeamTieType
    {
        /// <summary>Đai phụ đứng: nối 1 thanh Top1 với 1 thanh Bot1 cùng Index (chỉ thanh bên trong).</summary>
        Vertical = 0,
        /// <summary>Đai phụ ngang: nối 2 thanh ngoài cùng của lớp Top2/Top3/Bot2/Bot3 (lớp có &gt;= 2 thanh).</summary>
        Horizontal = 1,
    }
}
