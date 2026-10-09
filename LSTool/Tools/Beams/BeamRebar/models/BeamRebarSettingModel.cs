namespace LSTool.Tools.Beams.BeamRebar.models
{
    /// <summary>Một setting thép dầm đã lưu (Save / Save As) – gồm cấu hình 3 mặt cắt.</summary>
    public class BeamRebarSettingModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public BeamRebarSectionSettingModel SectionStart { get; set; } = new BeamRebarSectionSettingModel();
        public BeamRebarSectionSettingModel SectionMid { get; set; } = new BeamRebarSectionSettingModel();
        public BeamRebarSectionSettingModel SectionEnd { get; set; } = new BeamRebarSectionSettingModel();
    }

    /// <summary>Cấu hình thép của 1 mặt cắt: tên đường kính + số lượng (Stirrup / SideBar: Spacing).</summary>
    public class BeamRebarSectionSettingModel
    {
        public BeamRebarItemSettingModel RebarTop1 { get; set; } = new BeamRebarItemSettingModel();
        public BeamRebarItemSettingModel RebarTop2 { get; set; } = new BeamRebarItemSettingModel();
        public BeamRebarItemSettingModel RebarTop3 { get; set; } = new BeamRebarItemSettingModel();
        public BeamRebarItemSettingModel RebarBot1 { get; set; } = new BeamRebarItemSettingModel();
        public BeamRebarItemSettingModel RebarBot2 { get; set; } = new BeamRebarItemSettingModel();
        public BeamRebarItemSettingModel RebarBot3 { get; set; } = new BeamRebarItemSettingModel();
        public BeamRebarItemSettingModel SideBar { get; set; } = new BeamRebarItemSettingModel();
        public BeamRebarItemSettingModel Stirrup { get; set; } = new BeamRebarItemSettingModel();
    }

    public class BeamRebarItemSettingModel
    {
        public string Name { get; set; }
        public int Spacing { get; set; }
    }
}
