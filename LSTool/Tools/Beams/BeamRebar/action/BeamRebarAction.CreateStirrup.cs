using Autodesk.Revit.DB.Structure;
using LSTool.Compatibility;
using LSTool.Tools.Beams.BeamRebar.models;
using LSTool.Utils;

namespace LSTool.Tools.Beams.BeamRebar.action
{
    public partial class BeamRebarAction
    {
        private void CreateStirrup()
        {
            var beams = _viewModel.BeamRebarModels;
            if (beams == null || !beams.Any()) return;

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
                .Where(x=>x.Style== RebarStyle.StirrupTie)
                .ToList();

            if (!rebarHookTypes.Any())
                throw new Exception("Hook Type is null");
            var hook135 = rebarHookTypes
                .FirstOrDefault(x => Math.Abs(x.HookAngle.ToDegrees() - 135) <= 1);
            if (hook135 == null)
                throw new Exception("Hook 135° is null");

            var lb = _settingBeamModel?.StressZone ?? 0.25;
            if (lb <= 0 || lb > 0.4) lb = 0.25;

            using (var ts = new SubTransaction(_document))
            {
                ts.Start();
                foreach (var beam in beams)
                    _installStirrupForBeam(beam, lb, rebarBarTypes, hook135);
                ts.Commit();
            }
        }


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

                var dSt = stirrupS.Diameter;
                if (dSt <= 0) return;

                var spacingS = stirrupS.Spacing;
                var spacingM = stirrupM.Spacing;
                var spacingE = stirrupE.Spacing;
                if (spacingS <= 0 || spacingM <= 0 || spacingE <= 0) return;

                var vtx = beam.VTX;
                var vty = beam.VTY;
                var vtz = beam.VTZ;

                var halfLen = beam.Length.FromMillimeters() / 2;
                var bearingS = (beam.BeamBearingStart?.Thickness ?? 0).FromMillimeters() / 2;
                var bearingE = (beam.BeamBearingEnd?.Thickness ?? 0).FromMillimeters() / 2;

                var pStart = beam.Center - vtx * halfLen + vtx * ext;
                var pEnd = beam.Center + vtx * halfLen - vtx * ext;
                var netLen = pStart.DistanceTo(pEnd);

                var lbFt = lb * netLen;
                var zoneStartEnd = pStart + vtx * lbFt;
                var zoneEndStart = pEnd - vtx * lbFt;

                var cover = beam.Cover.FromMillimeters();
                var dStFt = dSt.MmToFoot();
                var halfHeight = beam.Height.FromMillimeters() / 2 - cover;
                var halfWidth  = beam.Width.FromMillimeters() / 2 - cover;

                var startPlane = Plane.CreateByNormalAndOrigin(vtx, pStart);
                var origin = beam.Center.RayIntersectPlane(startPlane.Normal, startPlane);

                var pTopLeft = origin + vtz * halfHeight - vty * halfWidth;
                var pBotLeft = origin - vtz * halfHeight - vty * halfWidth;
                var pBotRight = origin - vtz * halfHeight + vty * halfWidth;
                var pTopRight = origin + vtz * halfHeight + vty * halfWidth;

                var vtAlong = (pTopRight - pTopLeft).Normalize();
                var vtUp = (pTopLeft - pBotLeft).Normalize();
                var baseShape = new List<XYZ>
                {
                    pTopLeft + vtUp * (dStFt / 2),
                    pBotLeft,
                    pBotRight,
                    pTopRight,
                    pTopLeft - vtAlong * (dStFt / 2),
                };

                var host = _document.GetElement(beam.Id);

                var lenS = pStart.DistanceTo(zoneStartEnd).ToMillimeters();
                var lenM = zoneStartEnd.DistanceTo(zoneEndStart).ToMillimeters();
                var lenE = zoneEndStart.DistanceTo(pEnd).ToMillimeters();

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
            }
        }
    }
}
