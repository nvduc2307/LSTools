using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using HcBimUtils;

namespace RIMT.CreateRebarAssemblies.model
{
    public sealed class AssemblyInfoUtils
    {
        public string TypeName { get; }
        public string GridName { get; }

        public AssemblyInfoUtils(IEnumerable<Element> elements, Document document)
        {
            var elementList = ExpandElements(elements, document);
            TypeName = string.Join("-", elementList.Select(element => element.Name).Distinct());
            GridName = GetGridName(elementList, document);
        }

        private static List<Element> ExpandElements(IEnumerable<Element> elements, Document document)
        {
            var results = new List<Element>();
            foreach (var element in elements?.Where(element => element != null) ?? Enumerable.Empty<Element>())
            {
                if (element is AssemblyInstance assembly)
                    results.AddRange(assembly.GetMemberIds().Select(document.GetElement).Where(member => member != null));
                else
                    results.Add(element);
            }
            return results;
        }

        private static string GetGridName(IReadOnlyCollection<Element> elements, Document document)
        {
            try
            {
                var grids = new FilteredElementCollector(document)
                    .OfClass(typeof(Grid))
                    .Cast<Grid>()
                    .ToList();
                var directionGroups = GroupParallelGrids(grids);
                var elementGridNames = elements
                    .Select(element => GetElementGridName(element, directionGroups))
                    .Where(name => !string.IsNullOrWhiteSpace(name));
                return string.Join(",", elementGridNames);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static List<List<Grid>> GroupParallelGrids(IEnumerable<Grid> grids)
        {
            var groups = new List<List<Grid>>();
            foreach (var grid in grids.OrderBy(grid => grid.Name))
            {
                var direction = GetGridDirection(grid);
                var group = groups.FirstOrDefault(candidate =>
                    Math.Abs(Math.Abs(GetGridDirection(candidate[0]).DotProduct(direction)) - 1) < 1e-6);
                if (group == null)
                    groups.Add(new List<Grid> { grid });
                else
                    group.Add(grid);
            }
            return groups;
        }

        private static string GetElementGridName(Element element, IEnumerable<List<Grid>> gridGroups)
        {
            if (element.Category?.Id.Value == (long)BuiltInCategory.OST_StructuralColumns)
            {
                var locationMark = element.get_Parameter(BuiltInParameter.COLUMN_LOCATION_MARK)?.AsValueString();
                if (!string.IsNullOrWhiteSpace(locationMark)) return locationMark;
            }

            var points = GetReferencePoints(element);
            if (points == null) return string.Empty;
            var groupNames = new List<string>();
            foreach (var group in gridGroups)
            {
                var startGrid = FindNearestGrid(group, points.Item1);
                var endGrid = FindNearestGrid(group, points.Item2);
                var names = new[] { startGrid?.Name, endGrid?.Name }
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();
                if (names.Count > 0) groupNames.Add(string.Join("-", names));
            }
            return string.Join("_", groupNames);
        }

        private static Tuple<XYZ, XYZ> GetReferencePoints(Element element)
        {
            if (element.Location is LocationCurve locationCurve)
                return Tuple.Create(locationCurve.Curve.GetEndPoint(0), locationCurve.Curve.GetEndPoint(1));
            if (element.Location is LocationPoint locationPoint)
                return Tuple.Create(locationPoint.Point, locationPoint.Point);
            var box = element.get_BoundingBox(null);
            return box == null ? null : Tuple.Create(box.Min, box.Max);
        }

        private static Grid FindNearestGrid(IEnumerable<Grid> grids, XYZ point)
            => grids
                .Select(grid => new { Grid = grid, Projection = grid.Curve.Project(point) })
                .Where(item => item.Projection != null)
                .OrderBy(item => item.Projection.Distance)
                .Select(item => item.Grid)
                .FirstOrDefault();

        private static XYZ GetGridDirection(Grid grid)
        {
            var direction = grid.Curve.ComputeDerivatives(0.5, true).BasisX;
            return direction.GetLength() < 1e-9 ? XYZ.BasisX : direction.Normalize();
        }
    }
}

namespace RIMT.Utils.SelectFilters
{
    public sealed class GenericSelectionFilterFromCategory : ISelectionFilter
    {
        private readonly BuiltInCategory _category;

        public GenericSelectionFilterFromCategory(BuiltInCategory category)
        {
            _category = category;
        }

        public bool AllowElement(Element element)
        {
            if (element == null) return false;
            return IsRequestedCategory(element);
        }

        private bool IsRequestedCategory(Element element)
            => element?.Category != null && element.Category.Id.Value == (long)_category;

        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}
