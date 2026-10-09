namespace LSTool.Tools.Beams.BeamRebar.models
{
    /// <summary>Cài đặt chung cho dầm (SettingBeam.json).</summary>
    public class SettingBeamModel
    {
        public double StressZone { get; set; } = 0.25;
        public double E0 { get; set; } = 100; // mm – chênh lệch tối đa để thép chạy liên tục qua gối
    }
}
