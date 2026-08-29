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

namespace LSTool.Tools.Beams.InstallRebarBeamV2.service
{
    public partial class InstallRebarBeamInModelService
    {
        private List<Rebar> InstallRebarSubHorizontalStirrupForSideRebar(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            RebarExecutionContext context)
        {
            try
            {
                var offsetStart = 0.MmToFoot();
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

                var vectorX = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
                var vectorY = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
                var vectorZ = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;

                var rebarSides = _subInstallRebarBeamInModelService.GetSideBarBeamReals(
                    installRebarBeamV2ViewModel,
                    0);

                var cb = 0;
                var curvesInAllBeams = rebarSides.Select(x => x.StartPoint.CreateLine(x.EndPoint)).ToList();

                var curvesAtASide = curvesInAllBeams.Distinct(new GroupLineOfRebarSide(vectorY)).ToList();
                if (!curvesInAllBeams.Any()) return result;

                foreach (var subBeam in subBeams)
                {
                    var spanResultStartIndex = result.Count;
                    var rebarBeam = rebarBeams[cb];
                    var beamStressRule = rebarBeam.BeamStressRule;
                    var qbeamStressRule = beamStressRule.Stress.Count;
                    var boxPs = subBeam.BoxElementPoint;
                    var beamLength = boxPs.P1.DistanceTo(boxPs.P4);

                    var segmentStart = beamStressRule.Stress.Aggregate(new List<double> { 0 }, (list, d) =>
                    {
                        list.Add(list.LastOrDefault() + d * beamLength);
                        return list;
                    });

                    var diameter = context.GetBarType(
                            rebarBeam.RebarBeamSectionStart.RebarBeamStirrup.Diameter)
                        .RebarBarType.GetRebarDiameter();

                    List<SubHorizontalStirrupCollectionDto> stirrupStartSegment = [],
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
                        else lengthSegment -= diameter;

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

                        RebarBeamSection rebarBeamSection = null;
                        if (segmentType == RebarBeamSectionType.SectionStart)
                        {
                            rebarBeamSection = rebarBeam.RebarBeamSectionStart;
                        }
                        else if (segmentType == RebarBeamSectionType.SectionMid)
                        {
                            rebarBeamSection = rebarBeam.RebarBeamSectionMid;
                        }
                        else if (segmentType == RebarBeamSectionType.SectionEnd)
                        {
                            rebarBeamSection = rebarBeam.RebarBeamSectionEnd;
                        }

                        if (rebarBeamSection == null) continue;

                        var diameterName = rebarBeam.RebarBeamSectionStart.RebarBeamSideBar.Diameter;
                        var rebarBarTypeSideCustom = context.GetBarType(diameterName);

                        var spacing = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel
                            .SpacingHorizontalDaiPhuChongPhinh.MmToFoot();

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

                        var bPlane = BPlane.CreateByNormalAndOrigin(vectorX, segmentBoxInOneBeam.P1);

                        foreach (var lineAtASide in curvesAtASide)
                        {
                            // kiem tra thep chong phinh o tung doan dam
                            var curveLoops = new CurveLoop();
                            curveLoops.Append(segmentBoxInOneBeam.P1.CreateLine(segmentBoxInOneBeam.P2));
                            curveLoops.Append(segmentBoxInOneBeam.P2.CreateLine(segmentBoxInOneBeam.P3));
                            curveLoops.Append(segmentBoxInOneBeam.P3.CreateLine(segmentBoxInOneBeam.P4));
                            curveLoops.Append(segmentBoxInOneBeam.P4.CreateLine(segmentBoxInOneBeam.P1));
                            var solidBox = GeometryCreationUtilities.CreateExtrusionGeometry(
                                new List<CurveLoop>() { curveLoops }, XYZ.BasisZ,
                                (segmentBoxInOneBeam.P1.DistanceTo(segmentBoxInOneBeam.P5)));
                            if (!lineAtASide.GetInsideCurvesIntersectSolid(solidBox).Any())
                            {
                                continue;
                            }
                            var spOnLine = lineAtASide.SP();
                            var directionInside = (segmentBoxInOneBeam.P1 - segmentBoxInOneBeam.P5).Normalize();

                            var left = spOnLine.ProjectOnto(bPlane);
                            left = left.ProjectOnto(BPlane.CreateByNormalAndOrigin(vectorY, segmentBoxInOneBeam.P1));

                            var right = spOnLine.ProjectOnto(bPlane);
                            right = right.ProjectOnto(BPlane.CreateByNormalAndOrigin(vectorY, segmentBoxInOneBeam.P2));

                            left -= directionInside * (diameter + rebarBarTypeSideCustom.ModelBarDiameter * 0.5);
                            right -= directionInside * (diameter + rebarBarTypeSideCustom.ModelBarDiameter * 0.5);

                            left -= (left - right).Normalize() * coverFootBeam.LeftCover;
                            right -= (right - left).Normalize() * coverFootBeam.RightCover;

                            var subStirrupCollection = new SubHorizontalStirrupCollectionDto
                            {
                                BoxElementPoint = segmentBoxInOneBeam,
                                Left = left,
                                CoverFootBeam = coverFootBeam,
                                Direction = (segmentBoxInOneBeam.P4 - segmentBoxInOneBeam.P1).Normalize(),
                                Document = AC.Document,
                                Host = host,
                                RebarBarTypeCustom = context.GetBarType(
                                    installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.RebarDiameterHorizontalDaiPhuChongPhinh),
                                Spacing = spacing,
                                Right = right,
                                DirectionInside = directionInside,
                            };

                            if (segmentType == RebarBeamSectionType.SectionStart)
                                stirrupStartSegment.Add(subStirrupCollection);
                            else if (segmentType == RebarBeamSectionType.SectionMid)
                                stirrupMidSegment.Add(subStirrupCollection);
                            else stirrupEndSegment.Add(subStirrupCollection);
                        }
                    }

                    // Dầm không có thép hông thì cũng không có đai phụ chống
                    // phình cho nó. Đây là trường hợp hợp lệ, ví dụ dầm thấp
                    // hơn ngưỡng bố trí thép hông, nên bỏ qua chứ không báo
                    // lỗi. Phải tăng cb trước khi continue vì nó là chỉ số
                    // cấu hình của dầm, chỉ tăng ở cuối vòng lặp.
                    if (stirrupStartSegment.Count == 0
                        && stirrupEndSegment.Count == 0
                        && stirrupMidSegment.Count == 0)
                    {
                        context.DiagnosticLog?.Record("stirrup.side.skipped", new
                        {
                            beamId = subBeam.Id,
                            reason = "beam has no side bars"
                        });
                        cb++;
                        continue;
                    }

                    //rải thép ở segment Start, End trước
                    Tuple<LineHorizontalDto, int> lastPositionStartSegment = null, lastPositionEndSegment = null;
                    foreach (var stirrupStartSegment1 in stirrupStartSegment)
                    {
                        LSTool.Tools.Beams.InstallRebarBeamV2.service.SubHorizontalStirrup.InstallSubStirrupRebarBeam installStirrupRebarStartSegment =
                            installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.HorizontalDaiPhu
                            ? new LSTool.Tools.Beams.InstallRebarBeamV2.service.SubHorizontalStirrup.SubStirrupShape2(stirrupStartSegment1)
                            : new LSTool.Tools.Beams.InstallRebarBeamV2.service.SubHorizontalStirrup.SubStirrupShape1(stirrupStartSegment1);
                        lastPositionStartSegment = installStirrupRebarStartSegment.RunForEndAndStartSegment();
                        result.AddRange(installStirrupRebarStartSegment.Rebars);
                    }
                    foreach (var stirrupEndSegment1 in stirrupEndSegment)
                    {
                        LSTool.Tools.Beams.InstallRebarBeamV2.service.SubHorizontalStirrup.InstallSubStirrupRebarBeam
                            installStirrupRebarEndSegment = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.HorizontalDaiPhu
                                ? new LSTool.Tools.Beams.InstallRebarBeamV2.service.SubHorizontalStirrup.SubStirrupShape2(stirrupEndSegment1)
                                : new LSTool.Tools.Beams.InstallRebarBeamV2.service.SubHorizontalStirrup.SubStirrupShape1(stirrupEndSegment1);
                        lastPositionEndSegment = installStirrupRebarEndSegment.RunForEndAndStartSegment();
                        result.AddRange(installStirrupRebarEndSegment.Rebars);
                    }

                    //biến đổi lại box cua mid segment

                    if (lastPositionStartSegment == null
                        || lastPositionEndSegment == null)
                    {
                        throw new InvalidOperationException(
                            $"Horizontal secondary stirrups for side bars "
                            + $"in beam {subBeam.Id} require both start and "
                            + "end hook references; "
                            + $"start candidates: {stirrupStartSegment.Count}, "
                            + $"end candidates: {stirrupEndSegment.Count}, "
                            + $"mid candidates: {stirrupMidSegment.Count}.");
                    }

                    var startPlane = BPlane.CreateByNormalAndOrigin(vectorX,
                        lastPositionStartSegment.Item1.Transform.OfPoint(lastPositionStartSegment.Item1.Left));
                    var endPlane = BPlane.CreateByNormalAndOrigin(vectorX,
                        lastPositionEndSegment.Item1.Transform.OfPoint(lastPositionEndSegment.Item1.Left));

                    stirrupMidSegment = stirrupMidSegment.Select(x =>
                    {
                        x.BoxElementPoint.P1 = x.BoxElementPoint.P1.ProjectOnto(startPlane);
                        x.BoxElementPoint.P2 = x.BoxElementPoint.P2.ProjectOnto(startPlane);
                        x.BoxElementPoint.P5 = x.BoxElementPoint.P5.ProjectOnto(startPlane);
                        x.BoxElementPoint.P6 = x.BoxElementPoint.P6.ProjectOnto(startPlane);
                        x.BoxElementPoint.P4 = x.BoxElementPoint.P4.ProjectOnto(endPlane);
                        x.BoxElementPoint.P3 = x.BoxElementPoint.P3.ProjectOnto(endPlane);
                        x.BoxElementPoint.P8 = x.BoxElementPoint.P8.ProjectOnto(endPlane);
                        x.BoxElementPoint.P7 = x.BoxElementPoint.P7.ProjectOnto(endPlane);
                        return x;
                    }).ToList();
                    foreach (var stirrupMidSegment1 in stirrupMidSegment)
                    {
                        var bPlane = BPlane.CreateByNormalAndOrigin(vectorX, stirrupMidSegment1.BoxElementPoint.P1);
                        stirrupMidSegment1.Left = stirrupMidSegment1.Left.ProjectOnto(bPlane);
                        stirrupMidSegment1.Right = stirrupMidSegment1.Right.ProjectOnto(bPlane);

                        LSTool.Tools.Beams.InstallRebarBeamV2.service.SubHorizontalStirrup.InstallSubStirrupRebarBeam
                            installStirrupRebarMidSegment = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.HorizontalDaiPhu
                                ? new LSTool.Tools.Beams.InstallRebarBeamV2.service.SubHorizontalStirrup.SubStirrupShape2(stirrupMidSegment1)
                                : new LSTool.Tools.Beams.InstallRebarBeamV2.service.SubHorizontalStirrup.SubStirrupShape1(stirrupMidSegment1);
                        installStirrupRebarMidSegment.RunAtMidSegment(lastPositionStartSegment.Item1, lastPositionEndSegment.Item1);
                        result.AddRange(installStirrupRebarMidSegment.Rebars);
                    }
                    foreach (var rebar in result.Skip(spanResultStartIndex))
                        context.RegisterTargetHost(rebar, subBeam.Id);
                    cb++;
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create horizontal secondary stirrups for side bars.", ex);
            }
        }

    }
}
