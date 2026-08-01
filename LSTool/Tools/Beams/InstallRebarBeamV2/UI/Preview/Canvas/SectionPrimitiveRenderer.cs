using LSTool.Compatibility;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using RevitApp.Utils.RevElements.RevRebars;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using RIMT.Utils.canvass;
using RIMT.Utils.RevRebars;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using LSTool.Tools.Beams.InstallRebarBeamV2.models.SecondaryStirrups;
using Canvas = System.Windows.Controls.Canvas;
using Point = System.Windows.Point;
using Ellipse = System.Windows.Shapes.Ellipse;
using LSTool.Tools.Beams.InstallRebarBeamV2.iservices;
using LSTool.Tools.Beams.InstallRebarBeamV2.service;
using Brushes = System.Windows.Media.Brushes;
using LineSegment = System.Windows.Media.LineSegment;
using Size = System.Windows.Size;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.service
{
    public partial class DrawRebarBeamInCanvasSerice
    {
        private void _drawSectionBeamStirrup(RebarBeam rebarBeam, double coverMm, CanvasPageBase canvasPageBase, BeamFukashi beamFukashi = null)
        {
            var centerCanvas = canvasPageBase.Center;
            var scale = canvasPageBase.RatioScale * canvasPageBase.DistanceCrossScreen / Math.Sqrt(rebarBeam.BeamWidthMm * rebarBeam.BeamWidthMm + rebarBeam.BeamHeightMm * rebarBeam.BeamHeightMm);
            var option = OptionStyleInstanceInCanvas.OPTION_REBAR_LINE;
            var p1 = beamFukashi == null
                ? new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverMm))
                : new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverMm - beamFukashi.FukashiLeft.ValueMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverMm - beamFukashi.FukashiTop.ValueMm));
            var p2 = beamFukashi == null
                ? new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverMm))
                : new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverMm - beamFukashi.FukashiRight.ValueMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverMm - beamFukashi.FukashiTop.ValueMm));
            var p3 = beamFukashi == null
                ? new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverMm))
                : new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverMm - beamFukashi.FukashiRight.ValueMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverMm - beamFukashi.FukashiBot.ValueMm));
            var p4 = beamFukashi == null
                ? new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverMm))
                : new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverMm - beamFukashi.FukashiLeft.ValueMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverMm - beamFukashi.FukashiBot.ValueMm));
            var ps = new List<Point>() {
                p1,p2, p3,p4
            };
            var stirrup = new InstanceInCanvasPolygon(canvasPageBase, option, ps);
            stirrup.DrawInCanvas();
        }
        private void _drawSectionBeamConcrete(RebarBeam rebarBeam, CanvasPageBase canvasPageBase)
        {
            var centerCanvas = canvasPageBase.Center;
            var scale = canvasPageBase.RatioScale * canvasPageBase.DistanceCrossScreen / Math.Sqrt(rebarBeam.BeamWidthMm * rebarBeam.BeamWidthMm + rebarBeam.BeamHeightMm * rebarBeam.BeamHeightMm);
            var option = OptionStyleInstanceInCanvas.OPTION_CONCRETE_STRUCTURE;

            var p1 = new Point(centerCanvas.X - scale * rebarBeam.BeamWidthMm / 2, centerCanvas.Y - scale * rebarBeam.BeamHeightMm / 2);
            var p2 = new Point(centerCanvas.X + scale * rebarBeam.BeamWidthMm / 2, centerCanvas.Y - scale * rebarBeam.BeamHeightMm / 2);
            var p3 = new Point(centerCanvas.X + scale * rebarBeam.BeamWidthMm / 2, centerCanvas.Y + scale * rebarBeam.BeamHeightMm / 2);
            var p4 = new Point(centerCanvas.X - scale * rebarBeam.BeamWidthMm / 2, centerCanvas.Y + scale * rebarBeam.BeamHeightMm / 2);
            var ps = new List<Point>() {
                p1, p2, p3, p4
            };
            var sectionBeam = new InstanceInCanvasPolygon(canvasPageBase, option, ps);
            sectionBeam.DrawInCanvas();
        }
        private void _drawSectionBeamFukashi(RebarBeam rebarBeam, CanvasPageBase canvasPageBase, BeamFukashi beamFukashi)
        {
            try
            {
                if (beamFukashi.FukashiTop.Parameter == null) return;
                if (beamFukashi.FukashiRight.Parameter == null) return;
                if (beamFukashi.FukashiBot.Parameter == null) return;
                if (beamFukashi.FukashiLeft.Parameter == null) return;
                var centerCanvas = canvasPageBase.Center;
                var scale = canvasPageBase.RatioScale * canvasPageBase.DistanceCrossScreen / Math.Sqrt(rebarBeam.BeamWidthMm * rebarBeam.BeamWidthMm + rebarBeam.BeamHeightMm * rebarBeam.BeamHeightMm);
                var option = OptionStyleInstanceInCanvas.OPTION_FUKASHI;

                var p1 = new Point(
                    centerCanvas.X - scale * (-beamFukashi.FukashiLeft.ValueMm + rebarBeam.BeamWidthMm / 2),
                    centerCanvas.Y - scale * (-beamFukashi.FukashiTop.ValueMm + rebarBeam.BeamHeightMm / 2));
                var p2 = new Point(
                    centerCanvas.X + scale * (-beamFukashi.FukashiRight.ValueMm + rebarBeam.BeamWidthMm / 2),
                    centerCanvas.Y - scale * (-beamFukashi.FukashiTop.ValueMm + rebarBeam.BeamHeightMm / 2));
                var p3 = new Point(
                    centerCanvas.X + scale * (-beamFukashi.FukashiRight.ValueMm + rebarBeam.BeamWidthMm / 2),
                    centerCanvas.Y + scale * (-beamFukashi.FukashiBot.ValueMm + rebarBeam.BeamHeightMm / 2));
                var p4 = new Point(
                    centerCanvas.X - scale * (-beamFukashi.FukashiLeft.ValueMm + rebarBeam.BeamWidthMm / 2),
                    centerCanvas.Y + scale * (-beamFukashi.FukashiBot.ValueMm + rebarBeam.BeamHeightMm / 2));
                var ps = new List<Point>() {
                    p1, p2, p3, p4
                };
                var sectionBeam = new InstanceInCanvasPolygon(canvasPageBase, option, ps);
                sectionBeam.DrawInCanvas();
            }
            catch (Exception)
            {
            }
        }
    }
}

