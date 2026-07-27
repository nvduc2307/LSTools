using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HcBimUtils;
using HcBimUtils.GeometryUtils.Geometry;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using RIMT.Utils.Compares;
using RIMT.Utils.Geometries;
using RIMT.Utils.Solids;
using System.Diagnostics;

namespace RIMT.Utils.BoundingBoxs
{
    public static class BoxElementUtils
    {
        public static BoxElement GenerateCoordinateAndPointControl(
            this BoxElement boxElement,
            XYZ vtx,
            XYZ vty,
            XYZ vtz,
            BeamFukashi beamFukashi = null)
        {
            try
            {
                if (!vtx.IsParallel(boxElement.VTX)) throw new Exception();
                var ps = new List<XYZ>() {
                    boxElement.BoxElementPoint.P1,
                    boxElement.BoxElementPoint.P2,
                    boxElement.BoxElementPoint.P3,
                    boxElement.BoxElementPoint.P4,
                    boxElement.BoxElementPoint.P5,
                    boxElement.BoxElementPoint.P6,
                    boxElement.BoxElementPoint.P7,
                    boxElement.BoxElementPoint.P8,
                    };
                boxElement.VTX = vtx;
                boxElement.VTY = vty;
                boxElement.VTZ = vtz;

                var pzs = ps
                    .GroupBy(x => Math.Round(x.DotProduct(vtz).FootToMm(), 0))
                    .OrderBy(x => Math.Round(x.FirstOrDefault().DotProduct(vtz).FootToMm(), 0))
                    .ToList();
                var pbs = pzs.FirstOrDefault();
                var pts = pzs.LastOrDefault();

                var pxbs = pbs
                    .GroupBy(x => Math.Round(x.DotProduct(vtx).FootToMm(), 0))
                    .OrderBy(x => Math.Round(x.FirstOrDefault().DotProduct(vtx).FootToMm(), 0))
                    .Select(x => x.OrderBy(x => Math.Round(x.DotProduct(vty).FootToMm(), 0)))
                    .Select(x => x.ToList())
                    .ToList();
                var pxts = pts
                    .GroupBy(x => Math.Round(x.DotProduct(vtx).FootToMm(), 0))
                    .OrderBy(x => Math.Round(x.FirstOrDefault().DotProduct(vtx).FootToMm(), 0))
                    .Select(x => x.OrderBy(x => Math.Round(x.DotProduct(vty).FootToMm(), 0)))
                    .Select(x => x.ToList())
                .ToList();

                boxElement.BoxElementPoint.P1 = beamFukashi == null
                    ? pxbs[0][0]
                    : pxbs[0][0] + vty * beamFukashi.FukashiRight.ValueMm.MmToFoot() + vtz * beamFukashi.FukashiBot.ValueMm.MmToFoot();
                boxElement.BoxElementPoint.P2 = beamFukashi == null
                    ? pxbs[0][1]
                    : pxbs[0][1] - vty * beamFukashi.FukashiLeft.ValueMm.MmToFoot() + vtz * beamFukashi.FukashiBot.ValueMm.MmToFoot();
                boxElement.BoxElementPoint.P3 = beamFukashi == null
                    ? pxbs[1][1]
                    : pxbs[1][1] - vty * beamFukashi.FukashiLeft.ValueMm.MmToFoot() + vtz * beamFukashi.FukashiBot.ValueMm.MmToFoot();
                boxElement.BoxElementPoint.P4 = beamFukashi == null
                    ? pxbs[1][0]
                    : pxbs[1][0] + vty * beamFukashi.FukashiRight.ValueMm.MmToFoot() + vtz * beamFukashi.FukashiBot.ValueMm.MmToFoot();
                boxElement.BoxElementPoint.P5 = beamFukashi == null
                    ? pxts[0][0]
                    : pxts[0][0] + vty * beamFukashi.FukashiRight.ValueMm.MmToFoot() - vtz * beamFukashi.FukashiTop.ValueMm.MmToFoot();
                boxElement.BoxElementPoint.P6 = beamFukashi == null
                    ? pxts[0][1]
                    : pxts[0][1] - vty * beamFukashi.FukashiLeft.ValueMm.MmToFoot() - vtz * beamFukashi.FukashiTop.ValueMm.MmToFoot();
                boxElement.BoxElementPoint.P7 = beamFukashi == null
                    ? pxts[1][1]
                    : pxts[1][1] - vty * beamFukashi.FukashiLeft.ValueMm.MmToFoot() - vtz * beamFukashi.FukashiTop.ValueMm.MmToFoot();
                boxElement.BoxElementPoint.P8 = beamFukashi == null
                    ? pxts[1][0]
                    : pxts[1][0] + vty * beamFukashi.FukashiRight.ValueMm.MmToFoot() - vtz * beamFukashi.FukashiTop.ValueMm.MmToFoot();
            }
            catch (Exception)
            {
            }
            return boxElement;
        }
        public static void GenerateCoordinateBeam(this List<FamilyInstance> familyInstances, out XYZ vtxOut, out XYZ vtyOut, out XYZ vtzOut)
        {
            vtxOut = null;
            vtyOut = null;
            vtzOut = null;
            try
            {
                vtxOut = familyInstances
                    .Select(x =>
                    {
                        var transf = x.GetTransform();
                        return transf.OfVector(XYZ.BasisX);
                    })
                    .GroupBy(x => x, new ComparePoint())
                    .OrderBy(x => x.Count())
                    .Select(x => x.ToList())
                    .LastOrDefault()
                .FirstOrDefault()
                    .Normalize();
                vtyOut = familyInstances
                    .Select(x =>
                    {
                        var transf = x.GetTransform();
                        return transf.OfVector(XYZ.BasisY);
                    })
                    .GroupBy(x => x, new ComparePoint())
                    .OrderBy(x => x.Count())
                    .Select(x => x.ToList())
                    .LastOrDefault()
                    .FirstOrDefault()
                    .Normalize();
                vtzOut = vtxOut.CrossProduct(vtyOut).Normalize();
            }
            catch (Exception)
            {
            }
        }
    }
    public class BoxElement
    {
        private readonly IReadOnlyList<Element> _sourceElements;

        public long Id { get; }
        public string UniqueId { get; }
        public XYZ VTX { get; set; }
        public XYZ VTY { get; set; }
        public XYZ VTZ { get; set; }
        public Element Element { get; }
        public List<Solid> Solids { get; }
        public List<Curve> Curves { get; }
        public Outline Outline { get; set; }
        public Line LineBox { get; private set; }
        public Line LineBoxTop { get; private set; }
        public Line LineBoxBot { get; private set; }
        public Line LineBoxMid { get; private set; }
        public BoxElementPoint BoxElementPoint { get; set; }
        public BoxElement(Element ele)
        {
            Element = ele;
            Id = Element.Id.Value;
            UniqueId = ele.UniqueId;
            _sourceElements = ExpandSourceElements(ele);
            Solids = GetSolids();
            Curves = GetCurves();
            VTX = GetVTX();
            VTY = !VTX.IsParallel(XYZ.BasisZ) ? VTX.CrossProduct(XYZ.BasisZ).Normalize() : VTX.CrossProduct(XYZ.BasisX).Normalize();
            VTZ = VTX.CrossProduct(VTY).Normalize();
            Outline = GetOutLine(out BoxElementPoint boxElementPoint);
            LineBox = Outline != null ? Line.CreateBound(Outline.MinimumPoint, Outline.MaximumPoint) : null;
            var z = Outline != null ? (Outline.MinimumPoint.Z + Outline.MaximumPoint.Z) / 2 : 0;
            LineBoxMid = Outline != null ? Line.CreateBound(Outline.MinimumPoint.EditZ(z), Outline.MaximumPoint.EditZ(z)) : null;
            LineBoxTop = Outline != null ? Line.CreateBound(Outline.MinimumPoint.EditZ(Outline.MaximumPoint.Z), Outline.MaximumPoint) : null;
            LineBoxBot = Outline != null ? Line.CreateBound(Outline.MinimumPoint, Outline.MaximumPoint.EditZ(Outline.MinimumPoint.Z)) : null;
            BoxElementPoint = boxElementPoint;
        }

        public BoxElement(IEnumerable<Element> elements)
        {
            var sourceElements = elements?
                .Where(element => element != null)
                .GroupBy(element => element.Id.Value)
                .Select(group => group.First())
                .ToList() ?? new List<Element>();
            if (sourceElements.Count == 0)
                throw new ArgumentException(
                    "At least one source element is required.",
                    nameof(elements));

            Element = sourceElements[0];
            Id = Element.Id.Value;
            UniqueId = Element.UniqueId;
            _sourceElements = sourceElements;
            Solids = GetSolids();
            Curves = GetCurves();
            VTX = GetVTX();
            VTY = !VTX.IsParallel(XYZ.BasisZ) ? VTX.CrossProduct(XYZ.BasisZ).Normalize() : VTX.CrossProduct(XYZ.BasisX).Normalize();
            VTZ = VTX.CrossProduct(VTY).Normalize();
            Outline = GetOutLine(out BoxElementPoint boxElementPoint);
            LineBox = Outline != null ? Line.CreateBound(Outline.MinimumPoint, Outline.MaximumPoint) : null;
            var z = Outline != null ? (Outline.MinimumPoint.Z + Outline.MaximumPoint.Z) / 2 : 0;
            LineBoxMid = Outline != null ? Line.CreateBound(Outline.MinimumPoint.EditZ(z), Outline.MaximumPoint.EditZ(z)) : null;
            LineBoxTop = Outline != null ? Line.CreateBound(Outline.MinimumPoint.EditZ(Outline.MaximumPoint.Z), Outline.MaximumPoint) : null;
            LineBoxBot = Outline != null ? Line.CreateBound(Outline.MinimumPoint, Outline.MaximumPoint.EditZ(Outline.MinimumPoint.Z)) : null;
            BoxElementPoint = boxElementPoint;
        }

        private static IReadOnlyList<Element> ExpandSourceElements(Element element)
        {
            if (element is not AssemblyInstance assembly)
                return new[] { element };

            return assembly.GetMemberIds()
                .Select(element.Document.GetElement)
                .Where(member => member != null)
                .ToList();
        }

        private List<Solid> GetSolids()
        {
            var results = new List<Solid>();
            try
            {
                foreach (var element in _sourceElements)
                    results.AddRange(element.GetSolidsExtensions());
            }
            catch (Exception)
            {
            }
            return results;
        }
        private Outline GetOutLine(out BoxElementPoint boxElementPoint)
        {
            boxElementPoint = new BoxElementPoint();
            try
            {
                var ps = Curves
                    .Where(x => x is Line)
                    .Select(x => new List<XYZ>() { x.GetEndPoint(0), x.GetEndPoint(1) })
                    .Aggregate((a, b) => a.Concat(b).ToList())
                    .ToList();
                var pxs = ps.OrderBy(x => x.DotProduct(VTX)).ToList();
                var pys = ps.OrderBy(x => x.DotProduct(VTY)).ToList();
                var pzs = ps.OrderBy(x => x.DotProduct(VTZ)).ToList();

                if (pxs.Count <= 0) return null;
                if (pys.Count <= 0) return null;
                if (pzs.Count <= 0) return null;

                var fxStart = new FaceCustom(VTX, pxs.FirstOrDefault());
                var fxEnd = new FaceCustom(VTX, pxs.LastOrDefault());
                var fyStart = new FaceCustom(VTY, pys.FirstOrDefault());
                var fyEnd = new FaceCustom(VTY, pys.LastOrDefault());
                var fzStart = new FaceCustom(VTZ, pzs.FirstOrDefault());
                var fzEnd = new FaceCustom(VTZ, pzs.LastOrDefault());

                var lb1 = fxStart.FaceIntersectFace(fzStart);
                var lb2 = fxEnd.FaceIntersectFace(fzStart);

                var pb1 = lb1.BasePoint.RayPointToFace(fyStart.Normal, fyStart);
                var pb2 = lb1.BasePoint.RayPointToFace(fyEnd.Normal, fyEnd);
                var pb3 = lb2.BasePoint.RayPointToFace(fyEnd.Normal, fyEnd);
                var pb4 = lb2.BasePoint.RayPointToFace(fyStart.Normal, fyStart);
                boxElementPoint.P1 = pb1;
                boxElementPoint.P2 = pb2;
                boxElementPoint.P3 = pb3;
                boxElementPoint.P4 = pb4;

                var lt1 = fxStart.FaceIntersectFace(fzEnd);
                var lt2 = fxEnd.FaceIntersectFace(fzEnd);

                var pt1 = lt1.BasePoint.RayPointToFace(fyStart.Normal, fyStart);
                var pt2 = lt1.BasePoint.RayPointToFace(fyEnd.Normal, fyEnd);
                var pt3 = lt2.BasePoint.RayPointToFace(fyEnd.Normal, fyEnd);
                var pt4 = lt2.BasePoint.RayPointToFace(fyStart.Normal, fyStart);
                boxElementPoint.P5 = pt1;
                boxElementPoint.P6 = pt2;
                boxElementPoint.P7 = pt3;
                boxElementPoint.P8 = pt4;

                return new Outline(pb1, pt3);
            }
            catch (Exception)
            {
                return null;
            }
        }
        private List<Curve> GetCurves()
        {
            var results = new List<Curve>();
            try
            {
                foreach (var element in _sourceElements)
                {
                    var crs = GetCurvesFromElement(element);
                    results.AddRange(crs);
                }
            }
            catch (Exception)
            {
                Debug.WriteLine(Element.Id);
            }
            return results;
        }
        private List<Curve> GetCurvesFromElement(Element ele)
        {
            var results = new List<Curve>();
            if (ele is Rebar rb)
            {
                var crs = rb
                    .GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeAllMultiplanarCurves, 0)
                    .ToList();
                results.AddRange(crs);
            }
            else
            {
                try
                {
                    var crs = ele.GetSolids()
                        .Select(x => x.GetFacesFromSolid())
                        .Aggregate((a, b) => a.Concat(b).ToList())
                        .Select(x => x.GetFirstCurveLoop().ToList())
                        .Select(x => x)
                        .Aggregate((a, b) => a.Concat(b).ToList());
                    results.AddRange(crs);
                }
                catch (Exception)
                {
                }
            }
            return results;
        }
        private XYZ GetVTX()
        {
            var result = new XYZ();
            try
            {
                var l = Curves
                    .Where(x => x is Line)
                    .OrderBy(x => x.Length)
                    .LastOrDefault();
                if (l == null) throw new Exception();
                result = l.Direction();

            }
            catch (Exception)
            {
            }
            return result;
        }
        public XYZ GetCenter()
        {
            XYZ result = null;
            try
            {
                result = LineBox.Midpoint();
            }
            catch (Exception)
            {
            }
            return result;
        }
        public Line GetLineCenter(double extentMm = 0)
        {
            Line result = null;
            try
            {
                var center = GetCenter();
                var faceAlong = new FaceCustom(VTY, center);
                var facePlan = new FaceCustom(VTZ, center);

                var ps = new List<XYZ>() { LineBox.GetEndPoint(0), LineBox.GetEndPoint(1) };
                ps = ps
                    .Select(x => x.RayPointToFace(faceAlong.Normal, faceAlong))
                    .Select(x => x.RayPointToFace(facePlan.Normal, facePlan))
                    .OrderBy(x => x.DotProduct(VTX))
                    .ToList();
                if (ps.FirstOrDefault().IsSame(ps.LastOrDefault()))
                    return result;
                result = Line.CreateBound(ps.FirstOrDefault() - VTX * extentMm.MmToFoot(), ps.LastOrDefault() + VTX * extentMm.MmToFoot());
            }
            catch (Exception)
            {
            }
            return result;
        }
    }
    public class BoxElementPoint
    {
        public XYZ P1 { get; set; }
        public XYZ P2 { get; set; }
        public XYZ P3 { get; set; }
        public XYZ P4 { get; set; }
        public XYZ P5 { get; set; }
        public XYZ P6 { get; set; }
        public XYZ P7 { get; set; }
        public XYZ P8 { get; set; }
    }
}
