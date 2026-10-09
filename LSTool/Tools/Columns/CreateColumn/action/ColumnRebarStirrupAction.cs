using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using LSTool.AutoCad.Action.Actions;
using LSTool.Compatibility;
using LSTool.MVVM.model.SettingRebarColumnDatas;
using LSTool.Tools.Columns.CreateColumn.model;
using LSTool.Tools.Columns.CreateColumn.schemas;
using LSTool.Tools.Columns.CreateColumn.types;
using LSTool.Utils;

namespace LSTool.Tools.Columns.CreateColumn.action
{
    public class ColumnRebarStirrupAction
    {
        private UIDocument _uidocument;
        private Document _document;
        private List<RebarBarType> _rebarBarTypes;
        private Element _host;
        private SettingFrameModel _settingRebarColumnModel;
        private ColumnStirrupPositionSchema _columnStirrupPositionSchema;
        public ColumnRebarStirrupAction(
            UIDocument uidocument,
            Element host,
            SettingFrameModel settingRebarColumnModel)
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
            _settingRebarColumnModel = settingRebarColumnModel;
            _columnStirrupPositionSchema = new ColumnStirrupPositionSchema(
                ColumnStirrupPositionSchema.GUID,
                ColumnStirrupPositionSchema.NAME);
        }

        public void CreateStirrupMain(
            List<ColumnConcreteModel> ccRInfos)
        {
            var qty = ccRInfos.Count;
            using (var ts = new SubTransaction(_document))
            {
                ts.Start();
                foreach (var ccRInfo in ccRInfos)
                {
                    var index = ccRInfos.IndexOf(ccRInfo);
                    var hasBeamZone = index > 0 && index < qty - 1;
                    InstallStirrupMain(ccRInfo, hasBeamZone);
                }
                ts.Commit();
            }
        }
        public void CreateStirrupSub(List<ColumnConcreteModel> ccRInfos)
        {
            var qty = ccRInfos.Count;
            using (var ts = new SubTransaction(_document))
            {
                ts.Start();
                foreach (var ccRInfo in ccRInfos)
                {
                    var index = ccRInfos.IndexOf(ccRInfo);
                    var hasBeamZone = index > 0 && index < qty - 1;
                    InstallStirrupSub(ccRInfo, hasBeamZone);
                }
                ts.Commit();
            }
        }
        public void SaveSettingColumnStirrupPosition(
            List<ColumnConcreteModel> ccRInfos)
        {
            foreach (var col in ccRInfos)
            {
                if (!col.Ties.Any()) continue;
                var content = JsonConvert.SerializeObject(col.Ties);
                var ele = _document.GetElement(col.Id);
                _columnStirrupPositionSchema.Write(ele, content);
            }
        }
        public void GetSettingColumnStirrupPosition(
            List<ColumnConcreteModel> cols)
        {
            var result = new List<List<ColumnStirrupPositionModel>>();
            foreach (var col in cols)
            {
                var ele = _document.GetElement(col.Id);
                var content = _columnStirrupPositionSchema.Read(ele);
                if (string.IsNullOrEmpty(content)) continue;
                var objs = JsonConvert.DeserializeObject<List<List<ColumnStirrupPositionModel>>>(content);
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
                var stressZone = _settingRebarColumnModel.StressZone;
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
                    p1 + vtEnd * diamterSt.MmToFoot() / 2,
                    p4,
                    p3,
                    p2,
                    p1 - vtStart * diamterSt.MmToFoot() / 2,
                };
                var shapes_Start = _installStirrup(start_zone1, End_zone1, baseShapes, ccRInfo.SpacingSTE, 50, ccRInfo.SpacingSTE / 2);
                var shapes_Mid = _installStirrup(start_zone2, End_zone2, baseShapes, ccRInfo.SpacingST, ccRInfo.SpacingST / 2, ccRInfo.SpacingST / 2);
                var shapes_End = _installStirrup(start_zone3, End_zone3, baseShapes, ccRInfo.SpacingSTE, 50, ccRInfo.SpacingSTE / 2);

                var rebarHookTypes = new FilteredElementCollector(_document)
                    .WhereElementIsElementType()
                    .OfClass(typeof(RebarHookType))
                    .Cast<RebarHookType>()
                    .ToList();

                var hook135 = rebarHookTypes.FirstOrDefault(x => Math.Abs(x.HookAngle.ToDegrees() - 135) <= 1)
                    ?? rebarHookTypes.FirstOrDefault(x => x.Name.Contains("135"))
                    ?? rebarHookTypes.FirstOrDefault();
                if (hook135 == null)
                    throw new Exception("Hook 135 is null");
                var hookLengthMm = Math.Max(diamterSt * 10, 100);
                foreach (var item in shapes_Start)
                {
                    RebarHelper.CreateRebarStirrupTie(
                        _document,
                        item, ccRInfo.DiameterST, ccRInfo.VTZ, hook135, hook135, _rebarBarTypes, _host,
                        hookLengthMm: hookLengthMm);
                }
                foreach (var item in shapes_Mid)
                {
                    RebarHelper.CreateRebarStirrupTie(
                        _document,
                        item, ccRInfo.DiameterST, ccRInfo.VTZ, hook135, hook135, _rebarBarTypes, _host,
                        hookLengthMm: hookLengthMm);
                }
                foreach (var item in shapes_End)
                {
                    RebarHelper.CreateRebarStirrupTie(
                        _document,
                        item, ccRInfo.DiameterST, ccRInfo.VTZ, hook135, hook135, _rebarBarTypes, _host,
                        hookLengthMm: hookLengthMm);
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
                var stressZone = _settingRebarColumnModel.StressZone;

                var start_zone1 = start;
                var End_zone1 = start + ccRInfo.VTZ * length * stressZone;

                var start_zone2 = start + ccRInfo.VTZ * length * stressZone;
                var End_zone2 = end - ccRInfo.VTZ * length * stressZone;

                var start_zone3 = end - ccRInfo.VTZ * length * stressZone;
                var End_zone3 = end;

                if (ccRInfo.Ties == null || !ccRInfo.Ties.Any()) return;

                var rebarPos = (ccRInfo.RebarMainPositionss != null && ccRInfo.RebarMainPositionss.Any())
                    ? ccRInfo.RebarMainPositionss.Aggregate((a, b) => a.Concat(b).ToList()).ToList()
                    : GetRebarPositions(ccRInfo);

                if (rebarPos == null || !rebarPos.Any()) return;
                if (ccRInfo.RebarMainPositionss == null || !ccRInfo.RebarMainPositionss.Any())
                {
                    ccRInfo.RebarMainPositionss = new List<List<ColumnRebarPositionModel>> { rebarPos };
                }

                foreach (var tie in ccRInfo.Ties)
                {
                    var shape = new List<XYZ>();
                    var posTargets = new List<ColumnRebarPositionModel>();
                    foreach (var item in tie)
                    {
                        var posTarget = rebarPos.FirstOrDefault(x => x.Index == item.Index && x.Face == item.Face);
                        if (posTarget == null) continue;
                        if (posTargets.Any(x => x.Index == posTarget.Index && x.Face == posTarget.Face)) continue;
                        posTargets.Add(posTarget);
                    }
                    var cposTargets = posTargets.Count;
                    if (cposTargets < 2) continue;
                    if (cposTargets == 2)
                    {
                        foreach (var posTarget in posTargets)
                        {
                            shape.Add(posTarget.Position);
                        }
                    }
                    if (cposTargets > 2)
                    {
                        var diamterST = ccRInfo.DiameterST.FindInterger() * 1.0.FromMillimeters();
                        var diamterMain = 0.5.FromMillimeters()
                            * (ccRInfo.DiameterDX.FindInterger() + ccRInfo.DiameterDY.FindInterger());
                        var rOffset = (diamterMain + diamterST) / 2.0;

                        var zPlane = Plane.CreateByNormalAndOrigin(ccRInfo.VTZ, ccRInfo.Center);
                        var pts = posTargets
                            .Select(pt => pt.Position.RayIntersectPlane(zPlane.Normal, zPlane))
                            .ToList();

                        int n = pts.Count;
                        var cMid = new XYZ(pts.Average(p => p.X), pts.Average(p => p.Y), pts.Average(p => p.Z));

                        // Sắp xếp các điểm theo thứ tự chiều kim đồng hồ quanh trọng tâm
                        var vtx = ccRInfo.VTX;
                        var vty = ccRInfo.VTZ.CrossProduct(ccRInfo.VTX).Normalize();
                        var sortedPts = pts
                            .OrderByDescending(p =>
                            {
                                var v = p - cMid;
                                return Math.Atan2(v.DotProduct(vty), v.DotProduct(vtx));
                            })
                            .ToList();

                        // Tính vector cạnh và vector pháp tuyến ngoài cho từng cạnh
                        var edgeVecs = new XYZ[n];
                        var edgeNormals = new XYZ[n];
                        for (int i = 0; i < n; i++)
                        {
                            var pCurr = sortedPts[i];
                            var pNext = sortedPts[(i + 1) % n];
                            var eVec = (pNext - pCurr).Normalize();
                            edgeVecs[i] = eVec;

                            var normal = ccRInfo.VTZ.CrossProduct(eVec).Normalize();
                            var mid = 0.5 * (pCurr + pNext);
                            if (normal.DotProduct(mid - cMid) < 0)
                            {
                                normal = -normal;
                            }
                            edgeNormals[i] = normal;
                        }

                        // Tính các đỉnh giao nhau của các cạnh đai đa giác
                        for (int i = 0; i < n; i++)
                        {
                            int prevIdx = (i - 1 + n) % n;
                            var p1 = sortedPts[prevIdx] + edgeNormals[prevIdx] * rOffset;
                            var d1 = edgeVecs[prevIdx];

                            var p2 = sortedPts[i] + edgeNormals[i] * rOffset;
                            var d2 = edgeVecs[i];

                            var delta = p2 - p1;
                            var det = (d1.CrossProduct(d2)).DotProduct(ccRInfo.VTZ);
                            if (Math.Abs(det) > 1e-6)
                            {
                                var t1 = (delta.CrossProduct(d2)).DotProduct(ccRInfo.VTZ) / det;
                                shape.Add(p1 + t1 * d1);
                            }
                            else
                            {
                                shape.Add(sortedPts[i] + edgeNormals[i] * rOffset);
                            }
                        }
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
                    var diamterStInt = col.DiameterST.FindInterger();
                    var hookLengthMm = Math.Max(diamterStInt * 10, 100);

                    var hookOrientStart = RebarHookOrientation.Left;
                    var hookOrientEnd = RebarHookOrientation.Left;

                    if (qty == 2)
                    {
                        var vt = (ps[1] - ps[0]).Normalize();
                        var nor = vt.CrossProduct(col.VTZ).Normalize();
                        var extend2Pt = diamterSt + diamterMain / 2;
                        var p1 = ps[0] - vt * extend2Pt + nor * (diamterMain + diamterSt) / 2;
                        var p2 = ps[1] + vt * extend2Pt + nor * (diamterMain + diamterSt) / 2;
                        baseShapes.Add(p1);
                        baseShapes.Add(p2);

                        var rightStart = vt.CrossProduct(col.VTZ).Normalize();
                        var vInStart = (ps[0] - p1).Normalize();
                        hookOrientStart = rightStart.DotProduct(vInStart) > 0 ? RebarHookOrientation.Left : RebarHookOrientation.Right;

                        var vInEnd = (ps[1] - p2).Normalize();
                        hookOrientEnd = rightStart.DotProduct(vInEnd) > 0 ? RebarHookOrientation.Left : RebarHookOrientation.Right;
                    }
                    else // qty > 2 (đai đa giác kín 3, 4, 5, 6... đỉnh)
                    {
                        var pStart = ps[0];
                        var pNext = ps[1];
                        var pLast = ps[ps.Count - 1];
                        var vtStart = (pNext - pStart).Normalize();
                        var vtEnd = (pStart - pLast).Normalize();
                        baseShapes = new List<XYZ>
                        {
                            pStart - vtStart * diamterSt
                        };
                        for (int i = 1; i < ps.Count; i++)
                        {
                            baseShapes.Add(ps[i]);
                        }
                        baseShapes.Add(pStart + vtEnd * diamterSt);

                        var cMid = new XYZ(ps.Average(p => p.X), ps.Average(p => p.Y), ps.Average(p => p.Z));
                        var vIn = (cMid - pStart).Normalize();

                        var rightStart = vtStart.CrossProduct(col.VTZ).Normalize();
                        hookOrientStart = rightStart.DotProduct(vIn) > 0 ? RebarHookOrientation.Left : RebarHookOrientation.Right;

                        var rightEnd = vtEnd.CrossProduct(col.VTZ).Normalize();
                        hookOrientEnd = rightEnd.DotProduct(vIn) > 0 ? RebarHookOrientation.Left : RebarHookOrientation.Right;
                    }
                    baseShapes.Reverse();
                    var shapes_Start = _installStirrup(start_zone1, End_zone1, baseShapes, ccRInfo.SpacingSTE, 50, ccRInfo.SpacingSTE / 2);
                    var shapes_Mid = _installStirrup(start_zone2, End_zone2, baseShapes, ccRInfo.SpacingST, ccRInfo.SpacingST / 2, ccRInfo.SpacingST / 2);
                    var shapes_End = _installStirrup(start_zone3, End_zone3, baseShapes, ccRInfo.SpacingSTE, 50, ccRInfo.SpacingSTE / 2);

                    var rebarHookTypes = new FilteredElementCollector(_document)
                        .WhereElementIsElementType()
                        .OfClass(typeof(RebarHookType))
                        .Cast<RebarHookType>()
                        .ToList();

                    var hook135 = rebarHookTypes.FirstOrDefault(x => Math.Abs(x.HookAngle.ToDegrees() - 135) <= 1)
                        ?? rebarHookTypes.FirstOrDefault(x => x.Name.Contains("135"))
                        ?? rebarHookTypes.FirstOrDefault();
                    if (hook135 == null)
                        throw new Exception("Hook 135 is null");

                    foreach (var item in shapes_Start)
                    {
                        try
                        {
                            RebarHelper.CreateRebarStirrupTie(
                                _document,
                                item, ccRInfo.DiameterST, ccRInfo.VTZ, hook135, hook135, _rebarBarTypes, _host,
                                hookOrientStart, hookOrientEnd, hookLengthMm);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error creating sub stirrup start: {ex}");
                        }
                    }
                    foreach (var item in shapes_Mid)
                    {
                        try
                        {
                            RebarHelper.CreateRebarStirrupTie(
                                _document,
                                item, ccRInfo.DiameterST, ccRInfo.VTZ, hook135, hook135, _rebarBarTypes, _host,
                                hookOrientStart, hookOrientEnd, hookLengthMm);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error creating sub stirrup mid: {ex}");
                        }
                    }
                    foreach (var item in shapes_End)
                    {
                        try
                        {
                            RebarHelper.CreateRebarStirrupTie(
                                _document,
                                item, ccRInfo.DiameterST, ccRInfo.VTZ, hook135, hook135, _rebarBarTypes, _host,
                                hookOrientStart, hookOrientEnd, hookLengthMm);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error creating sub stirrup end: {ex}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in InstallStirrupSub: {ex}");
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

        private List<ColumnRebarPositionModel> GetRebarPositions(ColumnConcreteModel col)
        {
            var results = new List<ColumnRebarPositionModel>();
            if (col == null) return results;

            var qtyX = (int)Math.Round(col.SpacingDX, 0);
            var qtyY = (int)Math.Round(col.SpacingDY, 0);
            if (qtyX < 2) qtyX = 2;
            if (qtyY < 2) qtyY = 2;

            var dMainX = col.DiameterDX.FindInterger();
            var dMainY = col.DiameterDY.FindInterger();
            var dSt = col.DiameterST.FindInterger();

            // FaceLeft (Left = 0): Bottom-Left (Pb2) -> Top-Left (Pb1), qtyY
            if (col.FaceLeft != null)
            {
                var cover = (col.Cover + dSt + dMainY / 2.0).FromMillimeters();
                var pStart = col.FaceLeft.Pb2;
                var pEnd = col.FaceLeft.Pb1;
                var vtX = (pEnd - pStart).Normalize();
                var vtY = -col.FaceLeft.Plane.Normal;
                var sp = pStart + vtY * cover + vtX * cover;
                var ep = pEnd + vtY * cover - vtX * cover;
                results.AddRange(SolvePositionInstallRebar(sp, ep, qtyY, qtyY, col.FaceLeft));
            }

            // FaceTop (Top = 1): Top-Left (Pb2) -> Top-Right (Pb1), qtyX
            if (col.FaceTop != null)
            {
                var cover = (col.Cover + dSt + dMainX / 2.0).FromMillimeters();
                var pStart = col.FaceTop.Pb2;
                var pEnd = col.FaceTop.Pb1;
                var vtX = (pEnd - pStart).Normalize();
                var vtY = -col.FaceTop.Plane.Normal;
                var sp = pStart + vtY * cover + vtX * cover;
                var ep = pEnd + vtY * cover - vtX * cover;
                results.AddRange(SolvePositionInstallRebar(sp, ep, qtyX, qtyX, col.FaceTop));
            }

            // FaceRight (Right = 2): Top-Right (Pb2) -> Bottom-Right (Pb1), qtyY
            if (col.FaceRight != null)
            {
                var cover = (col.Cover + dSt + dMainY / 2.0).FromMillimeters();
                var pStart = col.FaceRight.Pb2;
                var pEnd = col.FaceRight.Pb1;
                var vtX = (pEnd - pStart).Normalize();
                var vtY = -col.FaceRight.Plane.Normal;
                var sp = pStart + vtY * cover + vtX * cover;
                var ep = pEnd + vtY * cover - vtX * cover;
                results.AddRange(SolvePositionInstallRebar(sp, ep, qtyY, qtyY, col.FaceRight));
            }

            // FaceBottom (Bottom = 3): Bottom-Right (Pb2) -> Bottom-Left (Pb1), qtyX
            if (col.FaceBottom != null)
            {
                var cover = (col.Cover + dSt + dMainX / 2.0).FromMillimeters();
                var pStart = col.FaceBottom.Pb2;
                var pEnd = col.FaceBottom.Pb1;
                var vtX = (pEnd - pStart).Normalize();
                var vtY = -col.FaceBottom.Plane.Normal;
                var sp = pStart + vtY * cover + vtX * cover;
                var ep = pEnd + vtY * cover - vtX * cover;
                results.AddRange(SolvePositionInstallRebar(sp, ep, qtyX, qtyX, col.FaceBottom));
            }

            return results;
        }

        private List<ColumnRebarPositionModel> SolvePositionInstallRebar(
            XYZ start,
            XYZ end,
            int qty,
            int maxQty,
            ColumnFaceModel hostFace)
        {
            var results = new List<ColumnRebarPositionModel>();
            try
            {
                var vt = (end - start).Normalize();
                var distance = start.DistanceTo(end);
                var spacing = (distance / (maxQty - 1));
                var qtyDu = qty % 2;
                var haft = (qty - qtyDu) / 2;
                for (int i = 0; i < haft; i++)
                {
                    var p = start + i * spacing * vt;
                    results.Add(new ColumnRebarPositionModel()
                    { Index = i + 1, Position = p, Face = hostFace.FaceType, HostId = hostFace.HostId });
                }
                if (qtyDu == 1)
                {
                    var p = 0.5 * (start + end);
                    results.Add(new ColumnRebarPositionModel()
                    { Index = 1 + maxQty / 2, Position = p, Face = hostFace.FaceType, HostId = hostFace.HostId });
                }
                for (int i = 0; i < haft; i++)
                {
                    var p = end - i * spacing * vt;
                    results.Add(new ColumnRebarPositionModel()
                    { Index = maxQty - i, Position = p, Face = hostFace.FaceType, HostId = hostFace.HostId });
                }
            }
            catch (Exception)
            {
            }
            if (!results.Any()) return results;
            return results.OrderBy(x => x.Index).ToList();
        }
    }
}
