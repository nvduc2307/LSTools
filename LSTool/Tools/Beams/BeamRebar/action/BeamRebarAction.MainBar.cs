using Autodesk.Revit.DB.Structure;
using LSTool.MVVM.Models;
using LSTool.Tools.Beams.BeamRebar.models;
using LSTool.Tools.Beams.BeamRebar.types;
using LSTool.Tools.Beams.BeamRebar.Utils;
using LSTool.Utils;

namespace LSTool.Tools.Beams.BeamRebar.action
{
    public partial class BeamRebarAction
    {
        private double ZoneSupportRatio
        {
            get
            {
                var z = _settingBeamModel?.StressZone ?? 0.25;
                return z > 0 && z <= 0.4 ? z : 0.25;
            }
        }
        private double ZoneMidStartRatio => ZoneSupportRatio / 2;
        private double ZoneMidEndRatio => 1.0 - ZoneSupportRatio / 2;
        private const double LayerClearMinMm = 25;

        private readonly List<string> _mainBarWarnings = new List<string>();

        private sealed class _BarSegment
        {
            public int BeamIndex { get; set; }
            public BeamRebarModel Beam { get; set; }
            public BeamRebarZoneType Zone { get; set; }
            public double XFrom { get; set; }
            public double XTo { get; set; }
            public bool AtStart { get; set; }
            public bool AtEnd { get; set; }
            public string Name { get; set; }
            public double DiameterMm { get; set; }
            public double Z { get; set; }
            public Dictionary<int, double> Ys { get; set; } = new Dictionary<int, double>();
        }

        private sealed class _BarShape
        {
            public string Name { get; set; }
            public string HostId { get; set; }
            public List<XYZ> Points { get; set; }
            public XYZ Vty { get; set; }
            public double Offset { get; set; }
        }


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

            var shapes = new List<_BarShape>();
            for (int idx = 1; idx <= gridQty; idx++)
            {
                for (int k = 0; k < segments.Count; k++)
                {
                    if (!segments[k].Ys.ContainsKey(idx)) continue;
                    if (Connected(k - 1, idx)) continue;

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

            using (var ts = new SubTransaction(_document))
            {
                ts.Start();
                _createBarsAsRebarSets(layer, shapes, rebarBarTypes);
                ts.Commit();
            }
        }


        private static int _getGridQty(List<BeamRebarModel> beams) => BeamRebarUtils.GetGridQty(beams);

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

        private static Dictionary<int, double> _solvePositions(int qty, int gridQty, double usableHalfW)
            => BeamRebarUtils.SolveGridPositions(qty, gridQty, usableHalfW);


        private bool _isBreakBetween(_BarSegment a, _BarSegment b)
        {
            if (!a.Ys.Any() || !b.Ys.Any()) return true;
            if (Math.Abs(a.DiameterMm - b.DiameterMm) > 0.5) return true;

            if (a.BeamIndex == b.BeamIndex)
                return Math.Abs(a.Z - b.Z).ToMillimeters() > 1.0;

            if (b.BeamIndex != a.BeamIndex + 1 || !a.AtEnd || !b.AtStart) return true;

            var vty = a.Beam.VTY;
            var vtz = a.Beam.VTZ;
            var pA = _pointOnBar(a, a.Beam.Length.FromMillimeters(), 0.0);
            var pB = _pointOnBar(b, 0.0, 0.0);
            var v = pB - pA;

            var dzMm = Math.Round(Math.Abs(v.DotProduct(vtz)).ToMillimeters(), 0);
            var dyMm = Math.Round(Math.Abs(v.DotProduct(vty)).ToMillimeters(), 0);
            var e0Mm = _settingBeamModel?.E0 ?? 100;
            return dzMm >= e0Mm || dyMm >= e0Mm;
        }


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

            var pFirst = _pointOnBar(first, first.XFrom, first.Ys[idx]);
            var startAtSupport = first.AtStart && first.XFrom < 1e-6;
            if (startAtSupport)
            {
                var anchor = _anchorPoints(first.Beam, first.Beam.BeamBearingStart, true, pFirst, -vtx,
                    isTop, first.DiameterMm, layerOrder, out bool replaceFace);
                anchor.Reverse();
                pts.AddRange(anchor);
                if (!replaceFace) pts.Add(pFirst);
            }
            else pts.Add(pFirst);

            for (int k = kStart; k < kEnd; k++)
            {
                var a = segments[k];
                var b = segments[k + 1];
                if (a.BeamIndex == b.BeamIndex) continue;
                pts.Add(_pointOnBar(a, a.Beam.Length.FromMillimeters(), a.Ys[idx]));
                pts.Add(_pointOnBar(b, 0.0, b.Ys[idx]));
            }

            var lenLastFt = last.Beam.Length.FromMillimeters();
            var pLast = _pointOnBar(last, last.XTo, last.Ys[idx]);
            var endAtSupport = last.AtEnd && Math.Abs(last.XTo - lenLastFt) < 1e-6;
            if (endAtSupport)
            {
                var anchor = _anchorPoints(last.Beam, last.Beam.BeamBearingEnd, false, pLast, vtx,
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


        private void _createBarsAsRebarSets(
            BeamRebarLayerType layer, List<_BarShape> shapes, List<RebarBarType> rebarBarTypes)
        {
            var tolFt = 1.0.FromMillimeters();

            var groups = shapes
                .Where(x => x?.Points != null && x.Points.Count >= 2)
                .GroupBy(x => $"{x.Name}|{x.HostId}|{x.Points.Count}");

            foreach (var g in groups)
            {
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

                    var canSet = _isPlanarNormalVty(sorted[0], tolFt);
                    if (!canSet && sorted.Count >= 2)
                        _mainBarWarnings.Add($"{layer} {sorted[0].Name}: {sorted.Count} thanh bị bẻ xiên (không nằm trong 1 mặt phẳng) → để thanh lẻ.");

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
                        catch (Exception) {  }
                    }
                }
            }
        }

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

        private static bool _isPlanarNormalVty(_BarShape a, double tolFt)
        {
            var p0 = a.Points[0];
            return a.Points.All(p => Math.Abs((p - p0).DotProduct(a.Vty)) <= tolFt);
        }

        private List<XYZ> _anchorPoints(
            BeamRebarModel beam,
            BeamBearingModel bearing,
            bool isStart,
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

            // L1 (top) / L2 (bottom): total anchorage length, L3: min hook length. Both are multiples of d.
            var totalFactor = isStart
                ? (isTop ? _viewModel.AnchorStartTopL1 : _viewModel.AnchorStartBotL2)
                : (isTop ? _viewModel.AnchorEndTopL1 : _viewModel.AnchorEndBotL2);
            var minHookFactor = isStart
                ? (isTop ? _viewModel.AnchorStartTopL3 : _viewModel.AnchorStartBotL3)
                : (isTop ? _viewModel.AnchorEndTopL3 : _viewModel.AnchorEndBotL3);
            var totalFt = Math.Max(totalFactor, 0) * dFt;
            var minHookFt = Math.Max(minHookFactor, LSTool.Tools.Beams.BeamRebar.viewModel.BeamRebarVM.MinAnchorHookFactor) * dFt;

            // Bar end stops at the concrete cover from the far face of the support.
            var offsetFt = coverFt;

            var layerPullFt = layerOrder * (Math.Max(LayerClearMinMm, dMm).FromMillimeters() + dFt);


            var thicknessFt = (bearing?.Thickness ?? 0).FromMillimeters();
            var availFt = thicknessFt - offsetFt - layerPullFt;

            if (bearing == null || thicknessFt <= 0 || availFt <= dFt)
            {
                replaceFace = true;
                var pEnd = pFace - outward * (coverFt + layerPullFt);
                result.Add(pEnd);
                result.Add(pEnd + hookDir * Math.Max(totalFt, minHookFt));
                return result;
            }

            // Straight part reaches the full anchorage length inside the support: no hook needed.
            if (availFt >= totalFt)
            {
                result.Add(pFace + outward * totalFt);
                return result;
            }

            // Otherwise run to the far side of the support and hook for the missing length (at least L3).
            var pOut = pFace + outward * availFt;
            result.Add(pOut);
            var hookFt = Math.Max(totalFt - availFt, minHookFt);
            result.Add(pOut + hookDir * hookFt);
            return result;
        }


        private static XYZ _pointOnBar(_BarSegment seg, double xFt, double yFt)
        {
            var beam = seg.Beam;
            var startFace = beam.Center - beam.VTX * (beam.Length.FromMillimeters() / 2);
            return startFace + beam.VTX * xFt + beam.VTY * yFt + beam.VTZ * seg.Z;
        }

        private static double _usableHalfWidth(BeamRebarModel beam, double dMm)
        {
            var coverFt = beam.Cover.FromMillimeters();
            var dStFt = _ParseDiameterMm(beam.SectionStart?.Stirrup?.Name).FromMillimeters();
            return beam.Width.FromMillimeters() / 2 - coverFt - dStFt / 2 - dMm.FromMillimeters() / 2;
        }

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


        private static bool _isTopLayer(BeamRebarLayerType layer) => BeamRebarUtils.IsTopLayer(layer);

        private static int _layerOrder(BeamRebarLayerType layer) => BeamRebarUtils.LayerOrder(layer);

        private static RebarModel _getLayerRebar(BeamRebarSectionModel sec, BeamRebarLayerType layer)
            => BeamRebarUtils.GetLayerRebar(sec, layer);

        private static int _getLayerQty(BeamRebarSectionModel sec, BeamRebarLayerType layer)
            => BeamRebarUtils.GetLayerQty(sec, layer);

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

        private static double _ParseDiameterMm(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return 0;
            var digits = new string(name.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray());
            return double.TryParse(digits, out var d) ? d : 0;
        }
    }
}
