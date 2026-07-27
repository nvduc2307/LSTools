using CommunityToolkit.Mvvm.ComponentModel;
using RIMT.Utils.RevParameters;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.models
{
    public partial class BeamFukashi : ObservableObject
    {
        [ObservableProperty]
        private RevParameter _fukashiTop;
        [ObservableProperty]
        private RevParameter _fukashiBot;
        [ObservableProperty]
        private RevParameter _fukashiRight;
        [ObservableProperty]
        private RevParameter _fukashiLeft;

        public BeamFukashi()
        {
            FukashiTop = new RevParameter();
            FukashiBot = new RevParameter();
            FukashiRight = new RevParameter();
            FukashiLeft = new RevParameter();
        }
    }
}


