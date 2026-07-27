using System.Windows.Media;

namespace RIMT.Utils.canvass
{
    public class OptionStyleInstanceInCanvas
    {
        public static OptionStyleInstanceInCanvas OPTION_FUKASHI =
            new OptionStyleInstanceInCanvas(
                StyleThicknessInCanvas.Thickness_1,
                StyleDashInCanvas.Style_Dash,
                StyleColorInCanvas.Color_Red,
                null);

        public static OptionStyleInstanceInCanvas OPTION_CONCRETE_STRUCTURE =
            new OptionStyleInstanceInCanvas(
                StyleThicknessInCanvas.Thickness_1,
                StyleDashInCanvas.Style_Solid,
                StyleColorInCanvas.Color_Concrete_OutLine,
                StyleColorInCanvas.Color_Concrete);

        public static OptionStyleInstanceInCanvas OPTION_CONCRETE =
            new OptionStyleInstanceInCanvas(
                StyleThicknessInCanvas.Thickness_2,
                StyleDashInCanvas.Style_Solid,
                StyleColorInCanvas.Color_Concrete_OutLine,
                StyleColorInCanvas.Color_Concrete);

        public static OptionStyleInstanceInCanvas OPTION_OPENING =
            new OptionStyleInstanceInCanvas(
                StyleThicknessInCanvas.Thickness_2,
                StyleDashInCanvas.Style_Solid,
                StyleColorInCanvas.Color_Concrete_OutLine,
                StyleColorInCanvas.Color_BackGround);

        public static OptionStyleInstanceInCanvas OPTION_REBAR =
             new OptionStyleInstanceInCanvas(
                StyleThicknessInCanvas.Thickness_1,
                StyleDashInCanvas.Style_Solid,
                StyleColorInCanvas.Color_Rebar,
                StyleColorInCanvas.Color_Rebar);
        public static OptionStyleInstanceInCanvas OPTION_REBAR_LINE =
             new OptionStyleInstanceInCanvas(
                StyleThicknessInCanvas.Thickness_2,
                StyleDashInCanvas.Style_Solid,
                StyleColorInCanvas.Color0,
                null);

        public static OptionStyleInstanceInCanvas OPTION_GRID =
             new OptionStyleInstanceInCanvas(
                StyleThicknessInCanvas.Thickness_0dot3,
                StyleDashInCanvas.Style_Grid,
                StyleColorInCanvas.Color_Red,
                null);
        public static OptionStyleInstanceInCanvas OPTION_GRID_Note =
             new OptionStyleInstanceInCanvas(
                StyleThicknessInCanvas.Thickness_0dot3,
                StyleDashInCanvas.Style_Solid,
                StyleColorInCanvas.Color_Red,
                null);

        public double Thickness { get; set; }
        public DoubleCollection LineStyle { get; set; }
        public SolidColorBrush ColorBrush { get; set; }
        public SolidColorBrush Fill { get; set; }
        public OptionStyleInstanceInCanvas(
            double thickness,
            DoubleCollection lineStyle,
            SolidColorBrush colorBrush,
            SolidColorBrush fill)
        {
            Thickness = thickness;
            LineStyle = lineStyle;
            ColorBrush = colorBrush;
            Fill = fill;
        }
    }
}
