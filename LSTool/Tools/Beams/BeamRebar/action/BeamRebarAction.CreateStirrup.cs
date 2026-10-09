using Autodesk.Revit.DB.Structure;
using LSTool.Compatibility;
using LSTool.Tools.Beams.BeamRebar.models;
using LSTool.Utils;

namespace LSTool.Tools.Beams.BeamRebar.action
{
    public partial class BeamRebarAction
    {
        /// <summary>
        /// Tạo thép đai chính cho tất cả nhịp dầm trong danh sách.
        /// Mỗi nhịp chia 3 vùng (Start / Mid / End) theo tỉ lệ LB từ cài đặt tiêu chuẩn.
        /// </summary>
        private void CreateStirrup()
        {
            var beams = _viewModel.BeamRebarModels;
            if (beams == null || !beams.Any()) return;

            // Lấy toàn bộ RebarBarType và RebarHookType một lần duy nhất
            var rebarBarTypes = new FilteredElementCollector(_document)
                .WhereElementIsElementType()
                .OfClass(typeof(RebarBarType))
                .Cast<RebarBarType>()
                .Where(x => x.Name.Contains("D"))
                .OrderBy(x => x.Name)
                .ToList();

            var rebarHookTypes = new FilteredElementCollector(_document)
                .WhereElementIsElementType()
                .OfClass(typeof(RebarHookType))
                .Cast<RebarHookType>()
                .ToList();

            if (!rebarHookTypes.Any())
                throw new Exception("Hook Type is null");
            var hook135 = rebarHookTypes
                .FirstOrDefault(x => Math.Abs(x.HookAngle.ToDegrees() - 135) <= 1);
            if (hook135 == null)
                throw new Exception("Hook 135° is null");

            var lb = _settingBeamModel?.StressZone ?? 0.25; // tỉ lệ zone đầu/cuối (0 < LB ≤ 0.4)
            if (lb <= 0 || lb > 0.4) lb = 0.25;

            using (var ts = new SubTransaction(_document))
            {
                ts.Start();
                foreach (var beam in beams)
                    _installStirrupForBeam(beam, lb, rebarBarTypes, hook135);
                ts.Commit();
            }
        }

        // ── Private helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Tạo thép đai cho 1 nhịp dầm: chia 3 zone bằng LB, vẽ đai từng zone.
        /// </summary>
        private void _installStirrupForBeam(
            BeamRebarModel beam,
            double lb,
            List<RebarBarType> rebarBarTypes,
            RebarHookType hook135)
        {
            try
            {
                var ext = 50.0.FromMillimeters();
                var stirrupS = beam.SectionStart?.Stirrup;
                var stirrupM = beam.SectionMid?.Stirrup;
                var stirrupE = beam.SectionEnd?.Stirrup;
                if (stirrupS == null || stirrupM == null || stirrupE == null) return;

                var dSt = stirrupS.Diameter; // mm – đường kính đai
                if (dSt <= 0) return;

                var spacingS = stirrupS.Spacing; // mm
                var spacingM = stirrupM.Spacing;
                var spacingE = stirrupE.Spacing;
                if (spacingS <= 0 || spacingM <= 0 || spacingE <= 0) return;

                var vtx = beam.VTX; // trục dọc dầm
                var vty = beam.VTY; // VTY = BasisY = hướng ngang tiết diện (Width)
                var vtz = beam.VTZ; // VTZ = Z global = hướng đứng tiết diện (Height ↑)

                // ── 1. Tính điểm đầu / cuối vùng bố trí đai ─────────────────
                var halfLen = beam.Length.FromMillimeters() / 2;
                var bearingS = (beam.BeamBearingStart?.Thickness ?? 0).FromMillimeters() / 2;
                var bearingE = (beam.BeamBearingEnd?.Thickness ?? 0).FromMillimeters() / 2;

                // pStart / pEnd = mặt trong của gối (half-bearing từ mặt đầu dầm)
                var pStart = beam.Center - vtx * halfLen + vtx * ext;
                var pEnd = beam.Center + vtx * halfLen - vtx * ext;
                var netLen = pStart.DistanceTo(pEnd); // feet

                // ── 2. Ranh giới 3 zone ──────────────────────────────────────
                var lbFt = lb * netLen;
                var zoneStartEnd = pStart + vtx * lbFt;  // ranh giới Start→Mid
                var zoneEndStart = pEnd - vtx * lbFt;  // ranh giới Mid→End

                // ── 3. Hình tiết diện đai (base shape) tại pStart ───────────
                // GetBeamDimensions: Width đo theo VTY (ngang ←→), Height đo theo VTZ (đứng ↑)
                var cover = beam.Cover.FromMillimeters();
                var dStFt = dSt.MmToFoot();
                var halfHeight = beam.Height.FromMillimeters() / 2 - cover; // VTZ (chiều đứng, Height)
                var halfWidth  = beam.Width.FromMillimeters() / 2 - cover;  // VTY (chiều ngang, Width)

                // Chiếu tâm dầm lên mặt phẳng tại pStart
                var startPlane = Plane.CreateByNormalAndOrigin(vtx, pStart);
                var origin = beam.Center.RayIntersectPlane(startPlane.Normal, startPlane);

                // Hook ở top-left → loop: topLeft → botLeft → botRight → topRight → topLeft (hook)
                var pTopLeft = origin + vtz * halfHeight - vty * halfWidth;
                var pBotLeft = origin - vtz * halfHeight - vty * halfWidth;
                var pBotRight = origin - vtz * halfHeight + vty * halfWidth;
                var pTopRight = origin + vtz * halfHeight + vty * halfWidth;

                // Hook tail: theo ColumnRebarStirrupAction — vtStart dọc mép trên, vtEnd dọc mép trái
                var vtAlong = (pTopRight - pTopLeft).Normalize(); // dọc mép trên (left→right)
                var vtUp = (pTopLeft - pBotLeft).Normalize(); // dọc mép trái (bot→top)
                var baseShape = new List<XYZ>
                {
                    pTopLeft + vtUp * (dStFt / 2), // end:   nhô lên trên pTopLeft
                    pBotLeft,
                    pBotRight,
                    pTopRight,
                    pTopLeft - vtAlong * (dStFt / 2), // start: lùi vào trên mép trên
                };

                // ── 4. Tạo 3 RebarSet (mỗi zone 1 set) ──────────────────────
                var host = _document.GetElement(beam.Id);

                // Zone lengths (mm) để SetLayoutAsMaximumSpacing biết phạm vi chia đai
                var lenS = pStart.DistanceTo(zoneStartEnd).ToMillimeters();
                var lenM = zoneStartEnd.DistanceTo(zoneEndStart).ToMillimeters();
                var lenE = zoneEndStart.DistanceTo(pEnd).ToMillimeters();

                // Chiếu shape lên từng zone start plane
                List<Curve> ShapeAt(XYZ zonePt)
                {
                    var pl = Plane.CreateByNormalAndOrigin(vtx, zonePt);
                    var pts = baseShape.Select(x => x.RayIntersectPlane(pl.Normal, pl)).ToList();
                    return pts.PointsToCurves();
                }

                RebarHelper.CreateRebarStirrupTieSet(
                    _document, ShapeAt(pStart), stirrupS.Name,
                    vtx, hook135, hook135, rebarBarTypes, host, spacingS, lenS);
                RebarHelper.CreateRebarStirrupTieSet(
                    _document, ShapeAt(zoneStartEnd), stirrupM.Name,
                    vtx, hook135, hook135, rebarBarTypes, host, spacingM, lenM);
                RebarHelper.CreateRebarStirrupTieSet(
                    _document, ShapeAt(zoneEndStart), stirrupE.Name,
                    vtx, hook135, hook135, rebarBarTypes, host, spacingE, lenE);
            }
            catch (Exception)
            {
                // Silent — tiếp tục dầm tiếp theo
            }
        }
    }
}
