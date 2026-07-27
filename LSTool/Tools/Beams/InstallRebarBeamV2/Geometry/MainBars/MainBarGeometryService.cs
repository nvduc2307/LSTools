using Autodesk.Revit.DB;
using HcBimUtils;
using HcBimUtils.DocumentUtils;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using RIMT.Utils;
using RIMT.Utils.BoundingBoxs;
using RIMT.Utils.Compares;
using RIMT.Utils.Geometries;
using RIMT.Utils.RevPoints;
using RIMT.Utils.Solids;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.service
{
    public partial class SubInstallRebarBeamInModelService
    {
        public List<MainBarBeamReal> GetMainBarBeamReals(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            RebarBeamMainBarLevelType rebarBeamMainBarLevelType,
            RebarBeamMainBarGroupType rebarBeamMainBarGroupType,
            double extentCoverSide)
        {
            try
            {
                var rebarBeams = installRebarBeamV2ViewModel.ElementInstances.RebarBeams;
                var subBeams = installRebarBeamV2ViewModel.ElementInstances.Beam.ElementSubs;
                var qRebarBeams = rebarBeams.Count;
                var rebarStirrupInfos = GetStirrupGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionStart)
                    .LastOrDefault()
                    ?? throw new InvalidOperationException("Stirrup configuration is unavailable.");
                var diameterStirrup = installRebarBeamV2ViewModel.ElementInstances
                    .GetRebarBarType(rebarStirrupInfos.Diameter);
                var rebarGroupInfosStart = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionStart,
                    rebarBeamMainBarLevelType,
                    rebarBeamMainBarGroupType);
                var rebarGroupInfosMid = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionMid,
                    rebarBeamMainBarLevelType,
                    rebarBeamMainBarGroupType);
                var rebarGroupInfosEnd = GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionEnd,
                    rebarBeamMainBarLevelType,
                    rebarBeamMainBarGroupType);
                var rqMax = rebarBeams
                    .SelectMany(EnumerateMainBars)
                    .Select(rebar => rebar.Quantity)
                    .DefaultIfEmpty(0)
                    .Max();
                // all RebarInfo on Beam, include Start, Mid, End
                var rebarInfos = new List<RebarBeamMainBar>();
                for (int i = 0; i < qRebarBeams; i++)
                {
                    rebarInfos.Add(rebarGroupInfosStart[i]);
                    rebarInfos.Add(rebarGroupInfosMid[i]);
                    rebarInfos.Add(rebarGroupInfosEnd[i]);
                }
                // refresh rebarInfo.QtyInstall
                foreach (var item in rebarInfos)
                {
                    item.QtyInstall = item.Quantity;
                }
                var qRebarsMax = Math.Max(rebarInfos.Max(x => x.Quantity), rqMax);
                if (qRebarsMax <= 0) return new List<MainBarBeamReal>();
                var rebarGroupInfo = rebarInfos.FirstOrDefault(
                        info => info.Quantity > 0)
                    ?? rebarGroupInfosStart.FirstOrDefault()
                    ?? throw new InvalidOperationException("Main-bar configuration is unavailable.");
                var diameter = installRebarBeamV2ViewModel.ElementInstances
                    .GetRebarBarType(rebarGroupInfo.Diameter);
                var vtx = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
                var vty = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
                var vtz = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;
                var coverDiameter = diameterStirrup.ModelBarDiameter + diameter.ModelBarDiameter / 2;
                var ps = GetPointControls(
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement,
                    rebarBeamMainBarLevelType,
                    rebarBeamMainBarGroupType,
                    coverDiameter,
                    extentCoverSide);
                var cursMain = ps.PointsToCurves(true);
                var lengArr = cursMain
                    .GroupBy(x => x.Length)
                    .OrderBy(x => x.FirstOrDefault().Length)
                    .FirstOrDefault()
                    .FirstOrDefault();
                if (lengArr == null)
                    throw new InvalidOperationException("The transverse main-bar layout line is unavailable.");
                var spacing = qRebarsMax == 1 ? 0 : lengArr.Length / (qRebarsMax - 1);
                var rebarBeamById = rebarBeams.ToDictionary(beam => beam.BeamId);
                var subBeamById = subBeams.ToDictionary(beam => beam.Id);
                var subBeamIndexById = subBeams
                    .Select((beam, index) => new { beam.Id, Index = index })
                    .ToDictionary(item => item.Id, item => item.Index);
                var pointControlsByBeamId = subBeams.ToDictionary(
                    beam => beam.Id,
                    beam => GetPointControls(
                        installRebarBeamV2ViewModel,
                        beam,
                        rebarBeamMainBarLevelType,
                        rebarBeamMainBarGroupType,
                        coverDiameter,
                        extentCoverSide));
                var rebars = new List<MainBarBeamReal>();
                for (int i = 0; i < qRebarsMax; i++)
                {
                    var curve = new MainBarBeamReal();
                    curve.MainPoints = new List<XYZ>();
                    var qRebarInfos = rebarInfos.Count;
                    var c = 0;
                    foreach (var rebarInfo in rebarInfos)
                    {
                        try
                        {

                            var qty = rebarInfo.Quantity;
                            if (!rebarBeamById.TryGetValue(rebarInfo.HostId, out var rebarBeam)
                                || !subBeamById.TryGetValue(rebarInfo.HostId, out var boxSubBeam)
                                || !subBeamIndexById.TryGetValue(rebarInfo.HostId, out var subBeamIndex))
                                throw new InvalidOperationException(
                                    $"Beam data for host {rebarInfo.HostId} is incomplete.");
                            var beamRule = rebarBeam.BeamStressRule;
                            var beamIndex = subBeamIndex - 1;
                            var sectionIndex = (RebarBeamSectionType)(c - (beamIndex + 1) * 3);
                            var sectionIndexPrev = (RebarBeamSectionType)(c - 1 - beamIndex * 3);
                            //check start hook
                            if (sectionIndex == RebarBeamSectionType.SectionStart && qty > 0) curve.StartHook = true;
                            var psSub = pointControlsByBeamId[boxSubBeam.Id];
                            var cursSub = psSub.PointsToCurves(true);
                            var curveSub = cursSub
                                .GroupBy(x => Math.Round(x.Length.FootToMm(), 0))
                                .OrderBy(x => x.FirstOrDefault().Length)
                                .LastOrDefault()
                                .OrderBy(x => x.Midpoint().DotProduct(vty))
                                .FirstOrDefault();

                            var lSub = curveSub.Direction().IsSameDirection(vtx)
                                ? curveSub
                                : curveSub.CreateReversed();
                            var transverseOffset = qRebarsMax == 1
                                ? lengArr.Length / 2
                                : i * spacing;
                            var psTarget = lSub.GetEndPoint(0) + vty * transverseOffset;
                            var peTarget = lSub.GetEndPoint(1) + vty * transverseOffset;
                            var lTarget = Line.CreateBound(psTarget, peTarget);
                            var pBase = lTarget.Direction().IsSameDirection(vtx)
                                ? lTarget.GetEndPoint(0)
                                : lTarget.GetEndPoint(1);
                            var spacingSub = qty <= 1 ? 0 : lengArr.Length / (qty - 1);
                            Line l = null;
                            switch (sectionIndex)
                            {
                                case RebarBeamSectionType.SectionStart:
                                    var distance = lTarget.Length * beamRule.Stress[0];
                                    var p1 = pBase;
                                    var p2 = p1
                                        + vtx * (distance + installRebarBeamV2ViewModel.ElementInstances.RebarExtend.RebarTopExtend * diameter.ModelBarDiameter);
                                    l = Line.CreateBound(p1, p2);
                                    break;
                                case RebarBeamSectionType.SectionMid:
                                    distance = lTarget.Length * beamRule.Stress[1];
                                    p1 = pBase
                                        + vtx * (lTarget.Length * beamRule.Stress[0] - installRebarBeamV2ViewModel.ElementInstances.RebarExtend.RebarTopExtend * diameter.ModelBarDiameter);
                                    p2 = pBase
                                        + vtx * (lTarget.Length * beamRule.Stress[0] + distance + installRebarBeamV2ViewModel.ElementInstances.RebarExtend.RebarTopExtend * diameter.ModelBarDiameter);
                                    l = Line.CreateBound(p1, p2);
                                    break;
                                case RebarBeamSectionType.SectionEnd:
                                    distance = lTarget.Length * beamRule.Stress[2];
                                    p1 = pBase
                                        + vtx * (lTarget.Length * beamRule.Stress[0] + lTarget.Length * beamRule.Stress[1] - installRebarBeamV2ViewModel.ElementInstances.RebarExtend.RebarTopExtend * diameter.ModelBarDiameter);
                                    p2 = pBase
                                        + vtx * (lTarget.Length * beamRule.Stress[0] + lTarget.Length * beamRule.Stress[1] + distance);
                                    l = Line.CreateBound(p1, p2);
                                    break;
                            }
                            if (qty != 0)
                            {
                                var isIndexRebarMain = CheckIndexRebarMain(i, qty, qRebarsMax, spacingSub, spacing);
                                if (isIndexRebarMain)
                                {
                                    if (i == 0 || i == qRebarsMax - 1)
                                    {
                                        curve.MainPoints.AddRange(new List<XYZ>() { l.GetEndPoint(0), l.GetEndPoint(1) });
                                        rebarInfo.QtyInstall--;
                                    }
                                    else
                                    {
                                        if (rebarInfo.QtyInstall > 1)
                                        {
                                            curve.MainPoints.AddRange(new List<XYZ>() { l.GetEndPoint(0), l.GetEndPoint(1) });
                                            rebarInfo.QtyInstall--;
                                        }
                                    }
                                }

                                else
                                {

                                    //check end hook
                                    if (sectionIndexPrev == RebarBeamSectionType.SectionEnd)
                                        curve.EndHook = true;
                                    if (curve.MainPoints.Count > 0)
                                    {
                                        rebars.Add(curve);
                                    }
                                    curve = new MainBarBeamReal();
                                    curve.MainPoints = new List<XYZ>();
                                }
                            }
                            else
                            {
                                if (curve.MainPoints.Any())
                                {
                                    //check end hook
                                    if (sectionIndexPrev == RebarBeamSectionType.SectionEnd)
                                        curve.EndHook = true;
                                    if (curve.MainPoints.Count > 0)
                                    {
                                        rebars.Add(curve);
                                    }
                                    curve = new MainBarBeamReal();
                                    curve.MainPoints = new List<XYZ>();
                                }
                            }
                            if (c == qRebarInfos - 1)
                            {
                                if (curve.MainPoints.Any())
                                {
                                    //check end hook
                                    if (sectionIndex == RebarBeamSectionType.SectionEnd)
                                        curve.EndHook = true;
                                    if (curve.MainPoints.Count > 0)
                                    {
                                        rebars.Add(curve);
                                    }
                                    curve = new MainBarBeamReal();
                                    curve.MainPoints = new List<XYZ>();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException(
                                $"Failed to calculate main-bar geometry at section index {c}.", ex);
                        }
                        c++;
                    }
                }
                rebars = rebars.Where(x => x.MainPoints.Any()).ToList();
                foreach (var rebar in rebars)
                {
                    var mainPs = rebar.MainPoints.OrderBy(x => x.DotProduct(vtx));
                    rebar.StartPoint = mainPs.FirstOrDefault();
                    rebar.EndPoint = mainPs.LastOrDefault();
                    rebar.Level = (int)rebarBeamMainBarLevelType;
                    rebar.Group = (int)rebarBeamMainBarGroupType;
                    GenerateRebarDeverlop(installRebarBeamV2ViewModel, rebar);
                }
                return rebars;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to calculate main-bar geometry.", ex);
            }
        }

        public List<XYZ> GetPointControls(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            BoxElement boxElement,
            RebarBeamMainBarLevelType rebarBeamMainBarLevelType,
            RebarBeamMainBarGroupType rebarBeamMainBarGroupType,
            double extentStirrupFt,
            double extentCoverSideFt = 0)
        {
            try
            {
                var vtx = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
                var vty = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
                var vtz = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;
                var coverBeam = installRebarBeamV2ViewModel.ElementInstances.CoverBeam;
                var ps = new List<XYZ>();
                var extentZ = 0.0;
                switch (rebarBeamMainBarGroupType)
                {
                    case RebarBeamMainBarGroupType.GroupLevel1:
                        extentZ = 0.0;
                        break;
                    case RebarBeamMainBarGroupType.GroupLevel2:
                        extentZ = installRebarBeamV2ViewModel.ElementInstances.DistanceRebarToRebarMm.MmToFoot();
                        break;
                    case RebarBeamMainBarGroupType.GroupLevel3:
                        extentZ = 2 * installRebarBeamV2ViewModel.ElementInstances.DistanceRebarToRebarMm.MmToFoot();
                        break;
                }
                switch (rebarBeamMainBarLevelType)
                {
                    case RebarBeamMainBarLevelType.RebarTop:
                        ps = new List<XYZ>()
                        {
                            boxElement.BoxElementPoint.P5.EditZ(boxElement.BoxElementPoint.P5.Z - coverBeam.TopCover.MmToFoot() - extentZ - extentStirrupFt)
                            + vty * (coverBeam.RightCover.MmToFoot() + extentStirrupFt + extentCoverSideFt),
                            boxElement.BoxElementPoint.P6.EditZ(boxElement.BoxElementPoint.P5.Z - coverBeam.TopCover.MmToFoot() - extentZ- extentStirrupFt)
                            - vty * (coverBeam.LeftCover.MmToFoot() + extentStirrupFt + extentCoverSideFt),
                            boxElement.BoxElementPoint.P7.EditZ(boxElement.BoxElementPoint.P5.Z - coverBeam.TopCover.MmToFoot() - extentZ- extentStirrupFt)
                            - vty * (coverBeam.LeftCover.MmToFoot() + extentStirrupFt + extentCoverSideFt),
                            boxElement.BoxElementPoint.P8.EditZ(boxElement.BoxElementPoint.P5.Z - coverBeam.TopCover.MmToFoot() - extentZ- extentStirrupFt)
                            + vty * (coverBeam.RightCover.MmToFoot() + extentStirrupFt + extentCoverSideFt),
                        };
                        break;
                    case RebarBeamMainBarLevelType.RebarBot:
                        ps = new List<XYZ>()
                        {
                            boxElement.BoxElementPoint.P1.EditZ(boxElement.BoxElementPoint.P1.Z + coverBeam.BottomCover.MmToFoot() + extentZ + extentStirrupFt)
                            + vty * (coverBeam.RightCover.MmToFoot() + extentStirrupFt + extentCoverSideFt),
                            boxElement.BoxElementPoint.P2.EditZ(boxElement.BoxElementPoint.P1.Z + coverBeam.BottomCover.MmToFoot() + extentZ + extentStirrupFt)
                            - vty * (coverBeam.LeftCover.MmToFoot() + extentStirrupFt + extentCoverSideFt),
                            boxElement.BoxElementPoint.P3.EditZ(boxElement.BoxElementPoint.P1.Z + coverBeam.BottomCover.MmToFoot() + extentZ + extentStirrupFt)
                            - vty * (coverBeam.LeftCover.MmToFoot() + extentStirrupFt + extentCoverSideFt),
                            boxElement.BoxElementPoint.P4.EditZ(boxElement.BoxElementPoint.P1.Z + coverBeam.BottomCover.MmToFoot() + extentZ + extentStirrupFt)
                            + vty * (coverBeam.RightCover.MmToFoot() + extentStirrupFt + extentCoverSideFt)
                        };
                        break;
                }
                return ps;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to calculate control points for beam {boxElement?.Id}.", ex);
            }
        }

        public void GenerateRebarDeverlop(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            MainBarBeamReal mainBarBeamReal)
        {
            var wSolid = 50;
            var rebarDevelopType = (RebarBeamAnchorType)installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchorType.Type;
            var levelType = (RebarBeamMainBarLevelType)mainBarBeamReal.Level;
            var groupType = (RebarBeamMainBarGroupType)mainBarBeamReal.Group;
            //var vt = (mainBarBeamReal.EndPoint - mainBarBeamReal.StartPoint).Normalize();
            var vt = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
            var vtx = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
            var vty = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
            var vtz = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;
            var fEvelator = new FaceCustom(vty, mainBarBeamReal.StartPoint);
            var fPlan = new FaceCustom(vtz, mainBarBeamReal.StartPoint);
            var allGroupInfos = GetRebarBeamGroupLevelInfo(
                installRebarBeamV2ViewModel,
                levelType,
                groupType);
            var rebarInfo = allGroupInfos.FirstOrDefault(info => info.Quantity > 0)
                ?? allGroupInfos.FirstOrDefault();
            if (rebarInfo == null)
                throw new InvalidOperationException(
                    "Main-bar configuration is unavailable.");
            var diameter = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                .FirstOrDefault(x => x.NameStyle == rebarInfo.Diameter);
            var radiusMm = diameter.ModelBarDiameter.FootToMm();
            var bendrRadiusMm = diameter.StandardBendDiameter.FootToMm();

            //Y
            var type1_L1_Y_Start = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L1_Y_Start > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L1_Y_Start - radiusMm / 2
                : 0;
            var type1_L3_Y_Start = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L3_Y_Start > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L3_Y_Start - radiusMm / 2
                : 0;
            var type1_L1_Y_End = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L1_Y_End > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L1_Y_End - radiusMm / 2
                : 0;
            var type1_L3_Y_End = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L3_Y_End > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L3_Y_End - radiusMm / 2
                : 0;
            var type2_L1_Y_Start = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L1_Y_Start > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L1_Y_Start - radiusMm / 2
                : 0;
            var type2_L3_Y_Start = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L3_Y_Start > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L3_Y_Start - radiusMm / 2
                : 0;
            var type2_L1_Y_End = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L1_Y_End > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L1_Y_End - radiusMm / 2
                : 0;
            var type2_L3_Y_End = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L3_Y_End > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L3_Y_End - radiusMm / 2
                : 0;

            //X
            var type1_L1_X_Start = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L1_Y_Start > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L1_X_Start + radiusMm / 2
                : installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L1_X_Start;
            var type1_L3_X_Start = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L3_Y_Start > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L3_X_Start + radiusMm / 2
                : installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L3_X_Start;
            var type1_L1_X_End = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L1_Y_End > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L1_X_End + radiusMm / 2
                : installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L1_X_End;
            var type1_L3_X_End = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L3_Y_End > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L3_X_End + radiusMm / 2
                : installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type1_L3_X_End;
            var type2_L1_X_Start = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L1_Y_Start > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L1_X_Start + radiusMm / 2
                : installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L1_X_Start;
            var type2_L3_X_Start = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L3_Y_Start > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L3_X_Start + radiusMm / 2
                : installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L3_X_Start;
            var type2_L1_X_End = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L1_Y_End > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L1_X_End + radiusMm / 2
                : installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L1_X_End;
            var type2_L3_X_End = installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L3_Y_End > bendrRadiusMm
                ? installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L3_X_End + radiusMm / 2
                : installRebarBeamV2ViewModel.ElementInstances.RebarBeamAnchor.Type2_L3_X_End;

            var type1_top_X_start = levelType == RebarBeamMainBarLevelType.RebarTop
                ? type1_L1_X_Start
                : type1_L3_X_Start;

            var type1_top_Y_start = levelType == RebarBeamMainBarLevelType.RebarTop
                ? type1_L1_Y_Start
                : type1_L3_Y_Start;

            var type1_top_X_end = levelType == RebarBeamMainBarLevelType.RebarTop
                ? type1_L1_X_End
                : type1_L3_X_End;

            var type1_top_Y_end = levelType == RebarBeamMainBarLevelType.RebarTop
                ? type1_L1_Y_End
                : type1_L3_Y_End;

            var type2_top_X_start = levelType == RebarBeamMainBarLevelType.RebarTop
                ? type2_L1_X_Start
                : type2_L3_X_Start;

            var type2_top_Y_start = levelType == RebarBeamMainBarLevelType.RebarTop
                ? type2_L1_Y_Start
                : type2_L3_Y_Start;

            var type2_top_X_end = levelType == RebarBeamMainBarLevelType.RebarTop
                ? type2_L1_X_End
                : type2_L3_X_End;

            var type2_top_Y_end = levelType == RebarBeamMainBarLevelType.RebarTop
                ? type2_L1_Y_End
                : type2_L3_Y_End;

            mainBarBeamReal.MainPoints = new List<XYZ>() { mainBarBeamReal.StartPoint, mainBarBeamReal.EndPoint };
            var extent = 0.0;
            switch (groupType)
            {
                case RebarBeamMainBarGroupType.GroupLevel1:
                    extent = 0;
                    break;
                case RebarBeamMainBarGroupType.GroupLevel2:
                    extent = installRebarBeamV2ViewModel.ElementInstances.DistanceRebarToRebarMm.MmToFoot();
                    break;
                case RebarBeamMainBarGroupType.GroupLevel3:
                    extent = 2 * installRebarBeamV2ViewModel.ElementInstances.DistanceRebarToRebarMm.MmToFoot();
                    break;
            }

            var dirHook = levelType == RebarBeamMainBarLevelType.RebarTop
                ? -vtz
                : vtz;
            XYZ sh0 = null;
            XYZ sh1 = null;
            XYZ eh0 = null;
            XYZ eh1 = null;
            switch (rebarDevelopType)
            {
                case RebarBeamAnchorType.Type1:
                    var lCheckS = mainBarBeamReal.StartPoint.CreateLine(mainBarBeamReal.StartPoint - vt * wSolid.MmToFoot());
                    var lCheckE = mainBarBeamReal.EndPoint.CreateLine(mainBarBeamReal.EndPoint + vt * wSolid.MmToFoot());
                    var solidS = lCheckS.CreateSolid(wSolid, wSolid);
                    var solidE = lCheckE.CreateSolid(wSolid, wSolid);
                    //solidE.CreateDirectShape(AC.Document);
                    var solidFilterS = new ElementIntersectsSolidFilter(solidS);
                    var solidFilterE = new ElementIntersectsSolidFilter(solidE);
                    var eleIdS = new FilteredElementCollector(AC.Document)
                        .WherePasses(solidFilterS)
                        .ToElementIds()
                        .Where(x => !installRebarBeamV2ViewModel.ElementInstances.Beam.ElementSubs.Any(y => y.Id == x.Value))
                        .FirstOrDefault();
                    var eleIdE = new FilteredElementCollector(AC.Document)
                        .WherePasses(solidFilterE)
                        .ToElementIds()
                        .Where(x => !installRebarBeamV2ViewModel.ElementInstances.Beam.ElementSubs.Any(y => y.Id == x.Value))
                        .FirstOrDefault();
                    if (eleIdS != null)
                    {
                        try
                        {
                            var ps = AC.Document.GetElement(eleIdS)
                                .GetSolidsExtensions()
                                .Select(x => x.GetPoints())
                                .Aggregate((a, b) => a.Concat(b).ToList())
                                .Select(x => x.RayPointToFace(vty, fEvelator))
                                .Select(x => x.RayPointToFace(vtz, fPlan))
                                .OrderBy(x => x.DotProduct(vt));
                            var min = ps.FirstOrDefault();
                            var max = ps.LastOrDefault();

                            sh1 = min + vt * (type1_top_X_start.MmToFoot() + extent);
                            sh0 = sh1 + dirHook * type1_top_Y_start.MmToFoot();
                        }
                        catch (Exception)
                        {
                            sh0 = mainBarBeamReal.StartPoint;
                            sh1 = mainBarBeamReal.StartPoint;
                        }
                    }
                    else
                    {
                        sh0 = mainBarBeamReal.StartPoint;
                        sh1 = mainBarBeamReal.StartPoint;
                    }
                    if (eleIdE != null)
                    {
                        try
                        {
                            var ps = AC.Document.GetElement(eleIdE)
                                .GetSolidsExtensions()
                                .Select(x => x.GetPoints())
                                .Aggregate((a, b) => a.Concat(b).ToList())
                                .Select(x => x.RayPointToFace(vty, fEvelator))
                                .Select(x => x.RayPointToFace(vtz, fPlan))
                                .OrderBy(x => x.DotProduct(vt));
                            var min = ps.FirstOrDefault();
                            var max = ps.LastOrDefault();

                            eh1 = max - vt * (type1_top_X_end.MmToFoot() + extent);
                            eh0 = eh1 + dirHook * type1_top_Y_end.MmToFoot();
                        }
                        catch (Exception)
                        {
                            eh0 = mainBarBeamReal.EndPoint;
                            eh1 = mainBarBeamReal.EndPoint;
                        }
                    }
                    else
                    {
                        eh0 = mainBarBeamReal.EndPoint;
                        eh1 = mainBarBeamReal.EndPoint;
                    }
                    break;
                case RebarBeamAnchorType.Type2:
                    sh1 = mainBarBeamReal.StartPoint
                        - vt * (type2_top_X_start.MmToFoot() - extent);
                    sh0 = sh1 + dirHook * type2_top_Y_start.MmToFoot();
                    eh1 = mainBarBeamReal.EndPoint
                        + vt * (type2_top_X_end.MmToFoot() - extent);
                    eh0 = eh1 + dirHook * type2_top_Y_end.MmToFoot();
                    break;
            }
            var case1 = new List<XYZ>() { sh0, sh1, eh1, eh0 };
            var case2 = new List<XYZ>() { sh0, sh1, mainBarBeamReal.EndPoint };
            var case3 = new List<XYZ>() { mainBarBeamReal.StartPoint, eh1, eh0 };
            var case4 = new List<XYZ>() { mainBarBeamReal.StartPoint, mainBarBeamReal.EndPoint };
            mainBarBeamReal.MainPoints = mainBarBeamReal.StartHook && mainBarBeamReal.EndHook
                ? case1
                : mainBarBeamReal.StartHook && !mainBarBeamReal.EndHook
                ? case2
                : !mainBarBeamReal.StartHook && mainBarBeamReal.EndHook
                ? case3
                : case4;
            mainBarBeamReal.MainPoints = mainBarBeamReal.MainPoints.Distinct(new ComparePoint()).ToList();
        }

    }
}
