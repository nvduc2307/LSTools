using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
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
        private Element _host;
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
        public void CreateRebarMain(List<ColumnConcreteModel> cCols)
        {
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

            InstallRebarFace(faceLefts, true);
            InstallRebarFace(faceBots);
            InstallRebarFace(faceRights, true);
            InstallRebarFace(faceTops);
        }
        private void InstallRebarFace(List<ColumnFaceModel> faces, bool ignoreFirstEnd = false)
        {
            var fCount = faces.Count;
            if (fCount == 1)
                CreateRebarColumn_Single(faces, ignoreFirstEnd);
            else
            {
                //Truong hop co nhieu cot
                CreateRebarColumn_Multi(faces, ignoreFirstEnd);
                CreateRebarColumn_Multi_Last(faces, ignoreFirstEnd);
            }
        }
        private void CreateRebarColumn_Single(List<ColumnFaceModel> faces, bool ignoreFirstEnd)
        {
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
                int.Parse(Math.Round(face.RebarQty, 0).ToString()));
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
                RebarHelper.CreateRebar(_document, cv, $"D{Math.Round(face.Diameter, 0)}", "A", _rebarBarTypes, _host);
            }
        }
        private void CreateRebarColumn_Multi(List<ColumnFaceModel> faces, bool ignoreFirstEnd)
        {
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
                    int.Parse(Math.Round(qtyMax, 0).ToString()));
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
                if (index == fCount - 1) continue;
                var length = face.Pb1.DistanceTo(face.Pt1);
                var rebarPosition = rebarPositions[index];
                var rebarPositionNext = rebarPositions[index + 1];
                var rebarPositionPrev = index == 0 ? null : rebarPositions[index - 1];
                var isLapDiff = IsDifferentFace(face, faces[index + 1]);
                var rbCount = rebarPosition.Count;
                foreach (var item in rebarPosition)
                {
                    var numberOf = rebarPosition.IndexOf(item);
                    if (ignoreFirstEnd && (numberOf == 0 || numberOf == rbCount - 1))
                        continue;
                    var condit1 = numberOf % 2 == 0;
                    var condit1Prev = item.Index % 2 == 0;
                    var isSole = !condit1 && !isOdd ? true : condit1 && isOdd ? true : false;
                    var isSolePrev = index == 0 ? false 
                        : !condit1Prev && !isOddPrev ? false : condit1Prev && isOddPrev ? false : true;
                    var lapLengthGap = lapLength + (isSole ? lapLength + gapLap : 0);
                    var rebarPositionNextTarget = rebarPositionNext.FirstOrDefault(x => x.Index == item.Index);
                    var rebarPositionPrevTarget = rebarPositionPrev == null 
                        ? null 
                        : rebarPositionPrev.FirstOrDefault(x => x.Index == item.Index);
                    if (isLapDiff)
                    {
                        var p1 = index == 0
                            ? item.Position - vtZ * anchor
                            : item.Position;
                        var p2 = item.Position + vtZ * (length - face.CoverBase.FromMillimeters());
                        var p3 = p2 - face.Plane.Normal * minHook;
                        var ps = index == 0 ?
                            new List<XYZ>() { p1 - vtY * minHook, p1, p2, p3 }
                            : new List<XYZ>() { p1 + vtZ * (isSolePrev ? lapLength + gapLap : 0), p2, p3 };
                        RebarHelper.CreateRebar(_document, ps.PointsToCurves(), $"D{Math.Round(face.Diameter, 0)}", "A", _rebarBarTypes, _host);
                    }
                    else
                    {
                        if (rebarPositionPrevTarget == null)
                        {
                            if (rebarPositionNextTarget == null)
                            {
                                var p1 = item.Position - vtZ * anchor;
                                var p2 = item.Position + vtZ * (length + lapLengthGap);
                                var ps = index == 0 ?
                                new List<XYZ>() { p1 - vtY * minHook, p1, p2 }
                                : new List<XYZ>() { p1 + vtZ * (isSolePrev ? lapLength + gapLap : 0), p2 };
                                RebarHelper.CreateRebar(_document, ps.PointsToCurves(), $"D{Math.Round(face.Diameter, 0)}", "A", _rebarBarTypes, _host);
                            }
                            else
                            {
                                var p1 = index == 0
                                ? item.Position - vtZ * anchor
                                : item.Position;
                                var p2 = item.Position + vtZ * (length - face.HeightBeamZone.FromMillimeters());
                                var p3 = rebarPositionNextTarget.Position;
                                var p4 = rebarPositionNextTarget.Position + vtZ * lapLengthGap;
                                var ps = index == 0 ?
                                new List<XYZ>() { p1 - vtY * minHook, p1, p2.Add(face.HeightBeamZone <= 5 ? vtZ * lapLengthGap : vtZ * 0.0), face.HeightBeamZone <= 5 ? null : p3, face.HeightBeamZone <= 5 ? null : p4 }
                                : new List<XYZ>() { p1 + vtZ * (isSolePrev ? lapLength + gapLap : 0), p2.Add(face.HeightBeamZone <= 5 ? vtZ * lapLengthGap : vtZ * 0.0), face.HeightBeamZone <= 5 ? null : p3, face.HeightBeamZone <= 5 ? null : p4 };
                                RebarHelper.CreateRebar(_document, ps.Where(x => x != null).ToList().PointsToCurves(), $"D{Math.Round(face.Diameter, 0)}", "A", _rebarBarTypes, _host);
                            }
                        }
                        else
                        {
                            if (rebarPositionNextTarget == null)
                            {
                                var p1 = item.Position;
                                var p2 = item.Position + vtZ * (length + lapLengthGap);
                                var ps = index == 0 ?
                                new List<XYZ>() { p1 - vtY * minHook, p1, p2 }
                                : new List<XYZ>() { p1 + vtZ * (isSolePrev ? lapLength + gapLap : 0), p2 };
                                RebarHelper.CreateRebar(_document, ps.PointsToCurves(), $"D{Math.Round(face.Diameter, 0)}", "A", _rebarBarTypes, _host);
                            }
                            else
                            {
                                var p1 = index == 0
                                ? item.Position - vtZ * anchor
                                : item.Position;
                                var p2 = item.Position + vtZ * (length - face.HeightBeamZone.FromMillimeters());
                                var p3 = rebarPositionNextTarget.Position;
                                var p4 = rebarPositionNextTarget.Position + vtZ * lapLengthGap;
                                var ps = index == 0 ?
                                new List<XYZ>() { p1 - vtY * minHook, p1, p2.Add(face.HeightBeamZone <= 5 ? vtZ * lapLengthGap : vtZ * 0.0), face.HeightBeamZone <= 5 ? null : p3, face.HeightBeamZone <= 5 ? null : p4 }
                                : new List<XYZ>() { p1 + vtZ * (isSolePrev ? lapLength + gapLap : 0), p2.Add(face.HeightBeamZone <= 5 ? vtZ * lapLengthGap : vtZ * 0.0), face.HeightBeamZone <= 5 ? null : p3, face.HeightBeamZone <= 5 ? null : p4 };
                                RebarHelper.CreateRebar(_document, ps.Where(x => x != null).ToList().PointsToCurves(), $"D{Math.Round(face.Diameter, 0)}", "A", _rebarBarTypes, _host);
                            }
                        }
                    }
                }
            }
        }
        private void CreateRebarColumn_Multi_Last(List<ColumnFaceModel> faces, bool ignoreFirstEnd)
        {
            var result = new List<List<Curve>>();
            var face = faces.Last();
            var faceCount = faces.Count;
            var maxQty = faces.Max(x => x.RebarQty);
            var faceType = (ColumnFaceType)face.FaceType;
            var isOdd = CheckPositionSole(faceType, face);
            var isOddPrev = CheckPositionSole(faceType, faces[faceCount - 2]);
            var diameter = face.Diameter.FromMillimeters();
            var lapLength = _settingRebarStandardModel.L1 * diameter;
            var minHook = _settingRebarStandardModel.HMin * diameter;
            var anchor = _settingRebarStandardModel.L2 * diameter;
            var gapLap = _settingRebarStandardModel.G * diameter;
            var cover = face.Cover.FromMillimeters();
            var vtX = (face.Pb2 - face.Pb1).Normalize();
            var vtY = -face.Plane.Normal;
            var vtZ = XYZ.BasisZ;
            var sp = face.Pb1 + vtY * cover + vtX * cover;
            var ep = face.Pb2 + vtY * cover - vtX * cover;
            var length = face.Pb1.DistanceTo(face.Pt1);
            var rebarPositions = SolvePositionInstallRebar(sp, ep,
                int.Parse(Math.Round(face.RebarQty, 0).ToString()),
                int.Parse(Math.Round(maxQty, 0).ToString()));
            var rbCount = rebarPositions.Count;
            var rebarPositionNext = rebarPositions[faceCount - 2];
            var isLapDiff = IsDifferentFace(face, faces[faceCount - 2]);
            foreach (var rebarPosition in rebarPositions)
            {
                var index = rebarPositions.IndexOf(rebarPosition);
                if (ignoreFirstEnd && (index == 0 || index == rbCount - 1))
                    continue;
                var condit1 = rebarPosition.Index % 2 == 0;
                var isSole = !condit1 && !isOddPrev ? false : condit1 && isOddPrev ? false : true;
                var lapLengthGap = lapLength + (isSole ? lapLength + gapLap : 0);
                var rbStart = isLapDiff
                    ? rebarPosition.Position - vtZ * anchor
                    : rebarPosition.Position + vtZ * (isSole ? lapLength + gapLap : 0);
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
                //_document.CreateCurves(cv);
                RebarHelper.CreateRebar(_document, cv, $"D{Math.Round(face.Diameter, 0)}", "A", _rebarBarTypes, _host);
            }
        }
        private bool IsDifferentFace(ColumnFaceModel fs, ColumnFaceModel fe)
        {
            var e = 100;
            var pCheck = fs.Pt1;
            var pInterSec = pCheck.RayIntersectPlane(fe.Plane.Normal, fe.Plane);
            var distance = Math.Round(pCheck.DistanceTo(pInterSec).ToMillimeters(), 0);
            return distance >= e;
        }
        private List<ColumnRebarPositionModel> SolvePositionInstallRebar(XYZ start, XYZ end, int qty, int maxQty)
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
                    results.Add(new ColumnRebarPositionModel() { Index = i + 1, Position = p });
                }
                if (qtyDu == 1)
                {
                    var p = start.MidPoint(end);
                    results.Add(new ColumnRebarPositionModel() { Index = 1 + maxQty / 2, Position = p });
                }
                for (int i = 0; i < haft; i++)
                {
                    var p = end - i * spacing * vt;
                    results.Add(new ColumnRebarPositionModel() { Index = maxQty - i, Position = p });
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
