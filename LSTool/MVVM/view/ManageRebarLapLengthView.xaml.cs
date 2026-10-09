using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LSTool.MVVM.view
{
    /// <summary>
    /// Interaction logic for ManageRebarLapLengthView.xaml
    /// </summary>
    public partial class ManageRebarLapLengthView : Window
    {
        public ManageRebarLapLengthView()
        {
            LSTool.Utils.UI.UiAssemblyLoader.Initialize();
            InitializeComponent();
        }
    }
}
