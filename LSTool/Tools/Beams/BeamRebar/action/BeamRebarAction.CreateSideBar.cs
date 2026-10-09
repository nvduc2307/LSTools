using Autodesk.Revit.DB.Structure;
using LSTool.Tools.Beams.BeamRebar.models;
using LSTool.Tools.Beams.BeamRebar.types;
using LSTool.Utils;

namespace LSTool.Tools.Beams.BeamRebar.action
{
    /// <summary>
    /// Thép hông (SideBar): dầm cao &gt;= 700 mm mới có. SideBar.Spacing = số thanh mỗi bên.
    /// Mỗi bên 1 Rebar Set chia đều theo phương đứng giữa mép dưới lớp thép trên và mép trên lớp thép dưới,
    /// chạy suốt nhịp, neo vào gối min(15d, chiều dày gối − cover); đầu tự do lùi vào 1 lớp bảo vệ.
    /// </summary>
    public partial class BeamRebarAction
    {
        private const double SideBarMinHeightMm = 700;
        private const double SideBarAnchorFactor = 15;

        private void CreateSideBar()
        {
            var beams = _viewModel.BeamRebarModels;
            if (beams == null || !beams.Any()) return;
            if (!beams.Any(b => b.Height >= SideBarMinHeightMm && (b.SectionStart?.SideBar?.Spacing ?? 0) > 0)) return;

            var rebarBarTypes = new FilteredElementCollector(_document)
                .WhereElementIsElementType()
                .OfClass(typeof(RebarBarType))
                .Cast<RebarBarType>()
                .Where(x => x.Name.Contains("D"))
                .OrderBy(x => x.Name)
                .ToList();

            foreach (var beam in beams)
            {
                try { _installSideBar(beam, rebarBarTypes); }
                catch (Exception ex) { _mainBarWarnings.Add($"Thép hông {beam.Name}: {ex.Message}"); }
            }
        }

        private void _installSideBar(BeamRebarModel beam, List<RebarBarType> rebarBarTypes)
        {
            if (!_sideBarLayout(beam, out var n, out var dMm, out var halfYFt, out var z0, out var spacing, out var warn))
            {
                if (warn != null) _mainBarWarnings.Add($"Thép hông {beam.Name}: {warn}");
                return;
            }
            var sb = beam.SectionStart.SideBar;
            var arrayLength = (n - 1) * spacing;

            var vtx = beam.VTX;
            var halfLen = beam.Length.FromMillimeters() / 2;
            var faceStart = beam.Center - vtx * halfLen;
            var faceEnd = beam.Center + vtx * halfLen;
            var pStart = _sideBarEnd(beam, beam.BeamBearingStart, faceStart, -vtx, dMm);
            var pEnd = _sideBarEnd(beam, beam.BeamBearingEnd, faceEnd, vtx, dMm);

            var host = _document.GetElement(beam.Id);
            if (n == 1)
            {
                // 1 thanh mỗi bên → gộp 2 thanh trái/phải thành 1 Rebar Set (phương rải VTY)
                var offL = beam.VTY * (-halfYFt) + beam.VTZ * z0;
                var curvesLR = new List<XYZ> { pStart + offL, pEnd + offL }.PointsToCurves();
                var setLR = RebarHelper.CreateRebarFixedNumberSet(
                    _document, curvesLR, sb.Name, beam.VTY, rebarBarTypes, host,
                    2, 2 * halfYFt, out string errorLR);
                if (setLR == null)
                    _mainBarWarnings.Add($"Thép hông {beam.Name}: tạo lỗi ({errorLR}).");
                return;
            }
            foreach (var side in new[] { -1.0, 1.0 })
            {
                var off = beam.VTY * (side * halfYFt) + beam.VTZ * z0;
                var curves = new List<XYZ> { pStart + off, pEnd + off }.PointsToCurves();
                // phương rải = VTZ, thanh đầu ở dưới cùng, rải lên trên
                var set = RebarHelper.CreateRebarFixedNumberSet(
                    _document, curves, sb.Name, beam.VTZ, rebarBarTypes, host,
                    n, arrayLength, out string error);
                if (set == null)
                    _mainBarWarnings.Add($"Thép hông {beam.Name}: tạo lỗi ({error}).");
            }
        }

        /// <summary>
        /// Thông số bố trí thép hông: n = số thanh mỗi bên, halfYFt = offset ngang (VTY) của thanh,
        /// z0 = cao độ (VTZ, ft) của thanh dưới cùng, spacing = khoảng cách đứng giữa các thanh (ft).
        /// Trả về false khi dầm thấp &lt; 700, không có thép hông hoặc không đủ chỗ (warn != null).
        /// </summary>
        private bool _sideBarLayout(
            BeamRebarModel beam, out int n, out double dMm, out double halfYFt,
            out double z0, out double spacing, out string warn)
        {
            n = 0; dMm = 0; halfYFt = 0; z0 = 0; spacing = 0; warn = null;
            if (beam.Height < SideBarMinHeightMm) return false;
            var sec = beam.SectionStart;
            var sb = sec?.SideBar;
            n = sb?.Spacing ?? 0;
            if (n <= 0) return false;
            dMm = _ParseDiameterMm(sb.Name);
            if (dMm <= 0) return false;

            var coverMm = beam.Cover;
            var dStMm = _ParseDiameterMm(sec.Stirrup?.Name);
            halfYFt = (beam.Width / 2 - coverMm - dStMm - dMm / 2).FromMillimeters();
            if (halfYFt <= 0) return false;

            // mép dưới của lớp thép trên / mép trên của lớp thép dưới (xét cả 3 mặt cắt)
            var zTop = (beam.Height / 2 - coverMm - dStMm).FromMillimeters();
            var zBot = -zTop;
            foreach (var s in new[] { beam.SectionStart, beam.SectionMid, beam.SectionEnd })
            {
                if (s == null) continue;
                foreach (BeamRebarLayerType layer in Enum.GetValues(typeof(BeamRebarLayerType)))
                {
                    if (_getLayerQty(s, layer) <= 0) continue;
                    var d = _ParseDiameterMm(_getLayerRebar(s, layer)?.Name).FromMillimeters();
                    if (d <= 0) continue;
                    var z = _layerOffsetZ(beam, s, layer);
                    if (_isTopLayer(layer)) zTop = Math.Min(zTop, z - d / 2);
                    else zBot = Math.Max(zBot, z + d / 2);
                }
            }
            var gap = zTop - zBot;
            var dFt = dMm.FromMillimeters();
            if (gap <= dFt * (n + 1))
            {
                warn = "không đủ chỗ giữa lớp thép trên và dưới.";
                return false;
            }
            spacing = gap / (n + 1);
            z0 = zBot + spacing;
            return true;
        }

        /// <summary>Đầu thép hông: có gối → neo min(15d, dày gối − cover); đầu tự do → lùi vào cover.</summary>
        private static XYZ _sideBarEnd(BeamRebarModel beam, BeamBearingModel bearing, XYZ face, XYZ outward, double dMm)
        {
            var coverFt = beam.Cover.FromMillimeters();
            if (bearing != null && bearing.Thickness > 0)
            {
                var anchorMm = Math.Min(SideBarAnchorFactor * dMm, Math.Max(bearing.Thickness - beam.Cover, 0));
                return face + outward * anchorMm.FromMillimeters();
            }
            return face - outward * coverFt;
        }
    }
}
