using Autodesk.Revit.DB;
using LSTool.Tools.Beams.InstallRebarBeamV2.models.MainStirrups;
using RIMT.Utils.BoundingBoxs;
using RIMT.Utils.RevRebars;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.models.SecondaryStirrups
{
    public class SubHorizontalStirrupCollectionDto
    {
        public RebarBarTypeCustom RebarBarTypeCustom { get; set; }
        public BoxElementPoint BoxElementPoint { get; set; }
        public double Spacing { get; set; }
        public MainStirrupShapeEnum Shape { get; set; }
        public Element Host { get; set; }
        public XYZ Direction { get; set; }
        public CoverFootBeam CoverFootBeam { get; set; }
        public XYZ Left { get; set; }
        public XYZ Right { get; set; }
        public Document Document { get; set; }
        /// <summary>
        /// Sử dụng để biết hướng hook lên trên hay xuống dưới cho thép đai
        /// </summary>
        public XYZ DirectionInside { get; set; }
    }
}


