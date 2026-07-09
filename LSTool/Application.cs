using Nice3point.Revit.Extensions.UI;
using Nice3point.Revit.Toolkit.External;

namespace LSTool
{
    /// <summary>
    ///     Application entry point
    /// </summary>
    public class Application : ExternalApplication
    {
        public override void OnStartup()
        {
            CreateRibbon_General();
            CreateRibbon_Beams();
            CreateRibbon_Columns();
        }
        private void CreateRibbon_General()
        {
            var panel = Application.CreatePanel("General", "LSTool");

            //panel.AddPushButton<StartupCommand>("Execute")
            //    .SetImage("/LSTool;component/Resources/Icons/RibbonIcon16.png")
            //    .SetLargeImage("/LSTool;component/Resources/Icons/RibbonIcon32.png");
        }
        private void CreateRibbon_Beams()
        {
            var panel = Application.CreatePanel("Beam", "LSTool");

            //panel.AddPushButton<StartupCommand>("Execute")
            //    .SetImage("/LSTool;component/Resources/Icons/RibbonIcon16.png")
            //    .SetLargeImage("/LSTool;component/Resources/Icons/RibbonIcon32.png");
        }
        private void CreateRibbon_Columns()
        {
            var panel = Application.CreatePanel("Column", "LSTool");

            //panel.AddPushButton<StartupCommand>("Execute")
            //    .SetImage("/LSTool;component/Resources/Icons/RibbonIcon16.png")
            //    .SetLargeImage("/LSTool;component/Resources/Icons/RibbonIcon32.png");
        }
    }
}