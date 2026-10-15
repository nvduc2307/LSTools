namespace LSTool.Tools.Beams.BeamRebar.models
{
    public class BeamRebarSettingModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public BeamRebarSectionSettingModel SectionStart { get; set; } = new BeamRebarSectionSettingModel();
        public BeamRebarSectionSettingModel SectionMid { get; set; } = new BeamRebarSectionSettingModel();
        public BeamRebarSectionSettingModel SectionEnd { get; set; } = new BeamRebarSectionSettingModel();
        public double AnchorStartTopL1 { get; set; } = 40;
        public double AnchorStartTopL3 { get; set; } = 10;
        public double AnchorStartBotL2 { get; set; } = 30;
        public double AnchorStartBotL3 { get; set; } = 10;
        public double AnchorEndTopL1 { get; set; } = 40;
        public double AnchorEndTopL3 { get; set; } = 10;
        public double AnchorEndBotL2 { get; set; } = 30;
        public double AnchorEndBotL3 { get; set; } = 10;
    }

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
