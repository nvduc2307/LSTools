namespace LSTool.Tools.Beams.InstallRebarBeamV2.models
{
    public enum RebarBeamMainBarGroupType
    {
        GroupLevel1 = 1,
        GroupLevel2 = 2,
        GroupLevel3 = 3,
    }
    public class RebarBeamMainBarGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public RebarBeamMainBarGroup(RebarBeamMainBarGroupType rebarBeamMainBarGroupType)
        {
            Id = (int)rebarBeamMainBarGroupType;
            Name = GetName(rebarBeamMainBarGroupType);
        }
        public static string GetName(RebarBeamMainBarGroupType rebarBeamMainBarGroupType)
        {
            var result = "";
            try
            {
                switch (rebarBeamMainBarGroupType)
                {
                    case RebarBeamMainBarGroupType.GroupLevel1:
                        result = "1（段筋）";
                        break;
                    case RebarBeamMainBarGroupType.GroupLevel2:
                        result = "2（段筋）";
                        break;
                    case RebarBeamMainBarGroupType.GroupLevel3:
                        result = "3（段筋）";
                        break;
                }
                return result;
            }
            catch (Exception)
            {
            }
            return string.Empty;
        }
        public static List<RebarBeamMainBarGroup> GetRebarBeamMainBarGroups()
        {
            return new List<RebarBeamMainBarGroup>()
            {
                new RebarBeamMainBarGroup(RebarBeamMainBarGroupType.GroupLevel1),
                new RebarBeamMainBarGroup(RebarBeamMainBarGroupType.GroupLevel2),
                new RebarBeamMainBarGroup(RebarBeamMainBarGroupType.GroupLevel3),
            };
        }
    }
}


