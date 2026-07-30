using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using HcBimUtils;
using LSTool.Tools.Columns.ColumnRebar.models;
using LSTool.Tools.Generals.SettingRebarStandard.models;
using LSTool.Utils;
using Newtonsoft.Json;
using System.Windows.Shapes;

namespace LSTool.Tools.Columns.ColumnRebar.actions
{
    public class ColumnRebarStirrupAction
    {
        private UIDocument _uidocument;
        private Document _document;
        private List<RebarBarType> _rebarBarTypes;
        private Element _host;
        private double _stressZone;
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
        public void CreateStirrupMain(
            List<ColumnConcreteModel> ccRInfos,
            SettingRebarStandardModelUI standard)
        {
            _stressZone = standard.LC;
            using (var ts = new SubTransaction(_document))
            {
                ts.Start();
                foreach (var columnStack in ColumnRebarStackGrouping.Group(ccRInfos))
                {
                    for (var index = 0; index < columnStack.Count; index++)
                    {
                        var hasBeamZone = index > 0 && index < columnStack.Count - 1;
                        InstallStirrupMain(columnStack[index], hasBeamZone);
                    }
                }
                ts.Commit();
            }
        }
        public void CreateStirrupSub(List<ColumnConcreteModel> ccRInfos)
        {
            using (var ts = new SubTransaction(_document))
            {
                ts.Start();
                foreach (var columnStack in ColumnRebarStackGrouping.Group(ccRInfos))
                {
                    for (var index = 0; index < columnStack.Count; index++)
                    {
                        var hasBeamZone = index > 0 && index < columnStack.Count - 1;
                        InstallStirrupSub(columnStack[index], hasBeamZone);
                    }
                }
                ts.Commit();
            }
        }
        public void SaveSettingColumnStirrupPosition(
            List<ColumnConcreteModel> ccRInfos,
            ColumnStirrupPositionSchema columnStirrupPositionSchema)
        {
            foreach (var col in ccRInfos)
            {
                if (!col.Ties.Any()) continue;
                var content = JsonConvert.SerializeObject(col.Ties);
                var ele = _document.GetElement(col.Id);
                columnStirrupPositionSchema.Write(ele, content);
            }
        }
        public void GetSettingColumnStirrupPosition(
            List<ColumnConcreteModel> cols,
            ColumnStirrupPositionSchema columnStirrupPositionSchema)
        {
            var result = new List<List<ColumnStirrupPosition>>();
            foreach (var col in cols)
            {
                var ele = _document.GetElement(col.Id);
                var content = columnStirrupPositionSchema.Read(ele);
                if (string.IsNullOrEmpty(content)) continue;
                var objs = JsonConvert.DeserializeObject<List<List<ColumnStirrupPosition>>>(content);
                if (objs == null) continue;
                col.Ties = objs;
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
                var stressZone = _stressZone;

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
                var p1 = baseShapes[0];
                var p2 = baseShapes[1];
                var p3 = baseShapes[2];
                var p4 = baseShapes[3];
                var vtStart = (p2 - p1).Normalize();
                var vtEnd = (p1 - p4).Normalize();
                baseShapes = new List<XYZ>()
                {
                    p1 - vtStart * diamterSt.MmToFoot() / 2,
                    p2,
                    p3,
                    p4,
                    p1 + vtEnd * diamterSt.MmToFoot() / 2
                };
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
                var hook135 = rebarHookTypes.FirstOrDefault(x => Math.Abs(x.HookAngle.ToDegrees() - 135) <= 1);
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
        private void InstallStirrupSub(ColumnConcreteModel ccRInfo, bool hasBeamZone)
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
                var stressZone = _stressZone;

                var start_zone1 = start;
                var End_zone1 = start + ccRInfo.VTZ * length * stressZone;

                var start_zone2 = start + ccRInfo.VTZ * length * stressZone;
                var End_zone2 = end - ccRInfo.VTZ * length * stressZone;

                var start_zone3 = end - ccRInfo.VTZ * length * stressZone;
                var End_zone3 = end;
                if (ccRInfo.RebarMainPositionss == null) return;
                if (!ccRInfo.RebarMainPositionss.Any()) return;
                var rebarPos = ccRInfo.RebarMainPositionss.Aggregate((a, b) => a.Concat(b).ToList()).ToList();

                foreach (var tie in ccRInfo.Ties)
                {
                    var shape = new List<XYZ>();
                    var posTargets = new List<ColumnRebarPositionModel>();
                    foreach (var item in tie)
                    {
                        var posTarget = rebarPos.FirstOrDefault(x => x.Index == item.Index && x.Face == item.Face);
                        if (posTarget == null) continue;
                        if (posTargets.Count >= 4) continue;
                        posTargets.Add(posTarget);
                    }
                    var cposTargets = posTargets.Count;
                    if (cposTargets != 2 && cposTargets != 4) continue;
                    if (cposTargets == 2)
                    {
                        foreach (var posTarget in posTargets)
                        {
                            var f = _GetFace(posTarget, ccRInfo);
                            if (f == null) continue;
                            var p = posTarget.Position.RayIntersectPlane(f.Plane.Normal, f.Plane);
                            shape.Add(p);
                        }
                    }
                    if (cposTargets == 4)
                    {
                        var diamterST = ccRInfo.DiameterST.FindInterger() * 1.0.FromMillimeters();
                        var diamterMain = 0.5.FromMillimeters()
                        * (ccRInfo.DiameterDX.FindInterger() + ccRInfo.DiameterDY.FindInterger());
                        var extent = cover + diamterMain * 0.5 + diamterST * 0.5;
                        var f = _GetFace(posTargets.First(), ccRInfo);
                        var vt = f.Plane.Normal.CrossProduct(ccRInfo.VTZ);
                        var grs = posTargets
                            .GroupBy(x => x.Face)
                            .Select(x => x.ToList())
                            .ToList();
                        var gr1 = grs.First()
                            .OrderBy(x => x.Position.DotProduct(vt))
                            .ToList();
                        var gr2 = grs.Last()
                            .OrderBy(x => x.Position.DotProduct(vt))
                            .ToList();
                        var f1 = _GetFace(gr1.First(), ccRInfo);
                        var f2 = _GetFace(gr2.First(), ccRInfo);

                        var p1 = gr1[0].Position.RayIntersectPlane(f1.Plane.Normal, f1.Plane)
                            - vt * extent;
                        var p2 = gr1[1].Position.RayIntersectPlane(f1.Plane.Normal, f1.Plane)
                            + vt * extent;
                        var p3 = gr2[1].Position.RayIntersectPlane(f2.Plane.Normal, f2.Plane)
                            + vt * extent;
                        var p4 = gr2[0].Position.RayIntersectPlane(f2.Plane.Normal, f2.Plane)
                            - vt * extent;

                        shape.Add(p4);
                        shape.Add(p3);
                        shape.Add(p2);
                        shape.Add(p1);
                    }
                    if (!shape.Any()) continue;
                    _InstallSub(shape, ccRInfo);
                }
                ColumnFaceModel _GetFace(ColumnRebarPositionModel rebarPos, ColumnConcreteModel ccRInfo)
                {
                    ColumnFaceModel result = ccRInfo.FaceLeft;
                    var facetype = (ColumnFaceType)rebarPos.Face;
                    switch (facetype)
                    {
                        case ColumnFaceType.Left:
                            result = ccRInfo.FaceLeft;
                            break;
                        case ColumnFaceType.Top:
                            result = ccRInfo.FaceTop;
                            break;
                        case ColumnFaceType.Right:
                            result = ccRInfo.FaceRight;
                            break;
                        case ColumnFaceType.Bottom:
                            result = ccRInfo.FaceBottom;
                            break;
                    }
                    return result;
                }
                void _InstallSub(List<XYZ> ps, ColumnConcreteModel col)
                {
                    var qty = ps.Count;
                    if (qty < 2) return;
                    var baseShapes = new List<XYZ>();
                    var diamterMain = 0.5.FromMillimeters()
                        * (col.DiameterDX.FindInterger() + col.DiameterDY.FindInterger());
                    var diamterSt = col.DiameterST.FindInterger() * 1.0.FromMillimeters();
                    if (qty == 2)
                    {
                        var vt = (ps[1] - ps[0]).Normalize();
                        var nor = vt.CrossProduct(col.VTZ);
                        var p1 = ps[0] + vt * col.Cover.FromMillimeters()
                            + nor * (diamterMain + diamterSt) / 2;
                        var p2 = ps[1] - vt * col.Cover.FromMillimeters()
                            + nor * (diamterMain + diamterSt) / 2;
                        baseShapes.Add(p1);
                        baseShapes.Add(p2);
                    }
                    else
                    {
                        baseShapes = CurveLoop.CreateViaOffset(ps
                            .PointsToCurveLoop(), cover, -ccRInfo.VTZ)
                            .Select(x => x.GetEndPoint(1))
                            .ToList();
                        var p1 = baseShapes[0];
                        var p2 = baseShapes[1];
                        var p3 = baseShapes[2];
                        var p4 = baseShapes[3];
                        var vtStart = (p2 - p1).Normalize();
                        var vtEnd = (p1 - p4).Normalize();
                        baseShapes = new List<XYZ>()
                        {
                            p1 - vtStart * diamterSt / 2,
                            p2,
                            p3,
                            p4,
                            p1 + vtEnd * diamterSt / 2
                        };
                    }

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
                    var hook135 = rebarHookTypes.FirstOrDefault(x => Math.Abs(x.HookAngle.ToDegrees() - 135) <= 1);
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
                    result.Add(shapes.PointsToCurves());
                    //_document.CreateCurves(shapes.PointsToCurves());
                    if (i != qty - 1) continue;
                    if (duSpacing < 0.3 * spacingMm) continue;
                    var shapesDu = shapes
                        .Select(x => x + vt * duSpacing.FromMillimeters())
                        .ToList();
                    result.Add(shapesDu.PointsToCurves());
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
