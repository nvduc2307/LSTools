using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LSTool.Compatibility
{
    public abstract class ViewModelBase : ObservableObject
    {
    }

    public static class AC
    {
        public static UIDocument UiDoc { get; private set; } = null!;

        public static Document Document { get; private set; } = null!;

        public static void GetInformation(UIDocument uiDocument)
        {
            if (uiDocument == null)
                throw new ArgumentNullException(nameof(uiDocument));

            UiDoc = uiDocument;
            Document = uiDocument.Document;
        }
    }

    public sealed class BPlane
    {
        private BPlane(XYZ normal, XYZ origin)
        {
            Normal = normal.Normalize();
            Origin = origin;
        }

        public XYZ Normal { get; }

        public XYZ Origin { get; }

        public static BPlane CreateByNormalAndOrigin(XYZ normal, XYZ origin)
        {
            if (normal == null)
                throw new ArgumentNullException(nameof(normal));
            if (origin == null)
                throw new ArgumentNullException(nameof(origin));

            return new BPlane(normal, origin);
        }
    }

    public static class LegacyRevitCompatibility
    {
        private const double MillimetersPerFoot = 304.79999999999995;
        private const double DefaultTolerance = 1e-9;

        public static double MmToFoot(this double value)
        {
            return value / MillimetersPerFoot;
        }

        public static double MmToFoot(this int value)
        {
            return value / MillimetersPerFoot;
        }

        public static double FootToMm(this double value)
        {
            return value * MillimetersPerFoot;
        }

        public static bool IsEqual(this double value, double other, double tolerance = DefaultTolerance)
        {
            return Math.Abs(other - value) < tolerance;
        }

        public static bool IsGreater(this double value, double other, double tolerance = DefaultTolerance)
        {
            return value > other + tolerance;
        }

        public static bool IsGreaterEqual(this double value, double other, double tolerance = DefaultTolerance)
        {
            return Math.Abs(other - value) < tolerance || value > other + tolerance;
        }

        public static bool IsSmallerEqual(this double value, double other, double tolerance = DefaultTolerance)
        {
            return value + tolerance < other || Math.Abs(other - value) < tolerance;
        }

        public static XYZ Direction(this Curve curve)
        {
            return (curve.GetEndPoint(1) - curve.GetEndPoint(0)).Normalize();
        }

        public static XYZ Midpoint(this Curve curve)
        {
            var startParameter = curve.GetEndParameter(0);
            var midpointParameter = startParameter
                + (curve.GetEndParameter(1) - startParameter) * 0.5;
            return curve.Evaluate(midpointParameter, false);
        }

        public static XYZ Midpoint(this XYZ point, XYZ other)
        {
            return 0.5 * (point + other);
        }

        public static Line CreateLine(this XYZ startPoint, XYZ endPoint)
        {
            return Line.CreateBound(startPoint, endPoint);
        }

        public static XYZ EditZ(this XYZ point, double z)
        {
            return new XYZ(point.X, point.Y, z);
        }

        public static XYZ SP(this Curve curve)
        {
            try
            {
                return curve.Tessellate()[0];
            }
            catch
            {
                return curve.GetEndPoint(0);
            }
        }

        public static XYZ EP(this Curve curve)
        {
            try
            {
                var points = curve.Tessellate();
                return points[points.Count - 1];
            }
            catch
            {
                return curve.GetEndPoint(1);
            }
        }

        public static XYZ ProjectOnto(this XYZ point, BPlane plane)
        {
            var vector = point - plane.Origin;
            var distance = vector.DotProduct(plane.Normal);
            return point - distance * plane.Normal;
        }

        public static Line ProjectOntoPlane(this Curve curve, BPlane plane)
        {
            return Line.CreateBound(curve.SP().ProjectOnto(plane), curve.EP().ProjectOnto(plane));
        }

        public static bool IsParallel(this XYZ vector, XYZ other)
        {
            return vector.CrossProduct(other).GetLength() < 0.01;
        }

        public static bool IsPerpendicular(this XYZ vector, XYZ other)
        {
            if (vector.GetLength() <= DefaultTolerance || other.GetLength() <= DefaultTolerance)
                return false;

            return Math.Abs(vector.DotProduct(other)) < DefaultTolerance;
        }

        public static bool IsSameDirection(this XYZ vector, XYZ other)
        {
            return vector.IsParallel(other) && vector.DotProduct(other) > 0;
        }

        public static bool IsPointInsideLine(this XYZ point, Line line, double tolerance)
        {
            var startPoint = line.GetEndPoint(0);
            var endPoint = line.GetEndPoint(1);
            var fromStart = point - startPoint;

            if (fromStart.IsAlmostEqualTo(XYZ.Zero, 0.001))
                return true;

            var lineVector = endPoint - startPoint;
            if (fromStart.CrossProduct(lineVector).GetLength() >= 0.001)
                return false;

            var toEnd = point - endPoint;
            return (fromStart.GetLength() + toEnd.GetLength())
                .IsEqual(lineVector.GetLength(), tolerance);
        }

        public static List<Face> GetFacesFromSolid(this Solid solid)
        {
            return LSTool.Utils.SolidHelper.GetFacesFromSolid(solid);
        }

        public static CurveLoop GetFirstCurveLoop(this Face face)
        {
            return LSTool.Utils.SolidHelper.GetFirstCurveLoop(face);
        }

        public static List<XYZ> GetPoints(this Face face)
        {
            return LSTool.Utils.SolidHelper.GetPoints(face);
        }

        public static List<Solid> GetSolids(this Element element)
        {
            return LSTool.Utils.SolidHelper.GetSolids(element);
        }

        public static Solid GetSingleSolid(this Element element)
        {
            return LSTool.Utils.SolidHelper.GetSingleSolid(element);
        }

        public static Solid SolidFromBoundingbox(this BoundingBoxXYZ boundingBox)
        {
            var min = boundingBox.Min;
            var max = boundingBox.Max;
            var corner2 = new XYZ(min.X, max.Y, min.Z);
            var corner3 = new XYZ(max.X, max.Y, min.Z);
            var corner4 = new XYZ(max.X, min.Y, min.Z);
            var curveLoop = new CurveLoop();
            curveLoop.Append(Line.CreateBound(min, corner2));
            curveLoop.Append(Line.CreateBound(corner2, corner3));
            curveLoop.Append(Line.CreateBound(corner3, corner4));
            curveLoop.Append(Line.CreateBound(corner4, min));

            return GeometryCreationUtilities.CreateExtrusionGeometry(
                new List<CurveLoop> { curveLoop },
                XYZ.BasisZ,
                max.Z - min.Z);
        }

        public static BuiltInCategory ToBuiltinCategory(this Category category)
        {
            return LSTool.Utils.CategoryHelper.ToBuiltinCategory(category);
        }

        public static void RebarScaleToBox(this Rebar rebar, XYZ origin, XYZ xVector, XYZ yVector)
        {
            rebar.GetShapeDrivenAccessor().ScaleToBox(origin, xVector, yVector);
        }

        public static XYZ RebarNormal(this Rebar rebar)
        {
            try
            {
                return rebar.GetShapeDrivenAccessor().Normal;
            }
            catch
            {
                var curve = rebar
                    .GetFreeFormAccessor()
                    .GetCustomDistributionPath()
                    .First();
                return (curve.GetEndPoint(1) - curve.GetEndPoint(0)).Normalize();
            }
        }
    }
}
