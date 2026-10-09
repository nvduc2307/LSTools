using Autodesk.Revit.UI;
using Nice3point.Revit.Extensions.UI;
using Nice3point.Revit.Toolkit.External;
using LSTool.Licensing;
using LSTool.Tools.Beams.BeamRebar;
using LSTool.Tools.Columns.CreateColumn;
using LSTool.Tools.Generals.ManageConcreteCover;
using LSTool.Tools.Generals.SettingDiameters;
using LSTool.Tools.Generals.SettingRebarBeam;
using LSTool.Tools.Generals.SettingRebarColumn;
using LSTool.Utils.UI;
namespace LSTool
{
    public class Application : ExternalApplication
    {
        public override void OnStartup()
        {
            CreateRibbon_General();
            CreateRibbon_Beams();
            CreateRibbon_Columns();
            OnlineLicenseService.BeginSilentActivation();
        }
        private void CreateRibbon_General()
        {
            var panel = Application.CreatePanel("General", "LSTool");
            panel.AddStackedItems(
                ButtonData<RebarDatabasesCmd>("Diameter", "Diameter"),
                ButtonData<ManageConcreteCoverCmd>("Concrete Cover", "Cover"));
            panel.AddStackedItems(
                ButtonData<SettingRebarColumnCmd>("Column settings", "ColumnSettings"),
                ButtonData<SettingRebarBeamCmd>("Beam settings", "BeamSettings"));
        }
        private static PushButtonData ButtonData<T>(string text, string icon)
        {
            var type = typeof(T);
            return new PushButtonData(type.Name, text, type.Assembly.Location, type.FullName)
            {
                Image = ReferenceDrawings.Ribbon(icon, 16),
                LargeImage = ReferenceDrawings.Ribbon(icon, 32)
            };
        }
        private void CreateRibbon_Beams()
        {
            var panel = Application.CreatePanel("Beam", "LSTool");
            var button = panel.AddPushButton<BeamRebarCmd>("Create");
            button.Image = ReferenceDrawings.Ribbon("Beam", 16);
            button.LargeImage = ReferenceDrawings.Ribbon("Beam", 32);
        }
        private void CreateRibbon_Columns()
        {
            var panel = Application.CreatePanel("Column", "LSTool");
            var button = panel.AddPushButton<CreateColumnCmd>("Create");
            button.Image = ReferenceDrawings.Ribbon("Column", 16);
            button.LargeImage = ReferenceDrawings.Ribbon("Column", 32);
        }
    }
}
