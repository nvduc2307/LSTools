using Autodesk.Revit.DB;
using HcBimUtils;
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
                            .OrderBy(x => x.Midpoint().DotProduct(vty));
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
                            .OrderBy(x => x.Midpoint().DotProduct(vty));
                        var pMid = csBot.FirstOrDefault().GetEndPoint(0).Midpoint(csTop.FirstOrDefault().GetEndPoint(0));
                        var heightSpace = csBot.FirstOrDefault().GetEndPoint(0).Distance(csTop.FirstOrDefault().GetEndPoint(0));
                        var spacingSide = heightSpace / (qtySide + 1);

                        var installSpace = (qtySide - 1) * spacingSide;
                        var pLast = pMid - vtz * installSpace / 2;

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
                                    StartPoint = lRight.GetEndPoint(0),
                                    EndPoint = lRight.GetEndPoint(1),
                                    Diameter = rebarSideInfo.Diameter
                                };
                                var rLeft = new MainBarBeamReal
                                {
                                    StartPoint = lLeft.GetEndPoint(0),
                                    EndPoint = lLeft.GetEndPoint(1),
                                    Diameter = rebarSideInfo.Diameter
                                };
                                results.Add(rRight);
                                results.Add(rLeft);
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidOperationException(
                                    $"Failed to calculate side-bar pair {i} for beam {subBeam.Id}.", ex);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to calculate side-bar geometry for beam {subBeam.Id}.", ex);
                    }
                }
                return results;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to calculate side-bar geometry.", ex);
            }
        }

        public List<MainBarBeamReal> GetDantoryBarBeamReals(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            double extentCover)
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
                foreach (var subBeam in subBeams)
                {
                    try
                    {
                        var rebarBeam = rebarBeams.FirstOrDefault(x => x.BeamId == subBeam.Id);
                        var rebarStirrupInfo = rebarBeam.RebarBeamSectionStart.RebarBeamStirrup;
                        var diameterStirrup = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                            .FirstOrDefault(x => x.NameStyle == rebarStirrupInfo.Diameter);
                        var diameterDantory = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms
                            .FirstOrDefault(x => x.NameStyle == installRebarBeamV2ViewModel.ElementInstances.RebarBeamDantory.Diameter);
                        var extentCoverSide = diameterStirrup.StandardBendDiameter / 2;
                        var qtyDantory = installRebarBeamV2ViewModel.ElementInstances.RebarBeamDantory.Quantity;
                        var psBot = GetPointControls(
                            installRebarBeamV2ViewModel,
                            subBeam,
                            RebarBeamMainBarLevelType.RebarBot,
                            RebarBeamMainBarGroupType.GroupLevel1,
                            extentCover,
                            extentCoverSide)
                            .Select(x => new XYZ(x.X, x.Y, x.Z - diameterDantory.ModelBarDiameter / 2))
                            .ToList()
                            .PointsToCurves(true)
                            .ToList();
                        var csMaxLength = psBot
                            .GroupBy(x => Math.Round(x.Length.FootToMm(), 0))
                            .OrderBy(x => x.FirstOrDefault().Length)
                            .LastOrDefault()
                            .OrderBy(x => x.Midpoint().DotProduct(vty))
                            .ToList();
                        var csMinLength = psBot
                            .GroupBy(x => Math.Round(x.Length.FootToMm(), 0))
                            .OrderBy(x => x.FirstOrDefault().Length)
                            .FirstOrDefault()
                            .OrderBy(x => x.Midpoint().DotProduct(vtx))
                            .ToList();
                        if (qtyDantory == 1)
                        {
                            var dtr = new MainBarBeamReal()
                            {
                                StartPoint = 0.5 * (csMaxLength.FirstOrDefault().GetEndPoint(0) + csMaxLength.LastOrDefault().GetEndPoint(0)),
                                EndPoint = 0.5 * (csMaxLength.FirstOrDefault().GetEndPoint(1) + csMaxLength.LastOrDefault().GetEndPoint(1)),
                                Diameter = installRebarBeamV2ViewModel.ElementInstances.RebarBeamDantory.Diameter
                            };
                            results.Add(dtr);
                            continue;
                        }
                        var spacing = csMinLength.FirstOrDefault().Length / (qtyDantory - 1);
                        for (int i = 0; i < qtyDantory; i++)
                        {
                            var dtr = new MainBarBeamReal()
                            {
                                StartPoint = csMaxLength.FirstOrDefault().GetEndPoint(0) + i * spacing * vty,
                                EndPoint = csMaxLength.FirstOrDefault().GetEndPoint(1) + i * spacing * vty,
                                Diameter = installRebarBeamV2ViewModel.ElementInstances.RebarBeamDantory.Diameter
                            };
                            results.Add(dtr);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to calculate dantory geometry for beam {subBeam.Id}.", ex);
                    }
                }
                return results;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to calculate dantory geometry.", ex);
            }
        }

    }
}
