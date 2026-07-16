using LSTool.Utils;
using System.Windows;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.views
{
    /// <summary>
    /// Interaction logic for InstallRebarBeamView.xaml
    /// </summary>
    public partial class InstallRebarBeamView : Window
    {
        public InstallRebarBeamView()
        {
            InitializeComponent();
            this.Escape();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void TitleBar_MouseLeftButtonDown(
            object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}



