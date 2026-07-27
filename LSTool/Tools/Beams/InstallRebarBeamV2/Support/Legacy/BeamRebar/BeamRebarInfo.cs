namespace RIMT.BeamRebar.ViewModel
{
    public class BeamRebarInfo
    {
        /// <summary>
        /// Id is Id
        /// Name is Name
        /// HostId is host Id
        /// RebarBeamGroup [none, level1, level2, level3]
        /// RebarBeamLevel [none, top, bot]
        /// RebarBeamType [mainbar, stirrup]
        /// </summary>
        public long Id { get; set; }
        public string UniqueId { get; set; }
        public string Name { get; set; }
        public long HostId { get; set; }
        public int RebarBeamType { get; set; }
        public int RebarBeamLevel { get; set; }
        public int RebarBeamGroup { get; set; }
    }
    public class RebarBeamGroupTypeInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public RebarBeamGroupTypeInfo(RebarBeamGroup rebarBeamGroup, string name = "")
        {
            Id = (int)rebarBeamGroup;
            Name = string.IsNullOrEmpty(name) ? rebarBeamGroup.ToString() : name;
        }
    }
    public class RebarBeamLevelInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public RebarBeamLevelInfo(RebarBeamLevel rebarBeamLevel, string name = "")
        {
            Id = (int)rebarBeamLevel;
            Name = string.IsNullOrEmpty(name) ? rebarBeamLevel.ToString() : name;
        }
    }
    public enum RebarBeamGroup
    {
        None = -1,
        Level1 = 0,
        Level2 = 1,
        Level3 = 2,
    }
    public enum RebarBeamLevel
    {
        None = -1,
        Top = 0,
        Bottom = 1,
    }
    public enum RebarBeamType
    {
        MainBar = 0,
        Stirrup = 1,
        SideBar = 2,
        Dantory = 3,
    }
}
