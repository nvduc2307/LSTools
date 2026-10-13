using LSTool.Cores.canvas;
using LSTool.MVVM.Models;
using LSTool.Tools.Beams.BeamRebar.models;
using LSTool.Tools.Beams.BeamRebar.types;
using LSTool.Tools.Beams.BeamRebar.Utils;
using System.ComponentModel;
using System.Windows.Controls;
using wd = System.Windows;
using wm = System.Windows.Media;
using ws = System.Windows.Shapes;

namespace LSTool.Tools.Beams.BeamRebar.action
{
    public class BeamCanvasSectionPreViewAction
    {
        private readonly Canvas _canvasStart;
        private readonly Canvas _canvasMid;
        private readonly Canvas _canvasEnd;

        private BeamRebarModel _currentModel;
        private List<BeamRebarModel> _allModels;

        private int _gridQty = 2;
        private readonly List<(int Id, int Host)> _picked = new List<(int, int)>();
        private List<InstanceInCanvasCircel> _curCircles = new List<InstanceInCanvasCircel>();
        private wd.Point _curCenter;

        private static readonly OptionStyleInstanceInCanvas BeamRebarDot = new OptionStyleInstanceInCanvas(
            StyleThicknessInCanvas.Thickness_1, StyleDashInCanvas.Style_Solid,
            StyleColorInCanvas.Color_Red, StyleColorInCanvas.Color_Red);
        private static readonly OptionStyleInstanceInCanvas BeamRebarLine = new OptionStyleInstanceInCanvas(
            StyleThicknessInCanvas.Thickness_2, StyleDashInCanvas.Style_Solid,
            StyleColorInCanvas.Color_Red, null);

        private const double Ratio = 0.9;
        private double _pixelsPerMm;
        private double _maxRebarDiameterInCanvas;
        private const double LayerGap = 4.0;
        private const double BeamDotMinDiameter = 12.0;
        private const double BeamDotMaxDiameter = 20.0;
        private const int SideLeftCode = 6;
        private const int SideRightCode = 7;
        public const int SideTieLayer = 6;
        private const double PadSide = 24.0;
        private const double PadTop = 26.0;
        private const double PadBottom = 44.0;

        public BeamCanvasSectionPreViewAction(Canvas canvasStart, Canvas canvasMid, Canvas canvasEnd)
        {
            _canvasStart = canvasStart;
            _canvasMid   = canvasMid;
            _canvasEnd   = canvasEnd;
            foreach (var canvas in new[] { _canvasStart, _canvasMid, _canvasEnd })
                canvas.SizeChanged += (sender, args) => Redraw();
        }


        public void SetAllModels(IEnumerable<BeamRebarModel> models)
        {
            _allModels = models?.Where(x => x != null && x.Width > 0 && x.Height > 0).ToList();
        }

        public void DrawSection(BeamRebarModel model, IEnumerable<BeamRebarModel> allModels = null)
        {
            if (model == null) return;
            if (allModels != null)
            {
                SetAllModels(allModels);
            }

            UnsubscribeRebarChanges(_currentModel);
            if (!ReferenceEquals(_currentModel, model)) _picked.Clear();
            _currentModel = model;
            SubscribeRebarChanges(_currentModel);

            Redraw();
        }


        private void SubscribeRebarChanges(BeamRebarModel model)
        {
            if (model == null) return;
            foreach (var rebar in GetAllRebars(model))
                rebar.PropertyChanged += OnRebarPropertyChanged;
        }

        private void UnsubscribeRebarChanges(BeamRebarModel model)
        {
            if (model == null) return;
            foreach (var rebar in GetAllRebars(model))
                rebar.PropertyChanged -= OnRebarPropertyChanged;
        }

        private static IEnumerable<RebarModel> GetAllRebars(BeamRebarModel model)
        {
            var seen = new HashSet<RebarModel>(ReferenceEqualityComparer.Instance);
            foreach (var section in new[] { model.SectionStart, model.SectionMid, model.SectionEnd })
            {
                if (section == null) continue;
                foreach (var rb in new[]
                {
                    section.RebarTop1, section.RebarTop2, section.RebarTop3,
                    section.RebarBot1, section.RebarBot2, section.RebarBot3,
                    section.SideBar,   section.Stirrup
                })
                {
                    if (rb != null && seen.Add(rb)) yield return rb;
                }
            }
        }


        private void OnRebarPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(RebarModel.Spacing) or nameof(RebarModel.Diameter) or nameof(RebarModel.Name))
                Redraw();
        }

        public void Redraw()
        {
            if (_currentModel == null) return;
            _gridQty = BeamRebarUtils.GetGridQty(
                (IEnumerable<BeamRebarModel>)_allModels ?? new[] { _currentModel });
            DrawOnCanvas(_canvasStart, _currentModel, _currentModel.SectionStart, 0);
            DrawOnCanvas(_canvasMid,   _currentModel, _currentModel.SectionMid, 1);
            DrawOnCanvas(_canvasEnd,   _currentModel, _currentModel.SectionEnd, 2);
        }


        private void DrawOnCanvas(Canvas canvas, BeamRebarModel beam, BeamRebarSectionModel section, int secIdx)
        {
            if (canvas == null) return;
            canvas.Children.Clear();
            _curCircles = new List<InstanceInCanvasCircel>();

            var cw = canvas.ActualWidth;
            var ch = canvas.ActualHeight;
            if (cw <= 0 || ch <= 0) return;

            var width  = beam.Width;
            var height = beam.Height;
            var cover  = beam.Cover / 2;
            if (width <= 0 || height <= 0) return;

            var validModels = _allModels?.Where(x => x != null && x.Width > 0 && x.Height > 0).ToList();
            var refWidth  = (validModels != null && validModels.Any()) ? Math.Max(width, validModels.Max(x => x.Width)) : width;
            var refHeight = (validModels != null && validModels.Any()) ? Math.Max(height, validModels.Max(x => x.Height)) : height;

            if (refWidth <= 0) refWidth = width;
            if (refHeight <= 0) refHeight = height;

            var areaW = Math.Max(10, cw - 2 * PadSide);
            var areaH = Math.Max(10, ch - PadTop - PadBottom);
            var center = new wd.Point(cw / 2, PadTop + areaH / 2);

            var scale = Ratio * Math.Min(areaW / MmToPx(refWidth), areaH / MmToPx(refHeight));

            var wPx = MmToPx(width)  * scale;
            var hPx = MmToPx(height) * scale;

            DrawConcrete(canvas, center, wPx, hPx);

            var coverPx      = MmToPx(cover) * scale;
            DrawStirrup(canvas, center, wPx, hPx, coverPx);

            DrawDimensionsAndInfo(canvas, center, wPx, hPx, width, height, section);

            if (section == null) return;

            var stirrupHalfThickness = BeamRebarLine.Thickness / 2;
            const double rebarGap = 1.5;
            var stirrupOffset = stirrupHalfThickness + rebarGap;
            var innerHalfW = Math.Max(0, wPx / 2 - coverPx - stirrupOffset);
            var innerHalfH = Math.Max(0, hPx / 2 - coverPx - stirrupOffset);

            _pixelsPerMm = MmToPx(1) * scale;
            var horizontalLimit = RebarSectionPreviewStyle.GetDiameterLimit(2 * innerHalfW, _gridQty);
            var layerCount = 2 + new[] { section.RebarTop2, section.RebarTop3,
                section.RebarBot2, section.RebarBot3 }.Count(rb => rb != null && rb.Spacing > 0);
            var verticalLimit = (2 * innerHalfH - (layerCount - 1) * LayerGap) / layerCount;
            _maxRebarDiameterInCanvas = Math.Min(BeamDotMaxDiameter,
                Math.Min(horizontalLimit, verticalLimit));
            if (_maxRebarDiameterInCanvas <= 0) return;


            _curCenter = center;
            const int S = 10;
            double topRowY  = center.Y - innerHalfH;
            topRowY = DrawRebarRow(canvas, center, innerHalfW, topRowY, section.RebarTop1, true, BeamRebarLayerType.Top1, secIdx * S);
            topRowY = DrawRebarRow(canvas, center, innerHalfW, topRowY, section.RebarTop2, true, BeamRebarLayerType.Top2, secIdx * S);
                      DrawRebarRow(canvas, center, innerHalfW, topRowY, section.RebarTop3, true, BeamRebarLayerType.Top3, secIdx * S);

            double botRowY  = center.Y + innerHalfH;
            botRowY = DrawRebarRow(canvas, center, innerHalfW, botRowY, section.RebarBot1, false, BeamRebarLayerType.Bot1, secIdx * S);
            botRowY = DrawRebarRow(canvas, center, innerHalfW, botRowY, section.RebarBot2, false, BeamRebarLayerType.Bot2, secIdx * S);
                      DrawRebarRow(canvas, center, innerHalfW, botRowY, section.RebarBot3, false, BeamRebarLayerType.Bot3, secIdx * S);

            DrawSideBars(canvas, center, innerHalfW, innerHalfH, section.SideBar, secIdx * S);

            DrawTies(canvas, beam, secIdx);
        }


        private static void DrawConcrete(Canvas canvas, wd.Point center, double wPx, double hPx)
        {
            var pts = new List<wd.Point>
            {
                new wd.Point(center.X - wPx / 2, center.Y - hPx / 2),
                new wd.Point(center.X + wPx / 2, center.Y - hPx / 2),
                new wd.Point(center.X + wPx / 2, center.Y + hPx / 2),
                new wd.Point(center.X - wPx / 2, center.Y + hPx / 2),
            };
            new InstanceInCanvasPolygon(
                canvas,
                OptionStyleInstanceInCanvas.OPTION_CONCRETE_STRUCTURE,
                pts).DrawInCanvas();
        }

        private static void DrawStirrup(Canvas canvas, wd.Point center,
            double wPx, double hPx, double coverPx)
        {
            var inW = wPx - 2 * coverPx;
            var inH = hPx - 2 * coverPx;
            if (inW <= 0 || inH <= 0) return;

            var cornerR = Math.Min(BeamDotMaxDiameter / 2, Math.Min(inW, inH) / 4);
            var opts    = BeamRebarLine;
            var rect = new System.Windows.Shapes.Rectangle
            {
                Width           = inW,
                Height          = inH,
                RadiusX         = cornerR,
                RadiusY         = cornerR,
                Stroke          = opts.ColorBrush,
                StrokeThickness = opts.Thickness,
                StrokeDashArray = opts.LineStyle,
                Fill            = System.Windows.Media.Brushes.Transparent,
            };
            Canvas.SetLeft(rect, center.X - inW / 2);
            Canvas.SetTop(rect,  center.Y - inH / 2);
            canvas.Children.Add(rect);
        }

        private double DrawRebarRow(
            Canvas canvas, wd.Point center, double innerHalfW,
            double rowY, MVVM.Models.RebarModel rebar, bool goingDown,
            BeamRebarLayerType layer, int hostBase)
        {
            var qty = rebar?.Spacing ?? 0;
            if (BeamRebarUtils.LayerOrder(layer) == 0) qty = Math.Max(2, qty);
            if (qty <= 0) return rowY;

            var d       = GetPreviewDiameter(rebar);
            var centerY = goingDown ? rowY + d / 2 : rowY - d / 2;
            var usableHalf = Math.Max(0, innerHalfW - d / 2);

            var pos = BeamRebarUtils.SolveGridPositions(qty, _gridQty, 1.0);
            var minIdx = pos.Keys.Min();
            var maxIdx = pos.Keys.Max();
            var isFirstLayer = BeamRebarUtils.LayerOrder(layer) == 0;

            foreach (var kv in pos)
            {
                var cx = center.X + kv.Value * usableHalf;
                var isOuter = kv.Key == minIdx || kv.Key == maxIdx;
                var pickable = isFirstLayer ? !isOuter : (isOuter && qty >= 2);

                var c = new InstanceInCanvasCircel(
                    canvas, BeamRebarDot,
                    new wd.Point(cx, centerY), d)
                {
                    Id = kv.Key,
                    HostId = (int)layer + hostBase,
                };
                if (pickable)
                {
                    c.LClickAction = _RebarClickAction;
                    c.RClickAction = _RClickAction;
                    if (_picked.Contains((c.Id, c.HostId)))
                    {
                        c.IsSelected = true;
                        c.UpdateStatus();
                    }
                }
                c.DrawInCanvas();
                _curCircles.Add(c);
            }

            return goingDown
                ? centerY + d / 2 + LayerGap
                : centerY - d / 2 - LayerGap;
        }


        private void _RebarClickAction(InstanceInCanvasCircel circel)
        {
            var key = (circel.Id, circel.HostId);
            if (!_picked.Remove(key)) _picked.Add(key);
            Redraw();
        }

        private void _RClickAction(InstanceInCanvasCircel circel)
        {
            if (_currentModel?.Ties == null) return;
            var layer = (BeamRebarLayerType)(circel.HostId % 10);
            _currentModel.Ties = _currentModel.Ties.Where(t => !IsTieOfBar(t, layer, circel.Id)).ToList();
            Redraw();
        }

        private static bool IsTieOfBar(BeamTieModel t, BeamRebarLayerType layer, int index)
        {
            if (t.Type == (int)BeamTieType.Vertical)
                return (layer == BeamRebarLayerType.Top1 || layer == BeamRebarLayerType.Bot1) && t.Index == index;
            if ((int)layer >= SideLeftCode)
                return t.Layer == SideTieLayer && t.Index == index;
            return t.Layer == (int)layer;
        }

        public bool CreateTies(BeamTieType type)
        {
            if (_currentModel == null) return false;
            try
            {
                if (_picked.Count == 0)
                    throw new Exception("Vui lòng pick thép chủ (chấm tròn) trên canvas trước.");
                if (_picked.Select(x => x.Host / 10).Distinct().Count() > 1)
                    throw new Exception("Các thanh pick phải cùng 1 mặt cắt.");
                var items = _picked.Select(x => (Id: x.Id, Layer: (BeamRebarLayerType)(x.Host % 10))).ToList();
                if (_currentModel.Ties == null) _currentModel.Ties = new List<BeamTieModel>();

                BeamTieModel tie;
                if (type == BeamTieType.Vertical)
                {
                    if (items.Count != 2
                        || !items.Any(x => x.Layer == BeamRebarLayerType.Top1)
                        || !items.Any(x => x.Layer == BeamRebarLayerType.Bot1))
                        throw new Exception("Đai phụ đứng: pick đúng 1 thanh lớp Top1 và 1 thanh lớp Bot1.");
                    if (items[0].Id != items[1].Id)
                        throw new Exception("Đai phụ đứng: 2 thanh Top1 và Bot1 phải cùng vị trí (thẳng đứng).");
                    tie = new BeamTieModel { Type = (int)BeamTieType.Vertical, Index = items[0].Id };
                    if (_currentModel.Ties.Any(t => t.Type == tie.Type && t.Index == tie.Index))
                        throw new Exception("Đai phụ đứng tại vị trí này đã tồn tại.");
                }
                else
                {
                    var layer = items[0].Layer;
                    if ((int)layer >= SideLeftCode)
                    {
                        if (items.Any(x => (int)x.Layer < SideLeftCode) || items.Any(x => x.Id != items[0].Id)
                            || items.Count > 2 || (items.Count == 2 && items[0].Layer == items[1].Layer))
                            throw new Exception("Đai phụ ngang sidebar: pick thanh trái và/hoặc phải của cùng 1 hàng.");
                        var sideTie = new BeamTieModel
                        {
                            Type = (int)BeamTieType.Horizontal,
                            Layer = SideTieLayer,
                            Index = items[0].Id,
                        };
                        if (_currentModel.Ties.Any(t => t.Type == sideTie.Type && t.Layer == sideTie.Layer
                                && t.Index == sideTie.Index))
                            throw new Exception("Đai phụ ngang của hàng sidebar này đã tồn tại.");
                        tie = sideTie;
                    }
                    else
                    {
                    if (items.Any(x => x.Layer != layer) || BeamRebarUtils.LayerOrder(layer) == 0)
                        throw new Exception("Đai phụ ngang: pick thanh ngoài cùng của cùng 1 lớp Top2/Top3/Bot2/Bot3.");
                    if (items.Count > 2 || (items.Count == 2 && items[0].Id == items[1].Id))
                        throw new Exception("Đai phụ ngang: chỉ pick tối đa 2 thanh ngoài cùng của lớp.");
                    tie = new BeamTieModel { Type = (int)BeamTieType.Horizontal, Layer = (int)layer };
                    if (_currentModel.Ties.Any(t => t.Type == tie.Type && t.Layer == tie.Layer))
                        throw new Exception("Đai phụ ngang của lớp này đã tồn tại.");
                    }
                }

                _currentModel.Ties.Add(tie);
                _picked.Clear();
                Redraw();
                return true;
            }
            catch (Exception ex)
            {
                wd.MessageBox.Show(ex.Message, "Đai phụ", wd.MessageBoxButton.OK, wd.MessageBoxImage.Warning);
                return false;
            }
        }

        private void DrawTies(Canvas canvas, BeamRebarModel beam, int secIdx)
        {
            if (beam?.Ties == null) return;
            foreach (var t in beam.Ties)
            {
                if (t.Type == (int)BeamTieType.Vertical)
                {
                    var top = _curCircles.FirstOrDefault(c => c.HostId % 10 == (int)BeamRebarLayerType.Top1 && c.Id == t.Index);
                    var bot = _curCircles.FirstOrDefault(c => c.HostId % 10 == (int)BeamRebarLayerType.Bot1 && c.Id == t.Index);
                    if (top == null || bot == null) continue;
                    var midX = (top.Point.X + bot.Point.X) / 2;
                    var outside = new wd.Vector(midX >= _curCenter.X ? 1 : -1, 0);
                    DrawTwoPointTie(canvas, top, bot, outside);
                }
                else
                {
                    if (t.Layer == SideTieLayer)
                    {
                        var l = _curCircles.FirstOrDefault(c => c.HostId % 10 == SideLeftCode && c.Id == t.Index);
                        var r = _curCircles.FirstOrDefault(c => c.HostId % 10 == SideRightCode && c.Id == t.Index);
                        if (l != null && r != null)
                            DrawTwoPointTie(canvas, l, r, new wd.Vector(0, -1));
                        continue;
                    }
                    var layer = (BeamRebarLayerType)t.Layer;
                    var cs = _curCircles.Where(c => c.HostId % 10 == t.Layer).OrderBy(c => c.Id).ToList();
                    if (cs.Count < 2) continue;
                    var inward = new wd.Vector(0, BeamRebarUtils.IsTopLayer(layer) ? 1 : -1);
                    DrawTwoPointTie(canvas, cs.First(), cs.Last(), inward);
                }
            }
        }

        private static void DrawTwoPointTie(Canvas canvas, InstanceInCanvasCircel first,
            InstanceInCanvasCircel second, wd.Vector sideDirection)
        {
            var firstCenter = first.Point;
            var secondCenter = second.Point;
            var dir = secondCenter - firstCenter;
            if (dir.Length == 0) return;
            dir.Normalize();
            var normal = new wd.Vector(-dir.Y, dir.X);
            if (normal * sideDirection < 0) normal = -normal;

            var options = BeamRebarLine;
            var diameter = Math.Max(first.Diameter, second.Diameter);
            var hookRadius = (diameter + options.Thickness) / 2 + options.Thickness;
            var hookLength = diameter;
            var firstTangent = firstCenter + normal * hookRadius;
            var secondTangent = secondCenter + normal * hookRadius;
            var firstHookEnd = firstCenter - normal * hookRadius;
            var secondHookEnd = secondCenter - normal * hookRadius;
            var firstHookTip = firstHookEnd + dir * hookLength;
            var secondHookTip = secondHookEnd - dir * hookLength;
            var sweep = dir.X * normal.Y - dir.Y * normal.X < 0
                ? wm.SweepDirection.Clockwise : wm.SweepDirection.Counterclockwise;

            var figure = new wm.PathFigure { StartPoint = firstHookTip };
            figure.Segments.Add(new wm.LineSegment(firstHookEnd, true));
            figure.Segments.Add(new wm.ArcSegment(firstTangent, new wd.Size(hookRadius, hookRadius), 0, false, sweep, true));
            figure.Segments.Add(new wm.LineSegment(secondTangent, true));
            figure.Segments.Add(new wm.ArcSegment(secondHookEnd, new wd.Size(hookRadius, hookRadius), 0, false, sweep, true));
            figure.Segments.Add(new wm.LineSegment(secondHookTip, true));

            canvas.Children.Add(new ws.Path
            {
                Data = new wm.PathGeometry(new[] { figure }),
                Stroke = options.ColorBrush,
                StrokeThickness = options.Thickness,
                StrokeDashArray = options.LineStyle,
                StrokeStartLineCap = wm.PenLineCap.Round,
                StrokeEndLineCap = wm.PenLineCap.Round,
                IsHitTestVisible = false,
            });
        }

        private void DrawSideBars(
            Canvas canvas, wd.Point center,
            double innerHalfW, double innerHalfH,
            MVVM.Models.RebarModel sideBar, int hostBase)
        {
            var qty = sideBar?.Spacing ?? 0;
            if (qty <= 0) return;

            var d      = GetPreviewDiameter(sideBar);
            var leftX  = center.X - innerHalfW + d / 2;
            var rightX = center.X + innerHalfW - d / 2;

            var usableH = 2 * innerHalfH - 2 * d;
            var spacing  = qty > 1 ? usableH / (qty + 1) : usableH / 2;

            for (int i = 0; i < qty; i++)
            {
                var cy = center.Y - innerHalfH + d + (i + 1) * spacing;
                var row = qty - i;
                foreach (var (x, layerCode) in new[] { (leftX, SideLeftCode), (rightX, SideRightCode) })
                {
                    var c = new InstanceInCanvasCircel(
                        canvas, BeamRebarDot,
                        new wd.Point(x, cy), d)
                    {
                        Id = row,
                        HostId = layerCode + hostBase,
                        LClickAction = _RebarClickAction,
                        RClickAction = _RClickAction,
                    };
                    if (_picked.Contains((c.Id, c.HostId)))
                    {
                        c.IsSelected = true;
                        c.UpdateStatus();
                    }
                    c.DrawInCanvas();
                    _curCircles.Add(c);
                }
            }
        }

        private double GetPreviewDiameter(RebarModel rebar)
        {
            var d = Math.Max(BeamDotMinDiameter, Math.Min(BeamDotMaxDiameter, (rebar?.Diameter ?? 0) * _pixelsPerMm));
            return Math.Max(0, Math.Min(_maxRebarDiameterInCanvas, d));
        }

        private static void DrawRebarCircle(Canvas canvas, wd.Point center, double diameter)
        {
            new InstanceInCanvasCircel(
                canvas,
                BeamRebarDot,
                center,
                diameter).DrawInCanvas();
        }

        private static void DrawDimensionsAndInfo(
            Canvas canvas, wd.Point center,
            double wPx, double hPx,
            double width, double height,
            BeamRebarSectionModel section)
        {
            var textBrush = new wm.SolidColorBrush(wm.Color.FromRgb(60, 60, 60));
            var infoBrush = new wm.SolidColorBrush(wm.Color.FromRgb(40, 80, 150));

            var textWidth = new TextBlock
            {
                Text = $"b = {width:0}",
                FontSize = 11,
                FontWeight = wd.FontWeights.SemiBold,
                Foreground = textBrush
            };
            textWidth.Measure(new wd.Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(textWidth, center.X - textWidth.DesiredSize.Width / 2);
            Canvas.SetTop(textWidth, center.Y - hPx / 2 - 20);
            canvas.Children.Add(textWidth);

            var textHeight = new TextBlock
            {
                Text = $"h = {height:0}",
                FontSize = 11,
                FontWeight = wd.FontWeights.SemiBold,
                Foreground = textBrush,
                LayoutTransform = new wm.RotateTransform(-90)
            };
            textHeight.Measure(new wd.Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(textHeight, center.X - wPx / 2 - textHeight.DesiredSize.Width - 6);
            Canvas.SetTop(textHeight, center.Y - textHeight.DesiredSize.Height / 2);
            canvas.Children.Add(textHeight);

            if (section == null) return;

            var top = FormatLayers(section.RebarTop1, section.RebarTop2, section.RebarTop3);
            var bot = FormatLayers(section.RebarBot1, section.RebarBot2, section.RebarBot3);
            var st = section.Stirrup;
            var stText = st != null && !string.IsNullOrWhiteSpace(st.Name) ? $"{st.Name}a{st.Spacing}" : "-";
            var side = section.SideBar;
            var sideText = side != null && side.Spacing > 0 ? $"{side.Spacing * 2}{side.Name}" : "-";

            var infoText = new TextBlock
            {
                Text = $"Trên: {top} | Dưới: {bot}\nĐai: {stText} | Hông: {sideText}",
                FontSize = 11,
                Foreground = infoBrush,
                FontWeight = wd.FontWeights.Medium,
                TextAlignment = wd.TextAlignment.Center
            };
            infoText.Measure(new wd.Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(infoText, center.X - infoText.DesiredSize.Width / 2);
            Canvas.SetTop(infoText, center.Y + hPx / 2 + 8);
            canvas.Children.Add(infoText);
        }

        private static string FormatLayers(params MVVM.Models.RebarModel[] layers)
        {
            var parts = layers
                .Where(x => x != null && x.Spacing > 0 && !string.IsNullOrWhiteSpace(x.Name))
                .Select(x => $"{x.Spacing}{x.Name}")
                .ToList();
            return parts.Any() ? string.Join(" + ", parts) : "-";
        }


        private static double MmToPx(double mm) => mm * 4.0;
    }

#if !NET6_0_OR_GREATER
    internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static ReferenceEqualityComparer Instance { get; } = new();
        public new bool Equals(object x, object y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
#endif
}
