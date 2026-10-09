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
    /// <summary>
    /// Vẽ sơ đồ mặt cắt ngang tiết diện dầm lên 3 Canvas (đầu dầm, giữa dầm, cuối dầm).
    /// Tham khảo CanvasSectionPreViewAction của cột.
    /// </summary>
    public class BeamCanvasSectionPreViewAction
    {
        // ── canvases ───────────────────────────────────────────────────────────
        private readonly Canvas _canvasStart;
        private readonly Canvas _canvasMid;
        private readonly Canvas _canvasEnd;

        // ── state ──────────────────────────────────────────────────────────────
        /// <summary>Model dầm đang được hiển thị — dùng để redraw khi rebar thay đổi.</summary>
        private BeamRebarModel _currentModel;
        /// <summary>Danh sách tất cả các dầm trong phiên làm việc — dùng để tính scale thống nhất.</summary>
        private List<BeamRebarModel> _allModels;

        /// <summary>Số vị trí trên lưới chia thép (số thanh lớn nhất) – giống lúc mô hình.</summary>
        private int _gridQty = 2;
        /// <summary>Các thanh đang được pick: (Index, HostId = layer + 10 * section).</summary>
        private readonly List<(int Id, int Host)> _picked = new List<(int, int)>();
        /// <summary>Vòng tròn thép của canvas đang vẽ (để vẽ đai phụ).</summary>
        private List<InstanceInCanvasCircel> _curCircles = new List<InstanceInCanvasCircel>();
        private wd.Point _curCenter;

        // ── layout constants ───────────────────────────────────────────────────
        /// <summary>Tỉ lệ phần trăm canvas dùng để vẽ tiết diện.</summary>
        private const double Ratio = 0.9;
        /// <summary>Giới hạn kích thước ký hiệu; đường kính thực được đổi theo scale mặt cắt.</summary>
        private double _pixelsPerMm;
        private double _maxRebarDiameterInCanvas;
        /// <summary>Khoảng cách giữa 2 lớp thép (px).</summary>
        private const double LayerGap = 2.0;
        /// <summary>Lề chừa cho text kích thước / thông tin (px).</summary>
        private const int SideLeftCode = 6;
        private const int SideRightCode = 7;
        /// <summary>BeamTieModel.Layer của đai ngang nối 2 thanh sidebar (Index = hàng).</summary>
        public const int SideTieLayer = 6;
        private const double PadSide = 24.0;   // trái/phải: "h = 700" (text xoay đứng)
        private const double PadTop = 26.0;    // trên: "b = 300"
        private const double PadBottom = 44.0; // dưới: 2 dòng thông tin thép

        // ──────────────────────────────────────────────────────────────────────
        public BeamCanvasSectionPreViewAction(Canvas canvasStart, Canvas canvasMid, Canvas canvasEnd)
        {
            _canvasStart = canvasStart;
            _canvasMid   = canvasMid;
            _canvasEnd   = canvasEnd;
            foreach (var canvas in new[] { _canvasStart, _canvasMid, _canvasEnd })
                canvas.SizeChanged += (sender, args) => Redraw();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Cập nhật danh sách tất cả các dầm đang thao tác để xác định scale tham chiếu thống nhất.
        /// </summary>
        public void SetAllModels(IEnumerable<BeamRebarModel> models)
        {
            _allModels = models?.Where(x => x != null && x.Width > 0 && x.Height > 0).ToList();
        }

        /// <summary>
        /// Vẽ lại cả 3 mặt cắt từ model dầm được chọn.
        /// Gọi sau khi canvas đã hoàn tất layout (ActualWidth/Height != 0).
        /// Tự động subscribe PropertyChanged của các RebarModel để cập nhật canvas khi qty/diameter thay đổi.
        /// </summary>
        public void DrawSection(BeamRebarModel model, IEnumerable<BeamRebarModel> allModels = null)
        {
            if (model == null) return;
            if (allModels != null)
            {
                SetAllModels(allModels);
            }

            // unsubscribe model cũ trước để tránh memory leak / double-draw
            UnsubscribeRebarChanges(_currentModel);
            if (!ReferenceEquals(_currentModel, model)) _picked.Clear();
            _currentModel = model;
            SubscribeRebarChanges(_currentModel);

            Redraw();
        }

        // ── Subscribe / Unsubscribe rebar PropertyChanged ──────────────────────

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

        /// <summary>
        /// Lấy toàn bộ RebarModel từ 3 section (bỏ qua null).
        /// Dùng Distinct theo reference để tránh subscribe 2 lần cho Top1/Bot1 shared.
        /// </summary>
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
            // redraw khi Spacing (qty), Diameter hoặc tên đường kính thay đổi
            if (e.PropertyName is nameof(RebarModel.Spacing) or nameof(RebarModel.Diameter) or nameof(RebarModel.Name))
                Redraw();
        }

        /// <summary>Vẽ lại 3 canvas từ _currentModel.</summary>
        public void Redraw()
        {
            if (_currentModel == null) return;
            _gridQty = BeamRebarUtils.GetGridQty(
                (IEnumerable<BeamRebarModel>)_allModels ?? new[] { _currentModel });
            DrawOnCanvas(_canvasStart, _currentModel, _currentModel.SectionStart, 0);
            DrawOnCanvas(_canvasMid,   _currentModel, _currentModel.SectionMid, 1);
            DrawOnCanvas(_canvasEnd,   _currentModel, _currentModel.SectionEnd, 2);
        }

        // ── Core draw for one canvas ───────────────────────────────────────────

        private void DrawOnCanvas(Canvas canvas, BeamRebarModel beam, BeamRebarSectionModel section, int secIdx)
        {
            if (canvas == null) return;
            canvas.Children.Clear();
            _curCircles = new List<InstanceInCanvasCircel>();

            var cw = canvas.ActualWidth;
            var ch = canvas.ActualHeight;
            if (cw <= 0 || ch <= 0) return;

            // kích thước tiết diện thực (mm)
            // GetBeamDimensions: Width đo theo VTY (ngang ←→), Height đo theo VTZ (đứng ↑)
            var width  = beam.Width;  // VTY = chiều ngang tiết diện → wPx
            var height = beam.Height; // VTZ = chiều đứng tiết diện  → hPx
            var cover  = beam.Cover / 2;
            if (width <= 0 || height <= 0) return;

            // Xác định kích thước tham chiếu lớn nhất giữa tất cả các dầm để thống nhất scale (ưu tiên hình to hơn)
            var validModels = _allModels?.Where(x => x != null && x.Width > 0 && x.Height > 0).ToList();
            var refWidth  = (validModels != null && validModels.Any()) ? Math.Max(width, validModels.Max(x => x.Width)) : width;
            var refHeight = (validModels != null && validModels.Any()) ? Math.Max(height, validModels.Max(x => x.Height)) : height;

            if (refWidth <= 0) refWidth = width;
            if (refHeight <= 0) refHeight = height;

            // vùng vẽ tiết diện: chừa lề cho text kích thước & thông tin thép
            var areaW = Math.Max(10, cw - 2 * PadSide);
            var areaH = Math.Max(10, ch - PadTop - PadBottom);
            var center = new wd.Point(cw / 2, PadTop + areaH / 2);

            // tính scale thống nhất: vừa vặn hình lớn nhất trong vùng vẽ theo Ratio, các hình nhỏ hơn dùng chung scale
            var scale = Ratio * Math.Min(areaW / MmToPx(refWidth), areaH / MmToPx(refHeight));

            var wPx = MmToPx(width)  * scale;
            var hPx = MmToPx(height) * scale;

            // ── 1. bê tông ──────────────────────────────────────────────────
            DrawConcrete(canvas, center, wPx, hPx);

            // ── 2. đai ──────────────────────────────────────────────────────
            var coverPx      = MmToPx(cover) * scale;
            DrawStirrup(canvas, center, wPx, hPx, coverPx);

            // ── kích thước & thông tin cốt thép (giống tool cột) ─────────────
            DrawDimensionsAndInfo(canvas, center, wPx, hPx, width, height, section);

            if (section == null) return;

            // biên trong của đai (tính từ tâm canvas, px)
            // cộng thêm: nửa bề dày nét đai + khoảng hở để thép không chồng lên đường đai
            var stirrupHalfThickness = OptionStyleInstanceInCanvas.OPTION_REBAR_LINE.Thickness / 2;
            const double rebarGap = 1.5;  // px khoảng thở giữa đai và thép
            var stirrupOffset = stirrupHalfThickness + rebarGap;
            var innerHalfW = Math.Max(0, wPx / 2 - coverPx - stirrupOffset);
            var innerHalfH = Math.Max(0, hPx / 2 - coverPx - stirrupOffset);

            _pixelsPerMm = MmToPx(1) * scale;
            // Dành ít nhất 25% bước lưới làm khoảng hở giữa các chấm, kể cả lưới dày.
            var horizontalLimit = RebarSectionPreviewStyle.GetDiameterLimit(2 * innerHalfW, _gridQty);
            var layerCount = 2 + new[] { section.RebarTop2, section.RebarTop3,
                section.RebarBot2, section.RebarBot3 }.Count(rb => rb != null && rb.Spacing > 0);
            var verticalLimit = (2 * innerHalfH - (layerCount - 1) * LayerGap) / layerCount;
            _maxRebarDiameterInCanvas = Math.Min(RebarSectionPreviewStyle.MaximumDiameter,
                Math.Min(horizontalLimit, verticalLimit));
            if (_maxRebarDiameterInCanvas <= 0) return;


            _curCenter = center;
            const int S = 10;
            // ── 3. thép Top (1→2→3 từ mép trên xuống) ───────────────────────
            double topRowY  = center.Y - innerHalfH;  // mép trên trong đai
            topRowY = DrawRebarRow(canvas, center, innerHalfW, topRowY, section.RebarTop1, true, BeamRebarLayerType.Top1, secIdx * S);
            topRowY = DrawRebarRow(canvas, center, innerHalfW, topRowY, section.RebarTop2, true, BeamRebarLayerType.Top2, secIdx * S);
                      DrawRebarRow(canvas, center, innerHalfW, topRowY, section.RebarTop3, true, BeamRebarLayerType.Top3, secIdx * S);

            // ── 4. thép Bot (1→2→3 từ mép dưới lên) ────────────────────────
            double botRowY  = center.Y + innerHalfH;  // mép dưới trong đai
            botRowY = DrawRebarRow(canvas, center, innerHalfW, botRowY, section.RebarBot1, false, BeamRebarLayerType.Bot1, secIdx * S);
            botRowY = DrawRebarRow(canvas, center, innerHalfW, botRowY, section.RebarBot2, false, BeamRebarLayerType.Bot2, secIdx * S);
                      DrawRebarRow(canvas, center, innerHalfW, botRowY, section.RebarBot3, false, BeamRebarLayerType.Bot3, secIdx * S);

            // ── 5. SideBar ────────────────────────────────────────────────────
            DrawSideBars(canvas, center, innerHalfW, innerHalfH, section.SideBar, secIdx * S);

            // ── 6. đai phụ (vẽ sau cùng để có đủ thép chủ + sidebar) ─────────
            DrawTies(canvas, beam, secIdx);
        }

        // ── Draw helpers ───────────────────────────────────────────────────────

        /// <summary>Vẽ hình chữ nhật bê tông (có fill).</summary>
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

        /// <summary>Vẽ đai chính (rectangle bo góc) bên trong bê tông.</summary>
        private static void DrawStirrup(Canvas canvas, wd.Point center,
            double wPx, double hPx, double coverPx)
        {
            var inW = wPx - 2 * coverPx;
            var inH = hPx - 2 * coverPx;
            if (inW <= 0 || inH <= 0) return;

            var cornerR = Math.Min(RebarSectionPreviewStyle.MaximumDiameter, Math.Min(inW, inH) / 4);
            var opts    = OptionStyleInstanceInCanvas.OPTION_REBAR_LINE;
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

        /// <summary>
        /// Vẽ 1 hàng thép theo lưới chia (giống lúc mô hình: thanh đầu/cuối ở góc, đối xứng).
        /// <paramref name="rowY"/>: mép ngoài (top nếu goingDown, bottom nếu !goingDown).
        /// Trả về Y kề tiếp cho hàng thép kế.
        /// </summary>
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
                // Top1/Bot1: chỉ thanh bên trong; Top2/3, Bot2/3: chỉ 2 thanh ngoài cùng (lớp >= 2 thanh)
                var pickable = isFirstLayer ? !isOuter : (isOuter && qty >= 2);

                var c = new InstanceInCanvasCircel(
                    canvas, OptionStyleInstanceInCanvas.OPTION_REBAR,
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

        // ── Pick & đai phụ ─────────────────────────────────────────────────────

        private void _RebarClickAction(InstanceInCanvasCircel circel)
        {
            var key = (circel.Id, circel.HostId);
            if (!_picked.Remove(key)) _picked.Add(key);
            Redraw();
        }

        /// <summary>Chuột phải vào thanh thép: xóa các đai phụ đi qua thanh đó.</summary>
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

        /// <summary>
        /// Tạo đai phụ từ các thanh đang pick trên canvas. Hình dạng áp dụng cho cả 3 mặt cắt của dầm.
        /// Trả về true nếu tạo được.
        /// </summary>
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
                        // đai ngang qua thép sidebar: thanh trái + thanh phải cùng hàng (pick 1 thanh cũng được)
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
                    // phía ngoài = phía mép gần nhất (giữa thì bên phải)
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
                            DrawTwoPointTie(canvas, l, r, new wd.Vector(0, -1)); // đai đi phía trên (+VTZ)
                        continue;
                    }
                    var layer = (BeamRebarLayerType)t.Layer;
                    var cs = _curCircles.Where(c => c.HostId % 10 == t.Layer).OrderBy(c => c.Id).ToList();
                    if (cs.Count < 2) continue;
                    // đai ngang đi phía trong (về tâm dầm), móc quay ra ngoài
                    var inward = new wd.Vector(0, BeamRebarUtils.IsTopLayer(layer) ? 1 : -1);
                    DrawTwoPointTie(canvas, cs.First(), cs.Last(), inward);
                }
            }
        }

        /// <summary>Vẽ đai 2 điểm có 2 móc (giống DrawTwoPointTieInCanvas của cột).</summary>
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

            var options = OptionStyleInstanceInCanvas.OPTION_REBAR_LINE;
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

        /// <summary>Vẽ thép sidebar: 2 cột bên trái/phải, chia đều chiều cao.</summary>
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

            // chia đều giữa vùng trong (tránh sát top/bot)
            var usableH = 2 * innerHalfH - 2 * d;
            var spacing  = qty > 1 ? usableH / (qty + 1) : usableH / 2;

            for (int i = 0; i < qty; i++)
            {
                var cy = center.Y - innerHalfH + d + (i + 1) * spacing;
                var row = qty - i; // hàng đếm từ dưới lên (giống lúc dựng)
                // HostId % 10: 6 = sidebar trái, 7 = sidebar phải; Id = hàng
                foreach (var (x, layerCode) in new[] { (leftX, SideLeftCode), (rightX, SideRightCode) })
                {
                    var c = new InstanceInCanvasCircel(
                        canvas, OptionStyleInstanceInCanvas.OPTION_REBAR,
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

        /// <summary>Giữ chấm dễ nhìn nhưng không vượt khoảng trống trên mặt cắt.</summary>
        private double GetPreviewDiameter(RebarModel rebar)
        {
            return RebarSectionPreviewStyle.GetDiameter(rebar?.Diameter ?? 0,
                _pixelsPerMm, _maxRebarDiameterInCanvas);
        }

        /// <summary>Vẽ 1 vòng tròn thép.</summary>
        private static void DrawRebarCircle(Canvas canvas, wd.Point center, double diameter)
        {
            new InstanceInCanvasCircel(
                canvas,
                OptionStyleInstanceInCanvas.OPTION_REBAR,
                center,
                diameter).DrawInCanvas();
        }

        /// <summary>
        /// Vẽ kích thước mặt cắt b x h và thông tin cốt thép (tham khảo ColumnCanvasSectionPreViewAction.DrawDimensionsAndInfo).
        /// b: phía trên tiết diện, h: bên trái tiết diện, thông tin thép: bên dưới.
        /// </summary>
        private static void DrawDimensionsAndInfo(
            Canvas canvas, wd.Point center,
            double wPx, double hPx,
            double width, double height,
            BeamRebarSectionModel section)
        {
            var textBrush = new wm.SolidColorBrush(wm.Color.FromRgb(60, 60, 60));
            var infoBrush = new wm.SolidColorBrush(wm.Color.FromRgb(40, 80, 150));

            // Kích thước chiều rộng b (trên đỉnh dầm)
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

            // Kích thước chiều cao h (bên trái dầm, xoay đứng đọc từ dưới lên)
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

            // Thông tin cốt thép bên dưới (2 dòng)
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

        /// <summary>"3D16 + 2D18" từ các lớp có số lượng &gt; 0.</summary>
        private static string FormatLayers(params MVVM.Models.RebarModel[] layers)
        {
            var parts = layers
                .Where(x => x != null && x.Spacing > 0 && !string.IsNullOrWhiteSpace(x.Name))
                .Select(x => $"{x.Spacing}{x.Name}")
                .ToList();
            return parts.Any() ? string.Join(" + ", parts) : "-";
        }

        // ── Unit conversion ────────────────────────────────────────────────────

        /// <summary>mm → pixel (4 px/mm, nhất quán với CanvasSectionPreViewAction gốc).</summary>
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
