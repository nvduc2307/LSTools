using Autodesk.Revit.DB.Structure;
using LSTool.Compatibility;
using LSTool.MVVM.model.Structures;
using LSTool.Tools.Generals.ManageConcreteCover.action;

namespace LSTool.Tools.Columns.CreateColumn.model
{
    public partial class ColumnConcreteModel
    {
        public Element FindFoundationBelow(Document document)
        {
            if (Center == null) return null;
            try
            {
                var host = document.GetElement(Id);
                if (host == null) return null;

                var bb = host.get_BoundingBox(null);
                if (bb == null) return null;

                var searchDepth = (10.0 * 1000.0).FromMillimeters();
                var tolerance   = 50.0.FromMillimeters();

                var min = new XYZ(bb.Min.X - tolerance, bb.Min.Y - tolerance, bb.Min.Z - searchDepth);
                var max = new XYZ(bb.Max.X + tolerance, bb.Max.Y + tolerance, bb.Min.Z + tolerance);

                var bbFilter = new BoundingBoxIntersectsFilter(new Outline(min, max));

                var foundations = new FilteredElementCollector(document)
                    .WhereElementIsNotElementType()
                    .WherePasses(bbFilter)
                    .OfCategory(BuiltInCategory.OST_StructuralFoundation)
                    .ToList();

                if (!foundations.Any()) return null;

                return foundations
                    .OrderByDescending(f =>
                    {
                        var fBb = f.get_BoundingBox(null);
                        return fBb?.Max.Z ?? double.MinValue;
                    })
                    .FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public double GetAnchorFoundationFt(
            Document document,
            double diameterMm,
            RebarAnchorageLengthModel anchorLengthModel)
        {
            var diameterFt = diameterMm.FromMillimeters();

            var theoreticalFt = anchorLengthModel != null
                ? (anchorLengthModel.Ldt * diameterMm).FromMillimeters()
                : 30.0 * diameterFt;

            var foundation = FindFoundationBelow(document);
            if (foundation == null)
                return theoreticalFt;

            var colElement = document.GetElement(Id);
            var colBb      = colElement?.get_BoundingBox(null);
            if (colBb == null) return theoreticalFt;

            var columnBaseZ     = colBb.Min.Z;
            var foundBb         = foundation.get_BoundingBox(null);
            if (foundBb == null) return theoreticalFt;

            var foundationBaseZ = foundBb.Min.Z;

            var coverAction        = new ManageConcreteCoverAction(document);
            var foundationCoverMm  = coverAction.GetFoundationCover()?.CoverValue ?? 50.0;
            var foundationCoverFt  = foundationCoverMm.FromMillimeters();

            var availableDepthFt = columnBaseZ - foundationBaseZ - 2.0 * foundationCoverFt;
            if (availableDepthFt <= 0) return theoreticalFt;

            return availableDepthFt >= theoreticalFt
                ? theoreticalFt
                : availableDepthFt;
        }
    }
}
