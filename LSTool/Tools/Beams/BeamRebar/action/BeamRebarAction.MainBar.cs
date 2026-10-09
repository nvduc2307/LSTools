using Autodesk.Revit.DB.Structure;
using LSTool.MVVM.Models;
using LSTool.Tools.Beams.BeamRebar.models;
using LSTool.Tools.Beams.BeamRebar.types;
using LSTool.Tools.Beams.BeamRebar.Utils;
using LSTool.Utils;

namespace LSTool.Tools.Beams.BeamRebar.action
{
    /// <summary>
    /// Phần lõi dựng thép chủ dầm (Top1..3, Bot1..3), tham khảo ColumnRebarMainAction.
    ///
    /// Quy ước (hệ tọa độ chung của dãy dầm):
    ///   VTX = dọc trục, VTY = ngang, VTZ = đứng (Z global)
    ///   Width = theo VTY, Height = theo VTZ, Length = chiều dài thông thủy nhịp.
    ///
    /// 1. Lưới chia vị trí (giống SolvePositionInstallRebar của cột):
    ///    GridQty = số thanh lớn nhất trong TẤT CẢ lớp / mặt cắt / nhịp.
    ///    Thanh đầu & cuối luôn ở góc, các thanh còn lại lấy đối xứng từ 2 góc vào,
    ///    số lẻ thì thêm 1 thanh ở giữa → các lớp/nhịp dùng chung Index để sau này tạo đai móc phụ.
    /// 2. Vùng theo chiều dài: lớp 1 chạy suốt nhịp; lớp 2, 3 (z = StressZone, mặc định 0.25):
    ///    Start = 0→zL, Mid = zL/2→(1−z/2)L, End = (1−z)L→L.
    /// 3. Tại gối giữa 2 nhịp: thanh cùng Index chạy liên tục qua gối nếu cùng đường kính,
    ///    chênh cao độ lớp &lt; E0 và lệch ngang &lt; E0. Ngược lại (hoặc Index không có ở nhịp bên kia)
    ///    → neo vào gối: thẳng La nếu đủ, không đủ thì tới mép xa gối rồi bẻ móc 90° (hook B).
    /// </summary>
    public partial class BeamRebarAction
    {
        /// <summary>Tỉ lệ vùng đầu/cuối nhịp = StressZone trong setting (mặc định 0.25 = L/4), kẹp trong (0, 0.4].</summary>
        private double ZoneSupportRatio
        {
            get
            {
                var z = _settingBeamModel?.StressZone ?? 0.25;
                return z > 0 && z <= 0.4 ? z : 0.25;
            }
        }
        private double ZoneMidStartRatio => ZoneSupportRatio / 2;        // Mid: từ L/8 (khi StressZone = 0.25)
        private double ZoneMidEndRatio => 1.0 - ZoneSupportRatio / 2;    //      đến 7L/8
        private const double LayerClearMinMm = 25;      // khe hở tối thiểu giữa 2 lớp

        /// <summary>Các cảnh báo khi dựng thép chủ (không tạo được Rebar Set) – hiển thị 1 lần sau khi dựng xong.</summary>
        private readonly List<string> _mainBarWarnings = new List<string>();

        private sealed class _BarSegment
        {
            public int BeamIndex { get; set; }
            public BeamRebarModel Beam { get; set; }
            public BeamRebarZoneType Zone { get; set; }
            public double XFrom { get; set; }      // ft – tính từ mặt đầu dầm theo VTX
            public double XTo { get; set; }        // ft
            public bool AtStart { get; set; }      // chạm gối đầu nhịp
            public bool AtEnd { get; set; }        // chạm gối cuối nhịp
            public string Name { get; set; }       // tên RebarBarType (D16…)
            public double DiameterMm { get; set; }
            public double Z { get; set; }          // ft – offset lớp theo VTZ so với tâm dầm
            public Dictionary<int, double> Ys { get; set; } = new Dictionary<int, double>(); // Index → offset VTY (ft)
        }

        /// <summary>Hình dạng 1 thanh thép chủ (chưa tạo trong Revit) – dùng để gom thành Rebar Set.</summary>
        private sealed class _BarShape
        {
            public string Name { get; set; }       // tên RebarBarType
            public string HostId { get; set; }     // UniqueId dầm host
            public List<XYZ> Points { get; set; }
            public XYZ Vty { get; set; }           // phương rải thép (ngang tiết diện)
            public double Offset { get; set; }     // vị trí theo VTY (ft) – để sắp xếp
        }

        // ── Entry chung cho từng lớp ──────────────────────────────────────────

        /// <summary>Dựng thép chủ cho 1 lớp trên toàn bộ dãy dầm.</summary>
        private void _createMainBarLayer(BeamRebarLayerType layer)
        {
            var beams = _viewModel.BeamRebarModels?.ToList();
            if (beams == null || !beams.Any()) return;

            var rebarBarTypes = new FilteredElementCollector(_document)
                .WhereElementIsElementType()
                .OfClass(typeof(RebarBarType))
                .Cast<RebarBarType>()
                .Where(x => x.Name.Contains("D"))
                .OrderBy(x => x.Name)
                .ToList();

            var gridQty = _getGridQty(beams);
            foreach (var beam in beams)
            {
                beam.GridQty = gridQty;
                beam.RebarMainPositions ??= new List<BeamRebarPositionModel>();
                beam.RebarMainPositions.RemoveAll(x => x.Layer == layer);
            }

            var segments = _buildSegments(beams, layer, gridQty);
            if (!segments.Any(x => x.Ys.Any())) return;

            // lưu vị trí để dùng cho đai móc phụ
            foreach (var seg in segments)
            {
                foreach (var kv in seg.Ys)
                {
                    seg.Beam.RebarMainPositions.Add(new BeamRebarPositionModel
                    {
                        HostId = seg.Beam.Id,
                        Layer = layer,
                        Zone = seg.Zone,
                        Index = kv.Key,
                        Y = kv.Value,
                        Z = seg.Z,
                        Diameter = seg.DiameterMm,
                    });
                }
            }

            // gối nào bị "cắt" (neo vào cột) – xét theo cả lớp
            var isBreak = new bool[segments.Count];
            for (int k = 0; k < segments.Count - 1; k++)
                isBreak[k] = _isBreakBetween(segments[k], segments[k + 1]);

            bool Connected(int k, int idx) =>
                k >= 0 && k < segments.Count - 1
                && !isBreak[k]
                && segments[k].Ys.ContainsKey(idx)
                && segments[k + 1].Ys.ContainsKey(idx)
                && _canRunThrough(segments[k], segments[k + 1], idx);

            var isTop = _isTopLayer(layer);
            var layerOrder = _layerOrder(layer);

            // 1. Dựng hình dạng tất cả các thanh của lớp
            var shapes = new List<_BarShape>();
            for (int idx = 1; idx <= gridQty; idx++)
            {
                for (int k = 0; k < segments.Count; k++)
                {
                    if (!segments[k].Ys.ContainsKey(idx)) continue;
                    if (Connected(k - 1, idx)) continue; // không phải đầu chuỗi

                    var kEnd = k;
                    while (Connected(kEnd, idx)) kEnd++;

                    try
                    {
                        var shape = _buildMainBarShape(segments, k, kEnd, idx, isTop, layerOrder);
                        if (shape != null) shapes.Add(shape);
                    }
                    catch (Exception ex)
                    {
                        _mainBarWarnings.Add($"{layer} Index {idx}: không dựng được hình thanh ({ex.Message}).");
                    }
                }
            }

            // 2. Gom các thanh giống nhau, cách đều → Rebar Set
            using (var ts = new SubTransaction(_document))
            {
                ts.Start();
                _createBarsAsRebarSets(layer, shapes, rebarBarTypes);
                ts.Commit();
            }
        }

        // ── Lưới chia & phân đoạn ────────────────────────────────────────────

        /// <summary>GridQty = số thanh lớn nhất của mọi lớp, mọi mặt cắt, mọi nhịp (tối thiểu 2).</summary>
        private static int _getGridQty(List<BeamRebarModel> beams) => BeamRebarUtils.GetGridQty(beams);

        /// <summary>
        /// Thanh Index này chạy liền qua gối?
        /// - trùng vị trí ngang (&lt; 1mm): liền thẳng;
        /// - lệch nhưng cùng số thanh và lệch &lt; E0: bẻ xiên trong gối;
        /// - khác số thanh (VD 3 ↔ 4): chỉ thanh trùng vị trí (2 thanh ngoài cùng) liền, thanh trong neo riêng.
        /// </summary>
        private bool _canRunThrough(_BarSegment a, _BarSegment b, int idx)
        {
            var dyMm = Math.Abs(a.Ys[idx] - b.Ys[idx]).ToMillimeters();
            if (dyMm < 1.0) return true;
            if (a.Ys.Count != b.Ys.Count) return false;
            return Math.Round(dyMm, 0) < (_settingBeamModel?.E0 ?? 100);
        }

        private List<_BarSegment> _buildSegments(
            List<BeamRebarModel> beams,
            BeamRebarLayerType layer,
            int gridQty)
        {
            var result = new List<_BarSegment>();
            var isFull = _layerOrder(layer) == 0;

            for (int i = 0; i < beams.Count; i++)
            {
                var beam = beams[i];
                var lenFt = beam.Length.FromMillimeters();

                var zones = isFull
                    ? new[] { (BeamRebarZoneType.Full, beam.SectionStart, 0.0, 1.0) }
                    : new[]
                    {
                        (BeamRebarZoneType.Start, beam.SectionStart, 0.0, ZoneSupportRatio),
                        (BeamRebarZoneType.Mid,   beam.SectionMid,   ZoneMidStartRatio, ZoneMidEndRatio),
                        (BeamRebarZoneType.End,   beam.SectionEnd,   1.0 - ZoneSupportRatio, 1.0),
                    };

                foreach (var (zone, sec, rFrom, rTo) in zones)
                {
                    var seg = new _BarSegment
                    {
                        BeamIndex = i,
                        Beam = beam,
                        Zone = zone,
                        XFrom = lenFt * rFrom,
                        XTo = lenFt * rTo,
                        AtStart = zone == BeamRebarZoneType.Full || zone == BeamRebarZoneType.Start,
                        AtEnd = zone == BeamRebarZoneType.Full || zone == BeamRebarZoneType.End,
                    };
                    // luôn thêm segment (kể cả rỗng) để giữ thứ tự liền kề khi nối thép
                    result.Add(seg);

                    var rb = sec != null ? _getLayerRebar(sec, layer) : null;
                    if (rb == null || string.IsNullOrWhiteSpace(rb.Name)) continue;
                    var qty = _getLayerQty(sec, layer);
                    if (qty <= 0) continue;

                    var dMm = _ParseDiameterMm(rb.Name);
                    if (dMm <= 0) continue;

                    seg.Name = rb.Name;
                    seg.DiameterMm = dMm;
                    seg.Z = _layerOffsetZ(beam, sec, layer);

                    var usableHalfW = _usableHalfWidth(beam, dMm);
                    if (usableHalfW <= 0) continue;
                    seg.Ys = _solvePositions(qty, gridQty, usableHalfW);
                }
            }
            return result;
        }

        /// <summary>Chia vị trí thép trên lưới chung (xem BeamRebarUtils.SolveGridPositions).</summary>
        private static Dictionary<int, double> _solvePositions(int qty, int gridQty, double usableHalfW)
            => BeamRebarUtils.SolveGridPositions(qty, gridQty, usableHalfW);

        // ── Kiểm tra nối liên tục / neo tại gối ───────────────────────────────

        /// <summary>
        /// true → thép lớp này KHÔNG chạy liên tục giữa 2 đoạn liền kề (phải neo / cắt).
        /// </summary>
        private bool _isBreakBetween(_BarSegment a, _BarSegment b)
        {
            if (!a.Ys.Any() || !b.Ys.Any()) return true;
            if (Math.Abs(a.DiameterMm - b.DiameterMm) > 0.5) return true; // khác đường kính → neo

            // cùng nhịp (Start→Mid→End): chỉ nối khi cùng cao độ lớp
            if (a.BeamIndex == b.BeamIndex)
                return Math.Abs(a.Z - b.Z).ToMillimeters() > 1.0;

            // khác nhịp: phải là End của nhịp trước → Start của nhịp sau
            if (b.BeamIndex != a.BeamIndex + 1 || !a.AtEnd || !b.AtStart) return true;

            var vty = a.Beam.VTY;
            var vtz = a.Beam.VTZ;
            var pA = _pointOnBar(a, a.Beam.Length.FromMillimeters(), 0.0);
            var pB = _pointOnBar(b, 0.0, 0.0);
            var v = pB - pA;

            var dzMm = Math.Round(Math.Abs(v.DotProduct(vtz)).ToMillimeters(), 0); // chênh cao độ lớp
            var dyMm = Math.Round(Math.Abs(v.DotProduct(vty)).ToMillimeters(), 0); // lệch ngang
            var e0Mm = _settingBeamModel?.E0 ?? 100;
            return dzMm >= e0Mm || dyMm >= e0Mm;
        }

        // ── Dựng hình dạng 1 thanh ───────────────────────────────────────────

        private _BarShape _buildMainBarShape(
            List<_BarSegment> segments,
            int kStart,
            int kEnd,
            int idx,
            bool isTop,
            int layerOrder)
        {
            var first = segments[kStart];
            var last = segments[kEnd];
            var vtx = first.Beam.VTX;

            var pts = new List<XYZ>();

            // đầu thanh
            var pFirst = _pointOnBar(first, first.XFrom, first.Ys[idx]);
            var startAtSupport = first.AtStart && first.XFrom < 1e-6;
            if (startAtSupport)
            {
                var anchor = _anchorPoints(first.Beam, first.Beam.BeamBearingStart, pFirst, -vtx,
                    isTop, first.DiameterMm, layerOrder, out bool replaceFace);
                anchor.Reverse(); // từ đầu móc → vào trong dầm
                pts.AddRange(anchor);
                if (!replaceFace) pts.Add(pFirst);
            }
            else pts.Add(pFirst);

            // qua các gối trung gian (bẻ xiên trong cột nếu 2 nhịp lệch nhau < E0)
            for (int k = kStart; k < kEnd; k++)
            {
                var a = segments[k];
                var b = segments[k + 1];
                if (a.BeamIndex == b.BeamIndex) continue;
                pts.Add(_pointOnBar(a, a.Beam.Length.FromMillimeters(), a.Ys[idx]));
                pts.Add(_pointOnBar(b, 0.0, b.Ys[idx]));
            }

            // cuối thanh
            var lenLastFt = last.Beam.Length.FromMillimeters();
            var pLast = _pointOnBar(last, last.XTo, last.Ys[idx]);
            var endAtSupport = last.AtEnd && Math.Abs(last.XTo - lenLastFt) < 1e-6;
            if (endAtSupport)
            {
                var anchor = _anchorPoints(last.Beam, last.Beam.BeamBearingEnd, pLast, vtx,
                    isTop, last.DiameterMm, layerOrder, out bool replaceFace);
                if (!replaceFace) pts.Add(pLast);
                pts.AddRange(anchor);
            }
            else pts.Add(pLast);

            pts = _cleanPoints(pts);
            if (pts.Count < 2) return null;

            return new _BarShape
            {
                Name = first.Name,
                HostId = first.Beam.Id,
                Points = pts,
                Vty = first.Beam.VTY,
                Offset = first.Ys[idx],
            };
        }

        // ── Gom thành Rebar Set ──────────────────────────────────────────────

        /// <summary>
        /// Gom các thanh thành Rebar Set (shape-driven, layout Fixed Number):
        ///   - cùng đường kính, cùng host;
        ///   - cùng hình dạng, chỉ tịnh tiến theo VTY (thanh nằm trong mặt phẳng VTX–VTZ);
        ///   - các thanh liên tiếp cách đều nhau.
        /// VD Top1 = 3 thanh (Index 1, 3, 5 trên lưới 5) → 1 set 3 thanh.
        /// Lưới không đều (VD 4 thanh trên lưới 5: 1, 2, 4, 5) → 2 set × 2 thanh.
        /// Thanh không gom được (bẻ xiên ngang qua gối, 3D…) → tạo thanh lẻ như cũ.
        /// </summary>
        private void _createBarsAsRebarSets(
            BeamRebarLayerType layer, List<_BarShape> shapes, List<RebarBarType> rebarBarTypes)
        {
            var tolFt = 1.0.FromMillimeters();

            // 1. nhóm theo tên + host + số điểm
            var groups = shapes
                .Where(x => x?.Points != null && x.Points.Count >= 2)
                .GroupBy(x => $"{x.Name}|{x.HostId}|{x.Points.Count}");

            foreach (var g in groups)
            {
                // 2. trong nhóm: gom cụm cùng hình dạng (tịnh tiến theo VTY)
                var clusters = new List<List<_BarShape>>();
                foreach (var shape in g.OrderBy(x => x.Offset))
                {
                    var cluster = clusters.FirstOrDefault(c => _isSameShapeAlongVty(c[0], shape, tolFt));
                    if (cluster != null) cluster.Add(shape);
                    else clusters.Add(new List<_BarShape> { shape });
                }

                foreach (var cluster in clusters)
                {
                    var sorted = cluster.OrderBy(x => x.Offset).ToList();

                    // thanh có nằm trong mặt phẳng VTX–VTZ (pháp tuyến VTY) không → mới rải set được
                    var canSet = _isPlanarNormalVty(sorted[0], tolFt);
                    if (!canSet && sorted.Count >= 2)
                        _mainBarWarnings.Add($"{layer} {sorted[0].Name}: {sorted.Count} thanh bị bẻ xiên (không nằm trong 1 mặt phẳng) → để thanh lẻ.");

                    // 3. tách thành các dãy cách đều
                    var runs = new List<List<_BarShape>>();
                    if (!canSet)
                    {
                        runs.AddRange(sorted.Select(x => new List<_BarShape> { x }));
                    }
                    else
                    {
                        runs.AddRange(_splitRuns(sorted, tolFt));
                    }

                    foreach (var r in runs)
                    {
                        try { _createRebarRun(layer, r, canSet, rebarBarTypes); }
                        catch (Exception) { /* Silent – tiếp tục dãy tiếp theo */ }
                    }
                }
            }
        }

        /// <summary>
        /// Chia các thanh (đã sắp theo VTY) thành các dãy:
        /// - cách đều toàn bộ → 1 set;
        /// - không đều → 2 thanh ngoài cùng vào 1 set, các thanh bên trong gom theo dãy cách đều (hoặc thanh đơn).
        /// </summary>
        private static List<List<_BarShape>> _splitRuns(List<_BarShape> sorted, double tolFt)
        {
            var result = new List<List<_BarShape>>();
            if (sorted.Count <= 2)
            {
                result.Add(sorted.ToList());
                return result;
            }
            if (_isEvenlySpaced(sorted, tolFt))
            {
                result.Add(sorted.ToList());
                return result;
            }
            result.Add(new List<_BarShape> { sorted[0], sorted[sorted.Count - 1] });
            var inner = sorted.Skip(1).Take(sorted.Count - 2).ToList();
            var run = new List<_BarShape> { inner[0] };
            double? spacing = null;
            for (int i = 1; i < inner.Count; i++)
            {
                var gap = inner[i].Offset - inner[i - 1].Offset;
                if (spacing == null || Math.Abs(gap - spacing.Value) <= tolFt)
                {
                    spacing ??= gap;
                    run.Add(inner[i]);
                }
                else
                {
                    result.Add(run);
                    run = new List<_BarShape> { inner[i] };
                    spacing = null;
                }
            }
            result.Add(run);
            return result;
        }

        private static bool _isEvenlySpaced(List<_BarShape> sorted, double tolFt)
        {
            var g0 = sorted[1].Offset - sorted[0].Offset;
            for (int i = 2; i < sorted.Count; i++)
                if (Math.Abs(sorted[i].Offset - sorted[i - 1].Offset - g0) > tolFt) return false;
            return true;
        }

        /// <summary>Tạo 1 dãy thanh: ≥ 2 thanh → Rebar Set; 1 thanh (hoặc tạo set lỗi) → thanh lẻ.</summary>
        private void _createRebarRun(
            BeamRebarLayerType layer, List<_BarShape> run, bool canSet, List<RebarBarType> rebarBarTypes)
        {
            var first = run[0];
            var host = _document.GetElement(first.HostId);

            if (canSet && run.Count >= 2)
            {
                var spacingFt = (run[1].Offset - run[0].Offset);
                var arrayLengthFt = run.Last().Offset - run[0].Offset;
                if (spacingFt > 1e-6)
                {
                    var set = RebarHelper.CreateRebarFixedNumberSet(
                        _document, first.Points.PointsToCurves(), first.Name,
                        first.Vty, rebarBarTypes, host, run.Count, arrayLengthFt, out string error);
                    if (set != null) return;
                    _mainBarWarnings.Add($"{layer} {first.Name} x{run.Count}: tạo Rebar Set lỗi ({error}) → để thanh lẻ.");
                }
            }

            // fallback: thanh lẻ
            foreach (var bar in run)
            {
                try
                {
                    var curves = bar.Points.PointsToCurves();
                    var isRebarFreeForm = RebarHelper.IsRebarFreeForm(curves, out XYZ normal);
                    if (isRebarFreeForm)
                        RebarHelper.CreateRebar(_document, curves, bar.Name, "A", rebarBarTypes, host);
                    else
                        RebarHelper.CreateRebar(_document, curves, bar.Name, normal, rebarBarTypes, host);
                }
                catch (Exception ex)
                {
                    _mainBarWarnings.Add($"{layer} {bar.Name}: không tạo được thanh lẻ ({ex.Message}).");
                }
            }
        }

        /// <summary>b = a tịnh tiến theo VTY (mọi điểm lệch cùng 1 vector song song VTY).</summary>
        private static bool _isSameShapeAlongVty(_BarShape a, _BarShape b, double tolFt)
        {
            if (a.Points.Count != b.Points.Count) return false;
            var shift = a.Vty * (b.Offset - a.Offset);
            for (int i = 0; i < a.Points.Count; i++)
            {
                if ((b.Points[i] - a.Points[i] - shift).GetLength() > tolFt) return false;
            }
            return true;
        }

        /// <summary>Tất cả điểm của thanh nằm trong 1 mặt phẳng vuông góc VTY.</summary>
        private static bool _isPlanarNormalVty(_BarShape a, double tolFt)
        {
            var p0 = a.Points[0];
            return a.Points.All(p => Math.Abs((p - p0).DotProduct(a.Vty)) <= tolFt);
        }

        /// <summary>
        /// Các điểm neo tính từ mặt đầu/cuối dầm đi ra ngoài (theo outward).
        /// - Có gối, đủ chiều dày: neo thẳng La = Ldt·d.
        /// - Có gối, không đủ: tới mép xa gối (trừ lớp bảo vệ, lùi thêm theo lớp) rồi bẻ móc 90° dài B.
        /// - Không có gối (đầu tự do): lùi vào lớp bảo vệ rồi bẻ móc 90° (replaceFace = true).
        /// Móc: thép trên bẻ xuống, thép dưới bẻ lên.
        /// </summary>
        private List<XYZ> _anchorPoints(
            BeamRebarModel beam,
            BeamBearingModel bearing,
            XYZ pFace,
            XYZ outward,
            bool isTop,
            double dMm,
            int layerOrder,
            out bool replaceFace)
        {
            replaceFace = false;
            var result = new List<XYZ>();

            var dFt = dMm.FromMillimeters();
            var coverFt = beam.Cover.FromMillimeters();
            var hookDir = isTop ? -beam.VTZ : beam.VTZ;

            var anchorModel = _rebarAnchorageLengthModels?.FirstOrDefault();
            var laFt = anchorModel != null ? (anchorModel.Ldt * dMm).FromMillimeters() : 30.0 * dFt;

            var hookModel = _rebarAnchorageHookMainBarModels?
                .OrderBy(x => Math.Abs(x.Diameter - (int)Math.Round(dMm)))
                .FirstOrDefault();
            var hookFt = hookModel != null && hookModel.B > 0 ? hookModel.B.FromMillimeters() : 12.0 * dFt;

            // lớp trong lùi vào để móc không chồng lên móc lớp ngoài
            var layerPullFt = layerOrder * (Math.Max(LayerClearMinMm, dMm).FromMillimeters() + dFt);

            var thicknessFt = (bearing?.Thickness ?? 0).FromMillimeters();
            var availFt = thicknessFt - coverFt - layerPullFt;

            if (bearing == null || thicknessFt <= 0 || availFt <= dFt)
            {
                // đầu tự do: lùi vào trong dầm 1 lớp bảo vệ rồi bẻ móc
                replaceFace = true;
                var pEnd = pFace - outward * (coverFt + layerPullFt);
                result.Add(pEnd);
                result.Add(pEnd + hookDir * hookFt);
                return result;
            }

            if (availFt >= laFt)
            {
                result.Add(pFace + outward * laFt); // neo thẳng
                return result;
            }

            var pOut = pFace + outward * availFt;   // tới mép xa gối
            result.Add(pOut);
            result.Add(pOut + hookDir * hookFt);    // bẻ móc 90°
            return result;
        }

        // ── Hình học tiết diện ───────────────────────────────────────────────

        /// <summary>Điểm trên thanh: x tính từ mặt đầu dầm theo VTX, y theo VTY, z = offset lớp.</summary>
        private static XYZ _pointOnBar(_BarSegment seg, double xFt, double yFt)
        {
            var beam = seg.Beam;
            var startFace = beam.Center - beam.VTX * (beam.Length.FromMillimeters() / 2);
            return startFace + beam.VTX * xFt + beam.VTY * yFt + beam.VTZ * seg.Z;
        }

        /// <summary>Nửa bề rộng khả dụng (tâm thanh góc) theo VTY, giống CreateStirrup (cover tới tim đai).</summary>
        private static double _usableHalfWidth(BeamRebarModel beam, double dMm)
        {
            var coverFt = beam.Cover.FromMillimeters();
            var dStFt = _ParseDiameterMm(beam.SectionStart?.Stirrup?.Name).FromMillimeters();
            return beam.Width.FromMillimeters() / 2 - coverFt - dStFt / 2 - dMm.FromMillimeters() / 2;
        }

        /// <summary>
        /// Offset theo VTZ (ft, so với tâm dầm) của tâm thanh lớp đang xét.
        /// Lớp 1 sát đai; lớp 2, 3 lần lượt cách lớp trước khe hở max(25mm, d lớn hơn).
        /// </summary>
        private static double _layerOffsetZ(BeamRebarModel beam, BeamRebarSectionModel sec, BeamRebarLayerType layer)
        {
            var isTop = _isTopLayer(layer);
            var order = _layerOrder(layer);
            var layers = isTop
                ? new[] { sec.RebarTop1, sec.RebarTop2, sec.RebarTop3 }
                : new[] { sec.RebarBot1, sec.RebarBot2, sec.RebarBot3 };
            var ds = layers.Select(x => _ParseDiameterMm(x?.Name)).ToArray();

            var coverMm = beam.Cover;
            var dStMm = _ParseDiameterMm(beam.SectionStart?.Stirrup?.Name);

            var zMm = beam.Height / 2 - coverMm - dStMm / 2 - ds[0] / 2;
            for (int i = 1; i <= order; i++)
            {
                var clearMm = Math.Max(LayerClearMinMm, Math.Max(ds[i - 1], ds[i]));
                zMm -= ds[i - 1] / 2 + clearMm + ds[i] / 2;
            }
            return (isTop ? zMm : -zMm).FromMillimeters();
        }

        // ── Tiện ích ─────────────────────────────────────────────────────────

        private static bool _isTopLayer(BeamRebarLayerType layer) => BeamRebarUtils.IsTopLayer(layer);

        /// <summary>0 = lớp 1, 1 = lớp 2, 2 = lớp 3.</summary>
        private static int _layerOrder(BeamRebarLayerType layer) => BeamRebarUtils.LayerOrder(layer);

        private static RebarModel _getLayerRebar(BeamRebarSectionModel sec, BeamRebarLayerType layer)
            => BeamRebarUtils.GetLayerRebar(sec, layer);

        /// <summary>Số thanh của lớp (RebarModel.Spacing = qty). Top1/Bot1 tối thiểu 2 thanh.</summary>
        private static int _getLayerQty(BeamRebarSectionModel sec, BeamRebarLayerType layer)
            => BeamRebarUtils.GetLayerQty(sec, layer);

        /// <summary>Bỏ điểm trùng và điểm giữa thẳng hàng.</summary>
        private static List<XYZ> _cleanPoints(List<XYZ> pts)
        {
            var tol = 1.0.FromMillimeters();
            var result = new List<XYZ>();
            foreach (var p in pts.Where(x => x != null))
            {
                if (result.Any() && result.Last().DistanceTo(p) < tol) continue;
                result.Add(p);
            }
            for (int i = result.Count - 2; i >= 1; i--)
            {
                var d1 = (result[i] - result[i - 1]).Normalize();
                var d2 = (result[i + 1] - result[i]).Normalize();
                if (d1.CrossProduct(d2).GetLength() < 1e-6 && d1.DotProduct(d2) > 0)
                    result.RemoveAt(i);
            }
            return result;
        }

        /// <summary>Parse đường kính (mm) từ tên kiểu "D18" → 18. Không parse được → 0.</summary>
        private static double _ParseDiameterMm(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return 0;
            var digits = new string(name.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray());
            return double.TryParse(digits, out var d) ? d : 0;
        }
    }
}
