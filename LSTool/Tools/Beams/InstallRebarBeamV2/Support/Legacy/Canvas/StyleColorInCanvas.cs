using System.Windows.Media;

namespace RIMT.Utils.canvass
{
    public class StyleColorInCanvas
    {
        public static SolidColorBrush Color0 = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(0, 0, 0) };
        public static SolidColorBrush Color1 = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(204, 204, 204) };
        public static SolidColorBrush Color2 = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(238, 238, 238) };
        public static SolidColorBrush Color3 = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(255, 255, 0) };
        public static SolidColorBrush Color4 = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(255, 0, 0) };
        public static SolidColorBrush Color5 = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(76, 117, 208) };
        public static SolidColorBrush Color_White = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(255, 255, 255) };
        public static SolidColorBrush Color_Red = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(255, 0, 0) };
        public static SolidColorBrush Color_BackGround = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(238, 238, 238) };

        public static SolidColorBrush Color_Concrete = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(197, 197, 197) };
        public static SolidColorBrush Color_Concrete_OutLine = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(0, 0, 0) };
        public static SolidColorBrush Color_Selected = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(160, 178, 218) };
        public static SolidColorBrush Color_Selected1 = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(0, 4, 255) };
        public static SolidColorBrush Color_Selected_OutLine = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(0, 59, 189) };

        public static SolidColorBrush Color_Enable = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(132, 132, 132) };

        public static SolidColorBrush Color_Weld = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(0, 0, 255) };
        public static SolidColorBrush Color_Coupler = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(0, 255, 0) };

        public static SolidColorBrush Color_Rebar = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(255, 0, 0) };
        public static SolidColorBrush Color_Rebar_Top = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(255, 0, 0) };
        public static SolidColorBrush Color_Rebar_Side = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(51, 102, 255) };
        public static SolidColorBrush Color_Rebar_Bottom = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(102, 0, 0) };

        public static Autodesk.Revit.DB.Color Revit_Color_Weld = new Autodesk.Revit.DB.Color(0, 0, 255);
        public static Autodesk.Revit.DB.Color Revit_Color_Coupler = new Autodesk.Revit.DB.Color(0, 255, 0);
        public static Autodesk.Revit.DB.Color Revit_Color_Lap_InValid = new Autodesk.Revit.DB.Color(127, 127, 127);
    }
}
