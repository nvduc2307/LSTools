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
        private List<Rebar> InstallRebarStirrup(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            RebarExecutionContext context)
        {
            try
            {
                var offsetStart = 0;
                var offsetEnd = 0;
                var result = new List<Rebar>();

                var host = context.TemporaryHost;
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
                var mainStirrupShape = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive;
                var defaultedMainStirrupShape = mainStirrupShape.EnsureMainStirrupShapeSelected();
                context.DiagnosticLog?.Record("stirrup.main.shape", new
                {
                    type1 = mainStirrupShape.MainStirrupType1,
                    type2 = mainStirrupShape.MainStirrupType2,
                    type3 = mainStirrupShape.MainStirrupType3,
                    typeHat = mainStirrupShape.MainStirrupTypeHat,
                    defaultedToType1 = defaultedMainStirrupShape
                });
                var cb = 0;
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

                    var diameterName = rebarBeam.RebarBeamSectionStart.RebarBeamStirrup.Diameter;
                    var diameter = context.GetBarType(diameterName).RebarBarType.GetRebarDiameter();

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
                        mainStirrupDto.RebarBarTypeCustom = context.GetBarType(diameterName);
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
                    if (mainStirrupShape.MainStirrupType1)
                    {
                        if (!mainStirrupShape.MainStirrupTypeHat)
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
                    else if (mainStirrupShape.MainStirrupType2)
                    {
                        installMainStirrupRebarStartSegment = new MainStirrupShape1(mainStirrupSegmentStart);
                        installMainStirrupRebarEndSegment = new MainStirrupShape1(mainStirrupSegmentEnd);
                    }
                    else if (mainStirrupShape.MainStirrupType3)
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

                    if (mainStirrupShape.MainStirrupType1)
                    {
                        installMainStirrupRebarMidSegment = !mainStirrupShape.MainStirrupTypeHat
                            ? new MainStirrupShape3(mainStirrupSegmentMid)
                            : new MainStirrupShape3_2(mainStirrupSegmentMid);
                    }
                    else if (mainStirrupShape.MainStirrupType2)
                    {
                        installMainStirrupRebarMidSegment = new MainStirrupShape1(mainStirrupSegmentMid);
                    }
                    else if (mainStirrupShape.MainStirrupType3)
                    {
                        installMainStirrupRebarMidSegment = new MainStirrupShape2(mainStirrupSegmentMid);
                    }

                    installMainStirrupRebarMidSegment.RunAtMidSegment(lastPositionStartSegment.Item1, lastPositionEndSegment.Item1);
                    result.AddRange(installMainStirrupRebarMidSegment.Rebars);

                    foreach (var rebar in result.Skip(spanResultStartIndex))
                        context.RegisterTargetHost(rebar, subBeam.Id);
                    cb++;
                }

                var diameterCommon = context.GetBarType(
                        installRebarBeamV2ViewModel.ElementInstances.RebarBeams.First()
                            .RebarBeamSectionStart.RebarBeamStirrup.Diameter)
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

    }
}
