using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using LSTool.Compatibility;
using LSTool.MVVM.model.SettingRebarColumnDatas;
using LSTool.MVVM.model.Structures;
using LSTool.Tools.Columns.CreateColumn.model;
using LSTool.Tools.Columns.CreateColumn.types;
using LSTool.Utils;

namespace LSTool.Tools.Columns.CreateColumn.action
{
    public class ColumnRebarMainAction
    {
        private UIDocument _uidocument;
        private Document _document;
        private List<RebarBarType> _rebarBarTypes;
        private Element _host;
        private SettingFrameModel _settingRebarColumnModel;
        private List<RebarLapLengthModel> _rebarLapLengthModels;
        private List<RebarAnchorageHookMainBarModel> _rebarAnchorageHookMainBarModels;
        private List<RebarAnchorageLengthModel> _rebarAnchorageLengthModels;
        private List<ColumnConcreteModel> _columnConcreteModels;

        public ColumnRebarMainAction(
            UIDocument uidocument,
            Element host,
            SettingFrameModel settingRebarColumnModel,
            List<RebarLapLengthModel> rebarLapLengthModels,
            List<RebarAnchorageHookMainBarModel> rebarAnchorageHookMainBarModels,
            List<RebarAnchorageLengthModel> rebarAnchorageLengthModels)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            _host = host;
            _settingRebarColumnModel = settingRebarColumnModel;
            _rebarLapLengthModels = rebarLapLengthModels;
            _rebarAnchorageHookMainBarModels = rebarAnchorageHookMainBarModels;
            _rebarAnchorageLengthModels = rebarAnchorageLengthModels;
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
            _columnConcreteModels = cCols;
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

            var faceLefts  = cCols.Select(x => x.FaceLeft).ToList();
            var faceTops   = cCols.Select(x => x.FaceTop).ToList();
            var faceRights = cCols.Select(x => x.FaceRight).ToList();
            var faceBots   = cCols.Select(x => x.FaceBottom).ToList();
            var rebarPositions = new List<List<ColumnRebarPositionModel>>();
            var facess = new List<List<ColumnFaceModel>>() { faceLefts, faceTops, faceRights, faceBots };

            rebarPositions.AddRange(InstallRebarFace(faceLefts,  facess, ignoreFirstEnd: true));
            rebarPositions.AddRange(InstallRebarFace(faceBots,   facess));
            rebarPositions.AddRange(InstallRebarFace(faceRights, facess, ignoreFirstEnd: true));
            rebarPositions.AddRange(InstallRebarFace(faceTops,   facess));

            foreach (var col in cCols)
            {
                var positions = rebarPositions
                    .Where(x => x.FirstOrDefault()?.HostId == col.Id)
                    .ToList();
                if (!positions.Any()) continue;
                col.RebarMainPositionss = positions;
            }
        }

        private List<List<ColumnRebarPositionModel>> InstallRebarFace(
            List<ColumnFaceModel> faces,
            List<List<ColumnFaceModel>> facess,
            bool ignoreFirstEnd = false)
        {
            var result = new List<List<ColumnRebarPositionModel>>();
            if (faces == null || !faces.Any()) return result;

            if (faces.Count == 1)
                InstallRebarFace_Column_Single(faces, ignoreFirstEnd);
            else
                InstallRebarFace_Column_Multi(faces, facess, ignoreFirstEnd);

            return result;
        }

        private void InstallRebarFace_Column_Single(
            List<ColumnFaceModel> faces,
            bool ignoreFirstEnd)
        {
            if (faces == null || !faces.Any()) return;
            var face = faces.First();

            var diameterMm = face.Diameter;
            var diameterFt = diameterMm.FromMillimeters();
            var cover      = face.Cover.FromMillimeters();

            var hookModel = _rebarAnchorageHookMainBarModels?
                .OrderBy(x => Math.Abs(x.Diameter - (int)Math.Round(diameterMm)))
                .FirstOrDefault();
            var minHookFt = hookModel != null ? hookModel.B.FromMillimeters() : 6.0 * diameterFt;

            var anchorModel    = _rebarAnchorageLengthModels?.FirstOrDefault();
            var colModel       = _columnConcreteModels?.FirstOrDefault(x => x.Id == face.HostId);
            var bottomColModel = _columnConcreteModels?.FirstOrDefault();
            var isBottomColumn = colModel != null && colModel.Id == bottomColModel?.Id;
            double anchorFoundationFt = isBottomColumn
                ? colModel!.GetAnchorFoundationFt(_document, diameterMm, anchorModel)
                : 0;

            var vtX = (face.Pb2 - face.Pb1).Normalize();
            var vtY = -face.Plane.Normal;
            var vtZ = XYZ.BasisZ;

            var sp = face.Pb1 + vtY * cover + vtX * cover;
            var ep = face.Pb2 + vtY * cover - vtX * cover;

            var rebarPositions = SolvePositionInstallRebar(
                sp, ep,
                qty:      (int)Math.Round(face.RebarQty),
                maxQty:   (int)Math.Round(face.RebarQtyNext),
                hostFace: face);
            var rbCount = rebarPositions.Count;

            foreach (var rebarPosition in rebarPositions)
            {
                try
                {
                    var idx = rebarPositions.IndexOf(rebarPosition);
                    if (ignoreFirstEnd && (idx == 0 || idx == rbCount - 1)) continue;

                    var posXY   = rebarPosition.Position;
                    var rbStart = new XYZ(posXY.X, posXY.Y, face.Pb1.Z);
                    var rbEnd   = new XYZ(posXY.X, posXY.Y, face.Pt1.Z - face.CoverBase.FromMillimeters());

                    List<XYZ> shape;
                    if (isBottomColumn)
                    {
                        var pAnchorBot = rbStart - vtZ * anchorFoundationFt;
                        shape = new List<XYZ>()
                        {
                            pAnchorBot - vtY * minHookFt,
                            pAnchorBot,
                            rbStart,
                            rbEnd,
                            rbEnd + vtY * minHookFt
                        };
                    }
                    else
                    {
                        shape = new List<XYZ>()
                        {
                            rbStart,
                            rbEnd,
                            rbEnd + vtY * minHookFt
                        };
                    }

                    var curves = shape.PointsToCurves();
                    var isRebarFreeForm = RebarHelper.IsRebarFreeForm(curves, out XYZ normal);
                    if (isRebarFreeForm)
                        RebarHelper.CreateRebar(_document, curves, $"D{Math.Round(diameterMm, 0)}", "A",     _rebarBarTypes, _host);
                    else
                        RebarHelper.CreateRebar(_document, curves, $"D{Math.Round(diameterMm, 0)}", normal, _rebarBarTypes, _host);
                }
                catch (Exception) { }
            }
        }

        private void InstallRebarFace_Column_Multi(
            List<ColumnFaceModel> faces,
            List<List<ColumnFaceModel>> facess,
            bool ignoreFirstEnd)
        {
            InstallRebarFace_Column_Base(faces, facess, ignoreFirstEnd);
            InstallRebarFace_Column_Mid(faces,  facess, ignoreFirstEnd);
            InstallRebarFace_Column_Roof(faces, facess, ignoreFirstEnd);
        }

        private void InstallRebarFace_Column_Base(
            List<ColumnFaceModel> faces,
            List<List<ColumnFaceModel>> facess,
            bool ignoreFirstEnd)
        {
            if (faces == null || faces.Count < 2) return;

            var face     = faces[0];
            var faceNext = faces[1];

            var diameterMm  = face.Diameter;
            var diameterFt  = diameterMm.FromMillimeters();
            var diameterNMm = faceNext.Diameter;
            var diameterNFt = diameterNMm.FromMillimeters();

            var hookModel = _rebarAnchorageHookMainBarModels
                .OrderBy(x => Math.Abs(x.Diameter - (int)Math.Round(diameterMm)))
                .FirstOrDefault();
            var minHookFt = hookModel != null ? hookModel.B.FromMillimeters() : 6.0 * diameterFt;

            var anchorModel        = _rebarAnchorageLengthModels?.FirstOrDefault();
            var colModel           = _columnConcreteModels?.FirstOrDefault(x => x.Id == face.HostId);
            var anchorFoundationFt = colModel != null
                ? colModel.GetAnchorFoundationFt(_document, diameterMm, anchorModel)
                : anchorModel != null
                    ? (anchorModel.Ldt * diameterMm).FromMillimeters()
                    : 30.0 * diameterFt;

            var lapModel     = _rebarLapLengthModels?.FirstOrDefault();
            var lapLengthNFt = lapModel != null ? (lapModel.Lst * diameterNMm).FromMillimeters() : 30.0 * diameterNFt;
            var gapLapNFt    = lapModel != null ? (lapModel.Gap * diameterNMm).FromMillimeters() : 5.0  * diameterNFt;

            var e0Mm = (_settingRebarColumnModel?.E0.FromMillimeters() ?? 25.0.FromMillimeters()).ToMillimeters();

            var cover  = face.Cover.FromMillimeters();
            var vtX    = (face.Pb2 - face.Pb1).Normalize();
            var vtY    = -face.Plane.Normal;
            var vtZ    = XYZ.BasisZ;
            var coverN = faceNext.Cover.FromMillimeters();
            var vtXN   = (faceNext.Pb2 - faceNext.Pb1).Normalize();
            var vtYN   = -faceNext.Plane.Normal;

            var qtyMax = (int)Math.Round(Math.Max(face.RebarQty, faceNext.RebarQty));
            var sp  = face.Pb1     + vtY  * cover  + vtX  * cover;
            var ep  = face.Pb2     + vtY  * cover  - vtX  * cover;
            var spN = faceNext.Pb1 + vtYN * coverN + vtXN * coverN;
            var epN = faceNext.Pb2 + vtYN * coverN - vtXN * coverN;

            var rebarPositions     = SolvePositionInstallRebar(sp,  ep,  (int)Math.Round(face.RebarQty),     qtyMax, face);
            var rebarPositionsNext = SolvePositionInstallRebar(spN, epN, (int)Math.Round(faceNext.RebarQty), qtyMax, faceNext);
            var rbCount = rebarPositions.Count;

            var isLapDiff = facess.Any(faceList =>
            {
                if (faceList == null || faceList.Count < 2) return false;
                var fBot = faceList[0];
                var fTop = faceList[1];
                if (fBot?.Pt1 == null || fTop?.Plane == null) return false;
                var pC = fBot.Pt1;
                var pI = pC.RayIntersectPlane(fTop.Plane.Normal, fTop.Plane);
                return Math.Round(pC.DistanceTo(pI).ToMillimeters(), 0) >= e0Mm;
            }) || Math.Abs(diameterMm - diameterNMm) > 0.5;

            var isOdd = CheckPositionSole((ColumnFaceType)face.FaceType, face);

            foreach (var rebarPosition in rebarPositions)
            {
                try
                {
                    var idx = rebarPositions.IndexOf(rebarPosition);
                    if (ignoreFirstEnd && (idx == 0 || idx == rbCount - 1)) continue;

                    var posXY   = rebarPosition.Position;
                    var rbStart = new XYZ(posXY.X, posXY.Y, face.Pb1.Z);
                    var rbEnd   = new XYZ(posXY.X, posXY.Y, face.Pt1.Z - face.CoverBase.FromMillimeters());
                    var posNext = rebarPositionsNext.FirstOrDefault(x => x.Index == rebarPosition.Index);

                    var condit1        = idx % 2 == 0;
                    var isSole         = (!condit1 && !isOdd) || (condit1 && isOdd);
                    var lapLengthGapFt = lapLengthNFt + (isSole ? lapLengthNFt + gapLapNFt : 0);
                    var pAnchorBot     = rbStart - vtZ * anchorFoundationFt;

                    List<XYZ> shape;
                    if (isLapDiff)
                    {
                        shape = new List<XYZ>()
                        {
                            pAnchorBot - vtY * minHookFt,
                            pAnchorBot,
                            rbStart,
                            rbEnd,
                            rbEnd + vtY * minHookFt
                        };
                    }
                    else
                    {
                        var beamZoneFt   = face.HeightBeamZone.FromMillimeters();
                        var coverNBaseFt = faceNext.CoverBase.FromMillimeters();

                        if (posNext != null)
                        {
                            var posNXY = posNext.Position;
                            var p2 = new XYZ(posXY.X,  posXY.Y,  face.Pt1.Z - beamZoneFt);
                            var p3 = new XYZ(posNXY.X, posNXY.Y, faceNext.Pb1.Z + coverNBaseFt);
                            var p4 = new XYZ(posNXY.X, posNXY.Y, faceNext.Pb1.Z + coverNBaseFt + lapLengthGapFt);
                            shape = beamZoneFt <= (5.0).FromMillimeters()
                                ? new List<XYZ>() { pAnchorBot - vtY * minHookFt, pAnchorBot, rbStart, p2 + vtZ * lapLengthGapFt }
                                : new List<XYZ>() { pAnchorBot - vtY * minHookFt, pAnchorBot, rbStart, p2, p3, p4 };
                        }
                        else
                        {
                            var nearestWithMatch = rebarPositions
                                .Where(x => rebarPositionsNext.Any(n => n.Index == x.Index))
                                .OrderBy(x => Math.Abs(x.Index - rebarPosition.Index))
                                .FirstOrDefault();
                            XYZ deltaXY;
                            if (nearestWithMatch != null)
                            {
                                var matchInNext = rebarPositionsNext.First(x => x.Index == nearestWithMatch.Index);
                                deltaXY = new XYZ(
                                    matchInNext.Position.X - nearestWithMatch.Position.X,
                                    matchInNext.Position.Y - nearestWithMatch.Position.Y, 0);
                            }
                            else
                                deltaXY = XYZ.Zero;

                            var tgtX = posXY.X + deltaXY.X;
                            var tgtY = posXY.Y + deltaXY.Y;
                            shape = beamZoneFt <= (5.0).FromMillimeters()
                                ? new List<XYZ>()
                                  {
                                      pAnchorBot - vtY * minHookFt, pAnchorBot, rbStart,
                                      new XYZ(tgtX, tgtY, faceNext.Pb1.Z + coverNBaseFt + lapLengthGapFt)
                                  }
                                : new List<XYZ>()
                                  {
                                      pAnchorBot - vtY * minHookFt,
                                      pAnchorBot,
                                      rbStart,
                                      new XYZ(posXY.X, posXY.Y, face.Pt1.Z - beamZoneFt),
                                      new XYZ(tgtX, tgtY, faceNext.Pb1.Z + coverNBaseFt),
                                      new XYZ(tgtX, tgtY, faceNext.Pb1.Z + coverNBaseFt + lapLengthGapFt)
                                  };
                        }
                    }

                    var curves = shape.Where(p => p != null).ToList().PointsToCurves();
                    var isRebarFreeForm = RebarHelper.IsRebarFreeForm(curves, out XYZ normal);
                    if (isRebarFreeForm)
                        RebarHelper.CreateRebar(_document, curves, $"D{Math.Round(diameterMm, 0)}", "A",     _rebarBarTypes, _host);
                    else
                        RebarHelper.CreateRebar(_document, curves, $"D{Math.Round(diameterMm, 0)}", normal, _rebarBarTypes, _host);
                }
                catch (Exception) { }
            }
        }

        private void InstallRebarFace_Column_Roof(
            List<ColumnFaceModel> faces,
            List<List<ColumnFaceModel>> facess,
            bool ignoreFirstEnd)
        {
            if (faces == null || faces.Count < 2) return;

            var face     = faces.Last();
            var facePrev = faces[faces.Count - 2];

            var diameterMm  = face.Diameter;
            var diameterFt  = diameterMm.FromMillimeters();
            var diameterPMm = facePrev.Diameter;
            var diameterPFt = diameterPMm.FromMillimeters();

            var hookModel = _rebarAnchorageHookMainBarModels
                .OrderBy(x => Math.Abs(x.Diameter - (int)Math.Round(diameterMm)))
                .FirstOrDefault();
            var minHookFt = hookModel != null ? hookModel.B.FromMillimeters() : 6.0 * diameterFt;

            var anchorModel = _rebarAnchorageLengthModels?.FirstOrDefault();
            var anchorFt    = anchorModel != null
                ? (anchorModel.Ldt * diameterMm).FromMillimeters()
                : 30.0 * diameterFt;

            var lapModel        = _rebarLapLengthModels?.FirstOrDefault();
            var lapLengthPrevFt = lapModel != null ? (lapModel.Lst * diameterPMm).FromMillimeters() : 30.0 * diameterPFt;
            var gapLapPrevFt    = lapModel != null ? (lapModel.Gap * diameterPMm).FromMillimeters() : 5.0  * diameterPFt;

            var e0Mm = (_settingRebarColumnModel?.E0.FromMillimeters() ?? 25.0.FromMillimeters()).ToMillimeters();

            var isLapDiff = facess.Any(faceList =>
            {
                if (faceList == null || faceList.Count < 2) return false;
                var fi   = faceList.Count - 1;
                var fBot = faceList[fi - 1];
                var fTop = faceList[fi];
                if (fBot?.Pt1 == null || fTop?.Plane == null) return false;
                var pC = fBot.Pt1;
                var pI = pC.RayIntersectPlane(fTop.Plane.Normal, fTop.Plane);
                return Math.Round(pC.DistanceTo(pI).ToMillimeters(), 0) >= e0Mm;
            }) || Math.Abs(diameterMm - diameterPMm) > 0.5;

            var cover     = face.Cover.FromMillimeters();
            var coverBase = face.CoverBase.FromMillimeters();
            var vtX  = (face.Pb2 - face.Pb1).Normalize();
            var vtY  = -face.Plane.Normal;
            var coverP = facePrev.Cover.FromMillimeters();
            var vtXP   = (facePrev.Pb2 - facePrev.Pb1).Normalize();
            var vtYP   = -facePrev.Plane.Normal;

            var qtyMax = (int)Math.Round(Math.Max(face.RebarQty, facePrev.RebarQty));
            var sp  = face.Pb1     + vtY  * cover  + vtX  * cover;
            var ep  = face.Pb2     + vtY  * cover  - vtX  * cover;
            var spP = facePrev.Pb1 + vtYP * coverP + vtXP * coverP;
            var epP = facePrev.Pb2 + vtYP * coverP - vtXP * coverP;

            var rebarPositions     = SolvePositionInstallRebar(sp,  ep,  (int)Math.Round(face.RebarQty),     qtyMax, face);
            var rebarPositionsPrev = SolvePositionInstallRebar(spP, epP, (int)Math.Round(facePrev.RebarQty), qtyMax, facePrev);
            var rbCount = rebarPositions.Count;

            var isOddPrev = CheckPositionSole((ColumnFaceType)facePrev.FaceType, facePrev);

            foreach (var rebarPosition in rebarPositions)
            {
                try
                {
                    var idx = rebarPositions.IndexOf(rebarPosition);
                    if (ignoreFirstEnd && (idx == 0 || idx == rbCount - 1)) continue;

                    var posXY   = rebarPosition.Position;
                    var rbTop   = new XYZ(posXY.X, posXY.Y, face.Pt1.Z - coverBase);
                    var hookTop = rbTop + vtY * minHookFt;

                    List<XYZ> shape;
                    if (isLapDiff)
                    {
                        shape = new List<XYZ>()
                        {
                            new XYZ(posXY.X, posXY.Y, face.Pb1.Z - anchorFt),
                            new XYZ(posXY.X, posXY.Y, face.Pb1.Z),
                            rbTop,
                            hookTop
                        };
                    }
                    else
                    {
                        var posPrev     = rebarPositionsPrev.FirstOrDefault(x => x.Index == rebarPosition.Index);
                        var idxInPrev   = posPrev != null ? rebarPositionsPrev.IndexOf(posPrev) : -1;
                        var condit1Prev = idxInPrev >= 0 && idxInPrev % 2 == 0;
                        var isSolePrev  = posPrev != null &&
                            ((!condit1Prev && !isOddPrev) || (condit1Prev && isOddPrev));
                        var soleOffsetFt = isSolePrev ? lapLengthPrevFt + gapLapPrevFt : 0.0;
                        var rbBot = new XYZ(posXY.X, posXY.Y, face.Pb1.Z + coverBase + soleOffsetFt);
                        shape = new List<XYZ>() { rbBot, rbTop, hookTop };
                    }

                    var curves = shape.Where(p => p != null).ToList().PointsToCurves();
                    var isRebarFreeForm = RebarHelper.IsRebarFreeForm(curves, out XYZ normal);
                    if (isRebarFreeForm)
                        RebarHelper.CreateRebar(_document, curves, $"D{Math.Round(diameterMm, 0)}", "A",     _rebarBarTypes, _host);
                    else
                        RebarHelper.CreateRebar(_document, curves, $"D{Math.Round(diameterMm, 0)}", normal, _rebarBarTypes, _host);
                }
                catch (Exception) { }
            }
        }

        private void InstallRebarFace_Column_Mid(
            List<ColumnFaceModel> faces,
            List<List<ColumnFaceModel>> facess,
            bool ignoreFirstEnd)
        {
            if (faces == null || faces.Count < 3) return;

            var fCount = faces.Count;
            var e0Mm   = (_settingRebarColumnModel?.E0.FromMillimeters() ?? 25.0.FromMillimeters()).ToMillimeters();

            for (int fi = 1; fi <= fCount - 2; fi++)
            {
                var face     = faces[fi];
                var facePrev = faces[fi - 1];
                var faceNext = faces[fi + 1];

                var diameterMm  = face.Diameter;
                var diameterFt  = diameterMm.FromMillimeters();
                var diameterPMm = facePrev.Diameter;
                var diameterPFt = diameterPMm.FromMillimeters();
                var diameterNMm = faceNext.Diameter;
                var diameterNFt = diameterNMm.FromMillimeters();

                var hookModel = _rebarAnchorageHookMainBarModels
                    .OrderBy(x => Math.Abs(x.Diameter - (int)Math.Round(diameterMm)))
                    .FirstOrDefault();
                var minHookFt = hookModel != null ? hookModel.B.FromMillimeters() : 6.0 * diameterFt;

                var anchorModel = _rebarAnchorageLengthModels?.FirstOrDefault();
                var anchorFt    = anchorModel != null
                    ? (anchorModel.Ldt * diameterMm).FromMillimeters()
                    : 30.0 * diameterFt;

                var lapModel        = _rebarLapLengthModels?.FirstOrDefault();
                var lapLengthPrevFt = lapModel != null ? (lapModel.Lst * diameterPMm).FromMillimeters() : 30.0 * diameterPFt;
                var gapLapPrevFt    = lapModel != null ? (lapModel.Gap * diameterPMm).FromMillimeters() : 5.0  * diameterPFt;
                var lapLengthNFt    = lapModel != null ? (lapModel.Lst * diameterNMm).FromMillimeters() : 30.0 * diameterNFt;
                var gapLapNFt       = lapModel != null ? (lapModel.Gap * diameterNMm).FromMillimeters() : 5.0  * diameterNFt;

                var isLapDiffTop = facess.Any(fl =>
                {
                    if (fl == null || fl.Count <= fi + 1) return false;
                    var fB = fl[fi]; var fT = fl[fi + 1];
                    if (fB?.Pt1 == null || fT?.Plane == null) return false;
                    var pI = fB.Pt1.RayIntersectPlane(fT.Plane.Normal, fT.Plane);
                    return Math.Round(fB.Pt1.DistanceTo(pI).ToMillimeters(), 0) >= e0Mm;
                }) || Math.Abs(diameterMm - diameterNMm) > 0.5;

                var isLapDiffBot = facess.Any(fl =>
                {
                    if (fl == null || fl.Count <= fi) return false;
                    var fB = fl[fi - 1]; var fT = fl[fi];
                    if (fB?.Pt1 == null || fT?.Plane == null) return false;
                    var pI = fB.Pt1.RayIntersectPlane(fT.Plane.Normal, fT.Plane);
                    return Math.Round(fB.Pt1.DistanceTo(pI).ToMillimeters(), 0) >= e0Mm;
                }) || Math.Abs(diameterMm - diameterPMm) > 0.5;

                var cover     = face.Cover.FromMillimeters();
                var coverBase = face.CoverBase.FromMillimeters();
                var vtX  = (face.Pb2     - face.Pb1    ).Normalize();
                var vtY  = -face.Plane.Normal;
                var coverN  = faceNext.Cover.FromMillimeters();
                var coverNB = faceNext.CoverBase.FromMillimeters();
                var vtXN = (faceNext.Pb2 - faceNext.Pb1).Normalize();
                var vtYN = -faceNext.Plane.Normal;
                var coverP  = facePrev.Cover.FromMillimeters();
                var vtXP = (facePrev.Pb2 - facePrev.Pb1).Normalize();
                var vtYP = -facePrev.Plane.Normal;
                var beamZoneFt = face.HeightBeamZone.FromMillimeters();

                var qtyMax = (int)Math.Round(new[] { face.RebarQty, facePrev.RebarQty, faceNext.RebarQty }.Max());
                var sp  = face.Pb1     + vtY  * cover  + vtX  * cover;
                var ep  = face.Pb2     + vtY  * cover  - vtX  * cover;
                var spN = faceNext.Pb1 + vtYN * coverN + vtXN * coverN;
                var epN = faceNext.Pb2 + vtYN * coverN - vtXN * coverN;
                var spP = facePrev.Pb1 + vtYP * coverP + vtXP * coverP;
                var epP = facePrev.Pb2 + vtYP * coverP - vtXP * coverP;

                var rebarPositions     = SolvePositionInstallRebar(sp,  ep,  (int)Math.Round(face.RebarQty),     qtyMax, face);
                var rebarPositionsNext = SolvePositionInstallRebar(spN, epN, (int)Math.Round(faceNext.RebarQty), qtyMax, faceNext);
                var rebarPositionsPrev = SolvePositionInstallRebar(spP, epP, (int)Math.Round(facePrev.RebarQty), qtyMax, facePrev);
                var rbCount = rebarPositions.Count;

                var isOdd     = CheckPositionSole((ColumnFaceType)face.FaceType,     face);
                var isOddPrev = CheckPositionSole((ColumnFaceType)facePrev.FaceType, facePrev);

                foreach (var rebarPosition in rebarPositions)
                {
                    try
                    {
                        var idx = rebarPositions.IndexOf(rebarPosition);
                        if (ignoreFirstEnd && (idx == 0 || idx == rbCount - 1)) continue;

                        var posXY = rebarPosition.Position;
                        var shape = new List<XYZ>();

                        if (isLapDiffBot)
                        {
                            shape.Add(new XYZ(posXY.X, posXY.Y, face.Pb1.Z - anchorFt));
                            shape.Add(new XYZ(posXY.X, posXY.Y, face.Pb1.Z));
                        }
                        else
                        {
                            var posPrev   = rebarPositionsPrev.FirstOrDefault(x => x.Index == rebarPosition.Index);
                            var idxInPrev = posPrev != null ? rebarPositionsPrev.IndexOf(posPrev) : -1;
                            var condit1P  = idxInPrev >= 0 && idxInPrev % 2 == 0;
                            var isSoleP   = posPrev != null && ((!condit1P && !isOddPrev) || (condit1P && isOddPrev));
                            var soleOff   = isSoleP ? lapLengthPrevFt + gapLapPrevFt : 0.0;
                            shape.Add(new XYZ(posXY.X, posXY.Y, face.Pb1.Z + coverBase + soleOff));
                        }

                        if (isLapDiffTop)
                        {
                            var rbTop = new XYZ(posXY.X, posXY.Y, face.Pt1.Z - coverBase);
                            shape.Add(rbTop);
                            shape.Add(rbTop + vtY * minHookFt);
                        }
                        else
                        {
                            var condit1  = idx % 2 == 0;
                            var isSole   = (!condit1 && !isOdd) || (condit1 && isOdd);
                            var lapGapFt = lapLengthNFt + (isSole ? lapLengthNFt + gapLapNFt : 0);
                            var posNext  = rebarPositionsNext.FirstOrDefault(x => x.Index == rebarPosition.Index);

                            XYZ tgtXY;
                            if (posNext != null)
                            {
                                tgtXY = posNext.Position;
                            }
                            else
                            {
                                var nearestWithMatch = rebarPositions
                                    .Where(x => rebarPositionsNext.Any(n => n.Index == x.Index))
                                    .OrderBy(x => Math.Abs(x.Index - rebarPosition.Index))
                                    .FirstOrDefault();
                                XYZ delta;
                                if (nearestWithMatch != null)
                                {
                                    var matchInNext = rebarPositionsNext.First(x => x.Index == nearestWithMatch.Index);
                                    delta = new XYZ(
                                        matchInNext.Position.X - nearestWithMatch.Position.X,
                                        matchInNext.Position.Y - nearestWithMatch.Position.Y, 0);
                                }
                                else
                                    delta = XYZ.Zero;
                                tgtXY = new XYZ(posXY.X + delta.X, posXY.Y + delta.Y, 0);
                            }

                            if (beamZoneFt <= (5.0).FromMillimeters())
                                shape.Add(new XYZ(tgtXY.X, tgtXY.Y, faceNext.Pb1.Z + coverNB + lapGapFt));
                            else
                            {
                                shape.Add(new XYZ(posXY.X, posXY.Y, face.Pt1.Z - beamZoneFt));
                                shape.Add(new XYZ(tgtXY.X, tgtXY.Y, faceNext.Pb1.Z + coverNB));
                                shape.Add(new XYZ(tgtXY.X, tgtXY.Y, faceNext.Pb1.Z + coverNB + lapGapFt));
                            }
                        }

                        var curves = shape.Where(p => p != null).ToList().PointsToCurves();
                        var isRebarFreeForm = RebarHelper.IsRebarFreeForm(curves, out XYZ normal);
                        if (isRebarFreeForm)
                            RebarHelper.CreateRebar(_document, curves, $"D{Math.Round(diameterMm, 0)}", "A",     _rebarBarTypes, _host);
                        else
                            RebarHelper.CreateRebar(_document, curves, $"D{Math.Round(diameterMm, 0)}", normal, _rebarBarTypes, _host);
                    }
                    catch (Exception) { }
                }
            }
        }

        private bool CheckPositionSole(ColumnFaceType faceType, ColumnFaceModel face)
        {
            bool isOddBot   = true;
            bool isOddRight = true;
            bool isOddTop   = true;
            switch (faceType)
            {
                case ColumnFaceType.Left:
                    return true;
                case ColumnFaceType.Bottom:
                    isOddBot = face.RebarQtyNext % 2 != 0;
                    return isOddBot;
                case ColumnFaceType.Right:
                    isOddBot   = face.RebarQty % 2 != 0;
                    isOddRight = !isOddBot ? face.RebarQtyNext % 2 == 0 : face.RebarQtyNext % 2 != 0;
                    return isOddRight;
                case ColumnFaceType.Top:
                    isOddBot   = face.RebarQtyNext % 2 != 0;
                    isOddRight = !isOddBot ? face.RebarQty % 2 == 0 : face.RebarQty % 2 != 0;
                    isOddTop   = isOddRight ? face.RebarQtyNext % 2 != 0 : face.RebarQtyNext % 2 == 0;
                    return isOddTop;
                default:
                    return true;
            }
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
                var vt       = (end - start).Normalize();
                var distance = start.DistanceTo(end);
                var spacing  = distance / (maxQty - 1);
                var qtyDu    = qty % 2;
                var haft     = (qty - qtyDu) / 2;

                for (int i = 0; i < haft; i++)
                {
                    results.Add(new ColumnRebarPositionModel()
                    {
                        Index    = i + 1,
                        Position = start + i * spacing * vt,
                        Face     = hostFace.FaceType,
                        HostId   = hostFace.HostId
                    });
                }
                if (qtyDu == 1)
                {
                    results.Add(new ColumnRebarPositionModel()
                    {
                        Index    = 1 + maxQty / 2,
                        Position = start.MidPoint(end),
                        Face     = hostFace.FaceType,
                        HostId   = hostFace.HostId
                    });
                }
                for (int i = 0; i < haft; i++)
                {
                    results.Add(new ColumnRebarPositionModel()
                    {
                        Index    = maxQty - i,
                        Position = end - i * spacing * vt,
                        Face     = hostFace.FaceType,
                        HostId   = hostFace.HostId
                    });
                }
            }
            catch (Exception) { }

            if (!results.Any()) return results;
            return results.OrderBy(x => x.Index).ToList();
        }
    }
}
