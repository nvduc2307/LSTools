using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using LSTool.Compatibility;
using RIMT.Utils.BoundingBoxs;
using RIMT.Utils.Geometries;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.models
{
    public partial class RebarBeam : ObservableObject
    {
        public long BeamId { get; set; }
        public int SpanIndex { get; set; }
        public string Name { get; set; }
        public string SpanDisplayName => SpanIndex > 0
            ? $"Span {SpanIndex} — ID {BeamId} — {Name}"
            : Name;
        public string NameType { get; set; }
        public double BeamWidthMm { get; set; }
        public double BeamHeightMm { get; set; }
        public RebarBeamSectionStart RebarBeamSectionStart { get; set; }
        public RebarBeamSectionMid RebarBeamSectionMid { get; set; }
        public RebarBeamSectionEnd RebarBeamSectionEnd { get; set; }
        public BeamStressRule BeamStressRule { get; set; }
        private bool _mainStirrupType1;
        public bool MainStirrupType1
        {
            get => _mainStirrupType1;
            set
            {
                _mainStirrupType1 = value;
                OnPropertyChanged();
                if (_mainStirrupType1)
                {
                    MainStirrupType2 = false;
                    MainStirrupType3 = false;
                }
            }
        }
        private bool _mainStirrupType2;
        public bool MainStirrupType2
        {
            get => _mainStirrupType2;
            set
            {
                _mainStirrupType2 = value;
                OnPropertyChanged();
                if (_mainStirrupType2)
                {
                    MainStirrupType1 = false;
                    MainStirrupType3 = false;
                }
            }
        }
        private bool _mainStirrupType3;
        public bool MainStirrupType3
        {
            get => _mainStirrupType3;
            set
            {
                _mainStirrupType3 = value;
                OnPropertyChanged();
                if (_mainStirrupType3)
                {
                    MainStirrupType1 = false;
                    MainStirrupType2 = false;
                }
            }
        }

        /// <summary>
        /// A persisted preset must always select exactly one main stirrup shape.
        /// Older presets can deserialize with every shape disabled, which leaves
        /// the stirrup writer without an implementation to execute.
        /// </summary>
        public bool EnsureMainStirrupShapeSelected()
        {
            if (MainStirrupType1 || MainStirrupType2 || MainStirrupType3)
                return false;

            MainStirrupType1 = true;
            return true;
        }
     
        [ObservableProperty]
        private bool _mainStirrupTypeHat;
        [ObservableProperty]
        private bool _horizontalDaiPhu;
        [ObservableProperty]
        private bool _verticalDaiPhu;
        [ObservableProperty]
        private int _quantityStirrupSupportHole = 2;
        public RebarBeam(BoxElement revBoxBeam)
        {
            BeamId = revBoxBeam.Id;
            Name = revBoxBeam.Element.Name;
            NameType = "";
            BeamWidthMm = GetBeamWidthMm(revBoxBeam, out double beamHeightMm);
            BeamHeightMm = beamHeightMm;
            BeamStressRule = new BeamStressRule()
            {
                Id = 0,
                Stress = new List<double> { 0.25, 0.5, 0.25 }
            };
        }
        public RebarBeam()
        {

        }
        private double GetBeamWidthMm(BoxElement revBoxBeam, out double beamHeightMm)
        {
            var result = 0.0;
            beamHeightMm = 0.0;
            try
            {
                var face = new FaceCustom(revBoxBeam.VTX, revBoxBeam.LineBox.Midpoint());
                var faceOXY = new FaceCustom(revBoxBeam.VTZ, revBoxBeam.LineBox.Midpoint());
                var faceOXZ = new FaceCustom(revBoxBeam.VTY, revBoxBeam.LineBox.Midpoint());
                var faceOYZ = new FaceCustom(revBoxBeam.VTX, revBoxBeam.LineBox.Midpoint());

                var p1OYZ = revBoxBeam.LineBox.GetEndPoint(0).RayPointToFace(revBoxBeam.VTX, faceOYZ);
                var p2OYZ = revBoxBeam.LineBox.GetEndPoint(1).RayPointToFace(revBoxBeam.VTX, faceOYZ);

                var p1OXZ = p1OYZ.RayPointToFace(revBoxBeam.VTY, faceOXZ);
                var p2OXZ = p2OYZ.RayPointToFace(revBoxBeam.VTY, faceOXZ);

                var p1OXY = p1OYZ.RayPointToFace(revBoxBeam.VTZ, faceOXY);
                var p2OXY = p2OYZ.RayPointToFace(revBoxBeam.VTZ, faceOXY);

                result = Math.Round(p1OXY.Distance(p2OXY).FootToMm(), 0);
                beamHeightMm = Math.Round(p1OXZ.Distance(p2OXZ).FootToMm(), 0);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to calculate section dimensions for beam {revBoxBeam?.Id}.", ex);
            }
            if (result <= 0 || beamHeightMm <= 0)
                throw new InvalidOperationException(
                    $"Beam {revBoxBeam?.Id} has invalid section dimensions ({result} x {beamHeightMm} mm).");
            return result;
        }
        public static void ResetActionChange(RebarBeam rebarBeam)
        {
            if (rebarBeam.RebarBeamSectionStart != null)
            {
                rebarBeam.RebarBeamSectionStart.RebarBeamTop.RebarBeamTopLevel1.QuantityChange = null;
                rebarBeam.RebarBeamSectionStart.RebarBeamTop.RebarBeamTopLevel2.QuantityChange = null;
                rebarBeam.RebarBeamSectionStart.RebarBeamTop.RebarBeamTopLevel3.QuantityChange = null;

                rebarBeam.RebarBeamSectionStart.RebarBeamBot.RebarBeamBotLevel1.QuantityChange = null;
                rebarBeam.RebarBeamSectionStart.RebarBeamBot.RebarBeamBotLevel2.QuantityChange = null;
                rebarBeam.RebarBeamSectionStart.RebarBeamBot.RebarBeamBotLevel3.QuantityChange = null;

                rebarBeam.RebarBeamSectionStart.RebarBeamSideBar.QuantitySideChange = null;
                rebarBeam.RebarBeamSectionStart.RebarBeamTop.RebarGroupTypeChange = null;
                rebarBeam.RebarBeamSectionStart.RebarBeamBot.RebarGroupTypeChange = null;
                rebarBeam.RebarBeamSectionStart.RebarBeamTop.RebarBeamTopLevel1.DiameterChange = null;
                rebarBeam.RebarBeamSectionStart.RebarBeamBot.RebarBeamBotLevel1.DiameterChange = null;

                rebarBeam.RebarBeamSectionStart.RebarBeamStirrup.SpacingChange = null;
                rebarBeam.RebarBeamSectionStart.RebarBeamStirrup.DiameterChange = null;
            }
            if (rebarBeam.RebarBeamSectionMid != null)
            {
                rebarBeam.RebarBeamSectionMid.RebarBeamTop.RebarBeamTopLevel1.QuantityChange = null;
                rebarBeam.RebarBeamSectionMid.RebarBeamTop.RebarBeamTopLevel2.QuantityChange = null;
                rebarBeam.RebarBeamSectionMid.RebarBeamTop.RebarBeamTopLevel3.QuantityChange = null;

                rebarBeam.RebarBeamSectionMid.RebarBeamBot.RebarBeamBotLevel1.QuantityChange = null;
                rebarBeam.RebarBeamSectionMid.RebarBeamBot.RebarBeamBotLevel2.QuantityChange = null;
                rebarBeam.RebarBeamSectionMid.RebarBeamBot.RebarBeamBotLevel3.QuantityChange = null;

                rebarBeam.RebarBeamSectionMid.RebarBeamSideBar.QuantitySideChange = null;
                rebarBeam.RebarBeamSectionMid.RebarBeamTop.RebarGroupTypeChange = null;
                rebarBeam.RebarBeamSectionMid.RebarBeamBot.RebarGroupTypeChange = null;
                rebarBeam.RebarBeamSectionMid.RebarBeamTop.RebarBeamTopLevel1.DiameterChange = null;
                rebarBeam.RebarBeamSectionMid.RebarBeamBot.RebarBeamBotLevel1.DiameterChange = null;

                rebarBeam.RebarBeamSectionMid.RebarBeamStirrup.SpacingChange = null;
                rebarBeam.RebarBeamSectionMid.RebarBeamStirrup.DiameterChange = null;
            }
            if (rebarBeam.RebarBeamSectionEnd != null)
            {
                rebarBeam.RebarBeamSectionEnd.RebarBeamTop.RebarBeamTopLevel1.QuantityChange = null;
                rebarBeam.RebarBeamSectionEnd.RebarBeamTop.RebarBeamTopLevel2.QuantityChange = null;
                rebarBeam.RebarBeamSectionEnd.RebarBeamTop.RebarBeamTopLevel3.QuantityChange = null;

                rebarBeam.RebarBeamSectionEnd.RebarBeamBot.RebarBeamBotLevel1.QuantityChange = null;
                rebarBeam.RebarBeamSectionEnd.RebarBeamBot.RebarBeamBotLevel2.QuantityChange = null;
                rebarBeam.RebarBeamSectionEnd.RebarBeamBot.RebarBeamBotLevel3.QuantityChange = null;

                rebarBeam.RebarBeamSectionEnd.RebarBeamSideBar.QuantitySideChange = null;
                rebarBeam.RebarBeamSectionEnd.RebarBeamTop.RebarGroupTypeChange = null;
                rebarBeam.RebarBeamSectionEnd.RebarBeamBot.RebarGroupTypeChange = null;
                rebarBeam.RebarBeamSectionEnd.RebarBeamTop.RebarBeamTopLevel1.DiameterChange = null;
                rebarBeam.RebarBeamSectionEnd.RebarBeamBot.RebarBeamBotLevel1.DiameterChange = null;

                rebarBeam.RebarBeamSectionEnd.RebarBeamStirrup.SpacingChange = null;
                rebarBeam.RebarBeamSectionEnd.RebarBeamStirrup.DiameterChange = null;
            }
        }
    }
    public class MainBarBeamReal
    {
        public int Id { get; set; }
        public long SourceBeamId { get; set; }
        public int Level { get; set; } //[Top, Bot]
        public int Group { get; set; } //[1, 2, 3]
        public bool StartHook { get; set; }
        public bool EndHook { get; set; }
        public List<XYZ> MainPoints { get; set; }
        public XYZ StartPoint { get; set; }
        public XYZ EndPoint { get; set; }
        public CurveLoop RebarShape { get; set; }
        public string Diameter { get; set; }
    }
}


