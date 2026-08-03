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
            using (var tsg = new TransactionGroup(document, "Install Rebar Beam V2"))
            {
                tsg.Start();
                try
                {
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
                    SynchronizeConfiguredRebarBarTypes(
                        Application.ActiveUIDocument);
                    var beamGroups =
                        BeamSelectionRunGrouping.Group(selectedBeams);

                    InstallRebarBeamV2ViewModel settingsSource = null;
                    foreach (var beamGroup in beamGroups)
                    {
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
                            viewModel.MainView.ShowDialog();
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
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    if (tsg.GetStatus() == TransactionStatus.Started)
                        tsg.RollBack();
                }
                catch (Exception ex)
                {
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


