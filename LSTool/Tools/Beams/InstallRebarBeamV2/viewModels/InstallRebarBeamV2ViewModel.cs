using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HcBimUtils;
using HcBimUtils.DocumentUtils;
using HcBimUtils.MoreLinq;
using HcBimUtils.WPFUtils;
using Newtonsoft.Json;
using RIMT.BeamRebar.ViewModel;
using RIMT.CreateRebarAssemblies.model;
using LSTool.Tools.Beams.InstallRebarBeamV2.iservices;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using LSTool.Tools.Beams.InstallRebarBeamV2.views;
using LSTool.Tools.Beams.InstallRebarBeamV2.Support.Legacy;
using RIMT.Utils;
using RIMT.Utils.canvass;
using RIMT.Utils.Entities;
using RIMT.Utils.RevitElements;
using RIMT.Utils.RevParameters;
using RIMT.Utils.RevRebars;
using RIMT.Utils.SelectFilters;
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
        public Element OBJ { get; set; }
        public MappingFukashiView MappingFukashiView { get; set; }
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

        public InstallRebarBeamV2ViewModel(
            IRebarBeamTypeService rebarBeamTypeService,
            IBeamStressRuleTypeService beamStressRuleTypeService,
            IDrawRebarBeamInCanvasSerice drawRebarBeamInCanvas,
            IInstallRebarBeamInModelService installRebarBeamInModelService)
        {
            _rebarBeamTypeService = rebarBeamTypeService;
            _beamStressRuleTypeService = beamStressRuleTypeService;
            _drawRebarBeamInCanvasSerice = drawRebarBeamInCanvas;
            _installRebarBeamInModelService = installRebarBeamInModelService;
            OBJ = AC.UiDoc.Selection.PickObject(
                Autodesk.Revit.UI.Selection.ObjectType.Element,
                new GenericSelectionFilterFromCategory(BuiltInCategory.OST_StructuralFraming)).ToElement();
            ElementInstances = new LSTool.Tools.Beams.InstallRebarBeamV2.models.ElementInstances(AC.UiDoc, OBJ);
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
            MappingFukashiView = new MappingFukashiView() { DataContext = this };
            MainView.Loaded += MainView_Loaded;
            InitAction();
        }

        [RelayCommand]
        private void OkMappingFukashi()
        {
            try
            {
                MappingFukashiView.Close();
                MainView.ShowDialog();
            }
            catch (Exception ex)
            {
                IO.ShowWarning(ex.Message);
            }
        }
        [RelayCommand]
        private void CancelMappingFukashi()
        {
            MappingFukashiView.Close();
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
            ElementInstances.RebarBeamActive.QuantityStirrupSupportHole = x2 == null ? 2 : x2.QuantityStirrupSupportHole;
            ElementInstances.RebarBeamActive.MainStirrupTypeHat = x2 == null ? true : x2.MainStirrupTypeHat;
            ElementInstances.RebarBeamActive.HorizontalDaiPhu = x2 == null ? true : x2.HorizontalDaiPhu;
            ElementInstances.RebarBeamActive.VerticalDaiPhu = x2 == null ? true : x2.VerticalDaiPhu;

            _drawRebarBeamInCanvasSerice.DrawSectionBeamConcrete(ElementInstances.RebarBeamActive, this);
            _drawRebarBeamInCanvasSerice.DrawOutLineFukashi(ElementInstances.RebarBeamActive, this);
            _drawRebarBeamInCanvasSerice.DrawSectionBeamStirrup(ElementInstances.RebarBeamActive, this);
            ElementInstances.MainRebarTopUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
            ElementInstances.SideBarUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamSideBar(ElementInstances.RebarBeamActive, this);

        }
        [RelayCommand]
        private void Save()
        {
            try
            {
                if (ElementInstances.RebarBeamTypeSelected == null) throw new Exception("Dont find data to save");
                ElementInstances.RebarBeamActive.NameType = ElementInstances.RebarBeamTypeSelected.NameType;
                _rebarBeamTypeService.Save(ElementInstances.RebarBeamTypes, ElementInstances.RebarBeamActive, ElementInstances.PathRebarBeamType);
                MainView.Close();
                ElementInstances = new LSTool.Tools.Beams.InstallRebarBeamV2.models.ElementInstances(AC.UiDoc, OBJ);
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
                MappingFukashiView = new MappingFukashiView() { DataContext = this };
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
        [RelayCommand]
        private void OK()
        {
            try
            {
                _beamStressRuleTypeService.Update(ElementInstances.RebarBeams, ElementInstances.BeamStressRuleType);
                using (var ts = new Transaction(AC.Document, "name transaction"))
                {
                    ts.SkipAllWarnings();
                    ts.Start();
                    try
                    {
                    RebarSharedParameterSupport.EnsureRequiredParameters(AC.Document);
                    //--------
                    var installRebarTop1 = _installRebarBeamInModelService.InstallRebarTop1(this);
                    var installRebarTop2 = _installRebarBeamInModelService.InstallRebarTop2(this);
                    var installRebarTop3 = _installRebarBeamInModelService.InstallRebarTop3(this);
                    var installRebarBot1 = _installRebarBeamInModelService.InstallRebarBot1(this);
                    var installRebarBot2 = _installRebarBeamInModelService.InstallRebarBot2(this);
                    var installRebarBot3 = _installRebarBeamInModelService.InstallRebarBot3(this);
                    var installRebarSide = _installRebarBeamInModelService.InstallRebarSide(this);
                    var installRebarDantories = _installRebarBeamInModelService.InstallRebarDantory(this);
                    var installRebarStirrup = _installRebarBeamInModelService.InstallRebarStirrup(this);
                    var installRebarSubVerticalStirrup = _installRebarBeamInModelService.InstallRebarSubVerticalStirrup(this);
                    var installRebarSubHorizontalStirrupForMainRebar = _installRebarBeamInModelService.InstallRebarSubHorizontalStirrupForMainRebar(this);
                    var installRebarSubHorizontalStirrupForSideRebar = _installRebarBeamInModelService.InstallRebarSubHorizontalStirrupForSideRebar(this);
                    #region write rebar type info
                    foreach (var rb in installRebarTop1)
                    {
                        RebarSharedParameterSupport.SetRequiredStringParameter(rb, LSTool.Properties.RTParams.RT_PARAMS_REBAR_TYPE, LSTool.Properties.Langs.RebarStructureType.BEAM_MAIN_REBAR_UPPER_STAGE_1);
                    }
                    foreach (var rb in installRebarTop2)
                    {
                        RebarSharedParameterSupport.SetRequiredStringParameter(rb, LSTool.Properties.RTParams.RT_PARAMS_REBAR_TYPE, LSTool.Properties.Langs.RebarStructureType.BEAM_MAIN_REBAR_UPPER_STAGE_2);
                    }
                    foreach (var rb in installRebarTop3)
                    {
                        RebarSharedParameterSupport.SetRequiredStringParameter(rb, LSTool.Properties.RTParams.RT_PARAMS_REBAR_TYPE, LSTool.Properties.Langs.RebarStructureType.BEAM_MAIN_REBAR_UPPER_STAGE_3);
                    }
                    foreach (var rb in installRebarBot1)
                    {
                        RebarSharedParameterSupport.SetRequiredStringParameter(rb, LSTool.Properties.RTParams.RT_PARAMS_REBAR_TYPE, LSTool.Properties.Langs.RebarStructureType.BEAM_MAIN_REBAR_LOWER_STAGE_1);
                    }
                    foreach (var rb in installRebarBot2)
                    {
                        RebarSharedParameterSupport.SetRequiredStringParameter(rb, LSTool.Properties.RTParams.RT_PARAMS_REBAR_TYPE, LSTool.Properties.Langs.RebarStructureType.BEAM_MAIN_REBAR_LOWER_STAGE_2);
                    }
                    foreach (var rb in installRebarBot3)
                    {
                        RebarSharedParameterSupport.SetRequiredStringParameter(rb, LSTool.Properties.RTParams.RT_PARAMS_REBAR_TYPE, LSTool.Properties.Langs.RebarStructureType.BEAM_MAIN_REBAR_LOWER_STAGE_3);
                    }
                    foreach (var rb in installRebarSide)
                    {
                        RebarSharedParameterSupport.SetRequiredStringParameter(rb, LSTool.Properties.RTParams.RT_PARAMS_REBAR_TYPE, LSTool.Properties.Langs.RebarStructureType.BEAM_ABDOMINAL_REBAR);
                    }
                    foreach (var rb in installRebarDantories)
                    {
                        RebarSharedParameterSupport.SetRequiredStringParameter(rb, LSTool.Properties.RTParams.RT_PARAMS_REBAR_TYPE, LSTool.Properties.Langs.RebarStructureType.BEAM_DANTORI_REBAR);
                    }
                    foreach (var rb in installRebarStirrup)
                    {
                        RebarSharedParameterSupport.SetRequiredStringParameter(rb, LSTool.Properties.RTParams.RT_PARAMS_REBAR_TYPE, LSTool.Properties.Langs.RebarStructureType.BEAM_STP);
                    }
                    foreach (var rb in installRebarSubVerticalStirrup)
                    {
                        RebarSharedParameterSupport.SetRequiredStringParameter(rb, LSTool.Properties.RTParams.RT_PARAMS_REBAR_TYPE, LSTool.Properties.Langs.RebarStructureType.BEAM_SECONDARY_STP_REBAR);
                    }
                    foreach (var rb in installRebarSubHorizontalStirrupForMainRebar)
                    {
                        RebarSharedParameterSupport.SetRequiredStringParameter(rb, LSTool.Properties.RTParams.RT_PARAMS_REBAR_TYPE, LSTool.Properties.Langs.RebarStructureType.BEAM_SECONDARY_STP_REBAR);
                    }
                    foreach (var rb in installRebarSubHorizontalStirrupForSideRebar)
                    {
                        RebarSharedParameterSupport.SetRequiredStringParameter(rb, LSTool.Properties.RTParams.RT_PARAMS_REBAR_TYPE, LSTool.Properties.Langs.RebarStructureType.BEAM_SECONDARY_STP_REBAR);
                    }
                    #endregion
                    #region Create Rebar Beam Assembly
                    var rebarIds = installRebarTop1
                        .Concat(installRebarTop2)
                        .Concat(installRebarTop3)
                        .Concat(installRebarBot1)
                        .Concat(installRebarBot2)
                        .Concat(installRebarBot3)
                        .Concat(installRebarSide)
                        .Concat(installRebarDantories)
                        .Concat(installRebarStirrup)
                        .Concat(installRebarSubVerticalStirrup)
                        .Concat(installRebarSubHorizontalStirrupForMainRebar)
                        .Concat(installRebarSubHorizontalStirrupForSideRebar)
                        .Select(x => x.Id)
                        .ToList();
                    var rebarBeamAss = AssemblyInstance.Create(
                        AC.Document, rebarIds,
                        Category.GetCategory(AC.Document,
                        BuiltInCategory.OST_Rebar).Id);
                    #endregion
                    #region Write Rebar Beam Info
                    var rebarinfos = new List<BeamRebarInfo>();

                    var installRebarTop1Info = installRebarTop1.Select(x =>
                    {
                        var rebarBeamInfo = new BeamRebarInfo()
                        {
                            Id = x.Id.Value,
                            UniqueId = x.UniqueId,
                            Name = x.Name,
                            HostId = x.GetHostId().Value,
                            RebarBeamType = (int)RIMT.BeamRebar.ViewModel.RebarBeamType.MainBar,
                            RebarBeamLevel = (int)RIMT.BeamRebar.ViewModel.RebarBeamLevel.Top,
                            RebarBeamGroup = (int)RIMT.BeamRebar.ViewModel.RebarBeamGroup.Level1,
                        };
                        return rebarBeamInfo;
                    });
                    var installRebarTop2Info = installRebarTop2.Select(x =>
                    {
                        var rebarBeamInfo = new BeamRebarInfo()
                        {
                            Id = x.Id.Value,
                            UniqueId = x.UniqueId,
                            Name = x.Name,
                            HostId = x.GetHostId().Value,
                            RebarBeamType = (int)RIMT.BeamRebar.ViewModel.RebarBeamType.MainBar,
                            RebarBeamLevel = (int)RIMT.BeamRebar.ViewModel.RebarBeamLevel.Top,
                            RebarBeamGroup = (int)RIMT.BeamRebar.ViewModel.RebarBeamGroup.Level2,
                        };
                        return rebarBeamInfo;
                    });
                    var installRebarTop3Info = installRebarTop3.Select(x =>
                    {
                        var rebarBeamInfo = new BeamRebarInfo()
                        {
                            Id = x.Id.Value,
                            UniqueId = x.UniqueId,
                            Name = x.Name,
                            HostId = x.GetHostId().Value,
                            RebarBeamType = (int)RIMT.BeamRebar.ViewModel.RebarBeamType.MainBar,
                            RebarBeamLevel = (int)RIMT.BeamRebar.ViewModel.RebarBeamLevel.Top,
                            RebarBeamGroup = (int)RIMT.BeamRebar.ViewModel.RebarBeamGroup.Level3,
                        };
                        return rebarBeamInfo;
                    });

                    var installRebarBot1Info = installRebarBot1.Select(x =>
                    {
                        var rebarBeamInfo = new BeamRebarInfo()
                        {
                            Id = x.Id.Value,
                            UniqueId = x.UniqueId,
                            Name = x.Name,
                            HostId = x.GetHostId().Value,
                            RebarBeamType = (int)RIMT.BeamRebar.ViewModel.RebarBeamType.MainBar,
                            RebarBeamLevel = (int)RIMT.BeamRebar.ViewModel.RebarBeamLevel.Bottom,
                            RebarBeamGroup = (int)RIMT.BeamRebar.ViewModel.RebarBeamGroup.Level1,
                        };
                        return rebarBeamInfo;
                    });
                    var installRebarBot2Info = installRebarBot2.Select(x =>
                    {
                        var rebarBeamInfo = new BeamRebarInfo()
                        {
                            Id = x.Id.Value,
                            UniqueId = x.UniqueId,
                            Name = x.Name,
                            HostId = x.GetHostId().Value,
                            RebarBeamType = (int)RIMT.BeamRebar.ViewModel.RebarBeamType.MainBar,
                            RebarBeamLevel = (int)RIMT.BeamRebar.ViewModel.RebarBeamLevel.Bottom,
                            RebarBeamGroup = (int)RIMT.BeamRebar.ViewModel.RebarBeamGroup.Level2,
                        };
                        return rebarBeamInfo;
                    });
                    var installRebarBot3Info = installRebarBot3.Select(x =>
                    {
                        var rebarBeamInfo = new BeamRebarInfo()
                        {
                            Id = x.Id.Value,
                            UniqueId = x.UniqueId,
                            Name = x.Name,
                            HostId = x.GetHostId().Value,
                            RebarBeamType = (int)RIMT.BeamRebar.ViewModel.RebarBeamType.MainBar,
                            RebarBeamLevel = (int)RIMT.BeamRebar.ViewModel.RebarBeamLevel.Bottom,
                            RebarBeamGroup = (int)RIMT.BeamRebar.ViewModel.RebarBeamGroup.Level3,
                        };
                        return rebarBeamInfo;
                    });

                    var installRebarSideInfo = installRebarSide.Select(x =>
                    {
                        var rebarBeamInfo = new BeamRebarInfo()
                        {
                            Id = x.Id.Value,
                            UniqueId = x.UniqueId,
                            Name = x.Name,
                            HostId = x.GetHostId().Value,
                            RebarBeamType = (int)RIMT.BeamRebar.ViewModel.RebarBeamType.SideBar,
                            RebarBeamLevel = (int)RIMT.BeamRebar.ViewModel.RebarBeamLevel.None,
                            RebarBeamGroup = (int)RIMT.BeamRebar.ViewModel.RebarBeamGroup.None,
                        };
                        return rebarBeamInfo;
                    });
                    var installRebarDantoryInfo = installRebarDantories.Select(x =>
                    {
                        var rebarBeamInfo = new BeamRebarInfo()
                        {
                            Id = x.Id.Value,
                            UniqueId = x.UniqueId,
                            Name = x.Name,
                            HostId = x.GetHostId().Value,
                            RebarBeamType = (int)RIMT.BeamRebar.ViewModel.RebarBeamType.Dantory,
                            RebarBeamLevel = (int)RIMT.BeamRebar.ViewModel.RebarBeamLevel.None,
                            RebarBeamGroup = (int)RIMT.BeamRebar.ViewModel.RebarBeamGroup.None,
                        };
                        return rebarBeamInfo;
                    });

                    var installRebarStirrupInfo = installRebarStirrup.Select(x =>
                    {
                        var rebarBeamInfo = new BeamRebarInfo()
                        {
                            Id = x.Id.Value,
                            UniqueId = x.UniqueId,
                            Name = x.Name,
                            HostId = x.GetHostId().Value,
                            RebarBeamType = (int)RIMT.BeamRebar.ViewModel.RebarBeamType.Stirrup,
                            RebarBeamLevel = (int)RIMT.BeamRebar.ViewModel.RebarBeamLevel.None,
                            RebarBeamGroup = (int)RIMT.BeamRebar.ViewModel.RebarBeamGroup.None,
                        };
                        return rebarBeamInfo;
                    });
                    var installRebarSubVerticalStirrupInfo = installRebarSubVerticalStirrup.Select(x =>
                    {
                        var rebarBeamInfo = new BeamRebarInfo()
                        {
                            Id = x.Id.Value,
                            UniqueId = x.UniqueId,
                            Name = x.Name,
                            HostId = x.GetHostId().Value,
                            RebarBeamType = (int)RIMT.BeamRebar.ViewModel.RebarBeamType.Stirrup,
                            RebarBeamLevel = (int)RIMT.BeamRebar.ViewModel.RebarBeamLevel.None,
                            RebarBeamGroup = (int)RIMT.BeamRebar.ViewModel.RebarBeamGroup.None,
                        };
                        return rebarBeamInfo;
                    });
                    var installRebarSubHorizontalStirrupForMainRebarInfo = installRebarSubHorizontalStirrupForMainRebar.Select(x =>
                    {
                        var rebarBeamInfo = new BeamRebarInfo()
                        {
                            Id = x.Id.Value,
                            UniqueId = x.UniqueId,
                            Name = x.Name,
                            HostId = x.GetHostId().Value,
                            RebarBeamType = (int)RIMT.BeamRebar.ViewModel.RebarBeamType.Stirrup,
                            RebarBeamLevel = (int)RIMT.BeamRebar.ViewModel.RebarBeamLevel.None,
                            RebarBeamGroup = (int)RIMT.BeamRebar.ViewModel.RebarBeamGroup.None,
                        };
                        return rebarBeamInfo;
                    });
                    var installRebarSubHorizontalStirrupForSideRebarInfo = installRebarSubHorizontalStirrupForSideRebar.Select(x =>
                    {
                        var rebarBeamInfo = new BeamRebarInfo()
                        {
                            Id = x.Id.Value,
                            UniqueId = x.UniqueId,
                            Name = x.Name,
                            HostId = x.GetHostId().Value,
                            RebarBeamType = (int)RIMT.BeamRebar.ViewModel.RebarBeamType.Stirrup,
                            RebarBeamLevel = (int)RIMT.BeamRebar.ViewModel.RebarBeamLevel.None,
                            RebarBeamGroup = (int)RIMT.BeamRebar.ViewModel.RebarBeamGroup.None,
                        };
                        return rebarBeamInfo;
                    });

                    rebarinfos.AddRange(installRebarTop1Info);
                    rebarinfos.AddRange(installRebarTop2Info);
                    rebarinfos.AddRange(installRebarTop3Info);
                    rebarinfos.AddRange(installRebarBot1Info);
                    rebarinfos.AddRange(installRebarBot2Info);
                    rebarinfos.AddRange(installRebarBot3Info);
                    rebarinfos.AddRange(installRebarSideInfo);
                    rebarinfos.AddRange(installRebarDantoryInfo);
                    rebarinfos.AddRange(installRebarStirrupInfo);
                    rebarinfos.AddRange(installRebarSubVerticalStirrupInfo);
                    rebarinfos.AddRange(installRebarSubHorizontalStirrupForMainRebarInfo);
                    rebarinfos.AddRange(installRebarSubHorizontalStirrupForSideRebarInfo);

                    foreach (var rb in rebarinfos)
                    {
                        var r = AC.Document.GetElement(rb.UniqueId)
                            ?? throw new InvalidOperationException(
                                $"Created rebar '{rb.UniqueId}' could not be found for metadata writing.");
                        var content = JsonConvert.SerializeObject(rb);
                        ElementInstances.RebarBeamSchemal.SchemaField.Value = content;
                        SchemaInfo.Write(ElementInstances.RebarBeamSchemal.SchemaBase, r, ElementInstances.RebarBeamSchemal.SchemaField);
                    }

                    #endregion
                    #region write rebar beam assembly info
                    var assemblyInfoUtils =
                        new AssemblyInfoUtils(new List<Element>() { ElementInstances.Beam.Element }, AC.Document);
                    RebarSharedParameterSupport.SetRequiredStringParameter(
                        rebarBeamAss,
                        BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS,
                        assemblyInfoUtils.GridName);
                    RebarSharedParameterSupport.SetRequiredStringParameter(
                        rebarBeamAss,
                        BuiltInParameter.ALL_MODEL_MARK,
                        assemblyInfoUtils.TypeName);
                    #endregion
                    #region Resetting host
                    var rebars = installRebarTop1
                        .Concat(installRebarTop2)
                        .Concat(installRebarTop3)
                        .Concat(installRebarBot1)
                        .Concat(installRebarBot2)
                        .Concat(installRebarBot3)
                        .Concat(installRebarSide)
                        .Concat(installRebarDantories)
                        .Concat(installRebarStirrup)
                        .Concat(installRebarSubVerticalStirrup)
                        .Concat(installRebarSubHorizontalStirrupForMainRebar)
                        .Concat(installRebarSubHorizontalStirrupForSideRebar);
                    if (rebars.Any())
                    {
                        foreach (var rb in rebars)
                        {
                            try
                            {
                                rb.SetHostId(AC.Document, ElementInstances.Beam.ElementSubs.FirstOrDefault().Element.Id);
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidOperationException(
                                    $"Failed to reset the host for rebar {rb.Id.Value}.", ex);
                            }
                        }
                    }
                    #endregion
                    //avoid hole
                    var rebarsSTP = new List<Rebar>();
                    rebarsSTP.AddRange(installRebarStirrup);
                    rebarsSTP.AddRange(installRebarSubVerticalStirrup);
                    rebarsSTP.AddRange(installRebarSubHorizontalStirrupForMainRebar);
                    rebarsSTP.AddRange(installRebarSubHorizontalStirrupForSideRebar);
                    var rbsHole = BypassOpening(rebarsSTP, installRebarSide, ElementInstances.RebarBeamActive, out List<Rebar> rebarDeletes)
                        .Select(x => x.Id)
                        .ToList();
                    if (rbsHole.Count != 0)
                    {
                        rebarBeamAss.AddMemberIds(rbsHole);
                        AC.Document.Delete(rebarDeletes.Select(x => x.Id).ToList());
                    }
                    //init segment
                    var rebarInAss = rebarBeamAss.GetMemberIds()
                        .Select(x => AC.Document.GetElement(x) as Rebar)
                        .ToList();
                    rebarInAss.InitSegment();
                    //--------
                    ts.Commit();
                    }
                    catch
                    {
                        if (ts.GetStatus() == TransactionStatus.Started)
                            ts.RollBack();
                        throw;
                    }
                }

                MainView.Close();
            }
            catch (Exception ex)
            {
                IO.ShowWarning(GetDetailedError(ex));
            }
        }

        private static string GetDetailedError(Exception exception)
        {
            var messages = new List<string>();
            for (var current = exception; current != null; current = current.InnerException)
            {
                if (!string.IsNullOrWhiteSpace(current.Message)
                    && !messages.Contains(current.Message))
                    messages.Add(current.Message);
            }
            return string.Join(Environment.NewLine, messages);
        }

        [RelayCommand]
        private void Cancel()
        {
            MainView.Close();
        }
        private List<Rebar> BypassOpening(List<Rebar> rebarStps, List<Rebar> rebarSides, RebarBeam rebarBeam, out List<Rebar> rebarDeletes)
        {
            var resutls = new List<Rebar>();
            rebarDeletes = new List<Rebar>();
            try
            {
                List<FamilyInstance> selectedBeams = ElementInstances.Beam.ElementSubs.Select(x => x.Element as FamilyInstance).ToList();
                foreach (var beam in selectedBeams)
                {
                    var bb = beam.get_BoundingBox(null);
                    var transform = beam.GetTransform();
                    var vtx = transform.OfVector(XYZ.BasisX);
                    var vty = transform.OfVector(XYZ.BasisY);
                    var vtz = transform.OfVector(XYZ.BasisZ);
                    var bbox = (new RevElement(beam)).BoxElement;
                    double beamThicknessMm = bbox.Curves
                        .Where(x => x.Direction().IsParallel(vty))
                        .Select(x => x.Length.FootToMm())
                        .Max();
                    double beamHeightMm = (bb.Max.Z - bb.Min.Z);
                    var mid = bbox.LineBox.Midpoint();
                    double botElevationMm = bb.Min.Z.FootToMm();
                    double topElevationMm = bb.Max.Z.FootToMm();
                    var rebars = RevBeamHole.DeleteMainStirrup(
                        AC.Document,
                        beam,
                        rebarStps,
                        beamThicknessMm,
                        beamHeightMm,
                        botElevationMm,
                        topElevationMm,
                        ElementInstances.RebarBeamActive.QuantityStirrupSupportHole,
                        ElementInstances.RebarBeamSchemal,
                        out List<Rebar> rebarDelete);
                    if (!rebars.Any())
                        continue;
                    resutls.AddRange(rebars);
                    rebarDeletes.AddRange(rebarDelete);
                }
                return resutls;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed while bypassing beam openings.", ex);
            }
        }
        private void MainView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {

            ElementInstances.GenerateCoordinateBeam();
            CanvasPageSectionStart = new CanvasPageBase(SettingRebarSectionView.FindName("CanvasSectionStart") as Canvas);
            CanvasPageSectionMid = new CanvasPageBase(SettingRebarSectionView.FindName("CanvasSectionMid") as Canvas);
            CanvasPageSectionEnd = new CanvasPageBase(SettingRebarSectionView.FindName("CanvasSectionEnd") as Canvas);
            _drawRebarBeamInCanvasSerice.DrawSectionBeamConcrete(ElementInstances.RebarBeamActive, this);
            _drawRebarBeamInCanvasSerice.DrawOutLineFukashi(ElementInstances.RebarBeamActive, this);
            _drawRebarBeamInCanvasSerice.DrawSectionBeamStirrup(ElementInstances.RebarBeamActive, this);
            ElementInstances.MainRebarTopUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
            //ElementInstances.MainRebarBotUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBarBot(ElementInstances.RebarBeamActive, this);
            ElementInstances.SideBarUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamSideBar(ElementInstances.RebarBeamActive, this);
        }
        private void RefreshAllVerticalStirrup(RebarBeam rebarBeam)
        {
            rebarBeam.RebarBeamSectionStart.RebarBeamTop.RebarBeamTopLevel1.Hooks2 = new();
            rebarBeam.RebarBeamSectionMid.RebarBeamTop.RebarBeamTopLevel1.Hooks2 = new();
            rebarBeam.RebarBeamSectionEnd.RebarBeamTop.RebarBeamTopLevel1.Hooks2 = new();
            rebarBeam.RebarBeamSectionStart.RebarBeamBot.RebarBeamBotLevel1.Hooks2 = new();
            rebarBeam.RebarBeamSectionMid.RebarBeamBot.RebarBeamBotLevel1.Hooks2 = new();
            rebarBeam.RebarBeamSectionEnd.RebarBeamBot.RebarBeamBotLevel1.Hooks2 = new();
        }
        private void InitAction()
        {
            ElementInstances.RebarBeamAnchorTypeChange = () =>
            {
                switch ((RebarBeamAnchorType)ElementInstances.RebarBeamAnchorType.Id)
                {
                    case RebarBeamAnchorType.Type1:
                        UserControlAnchorBeamTypeViewCurrent = AnchorBeamType1View;
                        break;
                    case RebarBeamAnchorType.Type2:
                        UserControlAnchorBeamTypeViewCurrent = AnchorBeamType2View;
                        break;
                }
            };
            ElementInstances.RebarBeamActiveChange = () =>
            {
                ElementInstances.MainRebarTopUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
                //ElementInstances.MainRebarBotUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBarBot(ElementInstances.RebarBeamActive, this);
                ElementInstances.SideBarUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamSideBar(ElementInstances.RebarBeamActive, this);
                var beamId = new ElementId(ElementInstances.RebarBeamActive.BeamId);
                AC.UiDoc.Selection.SetElementIds(new List<ElementId>() { beamId });
            };
            foreach (var rebarBeam in ElementInstances.RebarBeams)
            {
                rebarBeam.RebarBeamSectionStart.RebarBeamSideBar.QuantitySideChange = () =>
                {
                    ElementInstances.SideBarUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamSideBar(ElementInstances.RebarBeamActive, this);
                };
                rebarBeam.RebarBeamSectionMid.RebarBeamSideBar.QuantitySideChange = () =>
                {
                    ElementInstances.SideBarUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamSideBar(ElementInstances.RebarBeamActive, this);
                };
                rebarBeam.RebarBeamSectionEnd.RebarBeamSideBar.QuantitySideChange = () =>
                {
                    ElementInstances.SideBarUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamSideBar(ElementInstances.RebarBeamActive, this);
                };

                rebarBeam.RebarBeamSectionStart.RebarBeamTop.RebarBeamTopLevel1.QuantityChange = () =>
                {
                    RefreshAllVerticalStirrup(rebarBeam);
                    ElementInstances.MainRebarTopUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
                };
                rebarBeam.RebarBeamSectionMid.RebarBeamTop.RebarBeamTopLevel1.QuantityChange = () =>
                {
                    RefreshAllVerticalStirrup(rebarBeam);
                    ElementInstances.MainRebarTopUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
                };
                rebarBeam.RebarBeamSectionEnd.RebarBeamTop.RebarBeamTopLevel1.QuantityChange = () =>
                {
                    RefreshAllVerticalStirrup(rebarBeam);
                    ElementInstances.MainRebarTopUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
                };

                rebarBeam.RebarBeamSectionStart.RebarBeamTop.RebarBeamTopLevel2.QuantityChange = () =>
                {
                    ElementInstances.MainRebarTopUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
                };
                rebarBeam.RebarBeamSectionMid.RebarBeamTop.RebarBeamTopLevel2.QuantityChange = () =>
                {
                    ElementInstances.MainRebarTopUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
                };
                rebarBeam.RebarBeamSectionEnd.RebarBeamTop.RebarBeamTopLevel2.QuantityChange = () =>
                {
                    ElementInstances.MainRebarTopUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
                };

                rebarBeam.RebarBeamSectionStart.RebarBeamTop.RebarBeamTopLevel3.QuantityChange = () =>
                {
                    ElementInstances.MainRebarTopUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
                };
                rebarBeam.RebarBeamSectionMid.RebarBeamTop.RebarBeamTopLevel3.QuantityChange = () =>
                {
                    ElementInstances.MainRebarTopUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
                };
                rebarBeam.RebarBeamSectionEnd.RebarBeamTop.RebarBeamTopLevel3.QuantityChange = () =>
                {
                    ElementInstances.MainRebarTopUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
                };

                rebarBeam.RebarBeamSectionStart.RebarBeamBot.RebarBeamBotLevel1.QuantityChange = () =>
                {
                    RefreshAllVerticalStirrup(rebarBeam);
                    ElementInstances.MainRebarTopUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
                };
                rebarBeam.RebarBeamSectionMid.RebarBeamBot.RebarBeamBotLevel1.QuantityChange = () =>
                {
                    RefreshAllVerticalStirrup(rebarBeam);
                    ElementInstances.MainRebarTopUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
                };
                rebarBeam.RebarBeamSectionEnd.RebarBeamBot.RebarBeamBotLevel1.QuantityChange = () =>
                {
                    RefreshAllVerticalStirrup(rebarBeam);
                    ElementInstances.MainRebarTopUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
                };

                rebarBeam.RebarBeamSectionStart.RebarBeamBot.RebarBeamBotLevel2.QuantityChange = () =>
                {
                    ElementInstances.MainRebarTopUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
                };
                rebarBeam.RebarBeamSectionMid.RebarBeamBot.RebarBeamBotLevel2.QuantityChange = () =>
                {
                    ElementInstances.MainRebarTopUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
                };
                rebarBeam.RebarBeamSectionEnd.RebarBeamBot.RebarBeamBotLevel2.QuantityChange = () =>
                {
                    ElementInstances.MainRebarTopUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
                };

                rebarBeam.RebarBeamSectionStart.RebarBeamBot.RebarBeamBotLevel3.QuantityChange = () =>
                {
                    ElementInstances.MainRebarTopUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
                };
                rebarBeam.RebarBeamSectionMid.RebarBeamBot.RebarBeamBotLevel3.QuantityChange = () =>
                {
                    ElementInstances.MainRebarTopUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
                };
                rebarBeam.RebarBeamSectionEnd.RebarBeamBot.RebarBeamBotLevel3.QuantityChange = () =>
                {
                    ElementInstances.MainRebarTopUIElement = _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
                };
            }
        }
    }
}


