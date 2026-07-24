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
        private double _dimaterRebarInCanvas = 15;
        private ColumnConcreteModel _columnConcreteModel;
        private List<ColumnConcreteModel> _columnConcreteModels;
        public List<InstanceInCanvasCircel> RebarSelected { get; set; }
        public CanvasSectionPreViewAction(Canvas canvas)
        {
            RebarSelected = new List<InstanceInCanvasCircel>();
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
            _columnConcreteModels = columnConcreteModels;
            _columnConcreteModel = columnConcreteModel;
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
            var rbs = DrawRebarMain(height, width, cover, dx, dy, qtyMaxX, qtyMaxY);
            UpdateTies(columnConcreteModel, rbs);
        }
        public void UpdateTies(ColumnConcreteModel columnConcreteModel, List<InstanceInCanvasCircel> rbs)
        {
            if (!columnConcreteModel.Ties.Any()) return;
            foreach (var poss in columnConcreteModel.Ties)
            {
                var rebars = new List<InstanceInCanvasCircel>();
                foreach (var pos in poss)
                {
                    var rb = rbs.FirstOrDefault(x => x.Id == pos.Index && x.HostId == pos.Face);
                    if (rb == null) continue;
                    rebars.Add(rb);
                }
                var qty = rebars.Count;
                if (qty != 2 && qty != 5) continue;

                DrawTieInCanvas(rebars);
            }
        }
        public void CreateTies(ColumnConcreteModel columnConcreteModel, bool isAddData = true)
        {
            try
            {
                var qty = RebarSelected.Count;
                if (qty != 2 && qty != 4) throw new Exception("Số điểm của đai phụ phải là 2 hoặc 4");
                if (RebarSelected.GroupBy(x => x.HostId).Any(x => x.Count() > 2))
                    throw new Exception("Số điểm của đai phụ trên 1 mặt phẳng không được quá 3 điểm");
                if (qty == 4)
                    RebarSelected.Add(RebarSelected.First());
                DrawTieInCanvas(RebarSelected);
                if (isAddData)
                    columnConcreteModel.Ties.Add(
                        RebarSelected
                        .Select(x => new ColumnStirrupPosition() { Face = x.HostId, Index = x.Id })
                        .ToList());
            }
            catch (Exception ex)
            {
                IO.ShowWarning(ex.Message);
            }
            foreach (var item in RebarSelected)
            {
                item.IsSelected = false;
                item.UpdateStatus();
            }
            RebarSelected = new List<InstanceInCanvasCircel>();
        }
        private void DrawTieInCanvas(List<InstanceInCanvasCircel> rebars)
        {
            if (rebars.Count == 2)
            {
                DrawTwoPointTieInCanvas(rebars);
                return;
            }

            var points = GetTiePointsInCanvas(rebars);
            if (points.Count != 5)
            {
                var tie = new InstanceInCanvasPolyline(
                    _canvas,
                    OptionStyleInstanceInCanvas.OPTION_REBAR_LINE,
                    points);
                tie.DrawInCanvas();
                return;
            }

            DrawRoundedTieInCanvas(points);
        }
        private void DrawTwoPointTieInCanvas(List<InstanceInCanvasCircel> rebars)
        {
            var firstCenter = rebars[0].Point;
            var secondCenter = rebars[1].Point;
            var tieDirection = secondCenter - firstCenter;
            if (tieDirection.Length == 0) return;
            tieDirection.Normalize();

            var normal = new wd.Vector(-tieDirection.Y, tieDirection.X);
            var middle = firstCenter.Mid(secondCenter);
            var outsideDirection = middle - _canvasCenter;
            foreach (var rebar in rebars)
            {
                switch ((ColumnFaceType)rebar.HostId)
                {
                    case ColumnFaceType.Left:
                        outsideDirection -= _canvasVTX;
                        break;
                    case ColumnFaceType.Top:
                        outsideDirection -= _canvasVTY;
                        break;
                    case ColumnFaceType.Right:
                        outsideDirection += _canvasVTX;
                        break;
                    case ColumnFaceType.Bottom:
                        outsideDirection += _canvasVTY;
                        break;
                }
            }
            if (normal * outsideDirection < 0)
                normal = -normal;

            var options = OptionStyleInstanceInCanvas.OPTION_REBAR_LINE;
            var hookRadius =
                (_dimaterRebarInCanvas + options.Thickness) / 2
                + options.Thickness;
            var hookLength = _dimaterRebarInCanvas;
            var firstTangent = firstCenter + normal * hookRadius;
            var secondTangent = secondCenter + normal * hookRadius;
            var firstHookEnd = firstCenter - normal * hookRadius;
            var secondHookEnd = secondCenter - normal * hookRadius;
            var firstHookTip = firstHookEnd + tieDirection * hookLength;
            var secondHookTip = secondHookEnd - tieDirection * hookLength;
            var hookSweepDirection =
                tieDirection.X * normal.Y - tieDirection.Y * normal.X < 0
                ? wd.Media.SweepDirection.Clockwise
                : wd.Media.SweepDirection.Counterclockwise;
            var figure = new wd.Media.PathFigure
            {
                StartPoint = firstHookTip
            };
            figure.Segments.Add(new wd.Media.LineSegment(firstHookEnd, true));
            figure.Segments.Add(new wd.Media.ArcSegment(
                firstTangent,
                new wd.Size(hookRadius, hookRadius),
                0,
                false,
                hookSweepDirection,
                true));
            figure.Segments.Add(new wd.Media.LineSegment(secondTangent, true));
            figure.Segments.Add(new wd.Media.ArcSegment(
                secondHookEnd,
                new wd.Size(hookRadius, hookRadius),
                0,
                false,
                hookSweepDirection,
                true));
            figure.Segments.Add(new wd.Media.LineSegment(secondHookTip, true));

            var tie = new wd.Shapes.Path
            {
                Data = new wd.Media.PathGeometry(new[] { figure }),
                Stroke = options.ColorBrush,
                StrokeThickness = options.Thickness,
                StrokeDashArray = options.LineStyle,
                StrokeStartLineCap = wd.Media.PenLineCap.Round,
                StrokeEndLineCap = wd.Media.PenLineCap.Round
            };
            _canvas.Children.Add(tie);
        }
        private List<wd.Point> GetTiePointsInCanvas(List<InstanceInCanvasCircel> rebars)
        {
            var options = OptionStyleInstanceInCanvas.OPTION_REBAR_LINE;
            var rebarOffset = (_dimaterRebarInCanvas + options.Thickness) / 2;
            var stirrupGap = options.Thickness * 1.5;
            var tieRebars = rebars.Count == 5
                ? rebars.Take(4).ToList()
                : rebars;
            var result = tieRebars.Select(x => x.Point).ToList();

            if (_columnConcreteModel != null)
            {
                var heightInCanvas = MMToPixel(Math.Abs(
                    _columnConcreteModel.Height - 2 * _columnConcreteModel.Cover)) * _scale;
                var widthInCanvas = MMToPixel(Math.Abs(
                    _columnConcreteModel.Width - 2 * _columnConcreteModel.Cover)) * _scale;
                for (int i = 0; i < tieRebars.Count; i++)
                {
                    var point = result[i];
                    switch ((ColumnFaceType)tieRebars[i].HostId)
                    {
                        case ColumnFaceType.Left:
                            point.X = _canvasCenter.X - widthInCanvas / 2 - stirrupGap;
                            break;
                        case ColumnFaceType.Top:
                            point.Y = _canvasCenter.Y - heightInCanvas / 2 - stirrupGap;
                            break;
                        case ColumnFaceType.Right:
                            point.X = _canvasCenter.X + widthInCanvas / 2 + stirrupGap;
                            break;
                        case ColumnFaceType.Bottom:
                            point.Y = _canvasCenter.Y + heightInCanvas / 2 + stirrupGap;
                            break;
                    }
                    result[i] = point;
                }
            }

            foreach (var group in tieRebars
                .Select((rebar, index) => new { rebar, index })
                .GroupBy(x => x.rebar.HostId))
            {
                var face = (ColumnFaceType)group.Key;
                var indexes = face == ColumnFaceType.Top || face == ColumnFaceType.Bottom
                    ? group.OrderBy(x => x.rebar.Point.X).Select(x => x.index).ToList()
                    : group.OrderBy(x => x.rebar.Point.Y).Select(x => x.index).ToList();
                if (indexes.Count < 2) continue;

                var first = result[indexes.First()];
                var last = result[indexes.Last()];
                if (face == ColumnFaceType.Top || face == ColumnFaceType.Bottom)
                {
                    first.X -= rebarOffset;
                    last.X += rebarOffset;
                }
                else
                {
                    first.Y -= rebarOffset;
                    last.Y += rebarOffset;
                }
                result[indexes.First()] = first;
                result[indexes.Last()] = last;
            }

            if (result.Count == 4)
            {
                var center = new wd.Point(
                    result.Average(x => x.X),
                    result.Average(x => x.Y));
                result = result
                    .OrderBy(x => Math.Atan2(x.Y - center.Y, x.X - center.X))
                    .ToList();
                result.Add(result.First());
            }
            return result;
        }
        private void DrawRoundedTieInCanvas(List<wd.Point> points)
        {
            var corners = points.Take(points.Count - 1).ToList();
            var cornerStarts = new List<wd.Point>();
            var cornerEnds = new List<wd.Point>();
            var cornerRadii = new List<double>();
            var radius = _dimaterRebarInCanvas * 0.75;
            for (int i = 0; i < corners.Count; i++)
            {
                var previous = corners[(i - 1 + corners.Count) % corners.Count];
                var current = corners[i];
                var next = corners[(i + 1) % corners.Count];
                var cornerOffset = Math.Min(
                    radius,
                    Math.Min((current - previous).Length, (next - current).Length) / 2);
                cornerRadii.Add(cornerOffset);
                cornerStarts.Add(MovePointTowards(current, previous, cornerOffset));
                cornerEnds.Add(MovePointTowards(current, next, cornerOffset));
            }

            var area = 0.0;
            for (int i = 0; i < corners.Count; i++)
            {
                var next = corners[(i + 1) % corners.Count];
                area += corners[i].X * next.Y - next.X * corners[i].Y;
            }
            var sweepDirection = area >= 0
                ? wd.Media.SweepDirection.Clockwise
                : wd.Media.SweepDirection.Counterclockwise;
            var figure = new wd.Media.PathFigure
            {
                StartPoint = cornerEnds[0],
                IsClosed = true
            };
            for (int i = 1; i < corners.Count; i++)
            {
                figure.Segments.Add(new wd.Media.LineSegment(cornerStarts[i], true));
                figure.Segments.Add(new wd.Media.ArcSegment(
                    cornerEnds[i],
                    new wd.Size(cornerRadii[i], cornerRadii[i]),
                    0,
                    false,
                    sweepDirection,
                    true));
            }
            figure.Segments.Add(new wd.Media.LineSegment(cornerStarts[0], true));
            figure.Segments.Add(new wd.Media.ArcSegment(
                cornerEnds[0],
                new wd.Size(cornerRadii[0], cornerRadii[0]),
                0,
                false,
                sweepDirection,
                true));

            var options = OptionStyleInstanceInCanvas.OPTION_REBAR_LINE;
            var tie = new wd.Shapes.Path
            {
                Data = new wd.Media.PathGeometry(new[] { figure }),
                Stroke = options.ColorBrush,
                StrokeThickness = options.Thickness,
                StrokeDashArray = options.LineStyle
            };
            _canvas.Children.Add(tie);
        }
        private wd.Point MovePointTowards(wd.Point start, wd.Point end, double distance)
        {
            var direction = end - start;
            if (direction.Length == 0) return start;
            direction.Normalize();
            return start + direction * distance;
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
            var cornerRadius = Math.Min(
                _dimaterRebarInCanvas,
                Math.Min(heightInCanvas, widthInCanvas) / 2);
            var options = OptionStyleInstanceInCanvas.OPTION_REBAR_LINE;
            var stirrup = new System.Windows.Shapes.Rectangle
            {
                Width = widthInCanvas,
                Height = heightInCanvas,
                RadiusX = cornerRadius,
                RadiusY = cornerRadius,
                Stroke = options.ColorBrush,
                StrokeThickness = options.Thickness,
                StrokeDashArray = options.LineStyle
            };
            Canvas.SetLeft(stirrup, _canvasCenter.X - widthInCanvas / 2);
            Canvas.SetTop(stirrup, _canvasCenter.Y - heightInCanvas / 2);
            _canvas.Children.Add(stirrup);
        }
        private List<InstanceInCanvasCircel> DrawRebarMain(double height, double width, double cover, int dx, int dy, int qtyMaxX, int qtyMaxY)
        {
            var result = new List<InstanceInCanvasCircel>();
            var heightInCanvas = MMToPixel(Math.Abs(height - 2 * cover * 1.8)) * _scale;
            var widthInCanvas = MMToPixel(Math.Abs(width - 2 * cover * 1.8)) * _scale;

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
            var qtyL = _DrawRebarMain(dy, qtyMaxY, p4, p1, (int)ColumnFaceType.Left, true);
            var qtyT = _DrawRebarMain(dx, qtyMaxX, p1, p2, (int)ColumnFaceType.Top);
            var qtyR = _DrawRebarMain(dy, qtyMaxY, p2, p3, (int)ColumnFaceType.Right, true);
            var qtyB = _DrawRebarMain(dx, qtyMaxX, p3, p4, (int)ColumnFaceType.Bottom);
            result.AddRange(qtyL);
            result.AddRange(qtyT);
            result.AddRange(qtyR);
            result.AddRange(qtyB);
            return result;
            List<InstanceInCanvasCircel> _DrawRebarMain(
                int qty,
                int qtyMax,
                wd.Point pStart,
                wd.Point pEnd,
                int faceId,
                bool ignoreStartEnd = false)
            {
                var result = new List<InstanceInCanvasCircel>();
                var vtBase = pStart.GetVector(pEnd);
                var vt = vtBase.VtNormal();
                var distance = vtBase.VtDistance();
                var spacing = distance / (qty + 1);
                var rebarPoss = SolvePositionInstallRebar(pStart, pEnd, qty, qtyMax);
                foreach (var rebarPos in rebarPoss)
                {
                    var index = rebarPoss.IndexOf(rebarPos);
                    if (ignoreStartEnd && (index == 0 || index == qty - 1)) continue;
                    var c = new InstanceInCanvasCircel(_canvas, OptionStyleInstanceInCanvas.OPTION_REBAR, rebarPos.Position, _dimaterRebarInCanvas);
                    c.Id = rebarPos.Index;
                    c.HostId = faceId;
                    if (index != 0 && index != qty - 1)
                    {
                        c.LClickAction = _RebarClickAction;
                        c.RClickAction = _RClickAction;
                    }
                    c.DrawInCanvas();
                    result.Add(c);
                }
                return result;
            }
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
        private void _RebarClickAction(InstanceInCanvasCircel circel)
        {
            circel.IsSelected = !circel.IsSelected;
            circel.UpdateStatus();
            if (circel.IsSelected)
                RebarSelected.Add(circel);
            else
            {
                var circleTar = RebarSelected.FirstOrDefault(x => x.Id == circel.Id && x.HostId == circel.HostId);
                if (circleTar != null)
                    RebarSelected.Remove(circleTar);
            }
        }
        private void _RClickAction(InstanceInCanvasCircel circel)
        {
            if (_columnConcreteModel == null) return;
            var teisNew = new List<List<ColumnStirrupPosition>>();
            foreach (var item in _columnConcreteModel.Ties)
            {
                if (item.Any(x => x.Index == circel.Id && x.Face == circel.HostId)) continue;
                teisNew.Add(item);
            }
            _columnConcreteModel.Ties = teisNew;
            DrawSection(_columnConcreteModels, _columnConcreteModel);
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
