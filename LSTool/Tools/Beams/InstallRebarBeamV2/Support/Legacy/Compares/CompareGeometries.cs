using Autodesk.Revit.DB;
using HcBimUtils;
using RIMT.Utils.Geometries;

namespace RIMT.Utils.Compares
{
    public class CompareGeometries
    {
    }
    public class ComparePoint : IEqualityComparer<XYZ>
    {
        private double _toolean;
        public ComparePoint(double toolean = 1)
        {
            _toolean = toolean;
        }
        public bool Equals(XYZ x, XYZ y)
        {
            return x.IsSame(y, _toolean);
        }

        public int GetHashCode(XYZ obj)
        {
            return 0;
        }
    }
    public class CompareCurveHasSeemDirection : IEqualityComparer<Curve>
    {
        public bool Equals(Curve x, Curve y)
        {
            var dir1 = x.Direction();
            var dir2 = y.Direction();
            return dir1.IsSame(dir2);
        }

        public int GetHashCode(Curve obj)
        {
            return 0;
        }
    }
}
