using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using LSTool.Licensing;
using LSTool.Tools.Beams.InstallRebarBeamV2.Application.Diagnostics;
using LSTool.Tools.Beams.InstallRebarBeamV2.Application.Selection;
using LSTool.Utils;
using LSTool.Compatibility;
using Nice3point.Revit.Toolkit.External;
using LSTool.Tools.Beams.InstallRebarBeamV2.service;
using LSTool.Tools.Beams.InstallRebarBeamV2.iservices;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using LSTool.Tools.Generals.SettingDiameters.action;
using RIMT.Utils.SelectFilters;

namespace LSTool.Tools.Beams.InstallRebarBeamV2
{
    [Transaction(TransactionMode.Manual)]
    public class InstallRebarBeamV2Cmd : ExternalCommand
    {
        public override void Execute()
        {
            if (!LicenseGate.EnsureFeature(
                    LicenseFeatures.InstallRebarBeamV2))
            {
                return;
            }

            AC.GetInformation(Application.ActiveUIDocument);
            var document = Application.ActiveUIDocument.Document;
            var commandTracePath = RebarDiagnosticLog
                .StartCommandTrace(document);
            var stage = "transaction-group.start";
            var selectionCompleted = false;
            using (var tsg = new TransactionGroup(document, "Install Rebar Beam V2"))
            {
                tsg.Start();
                try
                {
                    stage = "beam-selection";
                    var selectedBeams = Application.ActiveUIDocument.Selection
                        .PickObjects(
                            ObjectType.Element,
                            new GenericSelectionFilterFromCategory(
                                BuiltInCategory.OST_StructuralFraming),
                            "Select one or more beams, then click Finish")
                        .Select(reference => document.GetElement(reference))
                        .Where(element => element != null)
                        .GroupBy(element => element.Id.Value)
                        .Select(group => group.First())
                        .ToList();
                    selectionCompleted = true;
                    RebarDiagnosticLog.RecordCommandTrace(
                        commandTracePath,
                        "selection.completed",
                        new
                        {
                            selectedBeamIds = selectedBeams
                                .Select(beam => beam.Id.Value)
                                .ToList()
                        });
                    stage = "bar-types.synchronize";
                    SynchronizeConfiguredRebarBarTypes(
                        Application.ActiveUIDocument);
                    stage = "beam-groups.resolve";
                    var beamGroups =
                        BeamSelectionRunGrouping.Group(selectedBeams);
                    RebarDiagnosticLog.RecordCommandTrace(
                        commandTracePath,
                        "beam-groups.resolved",
                        new
                        {
                            groupCount = beamGroups.Count,
                            groups = beamGroups.Select(group => group
                                .Select(beam => beam.Id.Value)
                                .ToList()).ToList()
                        });

                    InstallRebarBeamV2ViewModel settingsSource = null;
                    foreach (var beamGroup in beamGroups)
                    {
                        stage = "view-model.create";
                        ISubInstallRebarBeamInModelService subInstallService =
                            new SubInstallRebarBeamInModelService();
                        IDrawRebarBeamInCanvasSerice drawService =
                            new DrawRebarBeamInCanvasSerice(subInstallService);
                        IInstallRebarBeamInModelService installService =
                            new InstallRebarBeamInModelService(subInstallService);
                        var viewModel = new InstallRebarBeamV2ViewModel(
                            new RebarBeamTypeService(drawService),
                            new BeamStressRuleTypeService(),
                            drawService,
                            installService,
                            beamGroup);

                        if (settingsSource == null)
                        {
                            stage = "main-view.show-dialog";
                            RebarDiagnosticLog.RecordCommandTrace(
                                commandTracePath,
                                "main-view.showing",
                                new
                                {
                                    beamIds = beamGroup
                                        .Select(beam => beam.Id.Value)
                                        .ToList()
                                });
                            viewModel.MainView.ShowDialog();
                            RebarDiagnosticLog.RecordCommandTrace(
                                commandTracePath,
                                "main-view.closed");
                            settingsSource = viewModel;
                        }
                        else
                        {
                            viewModel.CopyInstallationSettingsFrom(
                                settingsSource);
                            viewModel.OKCommand.Execute(null);
                        }

                        if (!viewModel.InstallationCompleted)
                        {
                            tsg.RollBack();
                            return;
                        }
                    }

                    tsg.Assimilate();
                    RebarDiagnosticLog.RecordCommandTrace(
                        commandTracePath,
                        "command.completed");
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException ex)
                {
                    RebarDiagnosticLog.RecordCommandTrace(
                        commandTracePath,
                        "command.canceled",
                        new
                        {
                            stage,
                            selectionCompleted,
                            exception = ex.ToString()
                        });
                    if (selectionCompleted)
                    {
                        IO.ShowWarning(
                            RebarErrorMessageBuilder.Build(
                                ex,
                                $"Beam reinforcement startup failed at "
                                + stage));
                    }
                    if (tsg.GetStatus() == TransactionStatus.Started)
                        tsg.RollBack();
                }
                catch (Exception ex)
                {
                    RebarDiagnosticLog.RecordCommandTrace(
                        commandTracePath,
                        "command.failed",
                        new
                        {
                            stage,
                            exception = ex.ToString()
                        });
                    IO.ShowWarning(
                        RebarErrorMessageBuilder.Build(
                            ex,
                            "Beam reinforcement installation"));
                    if (tsg.GetStatus() == TransactionStatus.Started)
                        tsg.RollBack();
                }
            }
        }

        private static void SynchronizeConfiguredRebarBarTypes(
            Autodesk.Revit.UI.UIDocument uiDocument)
        {
            if (uiDocument?.Document == null)
                throw new InvalidOperationException(
                    "The active Revit document is unavailable.");

            var configuredTypes = RebarDatabasesAction
                .ReadConfiguredRebarBarTypes(uiDocument.Document);
            if (configuredTypes.Count == 0) return;

            using (var transaction = new Transaction(
                       uiDocument.Document,
                       "Synchronize Rebar Bar Types"))
            {
                transaction.Start();
                try
                {
                    RebarDatabasesAction.SynchronizeRebarBarTypes(
                        uiDocument.Document,
                        configuredTypes);
                    transaction.Commit();
                }
                catch
                {
                    if (transaction.GetStatus()
                        == TransactionStatus.Started)
                    {
                        transaction.RollBack();
                    }
                    throw;
                }
            }
        }
    }
}


