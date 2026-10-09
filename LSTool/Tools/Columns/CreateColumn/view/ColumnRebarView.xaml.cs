using LSTool.Utils;
using System.Windows;
using System.Windows.Controls;

namespace LSTool.Tools.Columns.CreateColumn.view
{
    /// <summary>
    /// Interaction logic for ColumnRebarView.xaml
    /// </summary>
    public partial class ColumnRebarView : Window
    {
        public Canvas CanvasSectionPreViewControl => CanvasSectionPreView;

        public ColumnRebarView()
        {
            LSTool.Utils.UI.UiAssemblyLoader.Initialize();
            InitializeComponent();
            this.Escape();
        }
    }
}
