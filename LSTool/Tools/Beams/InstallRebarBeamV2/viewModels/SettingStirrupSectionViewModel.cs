using LSTool.Compatibility;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using RIMT.Utils.canvass;
using System.Windows;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.viewModels
{
    public partial class SettingStirrupSectionViewModel : ViewModelBase
    {
        public CanvasPageBase CanvasPageStart { get; set; }
        public CanvasPageBase CanvasPageMid { get; set; }
        public CanvasPageBase CanvasPageEnd { get; set; }
        private InstallRebarBeamV2ViewModel MainViewModel { get; }
        public LSTool.Tools.Beams.InstallRebarBeamV2.models.ElementInstances ElementInstances { get; set; }
        //private int _daiChinh;
        //public int DaiChinh
        //{
        //    get => _daiChinh;
        //    set
        //    {
        //        _daiChinh = value;
        //        OnPropertyChanged();
        //    }
        //}
        //private int _verticalDaiPhu;
        //public int VerticalDaiPhu
        //{
        //    get => _verticalDaiPhu;
        //    set
        //    {
        //        _verticalDaiPhu = value;
        //        OnPropertyChanged();
        //    }
        //}
        //private int _horizontalDaiPhu;
        //public int HorizontalDaiPhu
        //{
        //    get => _horizontalDaiPhu;
        //    set
        //    {
        //        _horizontalDaiPhu = value;
        //        OnPropertyChanged();
        //    }
        //}
        public string RebarDiameterHorizontalDaiPhuChongPhinh { get; set; }
        private double _spacingHorizontalDaiPhuChongPhinh = 1000;
        public double SpacingHorizontalDaiPhuChongPhinh
        {
            get => _spacingHorizontalDaiPhuChongPhinh;
            set
            {
                _spacingHorizontalDaiPhuChongPhinh = value;
                OnPropertyChanged();
            }
        }
        public SettingStirrupSectionViewModel(InstallRebarBeamV2ViewModel mainViewModel)
        {
            MainViewModel = mainViewModel;
            ElementInstances = mainViewModel.ElementInstances;
            ElementInstances.MainRebarTopUIElement = new List<UIElement>();
            ElementInstances.MainRebarBotUIElement = new List<UIElement>();
            ElementInstances.SideBarUIElement = new List<UIElement>();
            RebarDiameterHorizontalDaiPhuChongPhinh = mainViewModel.ElementInstances.RebarDiameters.First();
            //DaiChinh = 1;
            //HorizontalDaiPhu = 1;
            //VerticalDaiPhu = 1;
        }
    }
}


