namespace LSTool.MVVM.model.Structures
{
    public class RebarAnchorageHookStirrupModel
    {
        public int Id { get; set; }
        public int Diameter { get; set; }
        public int R { get; set; } // bending radius
        public int LengthHook90 { get; set; } // 10d
        public int LengthHook135 { get; set; } // 10d
        public int LengthHook180 { get; set; } // 10d
    }
}
