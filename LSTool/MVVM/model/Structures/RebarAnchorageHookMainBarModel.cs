using CommunityToolkit.Mvvm.ComponentModel;

namespace LSTool.MVVM.model.Structures
{
    public class RebarAnchorageHookMainBarModel
    {
        public int Id { get; set; }
        public int Diameter { get; set; } // diameter
        public double A {  get; set; } // anchorage from concrete
        public double R {  get; set; } // bending radius
        public double B {  get; set; } // hook for angle 90
        public double C {  get; set; } // hook for angle 180
    }
}
