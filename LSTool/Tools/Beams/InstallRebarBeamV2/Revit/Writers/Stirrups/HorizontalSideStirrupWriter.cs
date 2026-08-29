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
                    var boxPs = subBeam.BoxElementPoint;
                    var beamLength = boxPs.P1.DistanceTo(boxPs.P4);
                    var diameter = context.GetBarType(
                            rebarBeam.RebarBeamSectionStart.RebarBeamStirrup.Diameter)
                        .RebarBarType.GetRebarDiameter();

                    // Đai phụ của side bar rải liên tục trên toàn dầm, không
                    // cắt theo ba đoạn ứng suất như đai chính. Bước của nó là
                    // một giá trị duy nhất cho cả dầm, và thanh side bar mà nó
                    // buộc cũng chạy liền mạch, nên việc cắt đoạn không mang ý
                    // nghĩa cấu tạo nào - chỉ tạo ra những mối nối lệch bước
                    // làm vỡ nhóm khi gom theo Fixed Number.
                    var beamBox = new BoxElementPoint
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
                    var runLength = beamLength - offsetStart - offsetEnd - diameter;
                    if (runLength <= 0.0)
                    {
                        context.DiagnosticLog?.Record("stirrup.side.skipped", new
                        {
                            beamId = subBeam.Id,
                            reason = "beam is too short for one tie run"
                        });
                        cb++;
                        continue;
                    }

                    var startOffset = vectorX * (diameter / 2 + offsetStart);
                    beamBox.P1 += startOffset;
                    beamBox.P2 += startOffset;
                    beamBox.P5 += startOffset;
                    beamBox.P6 += startOffset;
                    beamBox.P3 = beamBox.P2 + runLength * vectorX;
                    beamBox.P4 = beamBox.P1 + runLength * vectorX;
                    beamBox.P7 = beamBox.P6 + runLength * vectorX;
                    beamBox.P8 = beamBox.P5 + runLength * vectorX;

                    var diameterName = rebarBeam.RebarBeamSectionStart.RebarBeamSideBar.Diameter;
                    var rebarBarTypeSideCustom = context.GetBarType(diameterName);
                    var spacing = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel
                        .SpacingHorizontalDaiPhuChongPhinh.MmToFoot();

                    var bPlane = BPlane.CreateByNormalAndOrigin(vectorX, beamBox.P1);
                    var stirrupsInBeam = new List<SubHorizontalStirrupCollectionDto>();

                    foreach (var lineAtASide in curvesAtASide)
                    {
                        // kiem tra thep chong phinh o tung dam
                        var curveLoops = new CurveLoop();
                        curveLoops.Append(beamBox.P1.CreateLine(beamBox.P2));
                        curveLoops.Append(beamBox.P2.CreateLine(beamBox.P3));
                        curveLoops.Append(beamBox.P3.CreateLine(beamBox.P4));
                        curveLoops.Append(beamBox.P4.CreateLine(beamBox.P1));
                        var solidBox = GeometryCreationUtilities.CreateExtrusionGeometry(
                            new List<CurveLoop>() { curveLoops }, XYZ.BasisZ,
                            beamBox.P1.DistanceTo(beamBox.P5));
                        if (!lineAtASide.GetInsideCurvesIntersectSolid(solidBox).Any())
                        {
                            continue;
                        }

                        var spOnLine = lineAtASide.SP();
                        var directionInside = (beamBox.P1 - beamBox.P5).Normalize();

                        var left = spOnLine.ProjectOnto(bPlane);
                        left = left.ProjectOnto(BPlane.CreateByNormalAndOrigin(vectorY, beamBox.P1));

                        var right = spOnLine.ProjectOnto(bPlane);
                        right = right.ProjectOnto(BPlane.CreateByNormalAndOrigin(vectorY, beamBox.P2));

                        left -= directionInside * (diameter + rebarBarTypeSideCustom.ModelBarDiameter * 0.5);
                        right -= directionInside * (diameter + rebarBarTypeSideCustom.ModelBarDiameter * 0.5);

                        left -= (left - right).Normalize() * coverFootBeam.LeftCover;
                        right -= (right - left).Normalize() * coverFootBeam.RightCover;

                        stirrupsInBeam.Add(new SubHorizontalStirrupCollectionDto
                        {
                            BoxElementPoint = beamBox,
                            Left = left,
                            CoverFootBeam = coverFootBeam,
                            Direction = (beamBox.P4 - beamBox.P1).Normalize(),
                            Document = AC.Document,
                            Host = host,
                            RebarBarTypeCustom = context.GetBarType(
                                installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.RebarDiameterHorizontalDaiPhuChongPhinh),
                            Spacing = spacing,
                            Right = right,
                            DirectionInside = directionInside,
                        });
                    }

                    // Dầm không có thép hông thì cũng không có đai phụ chống
                    // phình cho nó. Trường hợp hợp lệ, ví dụ dầm thấp hơn
                    // ngưỡng bố trí thép hông. Phải tăng cb trước khi continue
                    // vì nó là chỉ số cấu hình dầm, chỉ tăng ở cuối vòng lặp.
                    if (stirrupsInBeam.Count == 0)
                    {
                        context.DiagnosticLog?.Record("stirrup.side.skipped", new
                        {
                            beamId = subBeam.Id,
                            reason = "beam has no side bars"
                        });
                        cb++;
                        continue;
                    }

                    foreach (var stirrupInBeam in stirrupsInBeam)
                    {
                        LSTool.Tools.Beams.InstallRebarBeamV2.service.SubHorizontalStirrup.InstallSubStirrupRebarBeam
                            installStirrupRebar = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.HorizontalDaiPhu
                                ? new LSTool.Tools.Beams.InstallRebarBeamV2.service.SubHorizontalStirrup.SubStirrupShape2(stirrupInBeam)
                                : new LSTool.Tools.Beams.InstallRebarBeamV2.service.SubHorizontalStirrup.SubStirrupShape1(stirrupInBeam);
                        installStirrupRebar.RunForEndAndStartSegment();
                        result.AddRange(installStirrupRebar.Rebars);
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
