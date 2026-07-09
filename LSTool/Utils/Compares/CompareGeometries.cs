namespace LSTool.Utils
{
    public class CompareGeometries
    {
    }
    public class ComparePoint : IEqualityComparer<XYZ>
    {
        public bool Equals(XYZ x, XYZ y)
        {
            return x.IsSame(y);
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
