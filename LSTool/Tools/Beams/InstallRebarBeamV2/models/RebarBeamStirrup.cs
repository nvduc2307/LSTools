using CommunityToolkit.Mvvm.ComponentModel;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.models
{
    public partial class RebarBeamStirrup : RebarBaseInfo
    {
        private double _spacing;
        public double Spacing
		{
			get => _spacing;
			set
			{
				_spacing = value;
				OnPropertyChanged();
				SpacingChange?.Invoke();
			}
		}
		public Action SpacingChange { get; set; }

	}
}


