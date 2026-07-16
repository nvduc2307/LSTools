using Autodesk.Revit.DB;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.models.SecondaryStirrups
{
    public class LineDto
    {
        public XYZ Top { get; set; }
        public XYZ Bottom { get; set; }
        public Transform Transform { get; set; }
        public XYZ DirectionToInside { get; set; }
    }
}


