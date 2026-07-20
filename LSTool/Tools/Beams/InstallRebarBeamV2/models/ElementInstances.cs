using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using HcBimUtils;
using Newtonsoft.Json;
using LSTool.Tools.Generals.SettingDiameters.models;
using LSTool.Tools.Beams.InstallRebarBeamV2.exceptions;
using RIMT.Utils.BoundingBoxs;
using RIMT.Utils.Directionaries;
using RIMT.Utils.Entities;
using RIMT.Utils.FilterElementsInRevit;
using RIMT.Utils.Paths;
using RIMT.Utils.RevitElements;
using RIMT.Utils.RevRebars;
using System.IO;
using System.Windows;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.models
{
    public partial class ElementInstances : ObservableObject
    {
        private Document _document;
        private UIDocument _uiDocument;
        public static string DIR_TOOL = $"{PathUtils.AppDataRimT}\\CreateRebarBeam";
        public string PathRebarBeamType { get; set; }
        public double DistanceRebarToRebarMm { get; set; }
        public RevElement Beam { get; set; }
        public List<RebarBarTypeCustom> RebarBarTypeCustoms { get; set; }
        public IReadOnlyDictionary<string, RebarBarTypeCustom> RebarBarTypesByName { get; private set; }
        public List<string> RebarDiameters { get; set; }
        public List<string> MainRebarDiameters { get; set; }
        public List<string> StirrupRebarDiameters { get; set; }
        [ObservableProperty]
        private List<RebarBeam> _rebarBeams;
        [ObservableProperty]
        private bool _isRebarBeamStirrupSame = false;
        public List<UIElement> MainRebarTopUIElement { get; set; }
        public List<UIElement> MainRebarBotUIElement { get; set; }
        public List<UIElement> SideBarUIElement { get; set; }
        public List<UIElement> MainRebarTopUIElementStirrup { get; set; }
        public List<UIElement> MainRebarBotUIElementStirrup { get; set; }
        public List<UIElement> SideBarUIElementStirrup { get; set; }
        [ObservableProperty]
        private List<RebarBeam> _rebarBeamTypes;
        [ObservableProperty]
        private RebarBeam _rebarBeamTypeSelected;
        [ObservableProperty]
        private string _rebarBeamTypeName;
        public double CoverMm { get; set; }
        private RebarBeam _rebarBeamActive;
        public RebarBeam RebarBeamActive
        {
            get => _rebarBeamActive;
            set
            {
                _rebarBeamActive = value;
                OnPropertyChanged();
                RebarBeamActiveChange?.Invoke();
            }
        }
        public RebarBeamDantory RebarBeamDantory { get; set; }
        public Action RebarBeamActiveChange { get; set; }
        public BeamFukashi BeamFukashi { get; set; }
        public CoverBeam CoverBeam { get; set; }
        public BeamStressRuleType BeamStressRuleType { get; set; }
        public RebarExtend RebarExtend { get; set; }
        public List<RebarBeamAnchorOption> RebarBeamAnchorTypes { get; set; }
        private RebarBeamAnchorOption _rebarBeamAnchorType;
        public RebarBeamAnchorOption RebarBeamAnchorType
        {
            get => _rebarBeamAnchorType;
            set
            {
                _rebarBeamAnchorType = value;
                OnPropertyChanged();
                RebarBeamAnchorTypeChange?.Invoke();
            }
        }
        public Action RebarBeamAnchorTypeChange { get; set; }
        public RebarBeamAnchor RebarBeamAnchor { get; set; }
        public SchemaInfo RebarBeamSchemal { get; set; }

        public ElementInstances(UIDocument uIDocument, Element obj)
        {
            PathUtils.MigrateCreateRebarBeamPresets();
            RebarBeamSchemal = new SchemaInfo(
                LSTool.Properties.Langs.SchemaInfo.REBAR_BEAM_SCHEMAL_INFO_GUID,
                LSTool.Properties.Langs.SchemaInfo.REBAR_BEAM_SCHEMAL_INFO_NAME,
                new SchemaField());
            _uiDocument = uIDocument;
            _document = uIDocument.Document;
            RebarBeamTypeName = "";
            PathRebarBeamType = $"{DIR_TOOL}\\{_document.ProjectInformation.UniqueId}\\RebarBeamTypes.json";
            DirectionaryExt.CreateDirectory(PathRebarBeamType);
            RebarBeamTypes = GetRebarBeamTypes();
            RebarBeamTypeSelected = RebarBeamTypes.FirstOrDefault();

            Beam = new RevElement(obj);
            if (Beam.ElementSubs == null || Beam.ElementSubs.Count == 0)
                throw new InvalidOperationException(
                    "No structural framing members were found in the selected element.");
            if (Beam.ElementSubs.Any(member =>
                    member?.Element is not FamilyInstance
                    || member.Element.Category?.Id.Value != (long)BuiltInCategory.OST_StructuralFraming))
                throw new InvalidOperationException(
                    "The selected assembly must contain only structural framing family instances.");
            RebarBarTypeCustoms = _document.GetElementsFromClass<RebarBarType>()
                .Select(x => new RebarBarTypeCustom(x))
                .Where(x => x.NameStyle.Contains("D") && x.NameStyle.Contains("("))
                .ToList();
            var duplicateBarTypeNames = RebarBarTypeCustoms
                .GroupBy(type => type.NameStyle, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .OrderBy(name => name)
                .ToList();
            if (duplicateBarTypeNames.Count > 0)
                throw new InvalidOperationException(
                    $"Duplicate rebar type names are not supported: {string.Join(", ", duplicateBarTypeNames)}");
            RebarBarTypesByName = RebarBarTypeCustoms.ToDictionary(
                type => type.NameStyle,
                StringComparer.OrdinalIgnoreCase);
            RebarDiameters = RebarBarTypeCustoms
                .Select(x => x.NameStyle)
                .Where(x => x.Contains("D"))
                .OrderBy(x => x)
                .ToList();
            if (!RebarDiameters.Any()) throw new Exception("Please Load Diameter");
            GetDiameterRebarBeam();
            RebarBeams = Beam.ElementSubs?
                .Select((member, index) => new RebarBeam(member)
                {
                    SpanIndex = index + 1
                })
                .ToList();
            InitDataRebarBeam();
            RebarBeamActive = RebarBeams.FirstOrDefault();
            var beamId = new ElementId(RebarBeamActive.BeamId);
            _uiDocument.Selection.SetElementIds(new List<ElementId>() { beamId });
            RebarBeamActive.MainStirrupType3 = RebarBeamTypeSelected == null ? true : RebarBeamTypeSelected.MainStirrupType3;
            RebarBeamActive.MainStirrupType2 = RebarBeamTypeSelected == null ? true : RebarBeamTypeSelected.MainStirrupType2;
            RebarBeamActive.MainStirrupType1 = RebarBeamTypeSelected == null ? true : RebarBeamTypeSelected.MainStirrupType1;
            RebarBeamActive.EnsureMainStirrupShapeSelected();
            RebarBeamActive.MainStirrupTypeHat = RebarBeamTypeSelected == null ? true : RebarBeamTypeSelected.MainStirrupTypeHat;
            RebarBeamActive.HorizontalDaiPhu = RebarBeamTypeSelected == null ? true : RebarBeamTypeSelected.HorizontalDaiPhu;
            RebarBeamActive.VerticalDaiPhu = RebarBeamTypeSelected == null ? true : RebarBeamTypeSelected.VerticalDaiPhu;
            DistanceRebarToRebarMm = 100;
            CoverMm = 30;
            MainRebarTopUIElement = new List<UIElement>();
            MainRebarBotUIElement = new List<UIElement>();
            SideBarUIElement = new List<UIElement>();
            MainRebarTopUIElementStirrup = new List<UIElement>();
            MainRebarBotUIElementStirrup = new List<UIElement>();
            SideBarUIElementStirrup = new List<UIElement>();
            // Client scope: Fukashi is always zero and must not read family parameters.
            BeamFukashi = new BeamFukashi();
            CoverBeam = new CoverBeam
            {
                TopCover = 30,
                RightCover = 30,
                BottomCover = 30,
                LeftCover = 30
            };
            BeamStressRuleType = new BeamStressRuleType
            {
                StressStart = 0.25,
                StressMid = 0.25,
                StressEnd = 0.25
            };
            RebarExtend = new RebarExtend
            {
                RebarTopExtend = 15,
                RebarBotExtend = 15
            };
            RebarBeamAnchorTypes = RebarBeamAnchorOption.DataInit();
            RebarBeamAnchorType = RebarBeamAnchorTypes.FirstOrDefault();
            RebarBeamAnchor = new RebarBeamAnchor
            {
                Type1_L1_X_Start = 100,
                Type1_L1_X_End = 100,
                Type1_L3_X_Start = 100,
                Type1_L3_X_End = 100,
                Type2_L1_X_Start = 1000,
                Type2_L1_X_End = 1000,
                Type2_L3_X_Start = 1000,
                Type2_L3_X_End = 1000,
                Type1_L1_Y_Start = 500,
                Type1_L1_Y_End = 500,
                Type1_L3_Y_Start = 500,
                Type1_L3_Y_End = 500,
                Type2_L1_Y_Start = 500,
                Type2_L1_Y_End = 500,
                Type2_L3_Y_Start = 500,
                Type2_L3_Y_End = 500
            };
            RebarBeamDantory = new RebarBeamDantory()
            {
                Diameter = RebarDiameters.FirstOrDefault(),
                Quantity = 2
            };
        }

        public RebarBarTypeCustom GetRebarBarType(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("A rebar type name is required.");
            if (!RebarBarTypesByName.TryGetValue(name, out var result))
                throw new InvalidOperationException(
                    $"Rebar type '{name}' was not found in the active document.");
            return result;
        }
        public void InitDataRebarBeam()
        {
            foreach (var rebarBeam in RebarBeams)
            {
                rebarBeam.MainStirrupType1 = RebarBeamTypeSelected == null ? true : RebarBeamTypeSelected.MainStirrupType1;
                rebarBeam.MainStirrupType2 = RebarBeamTypeSelected == null ? true : RebarBeamTypeSelected.MainStirrupType2;
                rebarBeam.MainStirrupType3 = RebarBeamTypeSelected == null ? true : RebarBeamTypeSelected.MainStirrupType3;
                rebarBeam.EnsureMainStirrupShapeSelected();
                rebarBeam.QuantityStirrupSupportHole = RebarBeamTypeSelected == null ? 2 : RebarBeamTypeSelected.QuantityStirrupSupportHole;
                rebarBeam.MainStirrupTypeHat = RebarBeamTypeSelected == null ? true : RebarBeamTypeSelected.MainStirrupTypeHat;
                rebarBeam.HorizontalDaiPhu = RebarBeamTypeSelected == null ? true : RebarBeamTypeSelected.HorizontalDaiPhu;
                rebarBeam.VerticalDaiPhu = RebarBeamTypeSelected == null ? true : RebarBeamTypeSelected.VerticalDaiPhu;
                rebarBeam.RebarBeamSectionStart = new RebarBeamSectionStart();
                rebarBeam.RebarBeamSectionMid = new RebarBeamSectionMid();
                rebarBeam.RebarBeamSectionEnd = new RebarBeamSectionEnd();
                InitDataRebarBeamSection(rebarBeam, rebarBeam.RebarBeamSectionStart);
                InitDataRebarBeamSection(rebarBeam, rebarBeam.RebarBeamSectionMid);
                InitDataRebarBeamSection(rebarBeam, rebarBeam.RebarBeamSectionEnd);
            }
        }
        public void InitDataRebarBeamApply()
        {
            ApplySection(this.RebarBeamActive.RebarBeamSectionStart);
            ApplySection(this.RebarBeamActive.RebarBeamSectionMid);
            ApplySection(this.RebarBeamActive.RebarBeamSectionEnd);
        }

        public void CopyActiveSpanSettingsToAll()
        {
            var source = RebarBeamActive
                ?? throw new InvalidOperationException("Select a source span before copying settings.");
            foreach (var target in RebarBeams.Where(beam => beam.BeamId != source.BeamId))
            {
                RebarBeam.ResetActionChange(target);
                target.MainStirrupType1 = source.MainStirrupType1;
                target.MainStirrupType2 = source.MainStirrupType2;
                target.MainStirrupType3 = source.MainStirrupType3;
                target.MainStirrupTypeHat = source.MainStirrupTypeHat;
                target.HorizontalDaiPhu = source.HorizontalDaiPhu;
                target.VerticalDaiPhu = source.VerticalDaiPhu;
                target.QuantityStirrupSupportHole = source.QuantityStirrupSupportHole;

                CopySectionSettings(target.RebarBeamSectionStart, source.RebarBeamSectionStart);
                CopySectionSettings(target.RebarBeamSectionMid, source.RebarBeamSectionMid);
                CopySectionSettings(target.RebarBeamSectionEnd, source.RebarBeamSectionEnd);
            }
        }
        private void GetDiameterRebarBeam()
        {
            var schema = new RebarBarTypeSchema(RebarBarTypeSchema.GUID, RebarBarTypeSchema.NAME);
            var content = schema.Read(_document.ProjectInformation);
            double stirrupLimit;
            if (string.IsNullOrWhiteSpace(content))
            {
                var documentDiameters = RebarBarTypeCustoms
                    .Where(type => type.ModelBarDiameter > 0)
                    .Select(type => type.ModelBarDiameter)
                    .Distinct()
                    .OrderBy(diameter => diameter)
                    .ToList();
                if (documentDiameters.Count < 3)
                    throw new InvalidOperationException(
                        "At least three distinct configured rebar diameters are required to classify stirrup and main-bar types.");
                stirrupLimit = documentDiameters[2];
            }
            else
            {
                List<RebarBarTypeModel> settings;
                try
                {
                    settings = JsonConvert.DeserializeObject<List<RebarBarTypeModel>>(content)
                        ?? throw new InvalidOperationException("Diameter settings are empty.");
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Diameter settings stored in the model are invalid.", ex);
                }

                var configuredDiameters = settings
                    .Where(setting => setting.BarDiameterReal > 0)
                    .Select(setting => setting.BarDiameterReal)
                    .Distinct()
                    .OrderBy(diameter => diameter)
                    .ToList();
                if (configuredDiameters.Count < 3)
                    throw new InvalidOperationException(
                        "Diameter settings must contain at least three distinct positive diameters.");
                stirrupLimit = configuredDiameters[2].MmToFoot();
            }

            if (stirrupLimit <= 0)
                throw new Exception(InstallRebarBeamV2Exceptions.EXCEPTION_DIAMETER_NOT_FOUND);

            MainRebarDiameters = RebarBarTypeCustoms
                .Where(type => type.ModelBarDiameter.IsGreater(stirrupLimit))
                .Select(type => type.NameStyle)
                .Where(name => name.Contains("D"))
                .OrderBy(name => name)
                .ToList();
            StirrupRebarDiameters = RebarBarTypeCustoms
                .Where(type => type.ModelBarDiameter.IsSmallerEqual(stirrupLimit))
                .Select(type => type.NameStyle)
                .Where(name => name.Contains("D"))
                .OrderBy(name => name)
                .ToList();
            if (MainRebarDiameters.Count == 0 || StirrupRebarDiameters.Count == 0)
                throw new InvalidOperationException(
                    "Diameter classification produced an empty main-bar or stirrup list. Check the configured rebar types.");
        }
        private void ApplySection(RebarBeamSection rebarBeamSection)
        {
            try
            {
                RebarBeamSection sectionActive = null;
                switch (rebarBeamSection.RebarBeamSectionType)
                {
                    case (int)RebarBeamSectionType.SectionStart:
                        sectionActive = RebarBeamTypeSelected?.RebarBeamSectionStart;
                        break;
                    case (int)RebarBeamSectionType.SectionMid:
                        sectionActive = RebarBeamTypeSelected?.RebarBeamSectionMid;
                        break;
                    case (int)RebarBeamSectionType.SectionEnd:
                        sectionActive = RebarBeamTypeSelected?.RebarBeamSectionEnd;
                        break;
                }
                rebarBeamSection.RebarBeamTop.RebarBeamTopLevel1.Diameter =
                    sectionActive.RebarBeamTop.RebarBeamTopLevel1.Diameter;
                rebarBeamSection.RebarBeamTop.RebarBeamTopLevel1.Quantity =
                    sectionActive.RebarBeamTop.RebarBeamTopLevel1.Quantity;

                rebarBeamSection.RebarBeamTop.RebarBeamTopLevel2.Diameter =
                    sectionActive.RebarBeamTop.RebarBeamTopLevel2.Diameter;
                rebarBeamSection.RebarBeamTop.RebarBeamTopLevel2.Quantity =
                    sectionActive.RebarBeamTop.RebarBeamTopLevel2.Quantity;

                rebarBeamSection.RebarBeamTop.RebarBeamTopLevel3.Diameter =
                    sectionActive.RebarBeamTop.RebarBeamTopLevel3.Diameter;
                rebarBeamSection.RebarBeamTop.RebarBeamTopLevel3.Quantity =
                    sectionActive.RebarBeamTop.RebarBeamTopLevel3.Quantity;

                rebarBeamSection.RebarBeamBot.RebarBeamBotLevel1.Diameter =
                    sectionActive.RebarBeamBot.RebarBeamBotLevel1.Diameter;
                rebarBeamSection.RebarBeamBot.RebarBeamBotLevel1.Quantity =
                    sectionActive.RebarBeamBot.RebarBeamBotLevel1.Quantity;

                rebarBeamSection.RebarBeamBot.RebarBeamBotLevel2.Diameter =
                    sectionActive.RebarBeamBot.RebarBeamBotLevel2.Diameter;
                rebarBeamSection.RebarBeamBot.RebarBeamBotLevel2.Quantity =
                    sectionActive.RebarBeamBot.RebarBeamBotLevel2.Quantity;

                rebarBeamSection.RebarBeamBot.RebarBeamBotLevel3.Diameter =
                    sectionActive.RebarBeamBot.RebarBeamBotLevel3.Diameter;
                rebarBeamSection.RebarBeamBot.RebarBeamBotLevel3.Quantity =
                    sectionActive.RebarBeamBot.RebarBeamBotLevel3.Quantity;

                rebarBeamSection.RebarBeamStirrup.Diameter =
                    sectionActive.RebarBeamStirrup.Diameter;
                rebarBeamSection.RebarBeamStirrup.Quantity =
                    sectionActive.RebarBeamStirrup.Quantity;
                rebarBeamSection.RebarBeamStirrup.SpacingChange = null;
                rebarBeamSection.RebarBeamStirrup.Spacing =
                    sectionActive.RebarBeamStirrup.Spacing;
                rebarBeamSection.RebarBeamStirrup.SpacingChange = () =>
                {
                    ReWriteSpacingStirrup(rebarBeamSection, this.RebarBeamActive);
                };

                rebarBeamSection.RebarBeamSideBar.Diameter =
                    sectionActive.RebarBeamSideBar.Diameter;
                rebarBeamSection.RebarBeamSideBar.QuantitySide =
                    sectionActive.RebarBeamSideBar.QuantitySide;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to apply reinforcement preset to section {rebarBeamSection?.RebarBeamSectionType}.", ex);
            }
        }
        private void ReWriteSpacingStirrup(RebarBeamSection rebarBeamSection, RebarBeam rebarBeam)
        {
            if (IsRebarBeamStirrupSame)
            {
                var spacing = rebarBeamSection.RebarBeamStirrup.Spacing;
                rebarBeam.RebarBeamSectionStart.RebarBeamStirrup.SpacingChange = null;
                rebarBeam.RebarBeamSectionMid.RebarBeamStirrup.SpacingChange = null;
                rebarBeam.RebarBeamSectionEnd.RebarBeamStirrup.SpacingChange = null;
                rebarBeam.RebarBeamSectionStart.RebarBeamStirrup.Spacing = spacing;
                rebarBeam.RebarBeamSectionMid.RebarBeamStirrup.Spacing = spacing;
                rebarBeam.RebarBeamSectionEnd.RebarBeamStirrup.Spacing = spacing;
                rebarBeam.RebarBeamSectionStart.RebarBeamStirrup.SpacingChange = () =>
                {
                    ReWriteSpacingStirrup(rebarBeam.RebarBeamSectionStart, rebarBeam);

                };
                rebarBeam.RebarBeamSectionMid.RebarBeamStirrup.SpacingChange = () =>
                {
                    ReWriteSpacingStirrup(rebarBeam.RebarBeamSectionMid, rebarBeam);

                };
                rebarBeam.RebarBeamSectionEnd.RebarBeamStirrup.SpacingChange = () =>
                {
                    ReWriteSpacingStirrup(rebarBeam.RebarBeamSectionEnd, rebarBeam);

                };
            }
        }
        private void InitDataRebarBeamSection(RebarBeam rebarBeam, RebarBeamSection rebarBeamSection)
        {
            RebarBeamSection sectionActive = null;
            switch (rebarBeamSection.RebarBeamSectionType)
            {
                case (int)RebarBeamSectionType.SectionStart:
                    sectionActive = RebarBeamTypeSelected?.RebarBeamSectionStart;
                    break;
                case (int)RebarBeamSectionType.SectionMid:
                    sectionActive = RebarBeamTypeSelected?.RebarBeamSectionMid;
                    break;
                case (int)RebarBeamSectionType.SectionEnd:
                    sectionActive = RebarBeamTypeSelected?.RebarBeamSectionEnd;
                    break;
            }
            rebarBeamSection.RebarBeamStirrup = new RebarBeamStirrup();
            rebarBeamSection.RebarBeamStirrup.HostId = rebarBeam.BeamId;
            rebarBeamSection.RebarBeamStirrup.Diameter = sectionActive == null
                ? StirrupRebarDiameters.FirstOrDefault()
                : sectionActive.RebarBeamStirrup.Diameter;
            rebarBeamSection.RebarBeamStirrup.Spacing = sectionActive == null
                ? 100 : sectionActive.RebarBeamStirrup.Spacing;
            rebarBeamSection.RebarBeamStirrup.RebarBeamType = (int)RebarBeamType.RebarBeamStirrup;

            rebarBeamSection.RebarBeamStirrup.SpacingChange = () =>
            {
                //check
                ReWriteSpacingStirrup(rebarBeamSection, rebarBeam);
            };

            rebarBeamSection.RebarBeamSideBar = new RebarBeamSideBar();
            rebarBeamSection.RebarBeamSideBar.HostId = rebarBeam.BeamId;
            rebarBeamSection.RebarBeamSideBar.Diameter = sectionActive == null
                ? MainRebarDiameters.FirstOrDefault()
                : sectionActive.RebarBeamSideBar.Diameter;
            rebarBeamSection.RebarBeamSideBar.QuantitySide = sectionActive == null
                ? 1 : sectionActive.RebarBeamSideBar.QuantitySide;
            rebarBeamSection.RebarBeamSideBar.RebarBeamType = (int)RebarBeamType.RebarBeamSideBar;

            rebarBeamSection.RebarBeamTop = new RebarBeamTop();
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel1 = new RebarBeamMainBar();
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel1.HostId = rebarBeam.BeamId;
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel1.Diameter = sectionActive == null
                ? MainRebarDiameters.FirstOrDefault()
                : sectionActive.RebarBeamTop.RebarBeamTopLevel1.Diameter;
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel1.Quantity = sectionActive == null
                ? 3 : sectionActive.RebarBeamTop.RebarBeamTopLevel1.Quantity;
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel1.RebarBeamType = sectionActive == null
                ? (int)RebarBeamType.RebarBeamMainBar : sectionActive.RebarBeamTop.RebarBeamTopLevel1.RebarBeamType;
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel1.RebarLevelType = sectionActive == null
                ? (int)RebarBeamMainBarLevelType.RebarTop
                : sectionActive.RebarBeamTop.RebarBeamTopLevel1.RebarLevelType;
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel1.RebarGroupType = sectionActive == null
                ? (int)RebarBeamMainBarGroupType.GroupLevel1
                : sectionActive.RebarBeamTop.RebarBeamTopLevel1.RebarGroupType;
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel1.RebarBeamType = (int)RebarBeamType.RebarBeamMainBar;

            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel2 = new RebarBeamMainBar();
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel2.HostId = rebarBeam.BeamId;
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel2.Diameter = sectionActive == null
                ? MainRebarDiameters.FirstOrDefault()
                : sectionActive.RebarBeamTop.RebarBeamTopLevel2.Diameter;
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel2.Quantity = sectionActive == null
                ? 0 : sectionActive.RebarBeamTop.RebarBeamTopLevel2.Quantity;
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel2.RebarBeamType = sectionActive == null
                ? (int)RebarBeamType.RebarBeamMainBar : sectionActive.RebarBeamTop.RebarBeamTopLevel2.RebarBeamType;
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel2.RebarLevelType = sectionActive == null
                ? (int)RebarBeamMainBarLevelType.RebarTop
                : sectionActive.RebarBeamTop.RebarBeamTopLevel2.RebarLevelType;
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel2.RebarGroupType = sectionActive == null
                ? (int)RebarBeamMainBarGroupType.GroupLevel2
                : sectionActive.RebarBeamTop.RebarBeamTopLevel2.RebarGroupType;
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel2.RebarBeamType = (int)RebarBeamType.RebarBeamMainBar;

            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel3 = new RebarBeamMainBar();
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel3.HostId = rebarBeam.BeamId;
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel3.Diameter = sectionActive == null
                ? MainRebarDiameters.FirstOrDefault()
                : sectionActive.RebarBeamTop.RebarBeamTopLevel3.Diameter;
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel3.Quantity = sectionActive == null
                ? 0 : sectionActive.RebarBeamTop.RebarBeamTopLevel3.Quantity;
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel3.RebarBeamType = sectionActive == null
                ? (int)RebarBeamType.RebarBeamMainBar : sectionActive.RebarBeamTop.RebarBeamTopLevel3.RebarBeamType;
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel3.RebarLevelType = sectionActive == null
                ? (int)RebarBeamMainBarLevelType.RebarTop
                : sectionActive.RebarBeamTop.RebarBeamTopLevel3.RebarLevelType;
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel3.RebarGroupType = sectionActive == null
                ? (int)RebarBeamMainBarGroupType.GroupLevel3
                : sectionActive.RebarBeamTop.RebarBeamTopLevel3.RebarGroupType;
            rebarBeamSection.RebarBeamTop.RebarBeamTopLevel3.RebarBeamType = (int)RebarBeamType.RebarBeamMainBar;

            rebarBeamSection.RebarBeamTop.RebarBeamMainBarGroups = RebarBeamMainBarGroup.GetRebarBeamMainBarGroups();

            rebarBeamSection.RebarBeamTop.RebarGroupTypeActive = rebarBeamSection.RebarBeamTop.RebarBeamMainBarGroups.FirstOrDefault();
            switch (rebarBeamSection.RebarBeamTop.RebarGroupTypeActive.Id)
            {
                case (int)RebarBeamMainBarGroupType.GroupLevel1:
                    rebarBeamSection.RebarBeamTop.RebarBeamTopLevelActive = rebarBeamSection.RebarBeamTop.RebarBeamTopLevel1;
                    break;
                case (int)RebarBeamMainBarGroupType.GroupLevel2:
                    rebarBeamSection.RebarBeamTop.RebarBeamTopLevelActive = rebarBeamSection.RebarBeamTop.RebarBeamTopLevel2;
                    break;
                case (int)RebarBeamMainBarGroupType.GroupLevel3:
                    rebarBeamSection.RebarBeamTop.RebarBeamTopLevelActive = rebarBeamSection.RebarBeamTop.RebarBeamTopLevel3;
                    break;
            }
            rebarBeamSection.RebarBeamTop.RebarGroupTypeChange = () =>
            {
                RebarBeamTop.TopRebarGroupTypeChangeFunc(rebarBeamSection, rebarBeam);
            };

            rebarBeamSection.RebarBeamBot = new RebarBeamBot();
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel1 = new RebarBeamMainBar();
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel1.HostId = rebarBeam.BeamId;
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel1.Diameter = sectionActive == null
                ? MainRebarDiameters.FirstOrDefault()
                : sectionActive.RebarBeamBot.RebarBeamBotLevel1.Diameter;
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel1.Quantity = sectionActive == null
                ? 3 : sectionActive.RebarBeamBot.RebarBeamBotLevel1.Quantity;
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel1.RebarBeamType = sectionActive == null
                ? (int)RebarBeamType.RebarBeamMainBar : sectionActive.RebarBeamBot.RebarBeamBotLevel1.RebarBeamType;
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel1.RebarLevelType = sectionActive == null
                ? (int)RebarBeamMainBarLevelType.RebarBot
                : sectionActive.RebarBeamBot.RebarBeamBotLevel1.RebarLevelType;
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel1.RebarGroupType = sectionActive == null
                ? (int)RebarBeamMainBarGroupType.GroupLevel1
                : sectionActive.RebarBeamBot.RebarBeamBotLevel1.RebarGroupType;
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel1.RebarBeamType = (int)RebarBeamType.RebarBeamMainBar;

            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel2 = new RebarBeamMainBar();
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel2.HostId = rebarBeam.BeamId;
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel2.Diameter = sectionActive == null
                ? MainRebarDiameters.FirstOrDefault()
                : sectionActive.RebarBeamBot.RebarBeamBotLevel2.Diameter;
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel2.Quantity = sectionActive == null
                ? 0 : sectionActive.RebarBeamBot.RebarBeamBotLevel2.Quantity;
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel2.RebarBeamType = sectionActive == null
                ? (int)RebarBeamType.RebarBeamMainBar : sectionActive.RebarBeamBot.RebarBeamBotLevel2.RebarBeamType;
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel2.RebarLevelType = sectionActive == null
                ? (int)RebarBeamMainBarLevelType.RebarBot
                : sectionActive.RebarBeamBot.RebarBeamBotLevel2.RebarLevelType;
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel2.RebarGroupType = sectionActive == null
                ? (int)RebarBeamMainBarGroupType.GroupLevel2
                : sectionActive.RebarBeamBot.RebarBeamBotLevel2.RebarGroupType;
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel2.RebarBeamType = (int)RebarBeamType.RebarBeamMainBar;

            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel3 = new RebarBeamMainBar();
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel3.HostId = rebarBeam.BeamId;
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel3.Diameter = sectionActive == null
                ? MainRebarDiameters.FirstOrDefault()
                : sectionActive.RebarBeamBot.RebarBeamBotLevel3.Diameter;
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel3.Quantity = sectionActive == null
                ? 0 : sectionActive.RebarBeamBot.RebarBeamBotLevel3.Quantity;
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel3.RebarBeamType = sectionActive == null
                ? (int)RebarBeamType.RebarBeamMainBar : sectionActive.RebarBeamBot.RebarBeamBotLevel3.RebarBeamType;
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel3.RebarLevelType = sectionActive == null
                ? (int)RebarBeamMainBarLevelType.RebarBot
                : sectionActive.RebarBeamBot.RebarBeamBotLevel3.RebarLevelType;
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel3.RebarGroupType = sectionActive == null
                ? (int)RebarBeamMainBarGroupType.GroupLevel3
                : sectionActive.RebarBeamBot.RebarBeamBotLevel3.RebarGroupType;
            rebarBeamSection.RebarBeamBot.RebarBeamBotLevel3.RebarBeamType = (int)RebarBeamType.RebarBeamMainBar;

            rebarBeamSection.RebarBeamBot.RebarBeamMainBarGroups = RebarBeamMainBarGroup.GetRebarBeamMainBarGroups();
            rebarBeamSection.RebarBeamBot.RebarGroupTypeActive = rebarBeamSection.RebarBeamBot.RebarBeamMainBarGroups.FirstOrDefault();
            switch (rebarBeamSection.RebarBeamBot.RebarGroupTypeActive.Id)
            {
                case (int)RebarBeamMainBarGroupType.GroupLevel1:
                    rebarBeamSection.RebarBeamBot.RebarBeamBotLevelActive = rebarBeamSection.RebarBeamBot.RebarBeamBotLevel1;
                    break;
                case (int)RebarBeamMainBarGroupType.GroupLevel2:
                    rebarBeamSection.RebarBeamBot.RebarBeamBotLevelActive = rebarBeamSection.RebarBeamBot.RebarBeamBotLevel2;
                    break;
                case (int)RebarBeamMainBarGroupType.GroupLevel3:
                    rebarBeamSection.RebarBeamBot.RebarBeamBotLevelActive = rebarBeamSection.RebarBeamBot.RebarBeamBotLevel3;
                    break;
            }
            rebarBeamSection.RebarBeamBot.RebarGroupTypeChange = () =>
            {
                RebarBeamBot.BotRebarGroupTypeChangeFunc(rebarBeamSection, rebarBeam);
            };

            if (sectionActive != null)
            {
                rebarBeamSection.RebarBeamTop.RebarBeamTopLevel1.Hooks2 = sectionActive.RebarBeamTop.RebarBeamTopLevel1.Hooks2;
                rebarBeamSection.RebarBeamBot.RebarBeamBotLevel1.Hooks2 = sectionActive.RebarBeamBot.RebarBeamBotLevel1.Hooks2;
            }
            else
            {
                rebarBeamSection.RebarBeamTop.RebarBeamTopLevel1.Hooks2 = new();
                rebarBeamSection.RebarBeamBot.RebarBeamBotLevel1.Hooks2 = new();
            }
        }
        private List<RebarBeam> GetRebarBeamTypes()
        {
            try
            {
                var rebarBeamTypes = JsonConvert.DeserializeObject<List<RebarBeam>>(
                    File.ReadAllText(PathRebarBeamType)) ?? new List<RebarBeam>();
                foreach (var rebarBeamType in rebarBeamTypes)
                    rebarBeamType?.EnsureMainStirrupShapeSelected();
                return rebarBeamTypes;
            }
            catch (Exception)
            {
            }
            return new List<RebarBeam>();
        }
        public void GenerateCoordinateBeam()
        {
            try
            {
                //vt phải là thằng đầu tiên
                var qBeam = Beam.ElementSubs.Count;
                var fBeam = Beam.ElementSubs.FirstOrDefault().Element as FamilyInstance;
                var fBeamCenter = Beam.ElementSubs.FirstOrDefault().LineBox.Midpoint();
                var trsBeam = fBeam.GetTransform();
                var vtx = trsBeam.OfVector(XYZ.BasisX);
                var vtz = trsBeam.OfVector(XYZ.BasisZ).DotProduct(XYZ.BasisZ).IsGreater(0)
                    ? trsBeam.OfVector(XYZ.BasisZ)
                    : -trsBeam.OfVector(XYZ.BasisZ);
                //
                if (qBeam > 1)
                {
                    var scBeam = Beam.ElementSubs[1].Element as FamilyInstance;
                    var scBeamCenter = Beam.ElementSubs[1].LineBox.Midpoint();
                    var vt = (scBeamCenter - fBeamCenter).Normalize();
                    vtx = vt.DotProduct(trsBeam.OfVector(XYZ.BasisX)).IsGreaterEqual(0) ? vtx : -vtx;
                }
                var vty = vtx.CrossProduct(vtz).Normalize();
                Beam.BoxElement.GenerateCoordinateAndPointControl(vtx, vty, vtz, BeamFukashi);

                foreach (var beam in Beam.ElementSubs)
                {
                    beam.GenerateCoordinateAndPointControl(vtx, vty, vtz, BeamFukashi);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to generate the coordinate system for the selected beam assembly.", ex);
            }
        }

        private static void CopySectionSettings(
            RebarBeamSection target,
            RebarBeamSection source)
        {
            if (target == null || source == null)
                throw new InvalidOperationException("The source or target span section is unavailable.");

            CopyMainBar(target.RebarBeamTop.RebarBeamTopLevel1, source.RebarBeamTop.RebarBeamTopLevel1);
            CopyMainBar(target.RebarBeamTop.RebarBeamTopLevel2, source.RebarBeamTop.RebarBeamTopLevel2);
            CopyMainBar(target.RebarBeamTop.RebarBeamTopLevel3, source.RebarBeamTop.RebarBeamTopLevel3);
            CopyMainBar(target.RebarBeamBot.RebarBeamBotLevel1, source.RebarBeamBot.RebarBeamBotLevel1);
            CopyMainBar(target.RebarBeamBot.RebarBeamBotLevel2, source.RebarBeamBot.RebarBeamBotLevel2);
            CopyMainBar(target.RebarBeamBot.RebarBeamBotLevel3, source.RebarBeamBot.RebarBeamBotLevel3);

            var topGroupId = source.RebarBeamTop.RebarGroupTypeActive?.Id;
            if (topGroupId != null)
            {
                target.RebarBeamTop.RebarGroupTypeActive = target.RebarBeamTop.RebarBeamMainBarGroups
                    .FirstOrDefault(group => group.Id == topGroupId);
                RebarBeamTop.TopRebarGroupTypeChangeFunc(target.RebarBeamTop);
            }

            var bottomGroupId = source.RebarBeamBot.RebarGroupTypeActive?.Id;
            if (bottomGroupId != null)
            {
                target.RebarBeamBot.RebarGroupTypeActive = target.RebarBeamBot.RebarBeamMainBarGroups
                    .FirstOrDefault(group => group.Id == bottomGroupId);
                RebarBeamBot.BotRebarGroupTypeChangeFunc(target.RebarBeamBot);
            }

            target.RebarBeamStirrup.Diameter = source.RebarBeamStirrup.Diameter;
            target.RebarBeamStirrup.Quantity = source.RebarBeamStirrup.Quantity;
            target.RebarBeamStirrup.QtyInstall = source.RebarBeamStirrup.QtyInstall;
            target.RebarBeamStirrup.Spacing = source.RebarBeamStirrup.Spacing;

            target.RebarBeamSideBar.Diameter = source.RebarBeamSideBar.Diameter;
            target.RebarBeamSideBar.QuantitySide = source.RebarBeamSideBar.QuantitySide;
            target.RebarBeamSideBar.RebarBeamType = source.RebarBeamSideBar.RebarBeamType;
        }

        private static void CopyMainBar(RebarBeamMainBar target, RebarBeamMainBar source)
        {
            target.Diameter = source.Diameter;
            target.Quantity = source.Quantity;
            target.QtyInstall = source.QtyInstall;
            target.RebarBeamType = source.RebarBeamType;
            target.RebarGroupType = source.RebarGroupType;
            target.RebarLevelType = source.RebarLevelType;
            target.HasHorizontalHook = source.HasHorizontalHook;
            target.Hooks2 = source.Hooks2 == null
                ? null
                : new Dictionary<int, bool>(source.Hooks2);
        }
    }
}


