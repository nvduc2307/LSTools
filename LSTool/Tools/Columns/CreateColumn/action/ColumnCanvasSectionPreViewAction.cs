using LSTool.Cores.canvas;
using LSTool.Tools.Columns.CreateColumn.model;
using LSTool.Tools.Columns.CreateColumn.types;
using LSTool.Utils;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Controls;
using wd = System.Windows;
using wm = System.Windows.Media;
using ws = System.Windows.Shapes;

namespace LSTool.Tools.Columns.CreateColumn.action
{
    /// <summary>
    /// Vẽ trực quan mặt cắt ngang cột lên Canvas trong công cụ CreateColumn.
    /// Hiển thị tiết diện bê tông cột, thép đai bao ngoài, thép dọc phương X, Y và đai phụ (ties/C-tie).
    /// Hỗ trợ chọn thanh thép để tạo đai phụ (_CreateTeiCommand) và xóa đai phụ bằng chuột phải.
    /// </summary>
    public class ColumnCanvasSectionPreViewAction
    {
        private readonly Canvas _canvas;
        private ColumnConcreteModel _columnConcreteModel;
        private List<ColumnConcreteModel> _columnConcreteModels;

        private wd.Point _canvasCenter;
        private wd.Vector _canvasVTX;
        private wd.Vector _canvasVTY;
        private double _canvasHeight;
        private double _canvasWidth;
        private double _scale;
        private double _maxRebarDiameterInCanvas;
        private const double PadSide = 65.0;
        private const double PadTop = 26.0;
        private const double PadBottom = 44.0;

        public List<InstanceInCanvasCircel> RebarSelected { get; set; }

        public ColumnCanvasSectionPreViewAction(Canvas canvas)
        {
            RebarSelected = new List<InstanceInCanvasCircel>();
            _canvas = canvas;
            if (_canvas != null)
            {
                _canvas.SizeChanged += (s, e) => Redraw();
            }
        }

        public void DrawSection(
            List<ColumnConcreteModel> columnConcreteModels,
            ColumnConcreteModel columnConcreteModel)
        {
            _columnConcreteModels = columnConcreteModels;
            DrawSection(columnConcreteModel);
        }

        /// <summary>
        /// Hiển thị mặt cắt cột từ model. Tự động subscribe PropertyChanged để cập nhật khi setting đổi.
        /// </summary>
        public void DrawSection(ColumnConcreteModel model)
        {
            if (_columnConcreteModel != null)
            {
                _columnConcreteModel.PropertyChanged -= OnModelPropertyChanged;
            }

            if (!ReferenceEquals(_columnConcreteModel, model)) RebarSelected.Clear();
            _columnConcreteModel = model;

            if (_columnConcreteModel != null)
            {
                _columnConcreteModel.PropertyChanged += OnModelPropertyChanged;
            }

            Redraw();
        }

        private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ColumnConcreteModel.SpacingDX)
                or nameof(ColumnConcreteModel.SpacingDY)
                or nameof(ColumnConcreteModel.DiameterDX)
                or nameof(ColumnConcreteModel.DiameterDY)
                or nameof(ColumnConcreteModel.DiameterST)
                or nameof(ColumnConcreteModel.Width)
                or nameof(ColumnConcreteModel.Height)
                or nameof(ColumnConcreteModel.Cover))
            {
                Redraw();
            }
        }

        /// <summary>
        /// Vẽ lại toàn bộ mặt cắt cột trên canvas bao gồm tiết diện bê tông, đai chính, thép chủ và đai phụ.
        /// </summary>
        public void Redraw()
        {
            if (_canvas == null) return;
            var selected = new HashSet<(int Id, int Host)>(RebarSelected.Select(x => (x.Id, x.HostId)));
            _canvas.Children.Clear();

            if (_columnConcreteModel == null) return;

            GetCanvasInfo(_canvas,
                out _canvasCenter,
                out _canvasVTX,
                out _canvasVTY,
                out _canvasHeight,
                out _canvasWidth);

            if (_canvasWidth <= 10 || _canvasHeight <= 10) return;

            var height = _columnConcreteModel.Height;
            var width = _columnConcreteModel.Width;
            if (width <= 0 || height <= 0) return;

            var cover = _columnConcreteModel.Cover > 0 ? _columnConcreteModel.Cover : 30.0;
            var dx = int.Parse(Math.Round(_columnConcreteModel.SpacingDX, 0).ToString());
            var dy = int.Parse(Math.Round(_columnConcreteModel.SpacingDY, 0).ToString());

            var qtyMaxX = _columnConcreteModels != null && _columnConcreteModels.Any()
                ? int.Parse(Math.Round(_columnConcreteModels.Max(x => x.SpacingDX), 0).ToString())
                : dx;
            var qtyMaxY = _columnConcreteModels != null && _columnConcreteModels.Any()
                ? int.Parse(Math.Round(_columnConcreteModels.Max(x => x.SpacingDY), 0).ToString())
                : dy;

            qtyMaxX = Math.Max(2, Math.Max(dx, qtyMaxX));
            qtyMaxY = Math.Max(2, Math.Max(dy, qtyMaxY));

            // 1. Tiết diện bê tông
            DrawSectionConcrete(height, width);

            // 2. Thép đai chính bao ngoài
            DrawSectionStirrupMain(height, width, cover);

            // 3. Bố trí thép chủ & gắn tương tác chuột chọn tạo đai
            var rbs = DrawRebarMain(height, width, cover, dx, dy, qtyMaxX, qtyMaxY);

            // Resize replaces the visual instances; keep selection on the same bars.
            RebarSelected = rbs.Where(x => selected.Contains((x.Id, x.HostId))).ToList();
            foreach (var bar in RebarSelected)
            {
                bar.IsSelected = true;
                bar.UpdateStatus();
            }

            // 4. Vẽ các đai phụ (ties) đã lưu
            UpdateTies(_columnConcreteModel, rbs);

            // 5. Kích thước & thông tin cốt thép
            DrawDimensionsAndInfo(_canvas, _canvasCenter, width, height, _columnConcreteModel);
        }

        #region Create / Delete / Update Ties

        private static int InvertFace(int faceId)
        {
            if (faceId == (int)ColumnFaceType.Top) return (int)ColumnFaceType.Bottom;
            if (faceId == (int)ColumnFaceType.Bottom) return (int)ColumnFaceType.Top;
            return faceId;
        }

        /// <summary>
        /// Tạo đai phụ từ các thanh thép đang được chọn trên Canvas.
        /// </summary>
        public void CreateTies(ColumnConcreteModel columnConcreteModel, bool isAddData = true)
        {
            if (columnConcreteModel == null) return;
            try
            {
                var qty = RebarSelected.Count;
                if (qty < 2)
                    throw new Exception("Vui lòng chọn ít nhất 2 thanh thép để tạo đai phụ");
                if (RebarSelected.GroupBy(x => x.HostId).Any(x => x.Count() > 2))
                    throw new Exception("Số điểm của đai phụ trên 1 mặt phẳng không được quá 2 điểm");

                if (columnConcreteModel.Ties == null)
                    columnConcreteModel.Ties = new List<List<ColumnStirrupPositionModel>>();

                if (isAddData)
                {
                    columnConcreteModel.Ties.Add(
                        RebarSelected
                        .Select(x => new ColumnStirrupPositionModel() { Face = InvertFace(x.HostId), Index = x.Id })
                        .ToList());
                }

                // Reset trạng thái chọn
                foreach (var item in RebarSelected)
                {
                    item.IsSelected = false;
                    item.UpdateStatus();
                }
                RebarSelected = new List<InstanceInCanvasCircel>();

                Redraw();
            }
            catch (Exception ex)
            {
                IO.ShowWarning(ex.Message);
                foreach (var item in RebarSelected)
                {
                    item.IsSelected = false;
                    item.UpdateStatus();
                }
                RebarSelected = new List<InstanceInCanvasCircel>();
            }
        }

        /// <summary>
        /// Vẽ toàn bộ đai phụ đã cấu hình trong Ties của cột lên Canvas.
        /// </summary>
        public void UpdateTies(ColumnConcreteModel columnConcreteModel, List<InstanceInCanvasCircel> rbs)
        {
            if (columnConcreteModel?.Ties == null || !columnConcreteModel.Ties.Any()) return;
            foreach (var poss in columnConcreteModel.Ties)
            {
                var rebars = new List<InstanceInCanvasCircel>();
                foreach (var pos in poss)
                {
                    var rb = rbs.FirstOrDefault(x => x.Id == pos.Index && x.HostId == InvertFace(pos.Face));
                    if (rb == null) continue;
                    rebars.Add(rb);
                }
                var qty = rebars.Count;
                if (qty < 2) continue;

                DrawTieInCanvas(rebars);
            }
        }

        private void DrawTieInCanvas(List<InstanceInCanvasCircel> rebars)
        {
            if (rebars == null || rebars.Count < 2) return;

            if (rebars.Count == 2)
            {
                DrawTwoPointTieInCanvas(rebars);
            }
            else
            {
                DrawPolygonTieInCanvas(rebars);
            }
        }

        /// <summary>
        /// Vẽ đai 2 điểm (đai móc / C-tie) có 2 đầu móc ôm lấy thép chủ.
        /// </summary>
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
            var diameter = rebars.Max(rb => rb.Diameter);
            var hookRadius = (diameter + options.Thickness) / 2 + options.Thickness;
            var hookLength = diameter;
            var firstTangent = firstCenter + normal * hookRadius;
            var secondTangent = secondCenter + normal * hookRadius;
            var firstHookEnd = firstCenter - normal * hookRadius;
            var secondHookEnd = secondCenter - normal * hookRadius;
            var firstHookTip = firstHookEnd + tieDirection * hookLength;
            var secondHookTip = secondHookEnd - tieDirection * hookLength;
            var hookSweepDirection =
                tieDirection.X * normal.Y - tieDirection.Y * normal.X < 0
                ? wm.SweepDirection.Clockwise
                : wm.SweepDirection.Counterclockwise;

            var figure = new wm.PathFigure
            {
                StartPoint = firstHookTip
            };
            figure.Segments.Add(new wm.LineSegment(firstHookEnd, true));
            figure.Segments.Add(new wm.ArcSegment(
                firstTangent,
                new wd.Size(hookRadius, hookRadius),
                0,
                false,
                hookSweepDirection,
                true));
            figure.Segments.Add(new wm.LineSegment(secondTangent, true));
            figure.Segments.Add(new wm.ArcSegment(
                secondHookEnd,
                new wd.Size(hookRadius, hookRadius),
                0,
                false,
                hookSweepDirection,
                true));
            figure.Segments.Add(new wm.LineSegment(secondHookTip, true));

            var tie = new ws.Path
            {
                Data = new wm.PathGeometry(new[] { figure }),
                Stroke = options.ColorBrush,
                StrokeThickness = options.Thickness,
                StrokeDashArray = options.LineStyle,
                StrokeStartLineCap = wm.PenLineCap.Round,
                StrokeEndLineCap = wm.PenLineCap.Round
            };
            _canvas.Children.Add(tie);
        }

        /// <summary>
        /// Vẽ đai phụ khép kín ôm quanh nhiều thanh thép chủ (>= 3 thanh).
        /// Các góc cong được vẽ chính xác theo bán kính của thanh thép chủ, tiếp xúc mượt mà với các đoạn thẳng nối.
        /// </summary>
        private void DrawPolygonTieInCanvas(List<InstanceInCanvasCircel> rebars)
        {
            if (rebars == null || rebars.Count < 3) return;

            // 1. Lấy tọa độ tâm các thanh thép
            var pts = rebars.Select(r => r.Point).ToList();

            // 2. Tính trọng tâm tập điểm
            var cx = pts.Average(p => p.X);
            var cy = pts.Average(p => p.Y);
            var center = new wd.Point(cx, cy);

            // 3. Sắp xếp các điểm theo thứ tự chiều kim đồng hồ quanh trọng tâm
            var sortedPoints = pts
                .OrderBy(p => Math.Atan2(p.Y - cy, p.X - cx))
                .ToList();

            int n = sortedPoints.Count;
            if (n < 3) return;

            var options = OptionStyleInstanceInCanvas.OPTION_REBAR_LINE;
            var t = options.Thickness;
            var rebarRadius = rebars.Max(rb => rb.Diameter) / 2.0;
            // Bán kính cung cong bo ngoài thanh thép (tính tới tim nét vẽ của đai)
            var arcRadius = rebarRadius + t / 2.0 + 0.5;

            // 4. Tính vector pháp tuyến hướng ra ngoài (outward normal) cho từng cạnh (i -> i+1)
            var edgeNormals = new wd.Vector[n];
            for (int i = 0; i < n; i++)
            {
                var pCurr = sortedPoints[i];
                var pNext = sortedPoints[(i + 1) % n];
                var edgeVec = pNext - pCurr;
                if (edgeVec.Length == 0)
                {
                    edgeNormals[i] = new wd.Vector(0, -1);
                    continue;
                }
                edgeVec.Normalize();

                // Vector vuông góc (u_y, -u_x)
                var normal = new wd.Vector(edgeVec.Y, -edgeVec.X);

                // Đảm bảo normal hướng ra ngoài polygon so với vector từ trọng tâm tới trung điểm cạnh
                var mid = new wd.Point(0.5 * (pCurr.X + pNext.X), 0.5 * (pCurr.Y + pNext.Y));
                var outVec = mid - center;
                if (normal * outVec < 0)
                {
                    normal = -normal;
                }
                edgeNormals[i] = normal;
            }

            // 5. Tính các điểm tiếp xúc tangent A_i và B_i tại mỗi đỉnh i:
            // A_i là điểm kết thúc đoạn thẳng cạnh (i-1 -> i)
            // B_i là điểm bắt đầu đoạn thẳng cạnh (i -> i+1)
            var startTangentPoints = new wd.Point[n]; // B_i
            var endTangentPoints = new wd.Point[n];   // A_i
            for (int i = 0; i < n; i++)
            {
                var pCurr = sortedPoints[i];
                var prevNormal = edgeNormals[(i - 1 + n) % n];
                var currNormal = edgeNormals[i];

                endTangentPoints[i] = new wd.Point(
                    pCurr.X + prevNormal.X * arcRadius,
                    pCurr.Y + prevNormal.Y * arcRadius);

                startTangentPoints[i] = new wd.Point(
                    pCurr.X + currNormal.X * arcRadius,
                    pCurr.Y + currNormal.Y * arcRadius);
            }

            // 6. Xây dựng PathFigure khép kín:
            // B_0 -> Line tới A_1 -> Arc quanh đỉnh 1 tới B_1 -> Line tới A_2 -> Arc tới B_2 -> ... -> Line tới A_0 -> Arc tới B_0
            var figure = new wm.PathFigure
            {
                StartPoint = startTangentPoints[0],
                IsClosed = true
            };

            for (int i = 0; i < n; i++)
            {
                int nextIdx = (i + 1) % n;

                // Đoạn thẳng từ B_i đến A_{nextIdx}
                figure.Segments.Add(new wm.LineSegment(endTangentPoints[nextIdx], true));

                // Cung tròn bo quanh thanh thép tại đỉnh nextIdx từ A_{nextIdx} đến B_{nextIdx}
                figure.Segments.Add(new wm.ArcSegment(
                    startTangentPoints[nextIdx],
                    new wd.Size(arcRadius, arcRadius),
                    0,
                    false, // isLargeArc: luôn < 180 độ
                    wm.SweepDirection.Clockwise,
                    true));
            }

            var tie = new ws.Path
            {
                Data = new wm.PathGeometry(new[] { figure }),
                Stroke = options.ColorBrush,
                StrokeThickness = options.Thickness,
                StrokeDashArray = options.LineStyle,
                StrokeStartLineCap = wm.PenLineCap.Round,
                StrokeEndLineCap = wm.PenLineCap.Round,
                StrokeLineJoin = wm.PenLineJoin.Round
            };
            _canvas.Children.Add(tie);
        }

        #endregion

        #region User Interaction Handlers

        private void _RebarClickAction(InstanceInCanvasCircel circel)
        {
            circel.IsSelected = !circel.IsSelected;
            circel.UpdateStatus();
            if (circel.IsSelected)
            {
                if (!RebarSelected.Any(x => x.Id == circel.Id && x.HostId == circel.HostId))
                    RebarSelected.Add(circel);
            }
            else
            {
                var circleTar = RebarSelected.FirstOrDefault(x => x.Id == circel.Id && x.HostId == circel.HostId);
                if (circleTar != null)
                    RebarSelected.Remove(circleTar);
            }
        }

        private void _RClickAction(InstanceInCanvasCircel circel)
        {
            if (_columnConcreteModel?.Ties == null) return;
            var teisNew = new List<List<ColumnStirrupPositionModel>>();
            foreach (var item in _columnConcreteModel.Ties)
            {
                if (item.Any(x => x.Index == circel.Id && x.Face == InvertFace(circel.HostId))) continue;
                teisNew.Add(item);
            }
            _columnConcreteModel.Ties = teisNew;
            Redraw();
        }

        #endregion

        #region Section & Rebar Geometry Drawing

        private void DrawSectionConcrete(double height, double width)
        {
            var areaWidth = Math.Max(1, _canvasWidth - 2 * PadSide);
            var areaHeight = Math.Max(1, _canvasHeight - PadTop - PadBottom);
            _scale = 0.9 * Math.Min(areaWidth / MMToPixel(width), areaHeight / MMToPixel(height));
            _canvasCenter = new wd.Point(_canvasWidth / 2, PadTop + areaHeight / 2);
            var heightInCanvas = MMToPixel(height) * _scale;
            var widthInCanvas = MMToPixel(width) * _scale;

            var shape = new List<wd.Point>()
            {
                _canvasCenter - _canvasVTY * heightInCanvas/2 - _canvasVTX * widthInCanvas/2,
                _canvasCenter - _canvasVTY * heightInCanvas/2 + _canvasVTX * widthInCanvas/2,
                _canvasCenter + _canvasVTY * heightInCanvas/2 + _canvasVTX * widthInCanvas/2,
                _canvasCenter + _canvasVTY * heightInCanvas/2 - _canvasVTX * widthInCanvas/2,
            };
            var rec = new InstanceInCanvasPolygon(
                _canvas,
                OptionStyleInstanceInCanvas.OPTION_CONCRETE_STRUCTURE,
                shape);
            rec.DrawInCanvas();
        }

        private void DrawSectionStirrupMain(double height, double width, double cover)
        {
            var heightInCanvas = MMToPixel(Math.Max(0, height - 2 * cover)) * _scale;
            var widthInCanvas = MMToPixel(Math.Max(0, width - 2 * cover)) * _scale;
            var cornerRadius = Math.Min(
                RebarSectionPreviewStyle.MaximumDiameter,
                Math.Min(heightInCanvas, widthInCanvas) / 2);
            var options = OptionStyleInstanceInCanvas.OPTION_REBAR_LINE;
            var stirrup = new ws.Rectangle
            {
                Width = widthInCanvas,
                Height = heightInCanvas,
                RadiusX = cornerRadius,
                RadiusY = cornerRadius,
                Stroke = options.ColorBrush,
                StrokeThickness = options.Thickness,
                StrokeDashArray = options.LineStyle,
                Fill = wm.Brushes.Transparent
            };
            Canvas.SetLeft(stirrup, _canvasCenter.X - widthInCanvas / 2);
            Canvas.SetTop(stirrup, _canvasCenter.Y - heightInCanvas / 2);
            _canvas.Children.Add(stirrup);
        }

        private List<InstanceInCanvasCircel> DrawRebarMain(
            double height, double width, double cover,
            int dx, int dy, int qtyMaxX, int qtyMaxY)
        {
            var result = new List<InstanceInCanvasCircel>();
            var stirrupH = MMToPixel(Math.Max(0, height - 2 * cover)) * _scale;
            var stirrupW = MMToPixel(Math.Max(0, width - 2 * cover)) * _scale;

            var opts = OptionStyleInstanceInCanvas.OPTION_REBAR_LINE;
            var edgeGap = opts.Thickness / 2.0 + 1.0;
            _maxRebarDiameterInCanvas = Math.Min(
                RebarSectionPreviewStyle.GetDiameterLimit(stirrupW - 2 * edgeGap, qtyMaxX),
                RebarSectionPreviewStyle.GetDiameterLimit(stirrupH - 2 * edgeGap, qtyMaxY));
            if (_maxRebarDiameterInCanvas <= 0) return result;
            var diameterX = GetPreviewDiameter(_columnConcreteModel.DiameterDX);
            var diameterY = GetPreviewDiameter(_columnConcreteModel.DiameterDY);
            // Khoảng cách từ tim đai tới tim thép chủ để thép chủ ôm sát mép trong đai đai (khoảng hở nhẹ ~1px)
            var offset = edgeGap + Math.Max(diameterX, diameterY) / 2.0;

            var rebarHalfW = Math.Max(0, stirrupW / 2.0 - offset);
            var rebarHalfH = Math.Max(0, stirrupH / 2.0 - offset);

            var p1 = new wd.Point(_canvasCenter.X - rebarHalfW, _canvasCenter.Y - rebarHalfH); // Top-Left
            var p2 = new wd.Point(_canvasCenter.X + rebarHalfW, _canvasCenter.Y - rebarHalfH); // Top-Right
            var p3 = new wd.Point(_canvasCenter.X + rebarHalfW, _canvasCenter.Y + rebarHalfH); // Bottom-Right
            var p4 = new wd.Point(_canvasCenter.X - rebarHalfW, _canvasCenter.Y + rebarHalfH); // Bottom-Left

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
                var list = new List<InstanceInCanvasCircel>();
                var rebarPoss = SolvePositionInstallRebar(pStart, pEnd, qty, qtyMax);
                foreach (var rebarPos in rebarPoss)
                {
                    var index = rebarPoss.IndexOf(rebarPos);
                    if (ignoreStartEnd && (index == 0 || index == qty - 1)) continue;

                    var diameter = faceId == (int)ColumnFaceType.Top || faceId == (int)ColumnFaceType.Bottom
                        ? diameterX : diameterY;
                    var c = new InstanceInCanvasCircel(_canvas, OptionStyleInstanceInCanvas.OPTION_REBAR, rebarPos.Position, diameter)
                    {
                        Id = rebarPos.Index,
                        HostId = faceId
                    };

                    // Kiểm tra xem rebar này có đang trong danh sách chọn không
                    if (RebarSelected.Any(x => x.Id == c.Id && x.HostId == c.HostId))
                    {
                        c.IsSelected = true;
                        c.UpdateStatus();
                    }

                    // Chỉ các thanh ở giữa (không phải góc) mới được chọn tạo đai phụ
                    if (index != 0 && index != qty - 1)
                    {
                        c.LClickAction = _RebarClickAction;
                        c.RClickAction = _RClickAction;
                    }

                    c.DrawInCanvas();
                    list.Add(c);
                }
                return list;
            }
        }

        private double GetPreviewDiameter(string name)
        {
            double.TryParse(name?.TrimStart('D', 'd'), NumberStyles.Float,
                CultureInfo.InvariantCulture, out var diameterMm);
            return RebarSectionPreviewStyle.GetDiameter(diameterMm, MMToPixel(1) * _scale,
                _maxRebarDiameterInCanvas);
        }

        private List<ColumnRebarPositionInCanvasModel> SolvePositionInstallRebar(
            wd.Point start, wd.Point end, int qty, int maxQty)
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
            canvasCenter = new wd.Point(canvasWidth / 2, canvasHeight / 2 - 10);
            canvasVTX = new wd.Vector(1, 0);
            canvasVTY = new wd.Vector(0, 1);
        }

        private static double MMToPixel(double distance) => distance * 4.0;

        /// <summary>
        /// Vẽ kích thước mặt cắt b x h và thông tin cốt thép.
        /// </summary>
        private void DrawDimensionsAndInfo(
            Canvas canvas, wd.Point center,
            double width, double height,
            ColumnConcreteModel model)
        {
            var heightInCanvas = MMToPixel(height) * _scale;
            var widthInCanvas = MMToPixel(width) * _scale;
            var textBrush = new wm.SolidColorBrush(wm.Color.FromRgb(60, 60, 60));

            // Kích thước chiều rộng b (trên đỉnh cột)
            var textWidth = new TextBlock
            {
                Text = $"b = {width:0}",
                FontSize = 11,
                FontWeight = wd.FontWeights.SemiBold,
                Foreground = textBrush
            };
            textWidth.Measure(new wd.Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(textWidth, center.X - textWidth.DesiredSize.Width / 2);
            Canvas.SetTop(textWidth, center.Y - heightInCanvas / 2 - 20);
            canvas.Children.Add(textWidth);

            // Kích thước chiều cao h (bên trái cột)
            var textHeight = new TextBlock
            {
                Text = $"h = {height:0}",
                FontSize = 11,
                FontWeight = wd.FontWeights.SemiBold,
                Foreground = textBrush
            };
            textHeight.Measure(new wd.Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(textHeight, center.X - widthInCanvas / 2 - textHeight.DesiredSize.Width - 10);
            Canvas.SetTop(textHeight, center.Y - textHeight.DesiredSize.Height / 2);
            canvas.Children.Add(textHeight);

            // Dòng thông tin cốt thép bên dưới
            var qtyX = (int)model.SpacingDX;
            var qtyY = (int)model.SpacingDY;
            var tieCount = model.Ties?.Count ?? 0;
            var infoText = new TextBlock
            {
                Text = $"X: {qtyX}-{model.DiameterDX} | Y: {qtyY}-{model.DiameterDY} | Đai: {model.DiameterST} | Đai phụ: {tieCount}",
                FontSize = 11,
                Foreground = new wm.SolidColorBrush(wm.Color.FromRgb(40, 80, 150)),
                FontWeight = wd.FontWeights.Medium
            };
            infoText.Measure(new wd.Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(infoText, center.X - infoText.DesiredSize.Width / 2);
            Canvas.SetTop(infoText, center.Y + heightInCanvas / 2 + 18);
            canvas.Children.Add(infoText);
        }

        #endregion
    }
}
