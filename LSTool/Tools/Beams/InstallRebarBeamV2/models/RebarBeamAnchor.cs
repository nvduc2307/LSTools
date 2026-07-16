using CommunityToolkit.Mvvm.ComponentModel;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.models
{
    public partial class RebarBeamAnchor : ObservableObject
    {
        [ObservableProperty]
        private double _type1_L1_X_Start;
        [ObservableProperty]
        private double _type1_L1_X_End;
        [ObservableProperty]
        private double _type1_L3_X_Start;
        [ObservableProperty]
        private double _type1_L3_X_End;
        [ObservableProperty]
        private double _type2_L1_X_Start;
        [ObservableProperty]
        private double _type2_L1_X_End;
        [ObservableProperty]
        private double _type2_L3_X_Start;
        [ObservableProperty]
        private double _type2_L3_X_End;

        [ObservableProperty]
        private double _type1_L1_Y_Start;
        [ObservableProperty]
        private double _type1_L1_Y_End;
        [ObservableProperty]
        private double _type1_L3_Y_Start;
        [ObservableProperty]
        private double _type1_L3_Y_End;
        [ObservableProperty]
        private double _type2_L1_Y_Start;
        [ObservableProperty]
        private double _type2_L1_Y_End;
        [ObservableProperty]
        private double _type2_L3_Y_Start;
        [ObservableProperty]
        private double _type2_L3_Y_End;
    }
    public class RebarBeamAnchorOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public static List<RebarBeamAnchorOption> DataInit()
        {
            try
            {
                return new List<RebarBeamAnchorOption>()
                {
                    new RebarBeamAnchorOption(){Id = 0, Name = "Type 1", Type = (int)RebarBeamAnchorType.Type1},
                    new RebarBeamAnchorOption(){Id = 1, Name = "Type 2", Type = (int)RebarBeamAnchorType.Type2},
                };
            }
            catch (Exception)
            {
            }
            return new List<RebarBeamAnchorOption>();
        }
    }
    public enum RebarBeamAnchorType
    {
        Type1 = 0,
        Type2 = 1,
    }
}


