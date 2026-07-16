using Autodesk.Revit.DB;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.models.SecondaryStirrups
{
    public class LineHorizontalDto
    {
        public XYZ Left { get; set; }
        public XYZ Right { get; set; }
        public Transform Transform { get; set; }
        public XYZ DirectionToInside { get; set; }
    }
}


