using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using HcBimUtils.DocumentUtils;
using LSTool.Licensing;
using LSTool.Utils;
using Nice3point.Revit.Toolkit.External;
using LSTool.Tools.Beams.InstallRebarBeamV2.service;
using LSTool.Tools.Beams.InstallRebarBeamV2.iservices;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;

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
                    ISubInstallRebarBeamInModelService subInstallService = new SubInstallRebarBeamInModelService();
                    IDrawRebarBeamInCanvasSerice drawService = new DrawRebarBeamInCanvasSerice(subInstallService);
                    IInstallRebarBeamInModelService installService = new InstallRebarBeamInModelService(subInstallService);
                    var installRebarBeamV2ViewModel = new InstallRebarBeamV2ViewModel(
                        new RebarBeamTypeService(drawService),
                        new BeamStressRuleTypeService(),
                        drawService,
                        installService);
                    installRebarBeamV2ViewModel.MainView.ShowDialog();
                    tsg.Assimilate();
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException) { }
                catch (Exception ex)
                {
                    IO.ShowWarning(ex.Message);
                    tsg.RollBack();
                }
            }
        }
    }
}


