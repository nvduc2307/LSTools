using CommunityToolkit.Mvvm.ComponentModel;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.models
{
    public class BeamStressRule
    {
        public int Id { get; set; }
        public List<double> Stress { get; set; }
    }
    public partial class BeamStressRuleType : ObservableObject
    {
        /// <summary>
        /// he so cua chieu dai dam
        /// </summary>
        [ObservableProperty]
        private double _stressStart;
        [ObservableProperty]
        private double _stressMid;
        [ObservableProperty]
        private double _stressEnd;
    }
}


