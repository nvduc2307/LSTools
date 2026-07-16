using Autodesk.Revit.DB;
using HcBimUtils;

namespace RIMT.Utils.Geometries
{
    public static class GeometryCustom
    {
        public static double Distance(this XYZ point, Line line)
        {
            var projection = line.Project(point);
            return projection == null ? 0 : point.DistanceTo(projection.XYZPoint);
        }

        public static double Distance(this XYZ vector)
            => Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z);

        public static double Distance(this XYZ first, XYZ second)
            => first.DistanceTo(second);

        public static double Distance(this XYZ point, FaceCustom face)
        {
            var normal = face.Normal.Normalize();
            return Math.Abs((point - face.BasePoint).DotProduct(normal));
        }

        public static XYZ VectorNormal(this XYZ vector)
        {
            var length = vector.Distance();
            return length < 1e-9 ? XYZ.Zero : vector / length;
        }

        public static XYZ MidPoint(this XYZ first, XYZ second) => (first + second) / 2;

        public static XYZ RayPointToFace(this XYZ point, XYZ rayDirection, FaceCustom face)
        {
            try
            {
                var denominator = face.Normal.DotProduct(rayDirection);
                if (Math.Abs(denominator) < 1e-9) return point;
                var distance = face.Normal.DotProduct(face.BasePoint - point) / denominator;
                return point + rayDirection * distance;
            }
            catch
            {
                return point;
            }
        }

        public static XYZ LineIntersectFace(this Line line, FaceCustom face)
            => line.GetEndPoint(0).RayPointToFace(line.Direction, face);

        public static bool IsSame(this XYZ first, XYZ second, double toleranceMm = 1)
            => first != null && second != null && first.DistanceTo(second).FootToMm() <= toleranceMm;

        public static LineCustom FaceIntersectFace(this FaceCustom first, FaceCustom second)
        {
            try
            {
                var direction = first.Normal.CrossProduct(second.Normal);
                if (direction.GetLength() < 1e-9) return null;
                var rayDirection = direction.CrossProduct(first.Normal);
                var basePoint = first.BasePoint.RayPointToFace(rayDirection, second);
                return new LineCustom(direction.Normalize(), basePoint);
            }
            catch
            {
                return null;
            }
        }
    }

    public sealed class LineCustom
    {
        public XYZ Direction { get; }
        public XYZ BasePoint { get; }
        public Line LineBase { get; }

        public LineCustom(XYZ direction, XYZ basePoint)
        {
            Direction = direction;
            BasePoint = basePoint;
            LineBase = Line.CreateBound(basePoint, basePoint + direction * 1000.MmToFoot());
        }
    }
}
