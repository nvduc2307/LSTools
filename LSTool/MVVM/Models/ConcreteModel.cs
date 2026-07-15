namespace LSTool.MVVM.Models
{
    public class ConcreteModel : ObservableObject
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public double Cover {  get; set; }
        public XYZ Center { get; set; }
        public XYZ VTX { get; set; }
        public XYZ VTY { get; set; }
        public XYZ VTZ { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Length { get; set; }
    }
}
