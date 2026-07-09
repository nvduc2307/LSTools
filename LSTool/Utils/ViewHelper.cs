using Autodesk.Revit.UI;

namespace LSTool.Utils
{
    public static class ViewHelper
    {
        public static bool IsSolidFullyInViewRange(Document doc, ViewPlan view, BoundingBoxXYZ bbox, double tol = 1e-6)
        {
            if (bbox == null) return false;

            var vr = view.GetViewRange();
            double zTop = GetPlaneZ(doc, vr, PlanViewPlane.TopClipPlane);
            double zBottom = GetPlaneZ(doc, vr, PlanViewPlane.BottomClipPlane);

            return bbox.Min.Z >= zBottom - tol &&
                   bbox.Max.Z <= zTop + tol;
        }

        public static double GetPlaneZ(Document doc, PlanViewRange vr, PlanViewPlane plane)
        {
            if (vr is null) return 0.0;

            ElementId levelId = vr.GetLevelId(plane);
            double offset = vr.GetOffset(plane);

            if (levelId == PlanViewRange.Unlimited ||
                levelId == ElementId.InvalidElementId ||
                int.Parse(levelId.ToString()) < 0)
            {
                return plane == PlanViewPlane.ViewDepthPlane
                    ? double.NegativeInfinity
                    : double.PositiveInfinity;
            }

            Level level = doc.GetElement(levelId) as Level;
            double elev = (level?.Elevation) ?? 0.0;
            return elev + offset;
        }

        public static List<View3D> GetAllView3D(Document doc)
        {
            return new FilteredElementCollector(doc).OfClass(typeof(View3D)).Cast<View3D>().Where(x => x.IsTemplate == false).ToList();
        }

        public static View3D CreateView3D(Document doc, string viewName)
        {
            View3D return3DView = null;
            var direction = new XYZ(-1, 1, -1);
            var collector = new FilteredElementCollector(doc);

            if (IsExistView3D(doc, viewName)) return null;

            var viewFamilyType = collector.OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
              .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);

            return3DView = View3D.CreateIsometric(doc, viewFamilyType.Id);

            return3DView.SetOrientation(new ViewOrientation3D(direction, new XYZ(0, 1, 1), new XYZ(0, 1, -1)));

            return3DView.Name = viewName;


            return return3DView;
        }

        public static UIView GetActiveUIView(UIDocument uiDocument)
        {
            var activeView = uiDocument.Document.ActiveView;
            if (activeView is null) return null;

            return uiDocument.GetOpenUIViews().FirstOrDefault(uiView => uiView.ViewId.Equals(activeView.Id));
        }

        public static View3D GetView3D(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(View3D))
                .Cast<View3D>()
                .Where(x => x.IsTemplate == false)
                .ToList()
                .FirstOrDefault();
        }

        public static bool IsExistView3D(Document doc, string viewName)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(View3D))
                .Cast<View3D>()
                .Any(x => x.Name == viewName);
        }
    }
}
