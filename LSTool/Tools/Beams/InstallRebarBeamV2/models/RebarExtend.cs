using CommunityToolkit.Mvvm.ComponentModel;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.models
{
    public partial class RebarExtend : ObservableObject
    {
        /// <summary>
        /// he so duong kinh
        /// </summary>
        [ObservableProperty]
        private double _rebarTopExtend;
        [ObservableProperty]
        private double _rebarBotExtend;
    }
}


