using LSTool.Utils;
using System.Windows;
using System.Windows.Controls;

namespace LSTool.Tools.Columns.CreateColumn.view
{
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
