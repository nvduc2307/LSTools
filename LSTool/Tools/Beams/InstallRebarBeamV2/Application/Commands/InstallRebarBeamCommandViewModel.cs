using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HcBimUtils;
using HcBimUtils.DocumentUtils;
using HcBimUtils.MoreLinq;
using HcBimUtils.WPFUtils;
using Newtonsoft.Json;
using RIMT.BeamRebar.ViewModel;
using RIMT.CreateRebarAssemblies.model;
using LSTool.Tools.Beams.InstallRebarBeamV2.iservices;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using LSTool.Tools.Beams.InstallRebarBeamV2.views;
using LSTool.Tools.Beams.InstallRebarBeamV2.Support.Legacy;
using LSTool.Tools.Beams.InstallRebarBeamV2.UI.Preview;
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
            try
            {
                _previewRefreshCoordinator.CancelPending();
                _beamStressRuleTypeService.Update(ElementInstances.RebarBeams, ElementInstances.BeamStressRuleType);
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
                        rebarinfos.AddRange(CreateRebarInfos(installRebarTop1, MetadataRebarBeamType.MainBar, RebarBeamLevel.Top, RebarBeamGroup.Level1));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarTop2, MetadataRebarBeamType.MainBar, RebarBeamLevel.Top, RebarBeamGroup.Level2));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarTop3, MetadataRebarBeamType.MainBar, RebarBeamLevel.Top, RebarBeamGroup.Level3));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarBot1, MetadataRebarBeamType.MainBar, RebarBeamLevel.Bottom, RebarBeamGroup.Level1));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarBot2, MetadataRebarBeamType.MainBar, RebarBeamLevel.Bottom, RebarBeamGroup.Level2));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarBot3, MetadataRebarBeamType.MainBar, RebarBeamLevel.Bottom, RebarBeamGroup.Level3));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarSide, MetadataRebarBeamType.SideBar, RebarBeamLevel.None, RebarBeamGroup.None));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarDantories, MetadataRebarBeamType.Dantory, RebarBeamLevel.None, RebarBeamGroup.None));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarStirrup, MetadataRebarBeamType.Stirrup, RebarBeamLevel.None, RebarBeamGroup.None));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarSubVerticalStirrup, MetadataRebarBeamType.Stirrup, RebarBeamLevel.None, RebarBeamGroup.None));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarSubHorizontalStirrupForMainRebar, MetadataRebarBeamType.Stirrup, RebarBeamLevel.None, RebarBeamGroup.None));
                        rebarinfos.AddRange(CreateRebarInfos(installRebarSubHorizontalStirrupForSideRebar, MetadataRebarBeamType.Stirrup, RebarBeamLevel.None, RebarBeamGroup.None));

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
                            new AssemblyInfoUtils(new List<Element>() { ElementInstances.Beam.Element }, AC.Document);
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
                        foreach (var rebar in allCreatedRebars)
                        {
                            try
                            {
                                rebar.SetHostId(
                                    AC.Document,
                                    installResult.TargetHostId);
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidOperationException(
                                    $"Failed to reset the host for rebar {rebar.Id.Value}.", ex);
                            }
                        }

                        if (installResult.TemporaryHostId != null
                            && installResult.TemporaryHostId != ElementId.InvalidElementId)
                        {
                            AC.Document.Delete(installResult.TemporaryHostId);
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
                    System.Diagnostics.Debug.WriteLine(
                        $"InstallRebarBeamV2: {installResult.Metrics.ToSummary()}");
                    //--------
                    ts.Commit();
                    }
                    catch
                    {
                        if (ts.GetStatus() == TransactionStatus.Started)
                            ts.RollBack();
                        throw;
                    }
                }

                MainView.Close();
            }
            catch (Exception ex)
            {
                IO.ShowWarning(GetDetailedError(ex));
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
            MetadataRebarBeamType type,
            RebarBeamLevel level,
            RebarBeamGroup group)
        {
            return rebars.Select(rebar => new BeamRebarInfo
            {
                Id = rebar.Id.Value,
                UniqueId = rebar.UniqueId,
                Name = rebar.Name,
                HostId = rebar.GetHostId().Value,
                RebarBeamType = (int)type,
                RebarBeamLevel = (int)level,
                RebarBeamGroup = (int)group
            }).ToList();
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
