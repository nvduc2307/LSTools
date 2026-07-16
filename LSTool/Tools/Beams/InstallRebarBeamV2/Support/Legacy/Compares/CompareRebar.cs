using Autodesk.Revit.DB.Structure;
using HcBimUtils;
using RIMT.Utils.Geometries;
using RIMT.Utils.RevPoints;
using RIMT.Utils.RevRebars;

namespace RIMT.Utils.Compares
{
    public sealed class CompareRebarFoLowFace : IEqualityComparer<Rebar>
    {
        private readonly FaceCustom _elevationFace;
        private readonly FaceCustom _planFace;

        public CompareRebarFoLowFace(FaceCustom elevationFace, FaceCustom planFace)
        {
            _elevationFace = elevationFace;
            _planFace = planFace;
        }

        public bool Equals(Rebar x, Rebar y)
        {
            try
            {
                var centerX = x.GetLinesOrigin()
                    .SelectMany(curve => new[] { curve.GetEndPoint(0), curve.GetEndPoint(1) })
                    .ToList()
                    .CenterPoint();
                var centerY = y.GetLinesOrigin()
                    .SelectMany(curve => new[] { curve.GetEndPoint(0), curve.GetEndPoint(1) })
                    .ToList()
                    .CenterPoint();
                var pointX = centerX
                    .RayPointToFace(_elevationFace.Normal, _elevationFace)
                    .RayPointToFace(_planFace.Normal, _planFace);
                var pointY = centerY
                    .RayPointToFace(_elevationFace.Normal, _elevationFace)
                    .RayPointToFace(_planFace.Normal, _planFace);
                return pointX.IsSame(pointY, 30);
            }
            catch
            {
                return false;
            }
        }

        public int GetHashCode(Rebar obj) => 0;
    }
}
