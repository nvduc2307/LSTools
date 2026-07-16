using Autodesk.Revit.DB;
using RIMT.Utils.BoundingBoxs;
using RIMT.Utils.RevRebars;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.models.MainStirrups
{
    public class MainStirrupCollectionDto
    {
        public RebarBarTypeCustom RebarBarTypeCustom { get; set; }
        public BoxElementPoint BoxElementPoint { get; set; }
        public double Spacing { get; set; }
        public MainStirrupShapeEnum Shape { get; set; }
        public Element Host { get; set; }
        public XYZ Direction { get; set; }
        public CoverFootBeam CoverFootBeam { get; set; }
        public Document Document { get; set; }
        public MainStirrupCollectionDto Copy()
        {
            return (MainStirrupCollectionDto)MemberwiseClone();
        }
    }

    public enum MainStirrupShapeEnum
    {
        Shape1,
        Shape2,
        Shape3,
        Shape4
    }
}


