using wd = System.Windows;
namespace LSTool.Tools.Columns.ColumnRebar.models
{
    public class ColumnRebarPositionModel
    {
        public string HostId {  get; set; }
        public int Face {  get; set; }
        public int Index { get; set; }
        public XYZ Position { get; set; }
    }
    public class ColumnRebarPositionInCanvasModel
    {
        public int Index { get; set; }
        public wd.Point Position { get; set; }
    }
}
