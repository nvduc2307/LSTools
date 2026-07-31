using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LSTool.Compatibility;
using Newtonsoft.Json;
using RIMT.BeamRebar.ViewModel;
using RIMT.CreateRebarAssemblies.model;
using LSTool.Tools.Beams.InstallRebarBeamV2.iservices;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using LSTool.Tools.Beams.InstallRebarBeamV2.views;
using LSTool.Tools.Beams.InstallRebarBeamV2.Support.Legacy;
using LSTool.Tools.Beams.InstallRebarBeamV2.UI.Preview;
using LSTool.Tools.Beams.InstallRebarBeamV2.Application.Diagnostics;
using LSTool.Tools.Generals.SettingRebarStandard.actions;
using LSTool.Tools.Generals.SettingRebarStandard.models;
using RIMT.Utils;
using RIMT.Utils.canvass;
using RIMT.Utils.Entities;
using RIMT.Utils.RevitElements;
using RIMT.Utils.RevRebars;
using RIMT.Utils.SkipWarning;
using System.IO;
using System.Windows.Controls;
using Rebar = Autodesk.Revit.DB.Structure.Rebar;
using RebarBeamAnchorType = LSTool.Tools.Beams.InstallRebarBeamV2.models.RebarBeamAnchorType;
using UserControl = System.Windows.Controls.UserControl;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.viewModels
{
    public partial class InstallRebarBeamV2ViewModel : ObservableObject
    {

        private IRebarBeamTypeService _rebarBeamTypeService;
        private IBeamStressRuleTypeService _beamStressRuleTypeService;
        private IDrawRebarBeamInCanvasSerice _drawRebarBeamInCanvasSerice;
        private IInstallRebarBeamInModelService _installRebarBeamInModelService;
        private readonly PreviewRefreshCoordinator _previewRefreshCoordinator;
        public Element OBJ { get; set; }
        public IReadOnlyList<Element> SelectedBeams { get; private set; }
        public bool InstallationCompleted { get; private set; }
        public InstallRebarBeamView MainView { get; set; }
        public SettingRebarSectionView SettingRebarSectionView { get; set; }
        public SettingStirrupRebarSectionView SettingStirrupRebarSectionView { get; set; }
        public SettingBeamView SettingBeamView { get; set; }
        public SettingSubSettingView SettingSubSettingView { get; set; }
        public AnchorBeamType1View AnchorBeamType1View { get; set; }
        public AnchorBeamType2View AnchorBeamType2View { get; set; }
        [ObservableProperty]
        private UserControl _userControlViewCurrent;
        [ObservableProperty]
        private UserControl _userControlAnchorBeamTypeViewCurrent;

        public LSTool.Tools.Beams.InstallRebarBeamV2.models.ElementInstances ElementInstances { get; set; }
        public CanvasPageBase CanvasPageSectionStart { get; set; }
        public CanvasPageBase CanvasPageSectionMid { get; set; }
        public CanvasPageBase CanvasPageSectionEnd { get; set; }
        public SettingStirrupSectionViewModel SettingStirrupSectionViewModel { get; set; }
        public SettingRebarStandardModelUI SettingRebarStandardModel { get; }
        internal RebarDiagnosticLog DiagnosticLog { get; set; }

        public InstallRebarBeamV2ViewModel(
            IRebarBeamTypeService rebarBeamTypeService,
            IBeamStressRuleTypeService beamStressRuleTypeService,
            IDrawRebarBeamInCanvasSerice drawRebarBeamInCanvas,
            IInstallRebarBeamInModelService installRebarBeamInModelService,
            IReadOnlyList<Element> selectedBeams)
        {
            _rebarBeamTypeService = rebarBeamTypeService;
            _beamStressRuleTypeService = beamStressRuleTypeService;
            _drawRebarBeamInCanvasSerice = drawRebarBeamInCanvas;
            _installRebarBeamInModelService = installRebarBeamInModelService;
            SettingRebarStandardModel =
                SettingRebarStandardAction.GetSetting(AC.UiDoc.Document);
            SelectedBeams = selectedBeams?
                .Where(element => element != null)
                .GroupBy(element => element.Id.Value)
                .Select(group => group.First())
                .ToList()
                ?? throw new ArgumentNullException(nameof(selectedBeams));
            if (SelectedBeams.Count == 0)
                throw new InvalidOperationException(
                    "Select at least one structural framing beam.");
            OBJ = SelectedBeams.FirstOrDefault();
            ElementInstances = new LSTool.Tools.Beams.InstallRebarBeamV2.models.ElementInstances(
                AC.UiDoc,
                SelectedBeams);
            SettingRebarSectionView = new SettingRebarSectionView() { DataContext = this };
            SettingSubSettingView = new SettingSubSettingView() { DataContext = this };

            SettingStirrupRebarSectionView = new SettingStirrupRebarSectionView() { DataContext = this };
            SettingStirrupSectionViewModel = new SettingStirrupSectionViewModel(this)
            {
                CanvasPageStart = new CanvasPageBase(SettingStirrupRebarSectionView.FindName("CanvasSectionStart") as Canvas),
                CanvasPageMid = new CanvasPageBase(SettingStirrupRebarSectionView.FindName("CanvasSectionMid") as Canvas),
                CanvasPageEnd = new CanvasPageBase(SettingStirrupRebarSectionView.FindName("CanvasSectionEnd") as Canvas)
            };

            AnchorBeamType1View = new AnchorBeamType1View() { DataContext = this };
            AnchorBeamType2View = new AnchorBeamType2View() { DataContext = this };
            SettingBeamView = new SettingBeamView() { DataContext = this };
            UserControlViewCurrent = SettingRebarSectionView;
            UserControlAnchorBeamTypeViewCurrent = AnchorBeamType1View;
            MainView = new InstallRebarBeamView() { DataContext = this };
            MainView.Loaded += MainView_Loaded;
            _previewRefreshCoordinator = new PreviewRefreshCoordinator(RefreshPreview, TimeSpan.FromMilliseconds(100));
            InitAction();
        }

        internal void CopyInstallationSettingsFrom(
            InstallRebarBeamV2ViewModel source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            ElementInstances.CopyInstallationSettingsFrom(
                source.ElementInstances);
            SettingStirrupSectionViewModel
                    .RebarDiameterHorizontalDaiPhuChongPhinh =
                source.SettingStirrupSectionViewModel
                    .RebarDiameterHorizontalDaiPhuChongPhinh;
            SettingStirrupSectionViewModel
                    .SpacingHorizontalDaiPhuChongPhinh =
                source.SettingStirrupSectionViewModel
                    .SpacingHorizontalDaiPhuChongPhinh;
        }

        [RelayCommand]
        private void TabSettingRebarSectionView()
        {
            UserControlViewCurrent = SettingRebarSectionView;
        }
        [RelayCommand]
        private void TabSettingStirrupRebarSectionView()
        {
            UserControlViewCurrent = SettingStirrupRebarSectionView;
        }
        [RelayCommand]
        private void TabSettingBeamView()
        {
            UserControlViewCurrent = SettingBeamView;
        }
        [RelayCommand]
        private void TabSettingSubSettingView()
        {
            UserControlViewCurrent = SettingSubSettingView;
        }
        [RelayCommand]
        private void Apply()
        {
            _rebarBeamTypeService.Apply(this);
            InitAction();

            List<RebarBeam> GetRebarBeamTypes()
            {
                try
                {
                    return JsonConvert.DeserializeObject<List<RebarBeam>>(File.ReadAllText(ElementInstances.PathRebarBeamType));
                }
                catch (Exception)
                {
                }
                return new List<RebarBeam>();
            }

            var x1 = GetRebarBeamTypes();
            var x2 = x1.FirstOrDefault(x => x.NameType == ElementInstances.RebarBeamTypeSelected.NameType);

            ElementInstances.RebarBeamActive.RebarBeamSectionStart.RebarBeamTop.RebarBeamTopLevel1.Hooks2 = x2.RebarBeamSectionStart.RebarBeamTop.RebarBeamTopLevel1.Hooks2;
            ElementInstances.RebarBeamActive.RebarBeamSectionStart.RebarBeamBot.RebarBeamBotLevel1.Hooks2 = x2.RebarBeamSectionStart.RebarBeamBot.RebarBeamBotLevel1.Hooks2;

            ElementInstances.RebarBeamActive.RebarBeamSectionMid.RebarBeamTop.RebarBeamTopLevel1.Hooks2 = x2.RebarBeamSectionMid.RebarBeamTop.RebarBeamTopLevel1.Hooks2;
            ElementInstances.RebarBeamActive.RebarBeamSectionMid.RebarBeamBot.RebarBeamBotLevel1.Hooks2 = x2.RebarBeamSectionMid.RebarBeamBot.RebarBeamBotLevel1.Hooks2;

            ElementInstances.RebarBeamActive.RebarBeamSectionEnd.RebarBeamTop.RebarBeamTopLevel1.Hooks2 = x2.RebarBeamSectionEnd.RebarBeamTop.RebarBeamTopLevel1.Hooks2;
            ElementInstances.RebarBeamActive.RebarBeamSectionEnd.RebarBeamBot.RebarBeamBotLevel1.Hooks2 = x2.RebarBeamSectionEnd.RebarBeamBot.RebarBeamBotLevel1.Hooks2;

            ElementInstances.RebarBeamActive.MainStirrupType1 = x2 == null ? true : x2.MainStirrupType1;
            ElementInstances.RebarBeamActive.MainStirrupType2 = x2 == null ? true : x2.MainStirrupType2;
            ElementInstances.RebarBeamActive.MainStirrupType3 = x2 == null ? true : x2.MainStirrupType3;
            ElementInstances.RebarBeamActive.EnsureMainStirrupShapeSelected();
            ElementInstances.RebarBeamActive.QuantityStirrupSupportHole = x2 == null ? 2 : x2.QuantityStirrupSupportHole;
            ElementInstances.RebarBeamActive.MainStirrupTypeHat = x2 == null ? true : x2.MainStirrupTypeHat;
            ElementInstances.RebarBeamActive.HorizontalDaiPhu = x2 == null ? true : x2.HorizontalDaiPhu;
            ElementInstances.RebarBeamActive.VerticalDaiPhu = x2 == null ? true : x2.VerticalDaiPhu;

            _drawRebarBeamInCanvasSerice.DrawSectionBeamConcrete(ElementInstances.RebarBeamActive, this);
            _drawRebarBeamInCanvasSerice.DrawSectionBeamStirrup(ElementInstances.RebarBeamActive, this);
            QueuePreviewRefresh(PreviewRegion.AllBars);

        }

        [RelayCommand]
        private void CopyCurrentSpanToAllSpans()
        {
            try
            {
                _previewRefreshCoordinator.CancelPending();
                var targetSpanCount = Math.Max(0, ElementInstances.RebarBeams.Count - 1);
                ElementInstances.CopyActiveSpanSettingsToAll();
                InitAction();
                QueuePreviewRefresh(PreviewRegion.AllBars);
                IO.ShowInfo(targetSpanCount == 0
                    ? "There are no additional spans to update."
                    : $"Current settings were copied to {targetSpanCount} span(s).");
            }
            catch (Exception ex)
            {
                IO.ShowWarning(ex.Message);
            }
        }
        [RelayCommand]
        private void Save()
        {
            try
            {
                _previewRefreshCoordinator.CancelPending();
                if (ElementInstances.RebarBeamTypeSelected == null) throw new Exception("Dont find data to save");
                ElementInstances.RebarBeamActive.NameType = ElementInstances.RebarBeamTypeSelected.NameType;
                _rebarBeamTypeService.Save(ElementInstances.RebarBeamTypes, ElementInstances.RebarBeamActive, ElementInstances.PathRebarBeamType);
                MainView.Close();
                ElementInstances = new LSTool.Tools.Beams.InstallRebarBeamV2.models.ElementInstances(
                    AC.UiDoc,
                    SelectedBeams);
                SettingRebarSectionView = new SettingRebarSectionView() { DataContext = this };
                SettingSubSettingView = new SettingSubSettingView() { DataContext = this };

                SettingStirrupRebarSectionView = new SettingStirrupRebarSectionView() { DataContext = this };
                SettingStirrupSectionViewModel = new SettingStirrupSectionViewModel(this)
                {
                    CanvasPageStart = new CanvasPageBase(SettingStirrupRebarSectionView.FindName("CanvasSectionStart") as Canvas),
                    CanvasPageMid = new CanvasPageBase(SettingStirrupRebarSectionView.FindName("CanvasSectionMid") as Canvas),
                    CanvasPageEnd = new CanvasPageBase(SettingStirrupRebarSectionView.FindName("CanvasSectionEnd") as Canvas)
                };

                AnchorBeamType1View = new AnchorBeamType1View() { DataContext = this };
                AnchorBeamType2View = new AnchorBeamType2View() { DataContext = this };
                SettingBeamView = new SettingBeamView() { DataContext = this };
                UserControlViewCurrent = SettingRebarSectionView;
                UserControlAnchorBeamTypeViewCurrent = AnchorBeamType1View;
                MainView = new InstallRebarBeamView() { DataContext = this };
                MainView.Loaded += MainView_Loaded;
                InitAction();
                MainView.ShowDialog();
            }
            catch (Exception ex)
            {
                IO.ShowWarning(ex.Message);
            }
        }
        [RelayCommand]
        private void Delete()
        {
            try
            {
                _rebarBeamTypeService.Delete(
                    ElementInstances.RebarBeamTypes,
                    ElementInstances.RebarBeamTypeSelected.NameType,
                    ElementInstances.PathRebarBeamType);
                ElementInstances.RebarBeamTypes = JsonConvert.DeserializeObject<List<RebarBeam>>(File.ReadAllText(ElementInstances.PathRebarBeamType));
                ElementInstances.RebarBeamTypeSelected = ElementInstances.RebarBeamTypes.FirstOrDefault();
            }
            catch (Exception)
            {

            }
        }
        [RelayCommand]
        private void SaveAs()
        {
            try
            {
                _rebarBeamTypeService.SaveAs(
                    ElementInstances.RebarBeamTypes,
                    ElementInstances.RebarBeamTypeName,
                    ElementInstances.PathRebarBeamType);
                ElementInstances.RebarBeamTypeName = string.Empty;

                ElementInstances.RebarBeamTypes = JsonConvert.DeserializeObject<List<RebarBeam>>(File.ReadAllText(ElementInstances.PathRebarBeamType));
                ElementInstances.RebarBeamTypeSelected = ElementInstances.RebarBeamTypes.LastOrDefault();
            }
            catch (Exception)
            {
            }
        }
    }
}


