using LSTool.Utils;
using System.Windows;
using System.Windows.Controls;

namespace LSTool.Tools.Beams.BeamRebar.view
{
    public partial class BeamRebarView : Window
    {
        public Canvas CanvasStart => canvas_start;
        public Canvas CanvasMid => canvas_mid;
        public Canvas CanvasEnd => canvas_end;

        public BeamRebarView()
        {
            LSTool.Utils.UI.UiAssemblyLoader.Initialize();
            InitializeComponent();
            this.Escape();
        }
    }
}
