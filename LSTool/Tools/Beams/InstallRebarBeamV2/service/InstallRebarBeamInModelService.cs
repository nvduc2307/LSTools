using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HcBimUtils;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using LSTool.Tools.Beams.InstallRebarBeamV2.service.MainStirrup;
using RIMT.Utils.BoundingBoxs;
using RIMT.Utils.RevitElements;
using RIMT.Utils.RevPoints;
using RIMT.Utils.RevRebars;
using LSTool.Tools.Beams.InstallRebarBeamV2.models.MainStirrups;
using RIMT.Utils.Revit;
using HcBimUtils.GeometryUtils;
using HcBimUtils.MoreLinq;
using LSTool.Tools.Beams.InstallRebarBeamV2.models.SecondaryStirrups;
using LSTool.Tools.Beams.InstallRebarBeamV2.service.SubVerticalStirrup;
using LSTool.Tools.Beams.InstallRebarBeamV2.iservices;
using LSTool.Tools.Beams.InstallRebarBeamV2;
using RIMT.Utils;
using HcBimUtils.DocumentUtils;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.service
{
    public class InstallRebarBeamInModelService : IInstallRebarBeamInModelService
    {
        private ISubInstallRebarBeamInModelService _subInstallRebarBeamInModelService;
        public InstallRebarBeamInModelService(ISubInstallRebarBeamInModelService subInstallRebarBeamInModelService)
        {
            _subInstallRebarBeamInModelService = subInstallRebarBeamInModelService;
        }

        public List<Rebar> InstallRebarStirrup(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {
            try
            {
                var offsetStart = 0;
                var offsetEnd = 0;
                var result = new List<Rebar>();

                var host = AC.Document.CreateHost(BuiltInCategory.OST_StructuralFraming);
                var cover = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.ElementInstances.CoverBeam;
                var mainStirrupDtoCommon = new MainStirrupCollectionDto
                {
                    CoverFootBeam = new CoverFootBeam(new CoverBeam
                    { BottomCover = cover.BottomCover, TopCover = cover.TopCover, LeftCover = cover.LeftCover, RightCover = cover.RightCover }),
                    Direction = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX,
                    Host = host,
                    Document = AC.Document
                };

                var rebarBeams = installRebarBeamV2ViewModel.ElementInstances.RebarBeams;
                var subBeams = installRebarBeamV2ViewModel.ElementInstances.Beam.ElementSubs;
                var cb = 0;
                foreach (var subBeam in subBeams)
                {
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

                    var diameterName = rebarBeam.RebarBeamSectionStart.RebarBeamStirrup.Diameter;
                    var diameter = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                        .First(x => x.NameStyle == diameterName).RebarBarType.GetRebarDiameter();

                    MainStirrupCollectionDto mainStirrupSegmentStart = null, mainStirrupSegmentMid = null, mainStirrupSegmentEnd = null;
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
                            lengthSegment = lengthSegment - offsetStart - diameter * 2;
                        }
                        else if (i == qbeamStressRule - 1)
                        {
                            segmentType = RebarBeamSectionType.SectionEnd;
                            lengthSegment = lengthSegment - offsetEnd - diameter * 2;
                        }
                        else
                        {
                            lengthSegment -= diameter * 2;
                        }

                        var mainStirrupDto = mainStirrupDtoCommon.Copy();
                        mainStirrupDto.RebarBarTypeCustom = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                            .FirstOrDefault(x => x.NameStyle == diameterName);
                        mainStirrupDto.Spacing = i switch
                        {
                            0 => rebarBeam.RebarBeamSectionStart.RebarBeamStirrup.Spacing.MmToFoot(),
                            1 => rebarBeam.RebarBeamSectionMid.RebarBeamStirrup.Spacing.MmToFoot(),
                            _ => rebarBeam.RebarBeamSectionEnd.RebarBeamStirrup.Spacing.MmToFoot(),
                        };

                        var transform = Transform.CreateTranslation(mainStirrupDtoCommon.Direction * segmentStart[i]);


                        segmentBoxInOneBeam.P1 = transform.OfPoint(segmentBoxInOneBeam.P1);
                        segmentBoxInOneBeam.P5 = transform.OfPoint(segmentBoxInOneBeam.P5);
                        segmentBoxInOneBeam.P2 = transform.OfPoint(segmentBoxInOneBeam.P2);
                        segmentBoxInOneBeam.P6 = transform.OfPoint(segmentBoxInOneBeam.P6);

                        switch (segmentType)
                        {
                            case RebarBeamSectionType.SectionStart:
                                {
                                    var offsetDirection = mainStirrupDtoCommon.Direction * (diameter * 0.5 + offsetStart);
                                    segmentBoxInOneBeam.P1 += offsetDirection;
                                    segmentBoxInOneBeam.P5 += offsetDirection;
                                    segmentBoxInOneBeam.P2 += offsetDirection;
                                    segmentBoxInOneBeam.P6 += offsetDirection;
                                    break;
                                }
                            case RebarBeamSectionType.SectionMid or RebarBeamSectionType.SectionEnd:
                                {
                                    var offsetDirection = mainStirrupDtoCommon.Direction * (diameter * 1.5);
                                    segmentBoxInOneBeam.P1 += offsetDirection;
                                    segmentBoxInOneBeam.P5 += offsetDirection;
                                    segmentBoxInOneBeam.P2 += offsetDirection;
                                    segmentBoxInOneBeam.P6 += offsetDirection;
                                    break;
                                }
                        }

                        segmentBoxInOneBeam.P3 = segmentBoxInOneBeam.P2 + lengthSegment * mainStirrupDtoCommon.Direction;
                        segmentBoxInOneBeam.P4 = segmentBoxInOneBeam.P1 + lengthSegment * mainStirrupDtoCommon.Direction;
                        segmentBoxInOneBeam.P7 = segmentBoxInOneBeam.P6 + lengthSegment * mainStirrupDtoCommon.Direction;
                        segmentBoxInOneBeam.P8 = segmentBoxInOneBeam.P5 + lengthSegment * mainStirrupDtoCommon.Direction;

                        if (segmentType is RebarBeamSectionType.SectionEnd)
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
                        mainStirrupDto.BoxElementPoint = segmentBoxInOneBeam;
                        mainStirrupDto.Direction = (segmentBoxInOneBeam.P4 - segmentBoxInOneBeam.P1).Normalize();
                        switch (segmentType)
                        {
                            case RebarBeamSectionType.SectionStart:
                                mainStirrupSegmentStart = mainStirrupDto;
                                break;
                            case RebarBeamSectionType.SectionMid:
                                mainStirrupSegmentMid = mainStirrupDto;
                                break;
                            case RebarBeamSectionType.SectionEnd:
                                mainStirrupSegmentEnd = mainStirrupDto;
                                break;
                        }
                    }

                    //rải thép ở segment Start, End trước
                    InstallMainStirrupRebarBeam installMainStirrupRebarStartSegment = null, installMainStirrupRebarEndSegment = null,
                        installMainStirrupRebarMidSegment = null;
                    if (installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.MainStirrupType1)
                    {
                        if (!installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.MainStirrupTypeHat)
                        {
                            installMainStirrupRebarStartSegment = new MainStirrupShape3(mainStirrupSegmentStart);
                            installMainStirrupRebarEndSegment = new MainStirrupShape3(mainStirrupSegmentEnd);
                        }
                        else
                        {
                            installMainStirrupRebarStartSegment = new MainStirrupShape3_2(mainStirrupSegmentStart);
                            installMainStirrupRebarEndSegment = new MainStirrupShape3_2(mainStirrupSegmentEnd);
                        }
                    }
                    else if (installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.MainStirrupType2)
                    {
                        installMainStirrupRebarStartSegment = new MainStirrupShape1(mainStirrupSegmentStart);
                        installMainStirrupRebarEndSegment = new MainStirrupShape1(mainStirrupSegmentEnd);
                    }
                    else if (installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.MainStirrupType3)
                    {
                        installMainStirrupRebarStartSegment = new MainStirrupShape2(mainStirrupSegmentStart);
                        installMainStirrupRebarEndSegment = new MainStirrupShape2(mainStirrupSegmentEnd);
                    }

                    var lastPositionStartSegment = installMainStirrupRebarStartSegment.RunForEndAndStartSegment();
                    result.AddRange(installMainStirrupRebarStartSegment.Rebars);

                    var lastPositionEndSegment = installMainStirrupRebarEndSegment.RunForEndAndStartSegment();
                    result.AddRange(installMainStirrupRebarEndSegment.Rebars);

                    //biến đổi lại box cua mid segment

                    var startPlane = BPlane.CreateByNormalAndOrigin(mainStirrupSegmentMid.Direction,
                        lastPositionStartSegment.Item1.Transform.OfPoint(lastPositionStartSegment.Item1.BottomLeft));
                    mainStirrupSegmentMid.BoxElementPoint.P1 =
                        mainStirrupSegmentMid.BoxElementPoint.P1.ProjectOnto(startPlane);
                    mainStirrupSegmentMid.BoxElementPoint.P2 =
                        mainStirrupSegmentMid.BoxElementPoint.P2.ProjectOnto(startPlane);
                    mainStirrupSegmentMid.BoxElementPoint.P5 =
                        mainStirrupSegmentMid.BoxElementPoint.P5.ProjectOnto(startPlane);
                    mainStirrupSegmentMid.BoxElementPoint.P6 =
                        mainStirrupSegmentMid.BoxElementPoint.P6.ProjectOnto(startPlane);
                    var endPlane = BPlane.CreateByNormalAndOrigin(mainStirrupSegmentMid.Direction,
                        lastPositionEndSegment.Item1.Transform.OfPoint(lastPositionEndSegment.Item1.BottomLeft));
                    mainStirrupSegmentMid.BoxElementPoint.P4 =
                        mainStirrupSegmentMid.BoxElementPoint.P4.ProjectOnto(endPlane);
                    mainStirrupSegmentMid.BoxElementPoint.P3 =
                        mainStirrupSegmentMid.BoxElementPoint.P3.ProjectOnto(endPlane);
                    mainStirrupSegmentMid.BoxElementPoint.P8 =
                        mainStirrupSegmentMid.BoxElementPoint.P8.ProjectOnto(endPlane);
                    mainStirrupSegmentMid.BoxElementPoint.P7 =
                        mainStirrupSegmentMid.BoxElementPoint.P7.ProjectOnto(endPlane);

                    if (installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.MainStirrupType1)
                    {
                        installMainStirrupRebarMidSegment = !installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.MainStirrupTypeHat
                            ? new MainStirrupShape3(mainStirrupSegmentMid)
                            : new MainStirrupShape3_2(mainStirrupSegmentMid);
                    }
                    else if (installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.MainStirrupType2)
                    {
                        installMainStirrupRebarMidSegment = new MainStirrupShape1(mainStirrupSegmentMid);
                    }
                    else if (installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.MainStirrupType3)
                    {
                        installMainStirrupRebarMidSegment = new MainStirrupShape2(mainStirrupSegmentMid);
                    }

                    installMainStirrupRebarMidSegment.RunAtMidSegment(lastPositionStartSegment.Item1, lastPositionEndSegment.Item1);
                    result.AddRange(installMainStirrupRebarMidSegment.Rebars);

                    cb++;
                }

                var diameterCommon = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                    .First(x => x.NameStyle == installRebarBeamV2ViewModel.ElementInstances.RebarBeams.First().RebarBeamSectionStart.RebarBeamStirrup.Diameter)
                    .ModelBarDiameter;

                ElementTransformUtils.MoveElements(
                    AC.Document,
                    result.Select(x => x.Id).ToList(),
                    installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX * diameterCommon);
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create main stirrups.", ex);
            }
        }

        public List<Rebar> InstallRebarSubVerticalStirrup(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {
            try
            {
                var offsetStart = 0;
                var offsetEnd = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                    .First(x => x.NameStyle == installRebarBeamV2ViewModel.ElementInstances.RebarBeams.First().RebarBeamSectionStart.RebarBeamStirrup.Diameter)
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

                var host = AC.Document.CreateHost(BuiltInCategory.OST_StructuralFraming);
                var rebarBeams = installRebarBeamV2ViewModel.ElementInstances.RebarBeams;
                var subBeams = installRebarBeamV2ViewModel.ElementInstances.Beam.ElementSubs;
                var cb = 0;

                var rebarInfo = _subInstallRebarBeamInModelService.GetRebarBeamGroupInfo(
                        installRebarBeamV2ViewModel,
                        RebarBeamSectionType.SectionStart,
                        RebarBeamMainBarLevelType.RebarTop,
                        RebarBeamMainBarGroupType.GroupLevel1)
                    .FirstOrDefault();
                var diameterTop1 = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                    .FirstOrDefault(x => x.NameStyle == rebarInfo.Diameter);

                var mainRebarReals = _subInstallRebarBeamInModelService.GetMainBarBeamReals(
                    installRebarBeamV2ViewModel,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamMainBarGroupType.GroupLevel1,
                    diameterTop1.ModelBarDiameter / 4);
                var mainRebarRealsBottom = _subInstallRebarBeamInModelService.GetMainBarBeamReals(
                    installRebarBeamV2ViewModel,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamMainBarGroupType.GroupLevel1,
                    diameterTop1.ModelBarDiameter / 4);
                if (mainRebarReals.Count < mainRebarRealsBottom.Count)
                {
                    mainRebarReals = mainRebarRealsBottom;
                }
                var curvesInAllBeams = mainRebarReals.Select(x => x.StartPoint.CreateLine(x.EndPoint)).ToList();

                var vectorX = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
                var vectorY = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
                var vectorZ = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;
                foreach (var subBeam in subBeams)
                {
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

                    var diameterName = rebarBeam.RebarBeamSectionStart.RebarBeamStirrup.Diameter;
                    var rebarTypeCustom = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                        .First(x => x.NameStyle == diameterName);
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

                        for (var j = 0; j < hooks.Count; j++)
                        {
                            if (!hooks[j]) continue;

                            var originBottomForPlane = segmentBoxInOneBeam.P1.Add((segmentBoxInOneBeam.P5 -
                                                                                   segmentBoxInOneBeam.P1).Normalize() *
                                                                                  coverFootBeam.BottomCover);

                            var originTopForPlane = segmentBoxInOneBeam.P5.Add((segmentBoxInOneBeam.P1 -
                                                                                segmentBoxInOneBeam.P5).Normalize() *
                                                                               coverFootBeam.TopCover);

                            var bottom = curveInSegment[j].SP()
                                .ProjectOnto(BPlane.CreateByNormalAndOrigin(
                                    vectorZ,
                                    originBottomForPlane));

                            var top = curveInSegment[j].SP()
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
                    cb++;
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create vertical secondary stirrups.", ex);
            }
        }

        public List<Rebar> InstallRebarSubHorizontalStirrupForMainRebar(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {
            try
            {
                var offsetStart = 0;
                var offsetEnd = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                    .First(x => x.NameStyle == installRebarBeamV2ViewModel.ElementInstances.RebarBeams.First().RebarBeamSectionStart.RebarBeamStirrup.Diameter)
                    .ModelBarDiameter; ;
                var cover = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.ElementInstances.CoverBeam;
                var coverFootBeam = new CoverFootBeam(new CoverBeam
                {
                    BottomCover = cover.BottomCover,
                    TopCover = cover.TopCover,
                    LeftCover = cover.LeftCover,
                    RightCover = cover.RightCover
                });
                var result = new List<Rebar>();

                var host = AC.Document.CreateHost(BuiltInCategory.OST_StructuralFraming);
                var rebarBeams = installRebarBeamV2ViewModel.ElementInstances.RebarBeams;
                var subBeams = installRebarBeamV2ViewModel.ElementInstances.Beam.ElementSubs;

                var vectorX = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
                var vectorY = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
                var vectorZ = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;

                // lặp 4 lần: top2, top3, bot2, bot 3
                for (var sectionIndex = 0; sectionIndex < 4; sectionIndex++)
                {
                    List<MainBarBeamReal> mainRebarReals = null;
                    if (sectionIndex == 0)
                    {
                        var rebarInfo = _subInstallRebarBeamInModelService.GetRebarBeamGroupInfo(
                                installRebarBeamV2ViewModel,
                                RebarBeamSectionType.SectionStart,
                                RebarBeamMainBarLevelType.RebarTop,
                                RebarBeamMainBarGroupType.GroupLevel2)
                            .FirstOrDefault();
                        var diameter = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                            .FirstOrDefault(x => x.NameStyle == rebarInfo.Diameter);

                        mainRebarReals = _subInstallRebarBeamInModelService.GetMainBarBeamReals(
                            installRebarBeamV2ViewModel,
                            RebarBeamMainBarLevelType.RebarTop,
                            RebarBeamMainBarGroupType.GroupLevel2,
                            diameter.ModelBarDiameter / 4);
                    }
                    else if (sectionIndex == 1)
                    {
                        var rebarInfo = _subInstallRebarBeamInModelService.GetRebarBeamGroupInfo(
                                installRebarBeamV2ViewModel,
                                RebarBeamSectionType.SectionStart,
                                RebarBeamMainBarLevelType.RebarTop,
                                RebarBeamMainBarGroupType.GroupLevel3)
                            .FirstOrDefault();
                        var diameter = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                            .FirstOrDefault(x => x.NameStyle == rebarInfo.Diameter);

                        mainRebarReals = _subInstallRebarBeamInModelService.GetMainBarBeamReals(
                            installRebarBeamV2ViewModel,
                            RebarBeamMainBarLevelType.RebarTop,
                            RebarBeamMainBarGroupType.GroupLevel3,
                            diameter.ModelBarDiameter / 4);
                    }
                    else if (sectionIndex == 2)
                    {
                        var rebarInfo = _subInstallRebarBeamInModelService.GetRebarBeamGroupInfo(
                                installRebarBeamV2ViewModel,
                                RebarBeamSectionType.SectionStart,
                                RebarBeamMainBarLevelType.RebarBot,
                                RebarBeamMainBarGroupType.GroupLevel2)
                            .FirstOrDefault();
                        var diameter = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                            .FirstOrDefault(x => x.NameStyle == rebarInfo.Diameter);

                        mainRebarReals = _subInstallRebarBeamInModelService.GetMainBarBeamReals(
                            installRebarBeamV2ViewModel,
                            RebarBeamMainBarLevelType.RebarBot,
                            RebarBeamMainBarGroupType.GroupLevel2,
                            diameter.ModelBarDiameter / 4);
                    }
                    else if (sectionIndex == 3)
                    {
                        var rebarInfo = _subInstallRebarBeamInModelService.GetRebarBeamGroupInfo(
                                installRebarBeamV2ViewModel,
                                RebarBeamSectionType.SectionStart,
                                RebarBeamMainBarLevelType.RebarBot,
                                RebarBeamMainBarGroupType.GroupLevel3)
                            .FirstOrDefault();
                        var diameter = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                            .FirstOrDefault(x => x.NameStyle == rebarInfo.Diameter);

                        mainRebarReals = _subInstallRebarBeamInModelService.GetMainBarBeamReals(
                            installRebarBeamV2ViewModel,
                            RebarBeamMainBarLevelType.RebarBot,
                            RebarBeamMainBarGroupType.GroupLevel3,
                            diameter.ModelBarDiameter / 4);
                    }

                    if (mainRebarReals == null || !mainRebarReals.Any()) continue;

                    var cb = 0;

                    var curvesInAllBeams = mainRebarReals.Select(x => x.StartPoint.CreateLine(x.EndPoint)).ToList();
                    foreach (var subBeam in subBeams)
                    {
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

                        var diameter = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                            .First(x => x.NameStyle == rebarBeam.RebarBeamSectionStart.RebarBeamStirrup.Diameter)
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
                            else
                            {
                                lengthSegment -= diameter;
                            }

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

                            var curveInSegment = curvesInAllBeams.Where(x =>
                            {
                                var pointIntersect = x.SP().ProjectOnto(BPlane.CreateByNormalAndOrigin(vectorX, lineBetween.SP()));
                                return pointIntersect != null && pointIntersect.IsPointInsideLine(x, 1.MmToFoot());
                            })
                                .OrderBy(x =>
                                    x.SP().DotProduct(vectorY)).ToList();

                            var hasHook = false;
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


                            RebarBarTypeCustom rebarTypeCustom = null;

                            switch (sectionIndex)
                            {
                                case 0:
                                    hasHook = rebarBeamSection.RebarBeamTop.RebarBeamTopLevel2.HasHorizontalHook;
                                    rebarTypeCustom = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                                        .First(x => x.NameStyle == rebarBeamSection.RebarBeamTop.RebarBeamTopLevel2.Diameter);
                                    break;
                                case 1:
                                    hasHook = rebarBeamSection.RebarBeamTop.RebarBeamTopLevel3.HasHorizontalHook;
                                    rebarTypeCustom = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                                        .First(x => x.NameStyle ==
                                                    rebarBeamSection.RebarBeamTop.RebarBeamTopLevel3.Diameter);
                                    break;
                                case 2:
                                    hasHook = rebarBeamSection.RebarBeamBot.RebarBeamBotLevel2.HasHorizontalHook;
                                    rebarTypeCustom = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                                        .First(x => x.NameStyle ==
                                                    rebarBeamSection.RebarBeamBot.RebarBeamBotLevel2.Diameter);
                                    break;
                                case 3:
                                    hasHook = rebarBeamSection.RebarBeamBot.RebarBeamBotLevel3.HasHorizontalHook;
                                    rebarTypeCustom = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                                        .First(x => x.NameStyle ==
                                                    rebarBeamSection.RebarBeamBot.RebarBeamBotLevel3.Diameter);
                                    break;
                            }

                            if (!hasHook) continue;

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
                            var left = curveInSegment.First().SP().ProjectOnto(bPlane);
                            var right = curveInSegment.Last().SP().ProjectOnto(bPlane);
                            var directionInside = sectionIndex is 0 or 1
                                ? (segmentBoxInOneBeam.P1 - segmentBoxInOneBeam.P5).Normalize()
                                : -(segmentBoxInOneBeam.P1 - segmentBoxInOneBeam.P5).Normalize();
                            left -= directionInside * (diameter + rebarTypeCustom.ModelBarDiameter * 0.5);
                            right -= directionInside * (diameter + rebarTypeCustom.ModelBarDiameter * 0.5);

                            left = left.ProjectOnto(BPlane.CreateByNormalAndOrigin(vectorY, segmentBoxInOneBeam.P5));
                            right = right.ProjectOnto(BPlane.CreateByNormalAndOrigin(vectorY, segmentBoxInOneBeam.P6));

                            left -= (left - right).Normalize() * coverFootBeam.LeftCover;
                            right -= (right - left).Normalize() * coverFootBeam.RightCover;
                            var spacing = segmentType switch
                            {
                                RebarBeamSectionType.SectionStart => rebarBeam.RebarBeamSectionStart.RebarBeamStirrup.Spacing.MmToFoot(),
                                RebarBeamSectionType.SectionMid => rebarBeam.RebarBeamSectionMid.RebarBeamStirrup.Spacing.MmToFoot(),
                                RebarBeamSectionType.SectionEnd => rebarBeam.RebarBeamSectionEnd.RebarBeamStirrup.Spacing.MmToFoot(),
                                _ => throw new ArgumentOutOfRangeException()
                            };

                            var subStirrupCollection = new SubHorizontalStirrupCollectionDto
                            {
                                BoxElementPoint = segmentBoxInOneBeam,
                                Left = left,
                                CoverFootBeam = coverFootBeam,
                                Direction = (segmentBoxInOneBeam.P4 - segmentBoxInOneBeam.P1).Normalize(),
                                Document = AC.Document,
                                Host = host,
                                RebarBarTypeCustom = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                                    .First(x => x.NameStyle == rebarBeam.RebarBeamSectionStart.RebarBeamStirrup.Diameter),
                                Spacing = spacing,
                                Right = right,
                                DirectionInside = directionInside,
                            };

                            if (segmentType == RebarBeamSectionType.SectionStart)
                            {
                                stirrupStartSegment.Add(subStirrupCollection);
                            }
                            else if (segmentType == RebarBeamSectionType.SectionMid)
                            {
                                stirrupMidSegment.Add(subStirrupCollection);
                            }
                            else
                            {
                                stirrupEndSegment.Add(subStirrupCollection);
                            }
                        }

                        if (!stirrupStartSegment.Any()) continue;

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

                            var curves = new List<Curve>()
                        {
                            stirrupMidSegment1.BoxElementPoint.P1.CreateLine(stirrupMidSegment1.BoxElementPoint.P2),
                            stirrupMidSegment1.BoxElementPoint.P2.CreateLine(stirrupMidSegment1.BoxElementPoint.P3),
                            stirrupMidSegment1.BoxElementPoint.P3.CreateLine(stirrupMidSegment1.BoxElementPoint.P4),
                            stirrupMidSegment1.BoxElementPoint.P4.CreateLine(stirrupMidSegment1.BoxElementPoint.P1),
                        };
                            var bPlane = BPlane.CreateByNormalAndOrigin(vectorX, stirrupMidSegment1.BoxElementPoint.P1);
                            stirrupMidSegment1.Left = stirrupMidSegment1.Left.ProjectOnto(bPlane);
                            stirrupMidSegment1.Right = stirrupMidSegment1.Right.ProjectOnto(bPlane);

                            LSTool.Tools.Beams.InstallRebarBeamV2.service.SubHorizontalStirrup.InstallSubStirrupRebarBeam
                                installStirrupRebarMidSegment =
                                installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.HorizontalDaiPhu
                                ? new LSTool.Tools.Beams.InstallRebarBeamV2.service.SubHorizontalStirrup.SubStirrupShape2(stirrupMidSegment1)
                                : new LSTool.Tools.Beams.InstallRebarBeamV2.service.SubHorizontalStirrup.SubStirrupShape1(stirrupMidSegment1);
                            installStirrupRebarMidSegment.RunAtMidSegment(lastPositionStartSegment.Item1, lastPositionEndSegment.Item1);
                            result.AddRange(installStirrupRebarMidSegment.Rebars);
                        }
                        cb++;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create horizontal secondary stirrups for main bars.", ex);
            }
        }

        public List<Rebar> InstallRebarSubHorizontalStirrupForSideRebar(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {
            try
            {
                var offsetStart = 0.MmToFoot();
                var offsetEnd = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                    .First(x => x.NameStyle == installRebarBeamV2ViewModel.ElementInstances.RebarBeams.First().RebarBeamSectionStart.RebarBeamStirrup.Diameter)
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

                var host = AC.Document.CreateHost(BuiltInCategory.OST_StructuralFraming);
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

                    var diameter = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                        .First(x => x.NameStyle == rebarBeam.RebarBeamSectionStart.RebarBeamStirrup.Diameter)
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
                        var rebarBarTypeSideCustom = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                            .First(x => x.NameStyle == diameterName);

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
                                RebarBarTypeCustom = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                                    .First(x => x.NameStyle == installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.RebarDiameterHorizontalDaiPhuChongPhinh),
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
                    cb++;
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create horizontal secondary stirrups for side bars.", ex);
            }
        }

        public List<Rebar> InstallRebarTop1(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {
            var result = new List<Rebar>();
            try
            {
                var host = AC.Document.CreateHost(BuiltInCategory.OST_StructuralFraming);
                var vtx = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
                var vty = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
                var vtz = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;

                var rebarInfo = _subInstallRebarBeamInModelService.GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamMainBarGroupType.GroupLevel1)
                    .FirstOrDefault();
                var diameter = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                    .FirstOrDefault(x => x.NameStyle == rebarInfo.Diameter);
                var mainRebarReals = _subInstallRebarBeamInModelService.GetMainBarBeamReals(
                    installRebarBeamV2ViewModel,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamMainBarGroupType.GroupLevel1,
                    diameter.ModelBarDiameter / 4);
                if (mainRebarReals == null)
                    throw new InvalidOperationException("Top level 1 bar geometry could not be calculated.");

                foreach (var mainBarBeamReal in mainRebarReals)
                {
                    try
                    {
                        var shapes = mainBarBeamReal.MainPoints.PointsToCurves();
                        var rebar = RebarCreationCompat.CreateFromCurves(
                            AC.Document,
                            RebarStyle.Standard,
                            diameter.RebarBarType,
                            host,
                            -vty,
                            shapes,
                            true,
                            true);
                        RevRebarUtils.SetSolidRebar3DView(rebar, AC.Document.ActiveView);
                        result.Add(rebar);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Failed to create a top level 1 bar.", ex);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create top level 1 bars.", ex);
            }
        }

        public List<Rebar> InstallRebarTop2(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {
            var result = new List<Rebar>();
            try
            {
                var host = AC.Document.CreateHost(BuiltInCategory.OST_StructuralFraming);
                var vtx = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
                var vty = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
                var vtz = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;

                var rebarInfo = _subInstallRebarBeamInModelService.GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamMainBarGroupType.GroupLevel2)
                    .FirstOrDefault();
                var diameter = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                    .FirstOrDefault(x => x.NameStyle == rebarInfo.Diameter);
                var mainRebarReals = _subInstallRebarBeamInModelService.GetMainBarBeamReals(
                    installRebarBeamV2ViewModel,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamMainBarGroupType.GroupLevel2,
                    diameter.ModelBarDiameter / 4);
                if (mainRebarReals == null)
                    throw new InvalidOperationException("Top level 2 bar geometry could not be calculated.");

                foreach (var mainBarBeamReal in mainRebarReals)
                {
                    try
                    {
                        var shapes = mainBarBeamReal.MainPoints.PointsToCurves();
                        var rebar = RebarCreationCompat.CreateFromCurves(
                            AC.Document,
                            RebarStyle.Standard,
                            diameter.RebarBarType,
                            host,
                            -vty,
                            shapes,
                            true,
                            true);
                        RevRebarUtils.SetSolidRebar3DView(rebar, AC.Document.ActiveView);
                        result.Add(rebar);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Failed to create a top level 2 bar.", ex);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create top level 2 bars.", ex);
            }
        }

        public List<Rebar> InstallRebarTop3(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {
            var result = new List<Rebar>();
            try
            {
                var host = AC.Document.CreateHost(BuiltInCategory.OST_StructuralFraming);
                var vtx = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
                var vty = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
                var vtz = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;

                var rebarInfo = _subInstallRebarBeamInModelService.GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamMainBarGroupType.GroupLevel3)
                    .FirstOrDefault();
                var diameter = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                    .FirstOrDefault(x => x.NameStyle == rebarInfo.Diameter);
                var mainRebarReals = _subInstallRebarBeamInModelService.GetMainBarBeamReals(
                    installRebarBeamV2ViewModel,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamMainBarGroupType.GroupLevel3,
                    diameter.ModelBarDiameter / 4);
                if (mainRebarReals == null)
                    throw new InvalidOperationException("Top level 3 bar geometry could not be calculated.");

                foreach (var mainBarBeamReal in mainRebarReals)
                {
                    try
                    {
                        var shapes = mainBarBeamReal.MainPoints.PointsToCurves();
                        var rebar = RebarCreationCompat.CreateFromCurves(
                            AC.Document,
                            RebarStyle.Standard,
                            diameter.RebarBarType,
                            host,
                            -vty,
                            shapes,
                            true,
                            true);
                        RevRebarUtils.SetSolidRebar3DView(rebar, AC.Document.ActiveView);
                        result.Add(rebar);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Failed to create a top level 3 bar.", ex);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create top level 3 bars.", ex);
            }
        }

        public List<Rebar> InstallRebarBot1(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {
            var result = new List<Rebar>();
            try
            {
                var host = AC.Document.CreateHost(BuiltInCategory.OST_StructuralFraming);
                var vtx = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
                var vty = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
                var vtz = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;

                var rebarInfo = _subInstallRebarBeamInModelService.GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamMainBarGroupType.GroupLevel1)
                    .FirstOrDefault();
                var diameter = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                    .FirstOrDefault(x => x.NameStyle == rebarInfo.Diameter);
                var mainRebarReals = _subInstallRebarBeamInModelService.GetMainBarBeamReals(
                    installRebarBeamV2ViewModel,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamMainBarGroupType.GroupLevel1,
                    diameter.ModelBarDiameter / 4);
                if (mainRebarReals == null)
                    throw new InvalidOperationException("Bottom level 1 bar geometry could not be calculated.");

                foreach (var mainBarBeamReal in mainRebarReals)
                {
                    try
                    {
                        var shapes = mainBarBeamReal.MainPoints.PointsToCurves();
                        var rebar = RebarCreationCompat.CreateFromCurves(
                            AC.Document,
                            RebarStyle.Standard,
                            diameter.RebarBarType,
                            host,
                            -vty,
                            shapes,
                            true,
                            true);
                        RevRebarUtils.SetSolidRebar3DView(rebar, AC.Document.ActiveView);
                        result.Add(rebar);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Failed to create a bottom level 1 bar.", ex);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create bottom level 1 bars.", ex);
            }
        }

        public List<Rebar> InstallRebarBot2(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {
            var result = new List<Rebar>();
            try
            {
                var host = AC.Document.CreateHost(BuiltInCategory.OST_StructuralFraming);
                var vtx = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
                var vty = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
                var vtz = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;

                var rebarInfo = _subInstallRebarBeamInModelService.GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamMainBarGroupType.GroupLevel2)
                    .FirstOrDefault();
                var diameter = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                    .FirstOrDefault(x => x.NameStyle == rebarInfo.Diameter);
                var mainRebarReals = _subInstallRebarBeamInModelService.GetMainBarBeamReals(
                    installRebarBeamV2ViewModel,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamMainBarGroupType.GroupLevel2,
                    diameter.ModelBarDiameter / 4);
                if (mainRebarReals == null)
                    throw new InvalidOperationException("Bottom level 2 bar geometry could not be calculated.");

                foreach (var mainBarBeamReal in mainRebarReals)
                {
                    try
                    {
                        var shapes = mainBarBeamReal.MainPoints.PointsToCurves();
                        var rebar = RebarCreationCompat.CreateFromCurves(
                            AC.Document,
                            RebarStyle.Standard,
                            diameter.RebarBarType,
                            host,
                            -vty,
                            shapes,
                            true,
                            true);
                        RevRebarUtils.SetSolidRebar3DView(rebar, AC.Document.ActiveView);
                        result.Add(rebar);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Failed to create a bottom level 2 bar.", ex);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create bottom level 2 bars.", ex);
            }
        }

        public List<Rebar> InstallRebarBot3(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {
            var result = new List<Rebar>();
            try
            {
                var host = AC.Document.CreateHost(BuiltInCategory.OST_StructuralFraming);
                var vtx = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
                var vty = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
                var vtz = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;

                var rebarInfo = _subInstallRebarBeamInModelService.GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamMainBarGroupType.GroupLevel3)
                    .FirstOrDefault();
                var diameter = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                    .FirstOrDefault(x => x.NameStyle == rebarInfo.Diameter);
                var mainRebarReals = _subInstallRebarBeamInModelService.GetMainBarBeamReals(
                    installRebarBeamV2ViewModel,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamMainBarGroupType.GroupLevel3,
                    diameter.ModelBarDiameter / 4);
                if (mainRebarReals == null)
                    throw new InvalidOperationException("Bottom level 3 bar geometry could not be calculated.");

                foreach (var mainBarBeamReal in mainRebarReals)
                {
                    try
                    {
                        var shapes = mainBarBeamReal.MainPoints.PointsToCurves();
                        var rebar = RebarCreationCompat.CreateFromCurves(
                            AC.Document,
                            RebarStyle.Standard,
                            diameter.RebarBarType,
                            host,
                            -vty,
                            shapes,
                            true,
                            true);
                        RevRebarUtils.SetSolidRebar3DView(rebar, AC.Document.ActiveView);
                        result.Add(rebar);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Failed to create a bottom level 3 bar.", ex);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create bottom level 3 bars.", ex);
            }
        }

        public List<Rebar> InstallRebarSide(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {
            try
            {
                var results = new List<Rebar>();
                var host = AC.Document.CreateHost(BuiltInCategory.OST_StructuralFraming);
                var vtx = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
                var vty = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
                var vtz = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;
                var rebarSides = _subInstallRebarBeamInModelService.GetSideBarBeamReals(
                    installRebarBeamV2ViewModel,
                    0);
                foreach (var r in rebarSides)
                {
                    var diameterSide = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                            .FirstOrDefault(x => x.NameStyle == r.Diameter);
                    var l = r.StartPoint.CreateLine(r.EndPoint);
                    var rebar = RebarCreationCompat.CreateFromCurves(
                            AC.Document,
                            RebarStyle.Standard,
                            diameterSide.RebarBarType,
                            host,
                            -vty,
                            new List<Curve>() { l },
                            true,
                            true);
                    RevRebarUtils.SetSolidRebar3DView(rebar, AC.Document.ActiveView);
                    results.Add(rebar);
                }
                return results;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create side bars.", ex);
            }
        }

        public List<Rebar> InstallRebarDantory(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {
            try
            {
                var results = new List<Rebar>();
                var host = AC.Document.CreateHost(BuiltInCategory.OST_StructuralFraming);
                var vtx = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
                var vty = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
                var vtz = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;
                var rebarDantories = _subInstallRebarBeamInModelService.GetDantoryBarBeamReals(
                    installRebarBeamV2ViewModel,
                    0);
                foreach (var r in rebarDantories)
                {
                    var diameterSide = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                            .FirstOrDefault(x => x.NameStyle == r.Diameter);
                    var l = r.StartPoint.CreateLine(r.EndPoint);
                    var rebar = RebarCreationCompat.CreateFromCurves(
                            AC.Document,
                            RebarStyle.Standard,
                            diameterSide.RebarBarType,
                            host,
                            -vty,
                            new List<Curve>() { l },
                            true,
                            true);
                    RevRebarUtils.SetSolidRebar3DView(rebar, AC.Document.ActiveView);
                    results.Add(rebar);
                }
                return results;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create dantory bars.", ex);
            }
        }
    }
}


