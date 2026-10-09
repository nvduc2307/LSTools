namespace LSTool.Tools.Beams.BeamRebar.types
{
    /// <summary>Lớp thép chủ của dầm.</summary>
    public enum BeamRebarLayerType
    {
        Top1 = 0,
        Top2 = 1,
        Top3 = 2,
        Bot1 = 3,
        Bot2 = 4,
        Bot3 = 5,
    }

    /// <summary>Vùng bố trí của 1 nhóm thép theo chiều dài nhịp.</summary>
    public enum BeamRebarZoneType
    {
        Full = 0,  // suốt nhịp (lớp 1)
        Start = 1, // gối đầu: 0 → L/4
        Mid = 2,   // giữa nhịp: L/8 → 7L/8
        End = 3,   // gối cuối: 3L/4 → L
    }
}
