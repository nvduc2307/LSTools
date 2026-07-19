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
using LSTool.Tools.Beams.InstallRebarBeamV2.Application;
using LSTool.Tools.Beams.InstallRebarBeamV2.Application.Diagnostics;
using LSTool.Tools.Beams.InstallRebarBeamV2.Revit.Writers;
using LSTool.Tools.Beams.InstallRebarBeamV2.Geometry.MainBars;
using LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Plans;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.service
{
    public partial class InstallRebarBeamInModelService : IInstallRebarBeamInModelService
    {
        private ISubInstallRebarBeamInModelService _subInstallRebarBeamInModelService;
        private readonly MainBarCreationPlanner _mainBarPlanner;
        private readonly MainBarRebarWriter _mainBarWriter;
        public InstallRebarBeamInModelService(ISubInstallRebarBeamInModelService subInstallRebarBeamInModelService)
        {
            _subInstallRebarBeamInModelService = subInstallRebarBeamInModelService;
            _mainBarPlanner = new MainBarCreationPlanner(subInstallRebarBeamInModelService);
            _mainBarWriter = new MainBarRebarWriter();
        }

        public RebarInstallationResult InstallAll(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {
            var context = RebarExecutionContext.Create(installRebarBeamV2ViewModel);
            var result = new RebarInstallationResult
            {
                TemporaryHostId = context.TemporaryHost.Id,
                TargetHostId = context.TargetHostId,
                Metrics = context.Metrics
            };

            result.TopLevel1 = CreateMainBars(installRebarBeamV2ViewModel, context, RebarBeamMainBarLevelType.RebarTop, RebarBeamMainBarGroupType.GroupLevel1, "main.top.1");
            result.TopLevel2 = CreateMainBars(installRebarBeamV2ViewModel, context, RebarBeamMainBarLevelType.RebarTop, RebarBeamMainBarGroupType.GroupLevel2, "main.top.2");
            result.TopLevel3 = CreateMainBars(installRebarBeamV2ViewModel, context, RebarBeamMainBarLevelType.RebarTop, RebarBeamMainBarGroupType.GroupLevel3, "main.top.3");
            result.BottomLevel1 = CreateMainBars(installRebarBeamV2ViewModel, context, RebarBeamMainBarLevelType.RebarBot, RebarBeamMainBarGroupType.GroupLevel1, "main.bottom.1");
            result.BottomLevel2 = CreateMainBars(installRebarBeamV2ViewModel, context, RebarBeamMainBarLevelType.RebarBot, RebarBeamMainBarGroupType.GroupLevel2, "main.bottom.2");
            result.BottomLevel3 = CreateMainBars(installRebarBeamV2ViewModel, context, RebarBeamMainBarLevelType.RebarBot, RebarBeamMainBarGroupType.GroupLevel3, "main.bottom.3");
            using (context.Metrics.Measure("side"))
                result.SideBars = InstallRebarSide(installRebarBeamV2ViewModel, context);
            using (context.Metrics.Measure("dantory"))
                result.DantoryBars = InstallRebarDantory(installRebarBeamV2ViewModel, context);
            using (context.Metrics.Measure("stirrup.main"))
                result.MainStirrups = InstallRebarStirrup(installRebarBeamV2ViewModel, context);
            using (context.Metrics.Measure("stirrup.secondary.vertical"))
                result.SecondaryVerticalStirrups = InstallRebarSubVerticalStirrup(installRebarBeamV2ViewModel, context);
            using (context.Metrics.Measure("stirrup.secondary.horizontal.main"))
                result.SecondaryHorizontalMainStirrups = InstallRebarSubHorizontalStirrupForMainRebar(installRebarBeamV2ViewModel, context);
            using (context.Metrics.Measure("stirrup.secondary.horizontal.side"))
            result.SecondaryHorizontalSideStirrups = InstallRebarSubHorizontalStirrupForSideRebar(installRebarBeamV2ViewModel, context);
            result.TargetHostIdsByRebarId = context.TargetHostIdsByRebarId;

            return result;
        }

        private List<Rebar> CreateMainBars(
            InstallRebarBeamV2ViewModel viewModel,
            RebarExecutionContext context,
            RebarBeamMainBarLevelType level,
            RebarBeamMainBarGroupType group,
            string stageName)
        {
            MainBarCreationPlan plan;
            using (context.Metrics.Measure($"{stageName}.plan"))
                plan = _mainBarPlanner.Plan(viewModel, context, level, group);
            using (context.Metrics.Measure($"{stageName}.write"))
                return _mainBarWriter.Create(plan, context);
        }

        private List<Rebar> InstallRebarSide(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            RebarExecutionContext context)
        {
            try
            {
                var results = new List<Rebar>();
                var host = context.TemporaryHost;
                var vtx = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
                var vty = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
                var vtz = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;
                var rebarSides = _subInstallRebarBeamInModelService.GetSideBarBeamReals(
                    installRebarBeamV2ViewModel,
                    0);
                context.DiagnosticLog?.Record("side.creation.started", new
                {
                    plannedBarCount = rebarSides.Count,
                    temporaryHostId = host.Id.Value
                });
                for (var index = 0; index < rebarSides.Count; index++)
                {
                    var r = rebarSides[index];
                    var targetHostId = context.GetBeamHost(r.SourceBeamId).Id;
                    context.DiagnosticLog?.Record("side.rebar.create.requested", new
                    {
                        plannedIndex = index,
                        sourceBeamId = r.SourceBeamId,
                        temporaryHostId = host.Id.Value,
                        targetHostId = targetHostId.Value,
                        r.Diameter,
                        start = RebarDiagnosticLog.PointSnapshot(r.StartPoint),
                        end = RebarDiagnosticLog.PointSnapshot(r.EndPoint)
                    });
                    var diameterSide = context.GetBarType(r.Diameter);
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
                    context.RegisterTargetHost(rebar, r.SourceBeamId);
                    context.DiagnosticLog?.RecordRebar(
                        "side.rebar.created",
                        rebar,
                        r.SourceBeamId,
                        targetHostId,
                        "side");
                    results.Add(rebar);
                }
                context.DiagnosticLog?.Record("side.creation.completed", new
                {
                    plannedBarCount = rebarSides.Count,
                    createdBarCount = results.Count,
                    rebarIds = results.Select(rebar => rebar.Id.Value).ToList()
                });
                return results;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create side bars.", ex);
            }
        }

        private List<Rebar> InstallRebarDantory(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            RebarExecutionContext context)
        {
            try
            {
                var results = new List<Rebar>();
                var host = context.TemporaryHost;
                var vtx = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTX;
                var vty = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTY;
                var vtz = installRebarBeamV2ViewModel.ElementInstances.Beam.BoxElement.VTZ;
                var rebarDantories = _subInstallRebarBeamInModelService.GetDantoryBarBeamReals(
                    installRebarBeamV2ViewModel,
                    0);
                foreach (var r in rebarDantories)
                {
                    var diameterSide = context.GetBarType(r.Diameter);
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
                    context.RegisterTargetHost(rebar, r.SourceBeamId);
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


