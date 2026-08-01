using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using LSTool.Compatibility;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using LSTool.Tools.Beams.InstallRebarBeamV2.service.MainStirrup;
using RIMT.Utils.BoundingBoxs;
using RIMT.Utils.RevitElements;
using RIMT.Utils.RevPoints;
using RIMT.Utils.RevRebars;
using LSTool.Tools.Beams.InstallRebarBeamV2.models.MainStirrups;
using RIMT.Utils.Revit;
using LSTool.Tools.Beams.InstallRebarBeamV2.models.SecondaryStirrups;
using LSTool.Tools.Beams.InstallRebarBeamV2.service.SubVerticalStirrup;
using LSTool.Tools.Beams.InstallRebarBeamV2.iservices;
using LSTool.Tools.Beams.InstallRebarBeamV2;
using RIMT.Utils;
using LSTool.Tools.Beams.InstallRebarBeamV2.Application;
using System.Diagnostics;
using LSTool.Tools.Beams.InstallRebarBeamV2.Revit.Writers;
using LSTool.Tools.Beams.InstallRebarBeamV2.Geometry.MainBars;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.service
{
    public partial class InstallRebarBeamInModelService
    {
        private List<Rebar> InstallRebarSubVerticalStirrup(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            RebarExecutionContext context)
        {
            try
            {
                var offsetStart = 0;
                var offsetEnd = context.GetBarType(
                        installRebarBeamV2ViewModel.ElementInstances.RebarBeams.First()
                            .RebarBeamSectionStart.RebarBeamStirrup.Diameter)
                    .ModelBarDiameter;
                var cover = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.ElementInstances.CoverBeam;
                var coverFootBeam = new CoverFootBeam(new CoverBeam
                {
                    BottomCover = cover.BottomCover,
                    TopCover = cover.TopCover,
                    LeftCover = cover.LeftCover,
                    RightCover = cover.RightCover
                });
                var result = new List<Rebar>();

                var host = context.TemporaryHost;
                var rebarBeams = installRebarBeamV2ViewModel.ElementInstances.RebarBeams;
                var subBeams = installRebarBeamV2ViewModel.ElementInstances.Beam.ElementSubs;
                if (rebarBeams.Count != subBeams.Count)
                {
                    throw new InvalidOperationException(
                        "Beam configuration count does not match the selected "
                        + "physical span count.");
                }
                var cb = 0;

                var vectorX = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
                var vectorY = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
                var vectorZ = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;
                foreach (var subBeam in subBeams)
                {
                    var spanResultStartIndex = result.Count;
                    var rebarBeam = rebarBeams[cb];
                    var topMainRebarLines = MainBarPlanCurveProvider
                        .GetLaneReferenceLines(
                            context,
                            RebarBeamMainBarLevelType.RebarTop,
                            RebarBeamMainBarGroupType.GroupLevel1,
                            subBeam.Id);
                    var bottomMainRebarLines =
                        MainBarPlanCurveProvider
                            .GetLaneReferenceLines(
                                context,
                                RebarBeamMainBarLevelType.RebarBot,
                                RebarBeamMainBarGroupType.GroupLevel1,
                                subBeam.Id);
                    var topReferencePlan = context.GetMainBarPlan(
                        RebarBeamMainBarLevelType.RebarTop,
                        RebarBeamMainBarGroupType.GroupLevel1);
                    var bottomReferencePlan = context.GetMainBarPlan(
                        RebarBeamMainBarLevelType.RebarBot,
                        RebarBeamMainBarGroupType.GroupLevel1);
                    var hasIndependentBottomAnchorage =
                        bottomReferencePlan.Runs.Any(run =>
                            run.IsIndependentJointAnchorage
                            && run.TargetHostBeamId == subBeam.Id);
                    var mainRebarLines = topMainRebarLines;
                    var referencePlan = topReferencePlan;
                    if (mainRebarLines.Count
                        < bottomMainRebarLines.Count)
                    {
                        mainRebarLines = bottomMainRebarLines;
                        referencePlan = bottomReferencePlan;
                    }
                    if (mainRebarLines.Count == 0)
                    {
                        cb++;
                        continue;
                    }
                    var diameterTop1 = referencePlan.Runs
                        .FirstOrDefault()?.BarType
                        ?? throw new InvalidOperationException(
                            "The vertical secondary stirrup reference plan "
                            + "has no main-bar type.");
                    var curvesInAllBeams = mainRebarLines;
                    var beamStressRule = rebarBeam.BeamStressRule;
                    var qbeamStressRule = beamStressRule.Stress.Count;
                    var boxPs = subBeam.BoxElementPoint;
                    var beamLength = boxPs.P1.DistanceTo(boxPs.P4);

                    var segmentStart = beamStressRule.Stress.Aggregate(new List<double> { 0 }, (list, d) =>
                    {
                        list.Add(list.LastOrDefault() + d * beamLength);
                        return list;
                    });

                    var diameterName = rebarBeam.RebarBeamSectionStart.RebarBeamStirrup.Diameter;
                    var rebarTypeCustom = context.GetBarType(diameterName);
                    var diameter = rebarTypeCustom.RebarBarType.GetRebarDiameter();

                    List<SubStirrupCollectionDto> stirrupStartSegment = [],
                        stirrupEndSegment = [],
                        stirrupMidSegment = [];

                    for (var i = 0; i < qbeamStressRule; i++)
                    {
                        var segmentBoxInOneBeam = new BoxElementPoint
                        {
                            P1 = boxPs.P1,
                            P2 = boxPs.P2,
                            P3 = boxPs.P3,
                            P4 = boxPs.P4,
                            P5 = boxPs.P5,
                            P6 = boxPs.P6,
                            P7 = boxPs.P7,
                            P8 = boxPs.P8
                        };
                        var lengthSegment = beamLength * beamStressRule.Stress[i];
                        var segmentType = RebarBeamSectionType.SectionMid;
                        if (i == 0)
                        {
                            segmentType = RebarBeamSectionType.SectionStart;
                            lengthSegment = lengthSegment - offsetStart - diameter;
                        }
                        else if (i == qbeamStressRule - 1)
                        {
                            segmentType = RebarBeamSectionType.SectionEnd;
                            lengthSegment = lengthSegment - offsetEnd - diameter;
                        }
                        else
                            lengthSegment -= diameter;
                        var transform = Transform.CreateTranslation(vectorX * segmentStart[i]);
                        segmentBoxInOneBeam.P1 = transform.OfPoint(segmentBoxInOneBeam.P1);
                        segmentBoxInOneBeam.P5 = transform.OfPoint(segmentBoxInOneBeam.P5);
                        segmentBoxInOneBeam.P2 = transform.OfPoint(segmentBoxInOneBeam.P2);
                        segmentBoxInOneBeam.P6 = transform.OfPoint(segmentBoxInOneBeam.P6);

                        switch (segmentType)
                        {
                            case RebarBeamSectionType.SectionStart:
                                {
                                    var offsetDirection = vectorX * (diameter / 2 + offsetStart);
                                    segmentBoxInOneBeam.P1 += offsetDirection;
                                    segmentBoxInOneBeam.P5 += offsetDirection;
                                    segmentBoxInOneBeam.P2 += offsetDirection;
                                    segmentBoxInOneBeam.P6 += offsetDirection;
                                    break;
                                }
                            case RebarBeamSectionType.SectionMid or RebarBeamSectionType.SectionEnd:
                                {
                                    var offsetDirection = vectorX * (diameter / 2);
                                    segmentBoxInOneBeam.P1 += offsetDirection;
                                    segmentBoxInOneBeam.P5 += offsetDirection;
                                    segmentBoxInOneBeam.P2 += offsetDirection;
                                    segmentBoxInOneBeam.P6 += offsetDirection;
                                    break;
                                }
                        }

                        segmentBoxInOneBeam.P3 = segmentBoxInOneBeam.P2 + lengthSegment * vectorX;
                        segmentBoxInOneBeam.P4 = segmentBoxInOneBeam.P1 + lengthSegment * vectorX;
                        segmentBoxInOneBeam.P7 = segmentBoxInOneBeam.P6 + lengthSegment * vectorX;
                        segmentBoxInOneBeam.P8 = segmentBoxInOneBeam.P5 + lengthSegment * vectorX;

                        var lineBetween =
                            ((segmentBoxInOneBeam.P1 + segmentBoxInOneBeam.P4) / 2).CreateLine((segmentBoxInOneBeam.P2 +
                                segmentBoxInOneBeam.P3) / 2);
                        var plane = BPlane.CreateByNormalAndOrigin(vectorZ,
                            curvesInAllBeams.First().SP());

                        lineBetween = lineBetween.ProjectOntoPlane(plane);

                        var vectorSort = -(installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.BoxElementPoint.P1
                            - installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.BoxElementPoint.P2).Normalize();
                        var curveInSegment = curvesInAllBeams.OrderBy(x =>
                                x.SP().DotProduct(vectorSort)).ToList();
                        var topCurvesInSegment = topMainRebarLines
                            .OrderBy(x => x.SP().DotProduct(vectorSort))
                            .ToList();
                        var bottomCurvesInSegment = bottomMainRebarLines
                            .OrderBy(x => x.SP().DotProduct(vectorSort))
                            .ToList();

                        //vectorSort = -vectorSort;

                        Dictionary<int, bool> hooks = null;
                        if (segmentType == RebarBeamSectionType.SectionStart)
                        {
                            hooks = rebarBeam.RebarBeamSectionStart.RebarBeamTop.RebarBeamTopLevel1.Hooks2;
                        }
                        else if (segmentType == RebarBeamSectionType.SectionMid)
                        {
                            hooks = rebarBeam.RebarBeamSectionMid.RebarBeamTop.RebarBeamTopLevel1.Hooks2;
                        }
                        else if (segmentType == RebarBeamSectionType.SectionEnd)
                        {
                            hooks = rebarBeam.RebarBeamSectionEnd.RebarBeamTop.RebarBeamTopLevel1.Hooks2;
                        }

                        if (hooks == null) continue;

                        var activeHookIndices = hooks
                            .Where(pair => pair.Value)
                            .Select(pair => pair.Key)
                            .OrderBy(index => index)
                            .ToList();
                        if (hasIndependentBottomAnchorage
                            && activeHookIndices.Count > 0)
                        {
                            var laneToleranceFt = 0.01.MmToFoot();
                            var mismatchReason = string.Empty;
                            if (topCurvesInSegment.Count
                                != bottomCurvesInSegment.Count)
                            {
                                mismatchReason =
                                    $"top lane count {topCurvesInSegment.Count} "
                                    + $"does not match bottom lane count "
                                    + $"{bottomCurvesInSegment.Count}";
                            }
                            else
                            {
                                foreach (var hookIndex in activeHookIndices)
                                {
                                    if (hookIndex < 0
                                        || hookIndex
                                        >= topCurvesInSegment.Count)
                                    {
                                        mismatchReason =
                                            $"hook index {hookIndex} is outside "
                                            + $"the {topCurvesInSegment.Count} "
                                            + "paired lanes";
                                        break;
                                    }

                                    var topLaneY = topCurvesInSegment[
                                            hookIndex]
                                        .SP()
                                        .DotProduct(vectorSort);
                                    var bottomLaneY =
                                        bottomCurvesInSegment[hookIndex]
                                            .SP()
                                            .DotProduct(vectorSort);
                                    if (Math.Abs(topLaneY - bottomLaneY)
                                        <= laneToleranceFt)
                                    {
                                        continue;
                                    }

                                    mismatchReason =
                                        $"lane {hookIndex + 1} is staggered by "
                                        + $"{Math.Abs(topLaneY - bottomLaneY).FootToMm():0.###} mm";
                                    break;
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(mismatchReason))
                            {
                                context.DiagnosticLog?.Record(
                                    "secondary.vertical.unsupported",
                                    new
                                    {
                                        beamId = subBeam.Id,
                                        segmentType =
                                            segmentType.ToString(),
                                        code =
                                            "IndependentStaggeredLaneUnsupported",
                                        reason = mismatchReason
                                    });
                                throw new InvalidOperationException(
                                    "Vertical secondary stirrups cannot be "
                                    + "created safely for transversely "
                                    + "staggered Independent Joint Anchorage "
                                    + $"lanes on beam {subBeam.Id}: "
                                    + mismatchReason
                                    + ". Disable the active vertical secondary "
                                    + "stirrup hooks for this span until a "
                                    + "multi-planar secondary shape is supported.");
                            }
                        }

                        if (segmentType == RebarBeamSectionType.SectionEnd)
                        {
                            (segmentBoxInOneBeam.P1, segmentBoxInOneBeam.P4) =
                                (segmentBoxInOneBeam.P4, segmentBoxInOneBeam.P1);
                            (segmentBoxInOneBeam.P2, segmentBoxInOneBeam.P3) =
                                (segmentBoxInOneBeam.P3, segmentBoxInOneBeam.P2);
                            (segmentBoxInOneBeam.P5, segmentBoxInOneBeam.P8) =
                                (segmentBoxInOneBeam.P8, segmentBoxInOneBeam.P5);
                            (segmentBoxInOneBeam.P6, segmentBoxInOneBeam.P7) =
                                (segmentBoxInOneBeam.P7, segmentBoxInOneBeam.P6);
                        }

                        foreach (var hook in hooks.OrderBy(pair => pair.Key))
                        {
                            if (!hook.Value) continue;
                            var j = hook.Key;
                            if (j < 0 || j >= curveInSegment.Count)
                            {
                                throw new InvalidOperationException(
                                    $"Vertical secondary stirrup hook index "
                                    + $"{j} exceeds the {curveInSegment.Count} "
                                    + "planned main-bar lanes.");
                            }

                            var originBottomForPlane = segmentBoxInOneBeam.P1.Add((segmentBoxInOneBeam.P5 -
                                                                                   segmentBoxInOneBeam.P1).Normalize() *
                                                                                  coverFootBeam.BottomCover);

                            var originTopForPlane = segmentBoxInOneBeam.P5.Add((segmentBoxInOneBeam.P1 -
                                                                                segmentBoxInOneBeam.P5).Normalize() *
                                                                               coverFootBeam.TopCover);

                            var topLaneCurve =
                                hasIndependentBottomAnchorage
                                    ? topCurvesInSegment[j]
                                    : curveInSegment[j];
                            var bottomLaneCurve =
                                hasIndependentBottomAnchorage
                                    ? bottomCurvesInSegment[j]
                                    : curveInSegment[j];
                            var bottom = bottomLaneCurve.SP()
                                .ProjectOnto(BPlane.CreateByNormalAndOrigin(
                                    vectorZ,
                                    originBottomForPlane));

                            var top = topLaneCurve.SP()
                                .ProjectOnto(BPlane.CreateByNormalAndOrigin(
                                    vectorZ,
                                    originTopForPlane));

                            var plane2 = BPlane.CreateByNormalAndOrigin(
                                vectorX,
                                segmentBoxInOneBeam.P1);

                            top = top.ProjectOnto(plane2);
                            bottom = bottom.ProjectOnto(plane2);
                            var centerSegmentOnPlane2 =
                                ((segmentBoxInOneBeam.P1 + segmentBoxInOneBeam.P3) / 2).ProjectOnto(plane2);
                            var centerSegmentOnPlaneOfTop =
                                centerSegmentOnPlane2.ProjectOnto(BPlane.CreateByNormalAndOrigin(vectorZ, top));
                            var directionInside = (centerSegmentOnPlaneOfTop - top).Normalize();
                            if (directionInside.IsAlmostEqualTo(XYZ.Zero))
                            {
                                directionInside = vectorY;
                            }

                            top -= directionInside * diameterTop1.RebarBarType.GetRebarDiameter() * 1.5;
                            bottom -= directionInside * diameterTop1.RebarBarType.GetRebarDiameter() * 1.5;

                            var spacing = segmentType switch
                            {
                                RebarBeamSectionType.SectionStart => rebarBeam.RebarBeamSectionStart.RebarBeamStirrup.Spacing.MmToFoot(),
                                RebarBeamSectionType.SectionMid => rebarBeam.RebarBeamSectionMid.RebarBeamStirrup.Spacing.MmToFoot(),
                                RebarBeamSectionType.SectionEnd => rebarBeam.RebarBeamSectionEnd.RebarBeamStirrup.Spacing.MmToFoot(),
                                _ => throw new ArgumentOutOfRangeException()
                            };
                            var subStirrupCollection = new SubStirrupCollectionDto
                            {
                                BoxElementPoint = segmentBoxInOneBeam,
                                Bottom = bottom,
                                CoverFootBeam = coverFootBeam,
                                Direction = (segmentBoxInOneBeam.P4 - segmentBoxInOneBeam.P1).Normalize(),
                                Document = AC.Document,
                                Host = host,
                                RebarBarTypeCustom = rebarTypeCustom,
                                Spacing = spacing,
                                Top = top,
                                DirectionInside = directionInside
                            };

                            if (segmentType == RebarBeamSectionType.SectionStart)
                                stirrupStartSegment.Add(subStirrupCollection);
                            else if (segmentType == RebarBeamSectionType.SectionEnd)
                                stirrupEndSegment.Add(subStirrupCollection);
                            else
                                stirrupMidSegment.Add(subStirrupCollection);
                        }
                    }
                    Tuple<LineDto, int> lastPositionStartSegment = null, lastPositionEndSegment = null;
                    if (stirrupStartSegment.Any())
                    {
                        foreach (var stirrupStartSegment1 in stirrupStartSegment)
                        {
                            InstallSubStirrupRebarBeam installStirrupRebarStartSegment = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.VerticalDaiPhu
                                ? new SubStirrupShape2(stirrupStartSegment1)
                                : new SubStirrupShape1(stirrupStartSegment1);
                            lastPositionStartSegment = installStirrupRebarStartSegment.RunForEndAndStartSegment();
                            result.AddRange(installStirrupRebarStartSegment.Rebars);
                        }
                    }
                    if (stirrupEndSegment.Any())
                    {
                        foreach (var stirrupEndSegment1 in stirrupEndSegment)
                        {
                            InstallSubStirrupRebarBeam installStirrupRebarEndSegment = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.VerticalDaiPhu
                                ? new SubStirrupShape2(stirrupEndSegment1)
                                : new SubStirrupShape1(stirrupEndSegment1);
                            lastPositionEndSegment = installStirrupRebarEndSegment.RunForEndAndStartSegment();
                            result.AddRange(installStirrupRebarEndSegment.Rebars);
                        }
                    }
                    //biến đổi lại box cua mid segment
                    if (stirrupMidSegment.Any())
                    {
                        foreach (var stirrupMidSegment1 in stirrupMidSegment)
                        {
                            var bPlane = BPlane.CreateByNormalAndOrigin(vectorX, stirrupMidSegment1.BoxElementPoint.P1);
                            stirrupMidSegment1.Bottom = stirrupMidSegment1.Bottom.ProjectOnto(bPlane);
                            stirrupMidSegment1.Top = stirrupMidSegment1.Top.ProjectOnto(bPlane);

                            InstallSubStirrupRebarBeam installStirrupRebarMidSegment = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.VerticalDaiPhu
                                ? new SubStirrupShape2(stirrupMidSegment1)
                                : new SubStirrupShape1(stirrupMidSegment1);
                            installStirrupRebarMidSegment.RunAtMidSegment1();
                            result.AddRange(installStirrupRebarMidSegment.Rebars);
                        }
                    }
                    foreach (var rebar in result.Skip(spanResultStartIndex))
                        context.RegisterTargetHost(rebar, subBeam.Id);
                    cb++;
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create vertical secondary stirrups.", ex);
            }
        }

    }
}
