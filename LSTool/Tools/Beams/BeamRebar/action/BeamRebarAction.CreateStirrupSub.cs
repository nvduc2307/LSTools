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
    /// <summary>
    /// Đai phụ của dầm (đứng / ngang), tham khảo ColumnRebarStirrupAction._InstallSub (2 điểm).
    /// Hình dạng (BeamTieModel) dùng chung cho 3 mặt cắt; spacing / vùng bố trí theo từng mặt cắt.
    /// </summary>
    public partial class BeamRebarAction
    {
        private BeamTiePositionSchema _beamTieSchema;
        private BeamTiePositionSchema BeamTieSchema =>
            _beamTieSchema ??= new BeamTiePositionSchema(BeamTiePositionSchema.GUID, BeamTiePositionSchema.NAME);

        // ── Lưu / đọc ──────────────────────────────────────────────────────────

        /// <summary>Đọc đai phụ đã lưu trong từng dầm.</summary>
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

        /// <summary>Ghi đai phụ vào từng dầm (luôn ghi, kể cả rỗng để xóa được đai đã bỏ). Gọi trong Transaction.</summary>
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

        // ── Dựng đai phụ ───────────────────────────────────────────────────────

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
                .ToList();
            var hook135 = hookTypes.FirstOrDefault(x => Math.Abs(x.HookAngle.ToDegrees() - 135) <= 1)
                ?? hookTypes.FirstOrDefault(x => x.Name.Contains("135"))
                ?? hookTypes.FirstOrDefault();
            if (hook135 == null)
            {
                _mainBarWarnings.Add("Đai phụ: không tìm thấy Hook Type.");
                return;
            }

            var gridQty = BeamRebarUtils.GetGridQty(beams);
            foreach (var beam in beams)
            {
                if (beam.Ties == null) continue;
                foreach (var tie in beam.Ties)
                {
                    try { _installTie(beam, tie, gridQty, rebarBarTypes, hook135); }
                    catch (Exception ex) { _mainBarWarnings.Add($"Đai phụ: {ex.Message}"); }
                }
            }
        }

        private void _installTie(
            BeamRebarModel beam, BeamTieModel tie, int gridQty,
            List<RebarBarType> rebarBarTypes, RebarHookType hook135)
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

                // tâm dầm tại mặt phẳng đầu vùng
                var origin = beam.Center + vtx * (s.Pt - beam.Center).DotProduct(vtx);

                XYZ p0, p1;
                XYZ sideDir;     // phía đai đi qua (đường thẳng của đai nằm phía này)
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
                    // phía ngoài = phía mép gần nhất (giữa thì +VTY)
                    var y = (top - origin).DotProduct(vty);
                    sideDir = y >= -1e-6 ? vty : -vty;
                }
                else if (tie.Layer == BeamCanvasSectionPreViewAction.SideTieLayer)
                {
                    // đai ngang qua thép sidebar (dầm cao >= 700): nối thanh trái - phải cùng hàng
                    if (!_sideBarLayout(beam, out var sn, out var sd, out var sHalfY, out var sz0, out var sSp, out _)
                        || tie.Index < 1 || tie.Index > sn) continue;
                    var zRow = sz0 + (tie.Index - 1) * sSp;
                    p0 = origin - vty * sHalfY + vtz * zRow;
                    p1 = origin + vty * sHalfY + vtz * zRow;
                    dMainMm = sd;
                    sideDir = vtz;   // đai đi phía trên (+VTZ), khớp canvas
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
                    // đai ngang đi phía trong (về tâm dầm)
                    sideDir = _isTopLayer(layer) ? -vtz : vtz;
                }
                if (p0.DistanceTo(p1) < 1e-6) continue;

                // đảo thứ tự để vt × VTX cùng phía sideDir (đường đai nằm phía sideDir)
                if ((p1 - p0).Normalize().CrossProduct(vtx).Normalize().DotProduct(sideDir) < 0)
                    (p0, p1) = (p1, p0);

                var ps = new List<XYZ> { p0, p1 };
                var dMainFt = dMainMm.FromMillimeters();
                var dStFt = dSt.FromMillimeters();
                var hookLengthMm = Math.Max(dSt * 10, 100);

                // ── copy từ ColumnRebarStirrupAction._InstallSub (qty == 2) ──
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
                var rebar = RebarHelper.CreateRebarStirrupTieSetEx(
                    _document, curves, st.Name, vtx, hook135, hook135,
                    orientStart, orientEnd, rebarBarTypes, host,
                    st.Spacing, s.Len.ToMillimeters(), hookLengthMm, out var error);
                if (rebar == null)
                    _mainBarWarnings.Add($"Đai phụ ({(BeamTieType)tie.Type}) mặt cắt {s.Name}: tạo lỗi ({error}).");
            }
        }

        /// <summary>Điểm tâm thanh (lớp, Index lưới) trên mặt phẳng mặt cắt đi qua <paramref name="origin"/>.</summary>
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
