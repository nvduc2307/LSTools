using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using LSTool.Compatibility;
using LSTool.Tools.Columns.ColumnRebar.models;
using LSTool.Tools.Generals.SettingRebarStandard.models;
using LSTool.Utils;

namespace LSTool.Tools.Columns.ColumnRebar.actions
{
    public class ColumnRebarMainAction
    {
        private UIDocument _uidocument;
        private Document _document;
        private ColumnRebarAnchorModelUI _columnRebarAnchorModel;
        private SettingRebarStandardModelUI _settingRebarStandardModel;
        private List<RebarBarType> _rebarBarTypes;
        private double _distanceCheckLimit = 40;
        private Element _host;
        private double _e;
        public ColumnRebarMainAction(
            UIDocument uidocument,
            ColumnRebarAnchorModelUI columnRebarAnchorModel,
            SettingRebarStandardModelUI settingRebarStandardModel,
            Element host)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            _columnRebarAnchorModel = columnRebarAnchorModel;
            _settingRebarStandardModel = settingRebarStandardModel;
            _host = host;
            _rebarBarTypes = new FilteredElementCollector(_document)
                .WhereElementIsElementType()
                .OfClass(typeof(RebarBarType))
                .Cast<RebarBarType>()
                .Where(x => x.Name.Contains("D"))
                .OrderBy(x => x.Name)
                .ToList();
        }
        public void CreateRebarMain(
            List<ColumnConcreteModel> cCols,
            SettingRebarStandardModelUI standard)
        {
            _e = standard.EC;
            foreach (ColumnConcreteModel cModel in cCols)
            {
                var qtyX = cModel.SpacingDX;
                var qtyY = cModel.SpacingDY;

                cModel.FaceLeft.RebarQty = qtyY;
                cModel.FaceLeft.RebarQtyNext = qtyX;
                cModel.FaceLeft.CoverBase = cModel.Cover;
                cModel.FaceLeft.Cover = cModel.Cover
                    + cModel.DiameterDY.FindInterger() / 2
                    + cModel.DiameterST.FindInterger();
                cModel.FaceLeft.Diameter = cModel.DiameterDY.FindInterger();
                cModel.FaceLeft.HeightBeamZone = cModel.HeightBeamZone;

                cModel.FaceTop.RebarQty = qtyX;
                cModel.FaceTop.RebarQtyNext = qtyY;
                cModel.FaceTop.CoverBase = cModel.Cover;
                cModel.FaceTop.Cover = cModel.Cover
                    + cModel.DiameterDX.FindInterger() / 2
                    + cModel.DiameterST.FindInterger();
                cModel.FaceTop.Diameter = cModel.DiameterDX.FindInterger();
                cModel.FaceTop.HeightBeamZone = cModel.HeightBeamZone;

                cModel.FaceRight.RebarQty = qtyY;
                cModel.FaceRight.RebarQtyNext = qtyX;
                cModel.FaceRight.CoverBase = cModel.Cover;
                cModel.FaceRight.Cover = cModel.Cover
                    + cModel.DiameterDY.FindInterger() / 2
                    + cModel.DiameterST.FindInterger();
                cModel.FaceRight.Diameter = cModel.DiameterDY.FindInterger();
                cModel.FaceRight.HeightBeamZone = cModel.HeightBeamZone;

                cModel.FaceBottom.RebarQty = qtyX;
                cModel.FaceBottom.RebarQtyNext = qtyY;
                cModel.FaceBottom.CoverBase = cModel.Cover;
                cModel.FaceBottom.Cover = cModel.Cover
                    + cModel.DiameterDX.FindInterger() / 2
                    + cModel.DiameterST.FindInterger();
                cModel.FaceBottom.Diameter = cModel.DiameterDX.FindInterger();
                cModel.FaceBottom.HeightBeamZone = cModel.HeightBeamZone;
            }
            var faceLefts = cCols.Select(x => x.FaceLeft).ToList();
            var faceTops = cCols.Select(x => x.FaceTop).ToList();
            var faceRights = cCols.Select(x => x.FaceRight).ToList();
            var faceBots = cCols.Select(x => x.FaceBottom).ToList();
            var rebarPositions = new List<List<ColumnRebarPositionModel>>();

            rebarPositions.AddRange(InstallRebarFace(faceLefts, true));
            rebarPositions.AddRange(InstallRebarFace(faceBots));
            rebarPositions.AddRange(InstallRebarFace(faceRights, true));
            rebarPositions.AddRange(InstallRebarFace(faceTops));

            foreach (var col in cCols)
            {
                var positions = rebarPositions
                    .Where(x => x.FirstOrDefault()?.HostId == col.Id)
                    .ToList();
                if (!positions.Any()) continue;
                col.RebarMainPositionss = positions;
            }
        }
        private List<List<ColumnRebarPositionModel>> InstallRebarFace(List<ColumnFaceModel> faces, bool ignoreFirstEnd = false)
        {
            var result = new List<List<ColumnRebarPositionModel>>();
            var fCount = faces.Count;
            if (fCount == 1)
            {
                CreateRebarColumn_Single(faces, ignoreFirstEnd, out List<List<ColumnRebarPositionModel>> rebarPositions0);
                result.AddRange(rebarPositions0);
            }
            else
            {
                //Truong hop co nhieu cot
                CreateRebarColumn_Multi(faces, ignoreFirstEnd, out List<List<ColumnRebarPositionModel>> rebarPositions1);
                CreateRebarColumn_Multi_Last(faces, ignoreFirstEnd, out List<List<ColumnRebarPositionModel>> rebarPositions2);
                result.AddRange(rebarPositions1);
                result.AddRange(rebarPositions2);
            }
            return result;
        }
        private void CreateRebarColumn_Single(
            List<ColumnFaceModel> faces,
            bool ignoreFirstEnd,
            out List<List<ColumnRebarPositionModel>> rebarPoss)
        {
            rebarPoss = new List<List<ColumnRebarPositionModel>>();
            var result = new List<List<Curve>>();
            var face = faces.First();
            var diameter = face.Diameter.FromMillimeters();
            var lapLength = _settingRebarStandardModel.L1 * diameter;
            var minHook = _settingRebarStandardModel.HMin * diameter;
            var anchor = _columnRebarAnchorModel.AC.FromMillimeters();
            var cover = face.Cover.FromMillimeters();
            var vtX = (face.Pb2 - face.Pb1).Normalize();
            var vtY = -face.Plane.Normal;
            var vtZ = XYZ.BasisZ;
            var sp = face.Pb1 + vtY * cover + vtX * cover;
            var ep = face.Pb2 + vtY * cover - vtX * cover;
            var length = face.Pb1.DistanceTo(face.Pt1);
            var rebarPositions = SolvePositionInstallRebar(sp, ep,
                int.Parse(Math.Round(face.RebarQty, 0).ToString()),
                int.Parse(Math.Round(face.RebarQty, 0).ToString()), face);
            rebarPoss.Add(rebarPositions);
            var rbCount = rebarPositions.Count;
            foreach (var rebarPosition in rebarPositions)
            {
                var index = rebarPositions.IndexOf(rebarPosition);
                if (ignoreFirstEnd && (index == 0 || index == rbCount - 1))
                    continue;
                var rbStart = rebarPosition.Position - vtZ * anchor;
                var rbEnd = rebarPosition.Position + vtZ * (length - face.CoverBase.FromMillimeters());
                var shape = new List<XYZ>()
                {
                    rbStart - vtY * minHook,
                    rbStart,
                    rbEnd,
                    rbEnd + vtY * minHook
                };
                result.Add(shape.PointsToCurves());
            }
            foreach (var cv in result)
            {
                var isRebarFreeForm = RebarHelper.IsRebarFreeForm(cv, out XYZ normal);
                if (isRebarFreeForm)
                    RebarHelper.CreateRebar(_document, cv, $"D{Math.Round(face.Diameter, 0)}", "A", _rebarBarTypes, _host);
                else
                    RebarHelper.CreateRebar(_document, cv, $"D{Math.Round(face.Diameter, 0)}", normal, _rebarBarTypes, _host);
            }
        }
        private void CreateRebarColumn_Multi(
            List<ColumnFaceModel> faces,
            bool ignoreFirstEnd,
            out List<List<ColumnRebarPositionModel>> rebarPoss)
        {
            rebarPoss = new List<List<ColumnRebarPositionModel>>();
            var rebarPositions = new List<List<ColumnRebarPositionModel>>();
            var qtyMax = faces.Max(x => x.RebarQty);
            var fCount = faces.Count;
            var faceType = (ColumnFaceType)faces.FirstOrDefault().FaceType;
            foreach (var face in faces)
            {
                var cover = face.Cover.FromMillimeters();
                var vtX = (face.Pb2 - face.Pb1).Normalize();
                var vtY = -face.Plane.Normal;
                var vtZ = XYZ.BasisZ;
                var sp = face.Pb1 + vtY * cover + vtX * cover;
                var ep = face.Pb2 + vtY * cover - vtX * cover;
                var ps = SolvePositionInstallRebar(sp, ep,
                    int.Parse(Math.Round(face.RebarQty, 0).ToString()),
                    int.Parse(Math.Round(qtyMax, 0).ToString()), face);
                rebarPositions.Add(ps);
            }
            foreach (var face in faces)
            {
                var index = faces.IndexOf(face);
                var isOdd = CheckPositionSole(faceType, face);
                var isOddPrev = index == 0 ? false
                    : CheckPositionSole(faceType, faces[index - 1]);
                var cover = face.Cover.FromMillimeters();
                var vtX = (face.Pb2 - face.Pb1).Normalize();
                var vtY = -face.Plane.Normal;
                var vtZ = XYZ.BasisZ;
                var diameter = face.Diameter.FromMillimeters();
                var lapLength = _settingRebarStandardModel.L1 * diameter;
                var minHook = _settingRebarStandardModel.HMin * diameter;
                var anchor = _columnRebarAnchorModel.AC.FromMillimeters();
                var gapLap = _settingRebarStandardModel.G * diameter;
                var diameterPrev = index == 0 ? 0 : faces[index - 1].Diameter.FromMillimeters();
                var lapLengthPrev = _settingRebarStandardModel.L1 * diameterPrev;
                var gapLapPrev = _settingRebarStandardModel.G * diameterPrev;
                if (index == fCount - 1) continue;
                var length = face.Pb1.DistanceTo(face.Pt1);
                var rebarPosition = rebarPositions[index];
                var rebarPositionNext = rebarPositions[index + 1];
                var rebarPositionPrev = index == 0 ? null : rebarPositions[index - 1];
                var isLapDiffTop = IsDifferentFace(face, faces[index + 1], out double distanceTop);
                var isLapDiffBot = index == 0
                    ? false
                    : IsDifferentFace(faces[index - 1], face, out double distanceBot);
                var rbCount = rebarPosition.Count;
                foreach (var item in rebarPosition)
                {
                    var numberOf = rebarPosition.IndexOf(item);
                    if (ignoreFirstEnd && (numberOf == 0 || numberOf == rbCount - 1))
                        continue;
                    var condit1 = numberOf % 2 == 0;
                    var isSole = !condit1 && !isOdd ? true : condit1 && isOdd ? true : false;
                    var lapLengthGap = lapLength + (isSole ? lapLength + gapLap : 0);
                    var rebarPositionNextTarget = rebarPositionNext.FirstOrDefault(x => x.Index == item.Index);
                    var rebarPositionPrevTarget = rebarPositionPrev == null
                        ? null
                        : rebarPositionPrev.FirstOrDefault(x => x.Index == item.Index);
                    var numberOfPrev = rebarPositionPrevTarget == null
                        ? -1
                        : rebarPositionPrev.IndexOf(rebarPositionPrevTarget);
                    var condit1Prev = numberOfPrev % 2 == 0;
                    var isSolePrev = rebarPositionPrevTarget != null
                        && (!condit1Prev && !isOddPrev ? true : condit1Prev && isOddPrev ? true : false);
                    var lapLengthGapPrev = isSolePrev
                        ? lapLengthPrev + gapLapPrev
                        : 0;
                    if (isLapDiffTop)
                        _install_Case_LapDiffTop();
                    else
                    {
                        if (rebarPositionPrevTarget == null)
                        {
                            if (rebarPositionNextTarget == null)
                                _install_Case_NotLapDiffTop_rebarPositionPrevTarget_Null_rebarPositionNextTarget_Null();
                            else
                                _install_Case_NotLapDiffTop_rebarPositionPrevTarget_Null_rebarPositionNextTarget_Valid();
                        }
                        else
                        {
                            if (rebarPositionNextTarget == null)
                                _install_Case_NotLapDiffTop_rebarPositionPrevTarget_Valid_rebarPositionNextTarget_Null();
                            else
                                _install_Case_NotLapDiffTop_rebarPositionPrevTarget_Valid_rebarPositionNextTarget_Valid();
                        }
                    }
                    void _install_Case_LapDiffTop()
                    {
                        var p1 = index == 0
                            ? item.Position - vtZ * anchor
                            : item.Position - vtZ * (isLapDiffBot ? anchor : 0);
                        var p2 = item.Position + vtZ * (length - face.CoverBase.FromMillimeters());
                        var p3 = p2 - face.Plane.Normal * (anchor * 0.5 + distanceTop.FromMillimeters());
                        var ps = index == 0 ?
                            new List<XYZ>() { p1 - vtY * minHook, p1, p2, p3 }
                            : new List<XYZ>() { p1 + vtZ * (isLapDiffBot ? 0 : lapLengthGapPrev), p2, p3 };
                        var cv = ps.PointsToCurves();
                        var isRebarFreeForm = RebarHelper.IsRebarFreeForm(cv, out XYZ normal);
                        if (isRebarFreeForm)
                            RebarHelper.CreateRebar(_document, cv, $"D{Math.Round(face.Diameter, 0)}", "A", _rebarBarTypes, _host);
                        else
                            RebarHelper.CreateRebar(_document, cv, $"D{Math.Round(face.Diameter, 0)}", normal, _rebarBarTypes, _host);
                    }
                    void _install_Case_NotLapDiffTop_rebarPositionPrevTarget_Null_rebarPositionNextTarget_Null(bool isLapDiffBotImportant = false)
                    {
                        //var p1 = index == 0
                        //        ? item.Position - vtZ * anchor
                        //        : item.Position - vtZ * (isLapDiffBot ? anchor : 0);
                        var p1 = item.Position - vtZ * anchor;
                        var p2 = item.Position + vtZ * (length + lapLengthGap);
                        var ps = index == 0 ?
                        new List<XYZ>() { p1 - vtY * minHook, p1, p2 }
                        : isLapDiffBotImportant ? new List<XYZ>() { p1, p2 }
                        : new List<XYZ>() { p1 + vtZ * (isLapDiffBot ? 0 : lapLengthGapPrev), p2 };
                        var cv = ps.PointsToCurves();
                        var isRebarFreeForm = RebarHelper.IsRebarFreeForm(cv, out XYZ normal);
                        if (isRebarFreeForm)
                            RebarHelper.CreateRebar(_document, cv, $"D{Math.Round(face.Diameter, 0)}", "A", _rebarBarTypes, _host);
                        else
                            RebarHelper.CreateRebar(_document, cv, $"D{Math.Round(face.Diameter, 0)}", normal, _rebarBarTypes, _host);
                    }
                    void _install_Case_NotLapDiffTop_rebarPositionPrevTarget_Null_rebarPositionNextTarget_Valid(bool isLapDiffBotImportant = false)
                    {
                        //var p1 = index == 0
                        //        ? item.Position - vtZ * anchor
                        //        : item.Position - vtZ * (isLapDiffBot ? anchor : 0);
                        var p1 = item.Position - vtZ * anchor;
                        var p2 = item.Position + vtZ * (length - face.HeightBeamZone.FromMillimeters());
                        var p3 = rebarPositionNextTarget.Position;
                        var p4 = rebarPositionNextTarget.Position + vtZ * lapLengthGap;

                        var fNor = Plane.CreateByNormalAndOrigin(face.Plane.Normal.CrossProduct(vtZ), item.Position);
                        var distanceCheck = rebarPositionNextTarget.Position
                            .RayIntersectPlane(fNor.Normal, fNor)
                            .DistanceTo(rebarPositionNextTarget.Position)
                            .FootToMm();
                        if(distanceCheck > _distanceCheckLimit)
                        {
                            _install_Case_LapDiffTop();
                            return;
                        }
                        var ps = index == 0 ?
                        new List<XYZ>() { p1 - vtY * minHook, p1, p2.Add(face.HeightBeamZone <= 5 ? vtZ * lapLengthGap : vtZ * 0.0), face.HeightBeamZone <= 5 ? null : p3, face.HeightBeamZone <= 5 ? null : p4 }
                        : isLapDiffBotImportant ? new List<XYZ>() { p1, p2.Add(face.HeightBeamZone <= 5 ? vtZ * lapLengthGap : vtZ * 0.0), face.HeightBeamZone <= 5 ? null : p3, face.HeightBeamZone <= 5 ? null : p4 }
                        : new List<XYZ>() { p1 + vtZ * (isLapDiffBot ? 0 : lapLengthGapPrev), p2.Add(face.HeightBeamZone <= 5 ? vtZ * lapLengthGap : vtZ * 0.0), face.HeightBeamZone <= 5 ? null : p3, face.HeightBeamZone <= 5 ? null : p4 };
                        var cv = ps.PointsToCurves();
                        var isRebarFreeForm = RebarHelper.IsRebarFreeForm(cv, out XYZ normal);
                        if (isRebarFreeForm)
                            RebarHelper.CreateRebar(_document, cv, $"D{Math.Round(face.Diameter, 0)}", "A", _rebarBarTypes, _host);
                        else
                            RebarHelper.CreateRebar(_document, cv, $"D{Math.Round(face.Diameter, 0)}", normal, _rebarBarTypes, _host);
                    }
                    void _install_Case_NotLapDiffTop_rebarPositionPrevTarget_Valid_rebarPositionNextTarget_Null()
                    {
                        var p1 = index == 0
                                ? item.Position - vtZ * anchor
                                : item.Position - vtZ * (isLapDiffBot ? anchor : 0);
                        var p2 = item.Position + vtZ * (length + lapLengthGap);

                        var fNor1 = Plane.CreateByNormalAndOrigin(face.Plane.Normal.CrossProduct(vtZ), item.Position);
                        var distanceCheck1 = rebarPositionPrevTarget.Position
                            .RayIntersectPlane(fNor1.Normal, fNor1)
                            .DistanceTo(rebarPositionPrevTarget.Position)
                            .FootToMm();
                        if (distanceCheck1 > _distanceCheckLimit)
                        {
                            _install_Case_NotLapDiffTop_rebarPositionPrevTarget_Null_rebarPositionNextTarget_Null(true);
                            return;
                        }

                        var ps = index == 0 ?
                        new List<XYZ>() { p1 - vtY * minHook, p1, p2 }
                        : new List<XYZ>() { p1 + vtZ * (isLapDiffBot ? 0 : lapLengthGapPrev), p2 };
                        var cv = ps.PointsToCurves();
                        var isRebarFreeForm = RebarHelper.IsRebarFreeForm(cv, out XYZ normal);
                        if (isRebarFreeForm)
                            RebarHelper.CreateRebar(_document, cv, $"D{Math.Round(face.Diameter, 0)}", "A", _rebarBarTypes, _host);
                        else
                            RebarHelper.CreateRebar(_document, cv, $"D{Math.Round(face.Diameter, 0)}", normal, _rebarBarTypes, _host);
                    }
                    void _install_Case_NotLapDiffTop_rebarPositionPrevTarget_Valid_rebarPositionNextTarget_Valid()
                    {
                        var p1 = index == 0
                                ? item.Position - vtZ * anchor
                                : item.Position - vtZ * (isLapDiffBot ? anchor : 0);
                        var p2 = item.Position + vtZ * (length - face.HeightBeamZone.FromMillimeters());
                        var p3 = rebarPositionNextTarget.Position;
                        var p4 = rebarPositionNextTarget.Position + vtZ * lapLengthGap;

                        var fNor = Plane.CreateByNormalAndOrigin(face.Plane.Normal.CrossProduct(vtZ), item.Position);
                        var distanceCheck = rebarPositionNextTarget.Position
                            .RayIntersectPlane(fNor.Normal, fNor)
                            .DistanceTo(rebarPositionNextTarget.Position)
                            .FootToMm();
                        if (distanceCheck > _distanceCheckLimit)
                        {
                            _install_Case_LapDiffTop();
                            return;
                        }

                        var fNor1 = Plane.CreateByNormalAndOrigin(face.Plane.Normal.CrossProduct(vtZ), item.Position);
                        var distanceCheck1 = rebarPositionPrevTarget.Position
                            .RayIntersectPlane(fNor1.Normal, fNor1)
                            .DistanceTo(rebarPositionPrevTarget.Position)
                            .FootToMm();
                        if (distanceCheck1 > _distanceCheckLimit)
                        {
                            _install_Case_NotLapDiffTop_rebarPositionPrevTarget_Null_rebarPositionNextTarget_Valid(true);
                            return;
                        }

                        var ps = index == 0 ?
                        new List<XYZ>() { p1 - vtY * minHook, p1, p2.Add(face.HeightBeamZone <= 5 ? vtZ * lapLengthGap : vtZ * 0.0), face.HeightBeamZone <= 5 ? null : p3, face.HeightBeamZone <= 5 ? null : p4 }
                        : new List<XYZ>() { p1 + vtZ * (isLapDiffBot ? 0 : lapLengthGapPrev), p2.Add(face.HeightBeamZone <= 5 ? vtZ * lapLengthGap : vtZ * 0.0), face.HeightBeamZone <= 5 ? null : p3, face.HeightBeamZone <= 5 ? null : p4 };
                        var cv = ps.PointsToCurves();
                        var isRebarFreeForm = RebarHelper.IsRebarFreeForm(cv, out XYZ normal);
                        if (isRebarFreeForm)
                            RebarHelper.CreateRebar(_document, cv, $"D{Math.Round(face.Diameter, 0)}", "A", _rebarBarTypes, _host);
                        else
                            RebarHelper.CreateRebar(_document, cv, $"D{Math.Round(face.Diameter, 0)}", normal, _rebarBarTypes, _host);
                    }
                }
            }
            rebarPoss = rebarPositions;
        }
        private void CreateRebarColumn_Multi_Last(
            List<ColumnFaceModel> faces,
            bool ignoreFirstEnd,
            out List<List<ColumnRebarPositionModel>> rebarPoss)
        {
            rebarPoss = new List<List<ColumnRebarPositionModel>>();
            var result = new List<List<Curve>>();
            var face = faces.Last();
            var faceCount = faces.Count;
            var maxQty = faces.Max(x => x.RebarQty);
            var faceType = (ColumnFaceType)face.FaceType;
            var facePrev = faces[faceCount - 2];
            var isOddPrev = CheckPositionSole(faceType, facePrev);
            var diameter = face.Diameter.FromMillimeters();
            var minHook = _settingRebarStandardModel.HMin * diameter;
            var anchor = _settingRebarStandardModel.L2 * diameter;
            var diameterPrev = facePrev.Diameter.FromMillimeters();
            var lapLengthPrev = _settingRebarStandardModel.L1 * diameterPrev;
            var gapLapPrev = _settingRebarStandardModel.G * diameterPrev;
            var cover = face.Cover.FromMillimeters();
            var vtX = (face.Pb2 - face.Pb1).Normalize();
            var vtY = -face.Plane.Normal;
            var vtZ = XYZ.BasisZ;
            var sp = face.Pb1 + vtY * cover + vtX * cover;
            var ep = face.Pb2 + vtY * cover - vtX * cover;
            var length = face.Pb1.DistanceTo(face.Pt1);
            var rebarPositions = SolvePositionInstallRebar(sp, ep,
                int.Parse(Math.Round(face.RebarQty, 0).ToString()),
                int.Parse(Math.Round(maxQty, 0).ToString()), face);
            var coverPrev = facePrev.Cover.FromMillimeters();
            var vtXPrev = (facePrev.Pb2 - facePrev.Pb1).Normalize();
            var vtYPrev = -facePrev.Plane.Normal;
            var spPrev = facePrev.Pb1 + vtYPrev * coverPrev + vtXPrev * coverPrev;
            var epPrev = facePrev.Pb2 + vtYPrev * coverPrev - vtXPrev * coverPrev;
            var rebarPositionsPrev = SolvePositionInstallRebar(spPrev, epPrev,
                int.Parse(Math.Round(facePrev.RebarQty, 0).ToString()),
                int.Parse(Math.Round(maxQty, 0).ToString()), facePrev);
            rebarPoss.Add(rebarPositions);
            var rbCount = rebarPositions.Count;
            var isLapDiff = IsDifferentFace(face, facePrev, out double distanceTop);
            foreach (var rebarPosition in rebarPositions)
            {
                var index = rebarPositions.IndexOf(rebarPosition);
                if (ignoreFirstEnd && (index == 0 || index == rbCount - 1))
                    continue;
                var rebarPositionPrevTarget = rebarPositionsPrev.FirstOrDefault(
                    x => x.Index == rebarPosition.Index);
                var numberOfPrev = rebarPositionPrevTarget == null
                    ? -1
                    : rebarPositionsPrev.IndexOf(rebarPositionPrevTarget);
                var condit1Prev = numberOfPrev % 2 == 0;
                var isSolePrev = rebarPositionPrevTarget != null
                    && (!condit1Prev && !isOddPrev ? true : condit1Prev && isOddPrev ? true : false);


                var fNor1 = Plane.CreateByNormalAndOrigin(face.Plane.Normal.CrossProduct(vtZ), rebarPosition.Position);
                var distanceCheck1 = rebarPositionPrevTarget.Position
                    .RayIntersectPlane(fNor1.Normal, fNor1)
                    .DistanceTo(rebarPositionPrevTarget.Position)
                    .FootToMm();
                if (distanceCheck1 > _distanceCheckLimit) isLapDiff = true;

                var rbStart = isLapDiff
                    ? rebarPosition.Position - vtZ * anchor
                    : rebarPosition.Position + vtZ * (isSolePrev ? lapLengthPrev + gapLapPrev : 0);
                var rbEnd = rebarPosition.Position + vtZ * (length - face.CoverBase.FromMillimeters());
                var shape = new List<XYZ>()
                {
                    rbStart,
                    rbEnd,
                    rbEnd + vtY * minHook
                };
                result.Add(shape.PointsToCurves());
            }
            foreach (var cv in result)
            {
                var isRebarFreeForm = RebarHelper.IsRebarFreeForm(cv, out XYZ normal);
                if (isRebarFreeForm)
                    RebarHelper.CreateRebar(_document, cv, $"D{Math.Round(face.Diameter, 0)}", "A", _rebarBarTypes, _host);
                else
                    RebarHelper.CreateRebar(_document, cv, $"D{Math.Round(face.Diameter, 0)}", normal, _rebarBarTypes, _host);
            }
        }
        private bool IsDifferentFace(ColumnFaceModel fs, ColumnFaceModel fe, out double distance)
        {
            var e = _e;
            var pCheck = fs.Pt1;
            var pInterSec = pCheck.RayIntersectPlane(fe.Plane.Normal, fe.Plane);
            distance = Math.Round(pCheck.DistanceTo(pInterSec).ToMillimeters(), 0);
            return distance >= e;
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
                    var p = start.MidPoint(end);
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
        private bool CheckPositionSole(ColumnFaceType faceType, ColumnFaceModel face)
        {
            var isOddLeft = true;
            var isOddBot = true;
            var isOddRight = true;
            var isOddTop = true;
            var isOdd = true;
            switch (faceType)
            {
                case ColumnFaceType.Left:
                    isOddLeft = true;
                    isOdd = isOddLeft;
                    break;
                case ColumnFaceType.Bottom:
                    isOddLeft = true;
                    isOddBot = face.RebarQtyNext % 2 != 0;
                    isOdd = isOddBot;
                    break;
                case ColumnFaceType.Right:
                    isOddLeft = true;
                    isOddBot = face.RebarQty % 2 != 0;
                    isOddRight = !isOddBot ? face.RebarQtyNext % 2 == 0 : face.RebarQtyNext % 2 != 0;
                    isOdd = isOddRight;
                    break;
                case ColumnFaceType.Top:
                    isOddLeft = true;
                    isOddBot = face.RebarQtyNext % 2 != 0;
                    isOddRight = !isOddBot ? face.RebarQty % 2 == 0 : face.RebarQty % 2 != 0;
                    isOddTop = isOddRight ? face.RebarQtyNext % 2 == 0 ? false : true : face.RebarQtyNext % 2 == 0 ? true : false;
                    isOdd = isOddTop;
                    break;
            }
            return isOdd;
        }
    }
}
