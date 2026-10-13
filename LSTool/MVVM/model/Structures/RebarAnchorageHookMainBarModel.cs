using CommunityToolkit.Mvvm.ComponentModel;

namespace LSTool.MVVM.model.Structures
{
    public class RebarAnchorageHookMainBarModel
    {
        public int Id { get; set; }
        public int Diameter { get; set; }
        public double A {  get; set; }
        public double R {  get; set; }
        public double B {  get; set; }
        public double C {  get; set; }
    }
}
