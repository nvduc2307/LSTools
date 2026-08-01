using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LSTool.Compatibility;
using Newtonsoft.Json;
using RIMT.BeamRebar.ViewModel;
using RIMT.CreateRebarAssemblies.model;
using LSTool.Tools.Beams.InstallRebarBeamV2.iservices;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using LSTool.Tools.Beams.InstallRebarBeamV2.views;
using LSTool.Tools.Beams.InstallRebarBeamV2.Support.Legacy;
using LSTool.Tools.Beams.InstallRebarBeamV2.UI.Preview;
using LSTool.Tools.Beams.InstallRebarBeamV2.Application;
using LSTool.Tools.Beams.InstallRebarBeamV2.Application.Diagnostics;
using LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Plans;
using LSTool.Tools.Beams.InstallRebarBeamV2.Revit.Writers;
using RIMT.Utils;
using RIMT.Utils.canvass;
using RIMT.Utils.Entities;
using RIMT.Utils.RevitElements;
using RIMT.Utils.RevParameters;
using RIMT.Utils.RevRebars;
using RIMT.Utils.SelectFilters;
using RIMT.Utils.SkipWarning;
using System.IO;
using System.Windows.Controls;
using Rebar = Autodesk.Revit.DB.Structure.Rebar;
using RebarBeamAnchorType = LSTool.Tools.Beams.InstallRebarBeamV2.models.RebarBeamAnchorType;
using UserControl = System.Windows.Controls.UserControl;
using MetadataRebarBeamType = RIMT.BeamRebar.ViewModel.RebarBeamType;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.viewModels
{
    public partial class InstallRebarBeamV2ViewModel
    {
        [RelayCommand]
        private void OK()
        {
            InstallationCompleted = false;
            RebarDiagnosticLog diagnosticLog = null;
            try
            {
                _previewRefreshCoordinator.CancelPending();
                ElementInstances.EnsureCoordinateBeamGenerated();
                diagnosticLog = RebarDiagnosticLog.Start(this);
                DiagnosticLog = diagnosticLog;
                diagnosticLog.Record("command.ok.requested", new
                {
                    activeViewId = AC.Document.ActiveView?.Id.Value,
                    activeViewName = AC.Document.ActiveView?.Name
                });
                _beamStressRuleTypeService.Update(ElementInstances.RebarBeams, ElementInstances.BeamStressRuleType);
                diagnosticLog.Record("beam.stress.updated", new
                {
                    rebarBeamCount = ElementInstances.RebarBeams?.Count ?? 0
                });
                using (var ts = new Transaction(AC.Document, "Install beam rebar V2"))
                {
                    ts.SkipAllWarnings();
                    ts.Start();
                    try
                    {
                    RebarSharedParameterSupport.EnsureRequiredParameters(AC.Document);
                    //--------
                    var installResult = _installRebarBeamInModelService.InstallAll(this);
                    var installRebarTop1 = installResult.TopLevel1;
                    var installRebarTop2 = installResult.TopLevel2;
                    var installRebarTop3 = installResult.TopLevel3;
                    var installRebarBot1 = installResult.BottomLevel1;
                    var installRebarBot2 = installResult.BottomLevel2;
                    var installRebarBot3 = installResult.BottomLevel3;
                    var installRebarSide = installResult.SideBars;
                    var installRebarDantories = installResult.DantoryBars;
                    var installRebarStirrup = installResult.MainStirrups;
                    var installRebarSubVerticalStirrup = installResult.SecondaryVerticalStirrups;
                    var installRebarSubHorizontalStirrupForMainRebar = installResult.SecondaryHorizontalMainStirrups;
                    var installRebarSubHorizontalStirrupForSideRebar = installResult.SecondaryHorizontalSideStirrups;
                    var allCreatedRebars = installResult.AllRebars.ToList();
                    var sideRebarIds = new HashSet<long>(
                        installRebarSide.Select(rebar => rebar.Id.Value));
                    diagnosticLog.Record("installation.created", new
                    {
                        totalCount = allCreatedRebars.Count,
                        sideCount = installRebarSide.Count,
                        dantoryCount = installRebarDantories.Count,
                        mainStirrupCount = installRebarStirrup.Count,
                        secondaryVerticalStirrupCount = installRebarSubVerticalStirrup.Count,
                        secondaryHorizontalMainStirrupCount = installRebarSubHorizontalStirrupForMainRebar.Count,
                        secondaryHorizontalSideStirrupCount = installRebarSubHorizontalStirrupForSideRebar.Count,
                        temporaryHostId = installResult.TemporaryHostId?.Value,
                        defaultTargetHostId = installResult.TargetHostId?.Value,
                        mappedTargetCount = installResult.TargetHostIdsByRebarId.Count
                    });
                    #region write rebar type info
                    using (installResult.Metrics.Measure("metadata.type"))
                    {
                        SetRebarType(installRebarTop1, LSTool.Properties.Langs.RebarStructureType.BEAM_MAIN_REBAR_UPPER_STAGE_1);
                        SetRebarType(installRebarTop2, LSTool.Properties.Langs.RebarStructureType.BEAM_MAIN_REBAR_UPPER_STAGE_2);
                        SetRebarType(installRebarTop3, LSTool.Properties.Langs.RebarStructureType.BEAM_MAIN_REBAR_UPPER_STAGE_3);
                        SetRebarType(installRebarBot1, LSTool.Properties.Langs.RebarStructureType.BEAM_MAIN_REBAR_LOWER_STAGE_1);
                        SetRebarType(installRebarBot2, LSTool.Properties.Langs.RebarStructureType.BEAM_MAIN_REBAR_LOWER_STAGE_2);
                        SetRebarType(installRebarBot3, LSTool.Properties.Langs.RebarStructureType.BEAM_MAIN_REBAR_LOWER_STAGE_3);
                        SetRebarType(installRebarSide, LSTool.Properties.Langs.RebarStructureType.BEAM_ABDOMINAL_REBAR);
                        SetRebarType(installRebarDantories, LSTool.Properties.Langs.RebarStructureType.BEAM_DANTORI_REBAR);
                        SetRebarType(installRebarStirrup, LSTool.Properties.Langs.RebarStructureType.BEAM_STP);
                        SetRebarType(installRebarSubVerticalStirrup, LSTool.Properties.Langs.RebarStructureType.BEAM_SECONDARY_STP_REBAR);
                        SetRebarType(installRebarSubHorizontalStirrupForMainRebar, LSTool.Properties.Langs.RebarStructureType.BEAM_SECONDARY_STP_REBAR);
                        SetRebarType(installRebarSubHorizontalStirrupForSideRebar, LSTool.Properties.Langs.RebarStructureType.BEAM_SECONDARY_STP_REBAR);
                    }
                    #endregion
                    #region Create Rebar Beam Assembly
                    AssemblyInstance rebarBeamAss;
                    using (installResult.Metrics.Measure("assembly.create"))
                    {
                        var rebarIds = allCreatedRebars.Select(rebar => rebar.Id).ToList();
                        rebarBeamAss = AssemblyInstance.Create(
                            AC.Document,
                            rebarIds,
                            Category.GetCategory(AC.Document, BuiltInCategory.OST_Rebar).Id);
                    }
                    #endregion
                    #region Write Rebar Beam Info
                    using (installResult.Metrics.Measure("metadata.schema"))
                    {
                        var rebarinfos = new List<BeamRebarInfo>();
                        rebarinfos.AddRange(CreateRebarInfos(installRebarTop1, installResult, MetadataRebarBeamType.MainBar, RebarBeamLevel.Top, RebarBeamGroup.Level1));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarTop2, installResult, MetadataRebarBeamType.MainBar, RebarBeamLevel.Top, RebarBeamGroup.Level2));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarTop3, installResult, MetadataRebarBeamType.MainBar, RebarBeamLevel.Top, RebarBeamGroup.Level3));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarBot1, installResult, MetadataRebarBeamType.MainBar, RebarBeamLevel.Bottom, RebarBeamGroup.Level1));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarBot2, installResult, MetadataRebarBeamType.MainBar, RebarBeamLevel.Bottom, RebarBeamGroup.Level2));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarBot3, installResult, MetadataRebarBeamType.MainBar, RebarBeamLevel.Bottom, RebarBeamGroup.Level3));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarSide, installResult, MetadataRebarBeamType.SideBar, RebarBeamLevel.None, RebarBeamGroup.None));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarDantories, installResult, MetadataRebarBeamType.Dantory, RebarBeamLevel.None, RebarBeamGroup.None));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarStirrup, installResult, MetadataRebarBeamType.Stirrup, RebarBeamLevel.None, RebarBeamGroup.None));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarSubVerticalStirrup, installResult, MetadataRebarBeamType.Stirrup, RebarBeamLevel.None, RebarBeamGroup.None));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarSubHorizontalStirrupForMainRebar, installResult, MetadataRebarBeamType.Stirrup, RebarBeamLevel.None, RebarBeamGroup.None));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarSubHorizontalStirrupForSideRebar, installResult, MetadataRebarBeamType.Stirrup, RebarBeamLevel.None, RebarBeamGroup.None));

                        var createdRebarsByUniqueId = allCreatedRebars
                            .ToDictionary(rebar => rebar.UniqueId, StringComparer.Ordinal);
                        foreach (var rebarInfo in rebarinfos)
                        {
                            if (!createdRebarsByUniqueId.TryGetValue(rebarInfo.UniqueId, out var rebar))
                                throw new InvalidOperationException(
                                    $"Created rebar '{rebarInfo.UniqueId}' could not be resolved for metadata writing.");
                            ElementInstances.RebarBeamSchemal.SchemaField.Value = JsonConvert.SerializeObject(rebarInfo);
                            SchemaInfo.Write(
                                ElementInstances.RebarBeamSchemal.SchemaBase,
                                rebar,
                                ElementInstances.RebarBeamSchemal.SchemaField);
                        }
                    }
                    #endregion
                    #region write rebar beam assembly info
                    using (installResult.Metrics.Measure("assembly.metadata"))
                    {
                        var assemblyInfoUtils =
                            new AssemblyInfoUtils(
                                ElementInstances.Beam.ElementSubs.Select(member => member.Element),
                                AC.Document);
                        RebarSharedParameterSupport.SetRequiredStringParameter(
                            rebarBeamAss,
                            BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS,
                            assemblyInfoUtils.GridName);
                        RebarSharedParameterSupport.SetRequiredStringParameter(
                            rebarBeamAss,
                            BuiltInParameter.ALL_MODEL_MARK,
                            assemblyInfoUtils.TypeName);
                    }
                    #endregion
                    #region Resetting host
                    using (installResult.Metrics.Measure("rehost"))
                    {
                        var temporaryHostId = installResult.TemporaryHostId;
                        diagnosticLog.Record("rehost.started", new
                        {
                            totalCount = allCreatedRebars.Count,
                            sideCount = installRebarSide.Count,
                            temporaryHostId = temporaryHostId?.Value,
                            defaultTargetHostId = installResult.TargetHostId?.Value,
                            mappedTargetCount = installResult.TargetHostIdsByRebarId.Count
                        });
                        foreach (var rebar in allCreatedRebars)
                        {
                            var targetHostId = GetRegisteredTargetHostId(
                                installResult,
                                rebar);
                            var currentHostId = rebar.GetHostId();
                            var diagnosticGroup = sideRebarIds.Contains(rebar.Id.Value)
                                ? "side"
                                : "other";
                            diagnosticLog.RecordRebar(
                                "rehost.before",
                                rebar,
                                intendedHostId: targetHostId,
                                group: diagnosticGroup);
                            if (currentHostId.Value == targetHostId.Value)
                            {
                                diagnosticLog.RecordRebar(
                                    "rehost.skipped.already-correct",
                                    rebar,
                                    intendedHostId: targetHostId,
                                    group: diagnosticGroup);
                                continue;
                            }
                            if (currentHostId.Value != temporaryHostId.Value)
                                throw new InvalidOperationException(
                                    $"Rebar {rebar.Id.Value} has unexpected host {currentHostId.Value}; " +
                                    $"expected temporary host {temporaryHostId.Value}.");

                            try
                            {
                                rebar.SetHostId(
                                    AC.Document,
                                    targetHostId);
                                if (rebar.GetHostId().Value != targetHostId.Value)
                                {
                                    throw new InvalidOperationException(
                                        $"Revit did not assign the expected host {targetHostId.Value}.");
                                }
                                diagnosticLog.RecordRebar(
                                    "rehost.after",
                                    rebar,
                                    intendedHostId: targetHostId,
                                    group: diagnosticGroup);
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidOperationException(
                                    $"Failed to reset the host for rebar {rebar.Id.Value}.", ex);
                            }
                        }

                        AC.Document.Regenerate();
                        var requiresStrictMainBarValidation =
                            installResult.MainBarRunsByRebarId.Values.Any(
                                run =>
                                    run.RequiresStrictGeometryValidation);
                        foreach (var pair in
                                 requiresStrictMainBarValidation
                                     ? installResult
                                         .MainBarRunsByRebarId
                                     : new Dictionary<
                                         long,
                                         MainBarRunPlan>())
                        {
                            var run = pair.Value;
                            var rebar = AC.Document.GetElement(
                                new ElementId(pair.Key)) as Rebar;
                            MainBarRebarWriter.ValidateActualCenterline(
                                run,
                                rebar,
                                AC.Document,
                                diagnosticLog,
                                "after-rehost");
                        }

                        var stillTemporaryHosted = allCreatedRebars
                            .Where(rebar => rebar.GetHostId().Value == temporaryHostId.Value)
                            .Select(rebar => rebar.Id.Value)
                            .ToList();
                        diagnosticLog.Record("temporary-host.cleanup.precheck", new
                        {
                            temporaryHostId = temporaryHostId?.Value,
                            stillTemporaryHostedCount = stillTemporaryHosted.Count,
                            stillTemporaryHostedIds = stillTemporaryHosted
                        });
                        if (stillTemporaryHosted.Count > 0)
                        {
                            throw new InvalidOperationException(
                                $"Temporary host cleanup was blocked because rebar ids " +
                                $"{string.Join(", ", stillTemporaryHosted)} were not rehosted.");
                        }

                        if (temporaryHostId != null
                            && temporaryHostId != ElementId.InvalidElementId)
                        {
                            var deletedIds = AC.Document.Delete(temporaryHostId)
                                .Select(id => id.Value)
                                .ToList();
                            diagnosticLog.Record("temporary-host.deleted", new
                            {
                                temporaryHostId = temporaryHostId.Value,
                                deletedIds
                            });
                            AC.Document.Regenerate();
                            var deletedCreatedRebars = allCreatedRebars
                                .Where(rebar => !rebar.IsValidObject
                                    || AC.Document.GetElement(rebar.Id) == null)
                                .Select(rebar => rebar.Id.Value)
                                .ToList();
                            diagnosticLog.Record("temporary-host.cleanup.completed", new
                            {
                                createdRebarCount = allCreatedRebars.Count,
                                deletedCreatedRebarCount = deletedCreatedRebars.Count,
                                deletedCreatedRebarIds = deletedCreatedRebars
                            });
                            if (deletedCreatedRebars.Count > 0)
                            {
                                throw new InvalidOperationException(
                                    $"Deleting the temporary host also deleted created rebar ids " +
                                    $"{string.Join(", ", deletedCreatedRebars)}.");
                            }
                        }
                        foreach (var sideRebar in installRebarSide)
                        {
                            var targetHostId = GetRegisteredTargetHostId(
                                installResult,
                                sideRebar);
                            diagnosticLog.RecordRebar(
                                "side.rebar.after-cleanup",
                                sideRebar,
                                intendedHostId: targetHostId,
                                group: "side");
                        }
                    }
                    #endregion
                    //avoid hole
                    using (installResult.Metrics.Measure("opening"))
                    {
                        var rebarsSTP = installResult.AllStirrups.ToList();
                        var rbsHole = BypassOpening(
                                rebarsSTP,
                                ElementInstances.RebarBeamActive,
                                out List<Rebar> rebarDeletes)
                            .Select(rebar => rebar.Id)
                            .ToList();
                        diagnosticLog.Record("opening.processed", new
                        {
                            inputStirrupCount = rebarsSTP.Count,
                            replacementCount = rbsHole.Count,
                            deletedOriginalCount = rebarDeletes.Count,
                            replacementIds = rbsHole.Select(id => id.Value).ToList(),
                            deletedOriginalIds = rebarDeletes.Select(rebar => rebar.Id.Value).ToList()
                        });
                        if (rbsHole.Count != 0)
                        {
                            rebarBeamAss.AddMemberIds(rbsHole);
                            AC.Document.Delete(rebarDeletes.Select(rebar => rebar.Id).ToList());
                        }
                    }
                    //init segment
                    using (installResult.Metrics.Measure("segments"))
                    {
                        var rebarInAss = rebarBeamAss.GetMemberIds()
                            .Select(id => AC.Document.GetElement(id) as Rebar)
                            .ToList();
                        rebarInAss.InitSegment();
                    }
                    foreach (var sideRebar in installRebarSide)
                    {
                        var targetHostId = GetRegisteredTargetHostId(
                            installResult,
                            sideRebar);
                        diagnosticLog.RecordRebar(
                            "side.rebar.before-commit",
                            sideRebar,
                            intendedHostId: targetHostId,
                            group: "side");
                    }
                    System.Diagnostics.Debug.WriteLine(
                        $"InstallRebarBeamV2: {installResult.Metrics.ToSummary()}");
                    diagnosticLog.Record("transaction.committing", new
                    {
                        metrics = installResult.Metrics.ToSummary(),
                        assemblyId = rebarBeamAss.Id.Value,
                        assemblyMemberCount = rebarBeamAss.GetMemberIds().Count,
                        finalSideRebarCount = installRebarSide.Count
                    });
                    //--------
                    ts.Commit();
                    diagnosticLog.Record("transaction.committed", new
                    {
                        transactionStatus = ts.GetStatus().ToString(),
                        finalSideRebarCount = installRebarSide.Count
                    });
                    foreach (var sideRebar in installRebarSide)
                    {
                        var targetHostId = GetRegisteredTargetHostId(
                            installResult,
                            sideRebar);
                        diagnosticLog.RecordRebar(
                            "side.rebar.after-commit",
                            sideRebar,
                            intendedHostId: targetHostId,
                            group: "side");
                    }
                    }
                    catch (Exception ex)
                    {
                        diagnosticLog?.RecordException("transaction.failed", ex);
                        if (ts.GetStatus() == TransactionStatus.Started)
                        {
                            ts.RollBack();
                            diagnosticLog?.Record("transaction.rolled-back", new
                            {
                                transactionStatus = ts.GetStatus().ToString()
                            });
                        }
                        throw;
                    }
                }

                diagnosticLog.Record("run.completed", new
                {
                    logPath = diagnosticLog.FilePath
                });
                InstallationCompleted = true;
                if (MainView.IsVisible)
                    MainView.Close();
            }
            catch (Exception ex)
            {
                diagnosticLog?.RecordException("command.failed", ex);
                var message = GetDetailedError(ex);
                if (diagnosticLog != null)
                    message += $"{Environment.NewLine}{Environment.NewLine}Diagnostic log:{Environment.NewLine}{diagnosticLog.FilePath}";
                IO.ShowWarning(message);
            }
            finally
            {
                DiagnosticLog = null;
                diagnosticLog?.Dispose();
            }
        }

        private static void SetRebarType(IEnumerable<Rebar> rebars, string rebarType)
        {
            foreach (var rebar in rebars)
            {
                RebarSharedParameterSupport.SetRequiredStringParameter(
                    rebar,
                    LSTool.Properties.RTParams.RT_PARAMS_REBAR_TYPE,
                    rebarType);
            }
        }

        private static List<BeamRebarInfo> CreateRebarInfos(
            IEnumerable<Rebar> rebars,
            RebarInstallationResult installResult,
            MetadataRebarBeamType type,
            RebarBeamLevel level,
            RebarBeamGroup group)
        {
            return rebars.Select(rebar =>
            {
                var targetHostId = GetRegisteredTargetHostId(
                    installResult,
                    rebar);

                return new BeamRebarInfo
                {
                    Id = rebar.Id.Value,
                    UniqueId = rebar.UniqueId,
                    Name = rebar.Name,
                    HostId = targetHostId.Value,
                    RebarBeamType = (int)type,
                    RebarBeamLevel = (int)level,
                    RebarBeamGroup = (int)group
                };
            }).ToList();
        }

        private static ElementId GetRegisteredTargetHostId(
            RebarInstallationResult installResult,
            Rebar rebar)
        {
            if (installResult == null)
                throw new ArgumentNullException(nameof(installResult));
            if (rebar == null)
                throw new ArgumentNullException(nameof(rebar));
            if (!installResult.TargetHostIdsByRebarId.TryGetValue(
                    rebar.Id.Value,
                    out var targetHostId)
                || targetHostId == null
                || targetHostId.Value == ElementId.InvalidElementId.Value)
            {
                throw new InvalidOperationException(
                    $"No target host was registered for rebar "
                    + $"{rebar.Id.Value}.");
            }
            return targetHostId;
        }

        private static string GetDetailedError(Exception exception)
        {
            var messages = new List<string>();
            for (var current = exception; current != null; current = current.InnerException)
            {
                if (!string.IsNullOrWhiteSpace(current.Message)
                    && !messages.Contains(current.Message))
                    messages.Add(current.Message);
            }
            return string.Join(Environment.NewLine, messages);
        }

        [RelayCommand]
        private void Cancel()
        {
            _previewRefreshCoordinator.CancelPending();
            MainView.Close();
        }
        private List<Rebar> BypassOpening(
            List<Rebar> rebarStirrups,
            RebarBeam rebarBeam,
            out List<Rebar> rebarDeletes)
        {
            var results = new List<Rebar>();
            rebarDeletes = new List<Rebar>();
            try
            {
                foreach (var beamBox in ElementInstances.Beam.ElementSubs)
                {
                    var beam = beamBox.Element as FamilyInstance
                        ?? throw new InvalidOperationException(
                            $"Beam member {beamBox.Id} is not a family instance.");
                    var bb = beam.get_BoundingBox(null);
                    var transform = beam.GetTransform();
                    var vty = transform.OfVector(XYZ.BasisY);
                    double beamThicknessMm = beamBox.Curves
                        .Where(x => x.Direction().IsParallel(vty))
                        .Select(x => x.Length.FootToMm())
                        .Max();
                    double botElevationMm = bb.Min.Z.FootToMm();
                    double topElevationMm = bb.Max.Z.FootToMm();
                    var rebars = RevBeamHole.DeleteMainStirrup(
                        AC.Document,
                        beam,
                        beamBox,
                        rebarStirrups,
                        beamThicknessMm,
                        botElevationMm,
                        topElevationMm,
                        ElementInstances.RebarBeamActive.QuantityStirrupSupportHole,
                        ElementInstances.RebarBeamSchemal,
                        out List<Rebar> rebarDelete);
                    if (!rebars.Any())
                        continue;
                    results.AddRange(rebars);
                    rebarDeletes.AddRange(rebarDelete);
                }
                return results;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed while bypassing beam openings.", ex);
            }
        }
    }
}
