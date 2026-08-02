using Autodesk.Revit.DB;
using LSTool.Compatibility;
using LSTool.Tools.Beams.InstallRebarBeamV2.Application.Diagnostics;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using RIMT.Utils;
using RIMT.Utils.BoundingBoxs;
using RIMT.Utils.Geometries;
using RIMT.Utils.RevPoints;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.service
{
    public partial class SubInstallRebarBeamInModelService
    {
        public List<MainBarBeamReal> GetSideBarBeamReals(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            double extentStirrupFt)
        {
            try
            {
                var results = new List<MainBarBeamReal>();
                var extend = 100.MmToFoot();
                var vtx = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
                var vty = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
                var vtz = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;
                var rebarBeams = installRebarBeamV2ViewModel.ElementInstances.RebarBeams;
                var subBeams = installRebarBeamV2ViewModel.ElementInstances.Beam.ElementSubs;
                var qRebarBeams = rebarBeams.Count;
                var expectedCount = 0;
                var diagnosticLog = installRebarBeamV2ViewModel.DiagnosticLog;
                diagnosticLog?.Record("side.geometry.started", new
                {
                    configuredSpanCount = qRebarBeams,
                    physicalSpanCount = subBeams.Count,
                    extentStirrupMm = Math.Round(extentStirrupFt.FootToMm(), 3),
                    extendMm = Math.Round(extend.FootToMm(), 3)
                });
                foreach (var subBeam in subBeams)
                {
                    try
                    {
                        var rebarBeam = rebarBeams.FirstOrDefault(x => x.BeamId == subBeam.Id);
                        var rebarStirrupInfo = rebarBeam.RebarBeamSectionStart.RebarBeamStirrup;
                        var diameterStirrup = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                            .FirstOrDefault(x => x.NameStyle == rebarStirrupInfo.Diameter);
                        var rebarSideInfo = rebarBeam.RebarBeamSectionStart.RebarBeamSideBar;
                        var diameterSide = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                            .FirstOrDefault(x => x.NameStyle == rebarSideInfo.Diameter);
                        var extentCoverSide = diameterStirrup.ModelBarDiameter + diameterSide.ModelBarDiameter / 2;
                        var qtySide = rebarSideInfo.QuantitySide;
                        diagnosticLog?.Record("side.span.input", new
                        {
                            beamId = subBeam.Id,
                            beamName = subBeam.Element?.Name,
                            startQuantity = rebarBeam.RebarBeamSectionStart?.RebarBeamSideBar?.QuantitySide,
                            midQuantity = rebarBeam.RebarBeamSectionMid?.RebarBeamSideBar?.QuantitySide,
                            endQuantity = rebarBeam.RebarBeamSectionEnd?.RebarBeamSideBar?.QuantitySide,
                            startDiameter = rebarBeam.RebarBeamSectionStart?.RebarBeamSideBar?.Diameter,
                            midDiameter = rebarBeam.RebarBeamSectionMid?.RebarBeamSideBar?.Diameter,
                            endDiameter = rebarBeam.RebarBeamSectionEnd?.RebarBeamSideBar?.Diameter,
                            stirrupDiameter = rebarStirrupInfo?.Diameter,
                            calculatedSideDiameter = diameterSide?.NameStyle,
                            axisX = RebarDiagnosticLog.VectorSnapshot(vtx),
                            axisY = RebarDiagnosticLog.VectorSnapshot(vty),
                            axisZ = RebarDiagnosticLog.VectorSnapshot(vtz)
                        });
                        if (qtySide < 0)
                            throw new InvalidOperationException(
                                $"Side-bar quantity cannot be negative for beam {subBeam.Id}.");
                        expectedCount += qtySide * 2;
                        var csBot = GetPointControls(
                            installRebarBeamV2ViewModel,
                            subBeam,
                            RebarBeamMainBarLevelType.RebarBot,
                            RebarBeamMainBarGroupType.GroupLevel3,
                            extentStirrupFt,
                            extentCoverSide)
                            .PointsToCurves(true)
                            .GroupBy(x => Math.Round(x.Length.FootToMm(), 0))
                            .OrderBy(x => x.FirstOrDefault().Length)
                            .LastOrDefault()
                            .OrderBy(x => x.Midpoint().DotProduct(vty))
                            .ToList();
                        var csTop = GetPointControls(
                            installRebarBeamV2ViewModel,
                            subBeam,
                            RebarBeamMainBarLevelType.RebarTop,
                            RebarBeamMainBarGroupType.GroupLevel3,
                            extentStirrupFt,
                            extentCoverSide)
                            .PointsToCurves(true)
                            .GroupBy(x => Math.Round(x.Length.FootToMm(), 0))
                            .OrderBy(x => x.FirstOrDefault().Length)
                            .LastOrDefault()
                            .OrderBy(x => x.Midpoint().DotProduct(vty))
                            .ToList();
                        var pMid = csBot.FirstOrDefault().GetEndPoint(0).Midpoint(csTop.FirstOrDefault().GetEndPoint(0));
                        var heightSpace = csBot.FirstOrDefault().GetEndPoint(0).Distance(csTop.FirstOrDefault().GetEndPoint(0));
                        var spacingSide = heightSpace / (qtySide + 1);

                        var installSpace = (qtySide - 1) * spacingSide;
                        var pLast = pMid - vtz * installSpace / 2;
                        diagnosticLog?.Record("side.span.layout", new
                        {
                            beamId = subBeam.Id,
                            quantitySide = qtySide,
                            heightSpaceMm = Math.Round(heightSpace.FootToMm(), 3),
                            spacingSideMm = Math.Round(spacingSide.FootToMm(), 3),
                            installSpaceMm = Math.Round(installSpace.FootToMm(), 3),
                            midPoint = RebarDiagnosticLog.PointSnapshot(pMid),
                            firstLevelPoint = RebarDiagnosticLog.PointSnapshot(pLast),
                            bottomControlFirst = RebarDiagnosticLog.PointSnapshot(csBot.FirstOrDefault()?.GetEndPoint(0)),
                            bottomControlLast = RebarDiagnosticLog.PointSnapshot(csBot.LastOrDefault()?.GetEndPoint(0)),
                            topControlFirst = RebarDiagnosticLog.PointSnapshot(csTop.FirstOrDefault()?.GetEndPoint(0)),
                            topControlLast = RebarDiagnosticLog.PointSnapshot(csTop.LastOrDefault()?.GetEndPoint(0))
                        });

                        for (int i = 0; i < qtySide; i++)
                        {
                            try
                            {
                                var pTarget = qtySide < 2
                                    ? pMid
                                    : pLast + vtz * i * spacingSide;
                                var lRight = Line.CreateBound(
                                    csBot.FirstOrDefault().GetEndPoint(1).EditZ(pTarget.Z) - vtx * extend,
                                    csBot.FirstOrDefault().GetEndPoint(0).EditZ(pTarget.Z) + vtx * extend);
                                var lLeft = Line.CreateBound(
                                    csBot.LastOrDefault().GetEndPoint(0).EditZ(pTarget.Z) - vtx * extend,
                                    csBot.LastOrDefault().GetEndPoint(1).EditZ(pTarget.Z) + vtx * extend);

                                var rRight = new MainBarBeamReal
                                {
                                    SourceBeamId = subBeam.Id,
                                    StartPoint = lRight.GetEndPoint(0),
                                    EndPoint = lRight.GetEndPoint(1),
                                    Diameter = rebarSideInfo.Diameter
                                };
                                var rLeft = new MainBarBeamReal
                                {
                                    SourceBeamId = subBeam.Id,
                                    StartPoint = lLeft.GetEndPoint(0),
                                    EndPoint = lLeft.GetEndPoint(1),
                                    Diameter = rebarSideInfo.Diameter
                                };
                                diagnosticLog?.Record("side.geometry.planned", new
                                {
                                    beamId = subBeam.Id,
                                    layerIndex = i,
                                    layerNumber = i + 1,
                                    levelAlongAxisZMm = Math.Round(pTarget.DotProduct(vtz).FootToMm(), 3),
                                    diameter = rebarSideInfo.Diameter,
                                    rightStart = RebarDiagnosticLog.PointSnapshot(rRight.StartPoint),
                                    rightEnd = RebarDiagnosticLog.PointSnapshot(rRight.EndPoint),
                                    leftStart = RebarDiagnosticLog.PointSnapshot(rLeft.StartPoint),
                                    leftEnd = RebarDiagnosticLog.PointSnapshot(rLeft.EndPoint)
                                });
                                results.Add(rRight);
                                results.Add(rLeft);
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidOperationException(
                                    $"Failed to calculate side-bar pair {i} for beam {subBeam.Id}.", ex);
                            }
                        }
                        var generatedForSpan = results
                            .Where(result => result.SourceBeamId == subBeam.Id)
                            .ToList();
                        diagnosticLog?.Record("side.span.geometry.completed", new
                        {
                            beamId = subBeam.Id,
                            expectedBarCount = qtySide * 2,
                            generatedBarCount = generatedForSpan.Count,
                            distinctLevelCount = generatedForSpan
                                .Select(result => Math.Round(
                                    result.StartPoint.DotProduct(vtz).FootToMm(),
                                    3))
                                .Distinct()
                                .Count(),
                            levelsMm = generatedForSpan
                                .Select(result => Math.Round(
                                    result.StartPoint.DotProduct(vtz).FootToMm(),
                                    3))
                                .Distinct()
                                .OrderBy(level => level)
                                .ToList()
                        });
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to calculate side-bar geometry for beam {subBeam.Id}.", ex);
                    }
                }
                diagnosticLog?.Record("side.geometry.completed", new
                {
                    expectedBarCount = expectedCount,
                    generatedBarCount = results.Count,
                    generatedByBeam = results
                        .GroupBy(result => result.SourceBeamId)
                        .Select(group => new
                        {
                            beamId = group.Key,
                            barCount = group.Count(),
                            distinctLevelCount = group
                                .Select(result => Math.Round(
                                    result.StartPoint.DotProduct(vtz).FootToMm(),
                                    3))
                                .Distinct()
                                .Count()
                        })
                        .ToList()
                });
                if (results.Count != expectedCount)
                    throw new InvalidOperationException(
                        $"Side-bar geometry count mismatch: expected {expectedCount}, generated {results.Count}.");
                foreach (var rebarBeam in rebarBeams)
                {
                    var expectedLevels = rebarBeam.RebarBeamSectionStart
                        .RebarBeamSideBar.QuantitySide;
                    var actualLevels = results
                        .Where(result => result.SourceBeamId == rebarBeam.BeamId)
                        .Select(result => Math.Round(
                            result.StartPoint.DotProduct(vtz).FootToMm(),
                            3))
                        .Distinct()
                        .Count();
                    if (actualLevels != expectedLevels)
                    {
                        throw new InvalidOperationException(
                            $"Beam {rebarBeam.BeamId} requires {expectedLevels} distinct side-bar levels, " +
                            $"but geometry produced {actualLevels}.");
                    }
                }
                return results;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to calculate side-bar geometry.", ex);
            }
        }

    }
}
