using Autodesk.Revit.DB;

namespace RIMT.Utils.FilterElementsInRevit
{
    public static class FilterElementExtensions
    {
        public static List<T> GetElementsFromClass<T>(this Document document, bool includeElementTypes = true)
            where T : Element
        {
            var collector = new FilteredElementCollector(document).OfClass(typeof(T));
            if (!includeElementTypes) collector.WhereElementIsNotElementType();
            return collector.Cast<T>().ToList();
        }
    }
}

namespace RIMT.Utils.RevitElements
{
    public static class RevitElementExtensions
    {
        public static Element CreateHost(this Document document, BuiltInCategory category)
            => DirectShape.CreateElement(document, new ElementId(category));
    }
}

namespace RIMT.Utils.Revit
{
    public static class CurveIntersectionExtensions
    {
        public static List<Curve> GetInsideCurvesIntersectSolid(this Curve curve, Solid solid)
        {
            var intersection = solid.IntersectWithCurve(curve, new SolidCurveIntersectionOptions
            {
                ResultType = SolidCurveIntersectionMode.CurveSegmentsInside
            });
            var results = new List<Curve>();
            for (var index = 0; index < intersection.SegmentCount; index++)
                results.Add(intersection.GetCurveSegment(index));
            return results;
        }
    }
}
