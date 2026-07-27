using Autodesk.Revit.DB;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.models.MainStirrups
{
    public class RectangleDto
    {
        public XYZ TopLeft { get; set; }
        public XYZ TopRight { get; set; }
        public XYZ BottomLeft { get; set; }
        public XYZ BottomRight { get; set; }
        public Transform Transform { get; set; }
    }
}


