using Autodesk.Revit.DB.Structure;
using Newtonsoft.Json;
using LSTool.Compatibility;
using LSTool.Tools.Beams.BeamRebar.models;
using LSTool.Tools.Beams.BeamRebar.schemas;
using LSTool.Tools.Beams.BeamRebar.types;
using LSTool.Tools.Beams.BeamRebar.Utils;
using LSTool.Utils;

namespace LSTool.Tools.Beams.BeamRebar.action
{
    public partial class BeamRebarAction
    {
        private RebarHookType _tieHook135;
        private RebarHookType _tieHook180;
        private BeamTiePositionSchema _beamTieSchema;
        private BeamTiePositionSchema BeamTieSchema =>
            _beamTieSchema ??= new BeamTiePositionSchema(BeamTiePositionSchema.GUID, BeamTiePositionSchema.NAME);


        private void LoadBeamTies(IEnumerable<BeamRebarModel> beams)
        {
            foreach (var beam in beams)
            {
                try
                {
                    var ele = _document.GetElement(beam.Id);
                    var content = BeamTieSchema.Read(ele);
                    if (string.IsNullOrEmpty(content)) continue;
                    var ties = JsonConvert.DeserializeObject<List<BeamTieModel>>(content);
                    if (ties != null) beam.Ties = ties;
                }
                catch (Exception) { }
            }
        }

        private void SaveBeamTies()
        {
            var beams = _viewModel.BeamRebarModels;
            if (beams == null) return;
            foreach (var beam in beams)
            {
                var ele = _document.GetElement(beam.Id);
                if (ele == null) continue;
                BeamTieSchema.Write(ele, JsonConvert.SerializeObject(beam.Ties ?? new List<BeamTieModel>()));
            }
        }


        private void CreateStirrupSub()
        {
            var beams = _viewModel.BeamRebarModels;
            if (beams == null || !beams.Any()) return;
            if (!beams.Any(b => b.Ties != null && b.Ties.Any())) return;

            var rebarBarTypes = new FilteredElementCollector(_document)
                .WhereElementIsElementType()
                .OfClass(typeof(RebarBarType))
                .Cast<RebarBarType>()
                .Where(x => x.Name.Contains("D"))
                .OrderBy(x => x.Name)
                .ToList();
            var hookTypes = new FilteredElementCollector(_document)
                .WhereElementIsElementType()
                .OfClass(typeof(RebarHookType))
                .Cast<RebarHookType>()
                .Where(x=>x.Style == RebarStyle.StirrupTie)
                .ToList();
            var hook135 = hookTypes.FirstOrDefault(x => Math.Abs(x.HookAngle.ToDegrees() - 135) <= 1)
                ?? hookTypes.FirstOrDefault(x => x.Name.Contains("135"))
                ?? hookTypes.FirstOrDefault();
            if (hook135 == null)
            {
                _mainBarWarnings.Add("Đai phụ: không tìm thấy Hook Type 135.");
                return;
            }
            _tieHook135 = hook135;
            var hook180 = hookTypes.FirstOrDefault(x => _hookAngleDeg(x) is > 179 and < 181)
                ?? hookTypes.FirstOrDefault(x => x.Name.Contains("180"));
            if (hook180 == null)
            {
                _mainBarWarnings.Add("Đai phụ: không tìm thấy Hook Type 180.");
                return;
            }
            _tieHook180 = hook180;
            var gridQty = BeamRebarUtils.GetGridQty(beams);
            foreach (var beam in beams)
            {
                if (beam.Ties == null) continue;
                foreach (var tie in beam.Ties)
                {
                    try { _installTie(beam, tie, gridQty, rebarBarTypes, _tieHook180); }
                    catch (Exception ex) { _mainBarWarnings.Add($"Đai phụ: {ex.Message}"); }
                }
            }
        }

        private static double _hookAngleDeg(RebarHookType hook)
        {
            var a = Convert.ToDouble(hook.HookAngle);
            return a > Math.PI + 0.01 ? a : a * 180 / Math.PI;
        }

        private void _installTie(
            BeamRebarModel beam, BeamTieModel tie, int gridQty,
            List<RebarBarType> rebarBarTypes, RebarHookType hook)
        {
            var vtx = beam.VTX;
            var vty = beam.VTY;
            var vtz = beam.VTZ;

            var lb = _settingBeamModel?.StressZone ?? 0.25;
            if (lb <= 0 || lb > 0.4) lb = 0.25;
            var ext = 50.0.FromMillimeters();
            var halfLen = beam.Length.FromMillimeters() / 2;
            var pStart = beam.Center - vtx * halfLen + vtx * ext;
            var pEnd = beam.Center + vtx * halfLen - vtx * ext;
            var netLen = pStart.DistanceTo(pEnd);
            var zoneStartEnd = pStart + vtx * lb * netLen;
            var zoneEndStart = pEnd - vtx * lb * netLen;

            var sections = new[]
            {
                (Sec: beam.SectionStart, Pt: pStart,       Len: pStart.DistanceTo(zoneStartEnd),        Name: "Start"),
                (Sec: beam.SectionMid,   Pt: zoneStartEnd, Len: zoneStartEnd.DistanceTo(zoneEndStart),  Name: "Mid"),
                (Sec: beam.SectionEnd,   Pt: zoneEndStart, Len: zoneEndStart.DistanceTo(pEnd),          Name: "End"),
            };

            var host = _document.GetElement(beam.Id);
            for (int si = 0; si < sections.Length; si++)
            {
                var s = sections[si];
                var sec = s.Sec;
                var st = sec?.Stirrup;
                if (sec == null || st == null || st.Spacing <= 0 || string.IsNullOrWhiteSpace(st.Name)) continue;
                var dSt = _ParseDiameterMm(st.Name);
                if (dSt <= 0) continue;

                var origin = beam.Center + vtx * (s.Pt - beam.Center).DotProduct(vtx);

                XYZ p0, p1;
                XYZ sideDir;
                double dMainMm;
                if (tie.Type == (int)BeamTieType.Vertical)
                {
                    if (!_barPoint(beam, sec, BeamRebarLayerType.Top1, gridQty, tie.Index, origin, out var top, out var dTop)
                        || !_barPoint(beam, sec, BeamRebarLayerType.Bot1, gridQty, tie.Index, origin, out var bot, out var dBot))
                    {
                        _mainBarWarnings.Add($"Đai phụ đứng (vị trí {tie.Index}) bỏ qua ở mặt cắt {s.Name}: không có thanh Top1/Bot1 tại vị trí này.");
                        continue;
                    }
                    p0 = top; p1 = bot;
                    dMainMm = (dTop + dBot) / 2;
                    var y = (top - origin).DotProduct(vty);
                    sideDir = y >= -1e-6 ? vty : -vty;
                }
                else if (tie.Layer == BeamCanvasSectionPreViewAction.SideTieLayer)
                {
                    if (!_sideBarLayout(beam, out var sn, out var sd, out var sHalfY, out var sz0, out var sSp, out _)
                        || tie.Index < 1 || tie.Index > sn) continue;
                    var zRow = sz0 + (tie.Index - 1) * sSp;
                    p0 = origin - vty * sHalfY + vtz * zRow;
                    p1 = origin + vty * sHalfY + vtz * zRow;
                    dMainMm = sd;
                    sideDir = vtz;
                }
                else
                {
                    var layer = (BeamRebarLayerType)tie.Layer;
                    var qty = _getLayerQty(sec, layer);
                    if (qty < 2 || _layerOrder(layer) == 0) continue;
                    var pos = _solvePositions(qty, gridQty, 1.0);
                    var first = pos.Keys.Min();
                    var last = pos.Keys.Max();
                    if (!_barPoint(beam, sec, layer, gridQty, first, origin, out p0, out var d0)
                        || !_barPoint(beam, sec, layer, gridQty, last, origin, out p1, out var d1))
                        continue;
                    dMainMm = (d0 + d1) / 2;
                    sideDir = _isTopLayer(layer) ? -vtz : vtz;
                }
                if (p0.DistanceTo(p1) < 1e-6) continue;

                if ((p1 - p0).Normalize().CrossProduct(vtx).Normalize().DotProduct(sideDir) < 0)
                    (p0, p1) = (p1, p0);

                var ps = new List<XYZ> { p0, p1 };
                var dMainFt = dMainMm.FromMillimeters();
                var dStFt = dSt.FromMillimeters();
                var hookLengthMm = _hookAngleDeg(hook) is > 179 and < 181 ? 0 : Math.Max(dSt * 10, 100);

                var vt = (ps[1] - ps[0]).Normalize();
                var nor = vt.CrossProduct(vtx).Normalize();
                var extend2Pt = dStFt + dMainFt / 2;
                var q1 = ps[0] - vt * extend2Pt + nor * (dMainFt + dStFt) / 2;
                var q2 = ps[1] + vt * extend2Pt + nor * (dMainFt + dStFt) / 2;
                var rightStart = nor;
                var vInStart = (ps[0] - q1).Normalize();
                var orientStart = rightStart.DotProduct(vInStart) > 0 ? RebarHookOrientation.Left : RebarHookOrientation.Right;
                var vInEnd = (ps[1] - q2).Normalize();
                var orientEnd = rightStart.DotProduct(vInEnd) > 0 ? RebarHookOrientation.Left : RebarHookOrientation.Right;
                var baseShape = new List<XYZ> { q1, q2 };
                baseShape.Reverse();

                var curves = baseShape.PointsToCurves();
                var flip = new Func<RebarHookOrientation, RebarHookOrientation>(o =>
                    o == RebarHookOrientation.Left ? RebarHookOrientation.Right : RebarHookOrientation.Left);
                var orients = new[]
                {
                    (orientStart, orientEnd), (flip(orientStart), flip(orientEnd)),
                    (orientStart, flip(orientEnd)), (flip(orientStart), orientEnd),
                }.Distinct().ToList();
                var is180 = !ReferenceEquals(hook, _tieHook135) && _hookAngleDeg(hook) is > 179 and < 181;
                Rebar rebar = null;
                string error = null;
                if (is180)
                {
                    var barType = rebarBarTypes.FirstOrDefault(x => x.Name == st.Name)
                        ?? rebarBarTypes.FirstOrDefault(x => x.Name.Contains(st.Name))
                        ?? rebarBarTypes.FirstOrDefault();
                    var shapeErr = string.Empty;
                    var diag = new System.Text.StringBuilder();
                    foreach (var (oS, oE) in orients)
                        if (RebarHelper.EnsureStraightTieShape(_document, 180, oS, oE, out var e) == null) shapeErr = e;
                    var shapes = new FilteredElementCollector(_document)
                        .OfClass(typeof(RebarShape)).Cast<RebarShape>()
                        .Where(x => x.RebarStyle == RebarStyle.StirrupTie).ToList();
                    diag.Append($"hook={hook.Name}/{_hookAngleDeg(hook):0}°, barType={barType?.Name}, shapes đai={shapes.Count}");
                    foreach (var (oS, oE) in orients)
                    {
                        var matched = 0;
                        foreach (var shp in shapes)
                        {
                            var match = false;
                            try { match = Rebar.RebarShapeMatchesCurvesAndHooks(shp, barType, vtx, curves, hook, hook, oS, oE); }
                            catch (Exception ex) { if (shp.Name.StartsWith("LSTool")) diag.Append($"; match lỗi {shp.Name}: {ex.Message}"); }
                            if (!match) continue;
                            matched++;
                            rebar = RebarHelper.CreateRebarStirrupTieSetEx(
                                _document, curves, st.Name, vtx, hook, hook,
                                oS, oE, rebarBarTypes, host,
                                st.Spacing, s.Len.ToMillimeters(), hookLengthMm, out error, shp);
                            if (rebar != null) break;
                            diag.Append($"; tạo với {shp.Name} lỗi: {error}");
                        }
                        diag.Append($"; {oS}-{oE}: khớp {matched}");
                        if (rebar != null) break;
                    }
                    if (rebar == null)
                        error = "không có Rebar Shape nào khớp hook 180°" + (shapeErr.Length > 0 ? $" (tạo shape: {shapeErr})" : "") + $" [{diag}]";
                }
                else
                {
                    foreach (var (oS, oE) in orients)
                    {
                        rebar = RebarHelper.CreateRebarStirrupTieSetEx(
                            _document, curves, st.Name, vtx, hook, hook,
                            oS, oE, rebarBarTypes, host,
                            st.Spacing, s.Len.ToMillimeters(), hookLengthMm, out error);
                        if (rebar != null) break;
                    }
                }
                if (rebar == null && is180)
                {
                    rebar = RebarHelper.CreateRebarStirrupTieSetEx(
                        _document, curves, st.Name, vtx, _tieHook135, _tieHook135,
                        orientStart, orientEnd, rebarBarTypes, host,
                        st.Spacing, s.Len.ToMillimeters(), Math.Max(dSt * 10, 100), out var err135);
                    if (rebar != null)
                        _mainBarWarnings.Add($"Đai phụ ({(BeamTieType)tie.Type}) mặt cắt {s.Name}: {error}, đã dùng hook 135°.");
                    else error = $"{error}; hook 135°: {err135}";
                }
                if (rebar == null)
                    _mainBarWarnings.Add($"Đai phụ ({(BeamTieType)tie.Type}) mặt cắt {s.Name}: tạo lỗi ({error}).");
            }
        }

        private bool _barPoint(
            BeamRebarModel beam, BeamRebarSectionModel sec, BeamRebarLayerType layer,
            int gridQty, int index, XYZ origin, out XYZ point, out double dMm)
        {
            point = null;
            dMm = 0;
            var rb = _getLayerRebar(sec, layer);
            var qty = _getLayerQty(sec, layer);
            if (rb == null || qty <= 0) return false;
            dMm = _ParseDiameterMm(rb.Name);
            if (dMm <= 0) return false;
            var pos = _solvePositions(qty, gridQty, _usableHalfWidth(beam, dMm));
            if (!pos.TryGetValue(index, out var y)) return false;
            point = origin + beam.VTY * y + beam.VTZ * _layerOffsetZ(beam, sec, layer);
            return true;
        }
    }
}
