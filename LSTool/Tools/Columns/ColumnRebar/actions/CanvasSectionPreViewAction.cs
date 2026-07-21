using LSTool.Cores.canvas;
using LSTool.Tools.Columns.ColumnRebar.models;
using LSTool.Utils;
using System.Windows.Controls;
using wd = System.Windows;

namespace LSTool.Tools.Columns.ColumnRebar.actions
{
    public class CanvasSectionPreViewAction
    {
        private Canvas _canvas;
        private wd.Point _canvasCenter;
        private wd.Vector _canvasVTX;
        private wd.Vector _canvasVTY;
        private double _canvasHeight;
        private double _canvasWidth;
        private double _ratio;
        private double _scale;
        public CanvasSectionPreViewAction(Canvas canvas)
        {
            _canvas = canvas;
            _ratio = 0.6;
            GetCanvasInfo(canvas,
            out _canvasCenter,
            out _canvasVTX,
            out _canvasVTY,
            out _canvasHeight,
            out _canvasWidth);
            
        }
        public void DrawSection(
            List<ColumnConcreteModel> columnConcreteModels,
            ColumnConcreteModel columnConcreteModel)
        {
            var height = columnConcreteModel.Height;
            var width = columnConcreteModel.Width;
            var cover = columnConcreteModel.Cover;
            var dx = int.Parse(Math.Round(columnConcreteModel.SpacingDX, 0).ToString());
            var dy = int.Parse(Math.Round(columnConcreteModel.SpacingDY, 0).ToString());
            var qtyMaxX = int.Parse(Math.Round(columnConcreteModels.Max(x => x.SpacingDX), 0).ToString());
            var qtyMaxY = int.Parse(Math.Round(columnConcreteModels.Max(x => x.SpacingDY), 0).ToString());

            ClearCanvas(_canvas);
            DrawSectionConcrete(height, width);
            DrawSectionStirrupMain(height, width, cover);
            DrawRebarMain(height, width, cover, dx, dy, qtyMaxX, qtyMaxY);
        }
        private void DrawSectionConcrete(double height, double width)
        {
            var cross = MMToPixel(Math.Sqrt(height * height + width * width));
            var crossCanvas = Math.Sqrt(_canvasHeight * _canvasHeight + _canvasWidth * _canvasWidth);
            _scale = crossCanvas * _ratio / cross;
            var heightInCanvas = MMToPixel(height) * _scale;
            var widthInCanvas = MMToPixel(width) * _scale;
            var shape = new List<wd.Point>()
            {
                _canvasCenter
                - _canvasVTY * heightInCanvas/2
                - _canvasVTX * widthInCanvas/2,
                _canvasCenter
                - _canvasVTY * heightInCanvas/2
                + _canvasVTX * widthInCanvas/2,
                _canvasCenter
                + _canvasVTY * heightInCanvas/2
                + _canvasVTX * widthInCanvas/2,
                _canvasCenter
                + _canvasVTY * heightInCanvas/2
                - _canvasVTX * widthInCanvas/2,
            };
            var rec = new InstanceInCanvasPolygon(
                _canvas, 
                OptionStyleInstanceInCanvas.OPTION_CONCRETE_STRUCTURE,
                shape);
            rec.DrawInCanvas();
        }
        private void DrawSectionStirrupMain(double height, double width, double cover)
        {
            var heightInCanvas = MMToPixel(Math.Abs(height - 2 * cover)) * _scale;
            var widthInCanvas = MMToPixel(Math.Abs(width - 2 * cover)) * _scale;
            var shape = new List<wd.Point>()
            {
                _canvasCenter
                - _canvasVTY * heightInCanvas/2
                - _canvasVTX * widthInCanvas/2,
                _canvasCenter
                - _canvasVTY * heightInCanvas/2
                + _canvasVTX * widthInCanvas/2,
                _canvasCenter
                + _canvasVTY * heightInCanvas/2
                + _canvasVTX * widthInCanvas/2,
                _canvasCenter
                + _canvasVTY * heightInCanvas/2
                - _canvasVTX * widthInCanvas/2,
            };
            var rec = new InstanceInCanvasPolygon(
                _canvas,
                OptionStyleInstanceInCanvas.OPTION_REBAR_LINE,
                shape);
            rec.DrawInCanvas();
        }
        private void DrawRebarMain(double height, double width, double cover, int dx, int dy, int qtyMaxX, int qtyMaxY)
        {
            var heightInCanvas = MMToPixel(Math.Abs(height - 2 * cover * 1.3)) * _scale;
            var widthInCanvas = MMToPixel(Math.Abs(width - 2 * cover * 1.3)) * _scale;

            var p1 = _canvasCenter
                - _canvasVTY * heightInCanvas / 2
                - _canvasVTX * widthInCanvas / 2;
            var p2 = _canvasCenter
                - _canvasVTY * heightInCanvas / 2
                + _canvasVTX * widthInCanvas / 2;
            var p3 = _canvasCenter
                + _canvasVTY * heightInCanvas / 2
                + _canvasVTX * widthInCanvas / 2;
            var p4 = _canvasCenter
                + _canvasVTY * heightInCanvas / 2
                - _canvasVTX * widthInCanvas / 2;
            _DrawRebarMain(dy, qtyMaxY, p4, p1, (int)ColumnFaceType.Left, true);
            _DrawRebarMain(dx, qtyMaxX, p1, p2, (int)ColumnFaceType.Top);
            _DrawRebarMain(dy, qtyMaxY, p2, p3, (int)ColumnFaceType.Right, true);
            _DrawRebarMain(dx, qtyMaxX, p3, p4, (int)ColumnFaceType.Bottom);

            void _DrawRebarMain(
                int qty, 
                int qtyMax, 
                wd.Point pStart, 
                wd.Point pEnd, 
                int faceId,
                bool ignoreStartEnd = false)
            {
                var vtBase = pStart.GetVector(pEnd);
                var vt = vtBase.VtNormal();
                var distance = vtBase.VtDistance();
                var spacing = distance / (qty + 1);
                var rebarPoss = SolvePositionInstallRebar(pStart, pEnd, qty, qtyMax);
                foreach (var rebarPos in rebarPoss)
                {
                    var index = rebarPoss.IndexOf(rebarPos);
                    if (ignoreStartEnd && (index == 0 || index == qty - 1)) continue;
                    var c = new InstanceInCanvasCircel(_canvas, OptionStyleInstanceInCanvas.OPTION_REBAR, rebarPos.Position, 10);
                    c.Id = rebarPos.Index;
                    c.HostId = faceId;
                    c.ClickAction = _RebarClickAction;
                    c.DrawInCanvas();
                }
            }
        }
        private void _RebarClickAction(InstanceInCanvasCircel circel)
        {
            IO.ShowInfo($"{circel.Id.ToString()}_{(ColumnFaceType)circel.HostId}");
        }
        private void GetCanvasInfo(
            Canvas canvas,
            out wd.Point canvasCenter,
            out wd.Vector canvasVTX,
            out wd.Vector canvasVTY,
            out double canvasHeight,
            out double canvasWidth)
        {
            canvasWidth = canvas.ActualWidth;
            canvasHeight = canvas.ActualHeight;
            canvasCenter = new wd.Point(canvasWidth / 2, canvasHeight / 2);
            canvasVTX = new wd.Vector(1, 0);
            canvasVTY = new wd.Vector(0, 1);
        }
        private double MMToPixel(double distance)
        {
            return distance * 4;
        }
        private void ClearCanvas(Canvas canvas)
        {
            canvas.Children.Clear();
        }
        private List<ColumnRebarPositionInCanvasModel> SolvePositionInstallRebar(wd.Point start, wd.Point end, int qty, int maxQty)
        {
            var results = new List<ColumnRebarPositionInCanvasModel>();
            try
            {
                var vtBase = start.GetVector(end);
                var vt = vtBase.VtNormal();
                var distance = vtBase.VtDistance();
                var spacing = (distance / (maxQty - 1));
                var qtyDu = qty % 2;
                var haft = (qty - qtyDu) / 2;
                for (int i = 0; i < haft; i++)
                {
                    var p = start.Translate(new wd.Point(vt.X * i * spacing, vt.Y * i * spacing));
                    results.Add(new ColumnRebarPositionInCanvasModel() { Index = i + 1, Position = p });
                }
                if (qtyDu == 1)
                {
                    var p = start.Mid(end);
                    results.Add(new ColumnRebarPositionInCanvasModel() { Index = 1 + maxQty / 2, Position = p });
                }
                for (int i = 0; i < haft; i++)
                {
                    var p = end.Translate(new wd.Point(-vt.X * i * spacing, -vt.Y * i * spacing));
                    results.Add(new ColumnRebarPositionInCanvasModel() { Index = maxQty - i, Position = p });
                }
            }
            catch (Exception)
            {
            }
            if (!results.Any()) return results;
            return results.OrderBy(x => x.Index).ToList();
        }
    }
}
