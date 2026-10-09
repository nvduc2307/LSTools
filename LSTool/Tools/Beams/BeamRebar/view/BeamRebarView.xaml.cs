using LSTool.Utils;
using System.Windows;
using System.Windows.Controls;

namespace LSTool.Tools.Beams.BeamRebar.view
{
    /// <summary>
    /// Interaction logic for BeamRebarView.xaml
    /// </summary>
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
