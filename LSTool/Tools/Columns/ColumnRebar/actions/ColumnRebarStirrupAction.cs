using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using LSTool.Tools.Columns.ColumnRebar.models;
using LSTool.Utils;

namespace LSTool.Tools.Columns.ColumnRebar.actions
{
    public class ColumnRebarStirrupAction
    {
        private UIDocument _uidocument;
        private Document _document;
        private List<RebarBarType> _rebarBarTypes;
        private Element _host;
        public ColumnRebarStirrupAction(
            UIDocument uidocument,
            Element host)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            _host = host;
            _rebarBarTypes = new FilteredElementCollector(_document)
                .WhereElementIsElementType()
                .OfClass(typeof(RebarBarType))
                .Cast<RebarBarType>()
                .Where(x => x.Name.Contains("D"))
                .OrderBy(x => x.Name)
                .ToList();
        }
        public void CreateStirrupMain(List<ColumnConcreteModel> ccRInfos)
        {
            using (var ts = new SubTransaction(_document))
            {
                ts.Start();
                var count = ccRInfos.Count;
                foreach (var ccRInfo in ccRInfos)
                {
                    var index = ccRInfos.IndexOf(ccRInfo);
                    InstallStirrupMain(ccRInfo, index == 0 || index == count - 1 ? false : true);
                }
                ts.Commit();
            }
        }
        private void InstallStirrupMain(ColumnConcreteModel ccRInfo, bool hasBeamZone)
        {
            try
            {
                var diamterSt = ccRInfo.DiameterST.FindInterger();
                if (diamterSt == 0) return;
                var cover = (ccRInfo.Cover + diamterSt / 2).FromMillimeters();
                var start = ccRInfo.Center - ccRInfo.VTZ * ccRInfo.Length.FromMillimeters() / 2;
                var end = ccRInfo.Center
                    + ccRInfo.VTZ * ccRInfo.Length.FromMillimeters() / 2
                    - ccRInfo.VTZ * (hasBeamZone ? ccRInfo.HeightBeamZone.FromMillimeters() : 0);
                var length = start.DistanceTo(end);
                var stressZone = 0.25;

                var start_zone1 = start;
                var End_zone1 = start + ccRInfo.VTZ * length * stressZone;

                var start_zone2 = start + ccRInfo.VTZ * length * stressZone;
                var End_zone2 = end - ccRInfo.VTZ * length * stressZone;

                var start_zone3 = end - ccRInfo.VTZ * length * stressZone;
                var End_zone3 = end;

                var ps = new List<XYZ>()
                    {
                        ccRInfo.FaceLeft.Pb1,
                        ccRInfo.FaceTop.Pb1,
                        ccRInfo.FaceRight.Pb1,
                        ccRInfo.FaceBottom.Pb1,
                    };
                var baseShapes = CurveLoop.CreateViaOffset(ps
                        .PointsToCurveLoop(), cover, -ccRInfo.VTZ)
                        .Select(x => x.GetEndPoint(1))
                        .ToList();
                var shapes_Start = _installStirrup(start_zone1, End_zone1, baseShapes, ccRInfo.SpacingSTE, 50, ccRInfo.SpacingSTE / 2);
                var shapes_Mid = _installStirrup(start_zone2, End_zone2, baseShapes, ccRInfo.SpacingST, ccRInfo.SpacingST / 2, ccRInfo.SpacingST / 2);
                var shapes_End = _installStirrup(start_zone3, End_zone3, baseShapes, ccRInfo.SpacingSTE, 50, ccRInfo.SpacingSTE / 2);

                var rebarHookTypes = new FilteredElementCollector(_document)
                    .WhereElementIsElementType()
                    .OfClass(typeof(RebarHookType))
                    .Cast<RebarHookType>()
                    .ToList();

                if (!rebarHookTypes.Any())
                    throw new Exception("Hook Type is null");
                var hook135 = rebarHookTypes.FirstOrDefault(x=>Math.Abs(x.HookAngle.ToDegrees() - 135) <= 1);
                if (hook135 == null)
                    throw new Exception("Hook 135 is null");
                foreach (var item in shapes_Start)
                {
                    RebarHelper.CreateRebarStirrupTie(
                        _document,
                        item, ccRInfo.DiameterST, XYZ.BasisZ, hook135, hook135, _rebarBarTypes, _host);
                }
                foreach (var item in shapes_Mid)
                {
                    RebarHelper.CreateRebarStirrupTie(
                        _document,
                        item, ccRInfo.DiameterST, XYZ.BasisZ, hook135, hook135, _rebarBarTypes, _host);
                }
                foreach (var item in shapes_End)
                {
                    RebarHelper.CreateRebarStirrupTie(
                        _document,
                        item, ccRInfo.DiameterST, XYZ.BasisZ, hook135, hook135, _rebarBarTypes, _host);
                }
            }
            catch (Exception)
            {
            }
        }
        private List<List<Curve>> _installStirrup(
            XYZ start,
            XYZ end,
            List<XYZ> baseShapes,
            double spacingMm,
            double extendS,
            double extendE)
        {
            var result = new List<List<Curve>>();
            try
            {
                var vt = (end - start).Normalize();
                var distance = start.DistanceTo(end).ToMillimeters() - (extendS + extendE);
                var duSpacing = distance % spacingMm;
                var qty = 1 + (distance - duSpacing) / spacingMm;
                var baseS = start + vt * extendS.FromMillimeters();
                var baseE = end - vt * extendE.FromMillimeters();
                var f = Plane.CreateByNormalAndOrigin(vt, baseS);
                baseShapes = baseShapes
                    .Select(x => x.RayIntersectPlane(f.Normal, f))
                    .ToList();
                for (int i = 0; i < qty; i++)
                {
                    var shapes = baseShapes
                        .Select(x => x + i * vt * spacingMm.FromMillimeters())
                        .ToList();
                    result.Add(shapes.PointsToCurves(true));
                    if (i != qty - 1) continue;
                    if (duSpacing < 0.3 * spacingMm) continue;
                    var shapesDu = shapes
                        .Select(x => x + vt * duSpacing.FromMillimeters())
                        .ToList();
                    result.Add(shapesDu.PointsToCurves(true));
                }
            }
            catch (Exception)
            {
                result = new List<List<Curve>>();
            }
            return result;
        }
    }
}
