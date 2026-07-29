using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using LSTool.Tools.Columns.ColumnRebar.models;
using LSTool.Tools.Generals.SettingRebarStandard.models;
using LSTool.Utils;
using System.IO;

namespace LSTool.Tools.Columns.ColumnRebar.actions
{
    public class ColumnConcreteAction
    {
        private double _cover = 50;
        private UIDocument _uidocument;
        private Document _document;
        public Action QtyActionChange { get; set; }
        public ColumnConcreteAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
        }
        public List<FamilyInstance> SelectColumns()
        {
            //selete beams
            var elements = _uidocument.Selection.PickElements(_document, null, _columnSelectedFilter);
            //valid beams
            var columns = ValidateColumns(elements);
            if (!columns.Any()) return columns;
            return columns;
        }
        public List<ColumnConcreteModel> GetColumnConcreteModels(
            List<FamilyInstance> columns,
            SettingRebarStandardModelUI standard)
        {
            _cover = standard.CoverC;
            var results = new List<ColumnConcreteModel>();
            if (!columns.Any()) return results;
            var diameters = new FilteredElementCollector(_document)
                .WhereElementIsElementType()
                .OfClass(typeof(RebarBarType))
                .Cast<RebarBarType>()
                .Where(x => x.Name.Contains("D"))
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .ToList();
            foreach (var cl in columns)
            {
                try
                {
                    var transform = cl.GetTransform();
                    var vtx = transform.BasisX;
                    var vtz = transform.BasisZ;
                    var vty = vtx.CrossProduct(vtz);
                    var index = columns.IndexOf(cl) + 1;
                    var ccM = new ColumnConcreteModel
                    {
                        Name = $"Item{index}",
                        Id = cl.UniqueId,
                        Cover = _cover,
                        VTX = vtx,
                        VTY = vty,
                        VTZ = vtz,
                        Ties = new List<List<ColumnStirrupPosition>>()
                    };
                    GetDistanceColumn(
                        cl,
                        vtx,
                        vty,
                        vtz,
                        out XYZ center,
                        out double width,
                        out double height,
                        out double length);
                    if (center == null) throw new Exception("center is null");
                    if (width == 0) throw new Exception("width is 0");
                    if (height == 0) throw new Exception("height is 0");
                    if (length == 0) throw new Exception("length is 0");

                    GetFaceColumn(
                        cl,
                        vtx,
                        vty,
                        vtz,
                        center,
                        width,
                        height,
                        length,
                        out ColumnFaceModel fLeft,
                        out ColumnFaceModel fTop,
                        out ColumnFaceModel fRight,
                        out ColumnFaceModel fBottom);
                    if (fLeft == null) throw new Exception("fLeft is null");
                    if (fTop == null) throw new Exception("fTop is null");
                    if (fRight == null) throw new Exception("fRight is null");
                    if (fBottom == null) throw new Exception("fBottom is null");
                    ccM.Center = center;
                    ccM.Width = width;
                    ccM.Height = height;
                    ccM.Length = length;
                    ccM.FaceLeft = fLeft;
                    ccM.FaceTop = fTop;
                    ccM.FaceRight = fRight;
                    ccM.FaceBottom = fBottom;

                    GetRebarSetting(
                        cl,
                        out string dx_diameter,
                        out double dx_spacing,
                        out string dy_diameter,
                        out double dy_spacing,
                        out string ts_diameter,
                        out double ts_spacing,
                        out double ts_spacing_end);
                    ccM.DiameterDXs = [.. diameters];
                    ccM.DiameterDX =
                        ccM.DiameterDXs.FirstOrDefault(x => x == dx_diameter)
                        ?? ccM.DiameterDXs.FirstOrDefault();
                    ccM.SpacingDX = dx_spacing;

                    ccM.DiameterDYs = [.. diameters];
                    ccM.DiameterDY =
                        ccM.DiameterDYs.FirstOrDefault(x => x == dy_diameter)
                        ?? ccM.DiameterDYs.FirstOrDefault();
                    ccM.SpacingDY = dy_spacing;

                    ccM.DiameterSTs = [.. diameters];
                    ccM.DiameterST =
                        ccM.DiameterSTs.FirstOrDefault(x => x == ts_diameter)
                        ?? ccM.DiameterSTs.FirstOrDefault();
                    ccM.SpacingST = ts_spacing;
                    ccM.SpacingSTE = ts_spacing_end;
                    ccM.SpacingDXAction = QtyActionChange;
                    ccM.SpacingDYAction = QtyActionChange;
                    results.Add(ccM);
                }
                catch (Exception ex)
                {
                    IO.ShowWarning(ex.Message);
                }
            }
            return results;
        }
        public void SetRebarSetting(
            Document document,
            List<ColumnConcreteModel> cls)
        {
            using (var ts = new SubTransaction(document))
            {
                ts.Start();
                foreach (var item in cls)
                {
                    try
                    {
                        var cl = document.GetElement(item.Id);
                        var par_dx_diameter = cl.LookupParameter(ColumnConcreteModelParameterName.LS_DX_Diameter);
                        var par_dx_spacing = cl.LookupParameter(ColumnConcreteModelParameterName.LS_DX_Spacing);

                        var par_dy_diameter = cl.LookupParameter(ColumnConcreteModelParameterName.LS_DY_Diameter);
                        var par_dy_spacing = cl.LookupParameter(ColumnConcreteModelParameterName.LS_DY_Spacing);

                        var par_ts_diameter = cl.LookupParameter(ColumnConcreteModelParameterName.LS_ST_Diameter);
                        var par_ts_spacing = cl.LookupParameter(ColumnConcreteModelParameterName.LS_ST_Spacing);
                        var par_ts_spacing_end = cl.LookupParameter(ColumnConcreteModelParameterName.LS_ST_Spacing_End);

                        if (par_dx_diameter == null) throw new Exception();
                        if (par_dx_spacing == null) throw new Exception();
                        if (par_dy_diameter == null) throw new Exception();
                        if (par_dy_spacing == null) throw new Exception();
                        if (par_ts_diameter == null) throw new Exception();
                        if (par_ts_spacing == null) throw new Exception();
                        if (par_ts_spacing_end == null) throw new Exception();
                        par_dx_diameter.Set(item.DiameterDX);
                        par_dx_spacing.Set(item.SpacingDX.FromMillimeters());

                        par_dy_diameter.Set(item.DiameterDY);
                        par_dy_spacing.Set(item.SpacingDY.FromMillimeters());

                        par_ts_diameter.Set(item.DiameterST);
                        par_ts_spacing.Set(item.SpacingST.FromMillimeters());
                        par_ts_spacing_end.Set(item.SpacingSTE.FromMillimeters());
                    }
                    catch (Exception ex)
                    {
                        IO.ShowWarning(ex.Message);
                    }
                }
                ts.Commit();
            }
        }
        public void ValidateShareParameter()
        {
            var pathShareParameter = $"{PathHelper.Templates}\\ShareParameterConcreteColumn.txt";
            if (!File.Exists(pathShareParameter)) return;
            using (var ts = new Transaction(_document, "AddParameter"))
            {
                ts.SkipAllWarnings();
                ts.Start();
                ParameterHelper
                    .CreateSharedParameters(
                        _document,
                        pathShareParameter,
                        BuiltInCategory.OST_StructuralColumns);
                _document.Regenerate();
                ts.Commit();
            }
        }
        private void GetRebarSetting(
            FamilyInstance cl,
            out string dx_diameter,
            out double dx_spacing,
            out string dy_diameter,
            out double dy_spacing,
            out string ts_diameter,
            out double ts_spacing,
            out double ts_spacing_end)
        {
            dx_diameter = "D10";
            dx_spacing = 5;
            dy_diameter = "D10";
            dy_spacing = 5;
            ts_diameter = "D10";
            ts_spacing = 100;
            ts_spacing_end = 100;
            try
            {
                var par_dx_diameter = cl.LookupParameter(ColumnConcreteModelParameterName.LS_DX_Diameter);
                var par_dx_spacing = cl.LookupParameter(ColumnConcreteModelParameterName.LS_DX_Spacing);

                var par_dy_diameter = cl.LookupParameter(ColumnConcreteModelParameterName.LS_DY_Diameter);
                var par_dy_spacing = cl.LookupParameter(ColumnConcreteModelParameterName.LS_DY_Spacing);

                var par_ts_diameter = cl.LookupParameter(ColumnConcreteModelParameterName.LS_ST_Diameter);
                var par_ts_spacing = cl.LookupParameter(ColumnConcreteModelParameterName.LS_ST_Spacing);
                var par_ts_spacing_end = cl.LookupParameter(ColumnConcreteModelParameterName.LS_ST_Spacing_End);

                if (par_dx_diameter == null) throw new Exception();
                if (par_dx_spacing == null) throw new Exception();
                if (par_dy_diameter == null) throw new Exception();
                if (par_dy_spacing == null) throw new Exception();
                if (par_ts_diameter == null) throw new Exception();
                if (par_ts_spacing == null) throw new Exception();
                if (par_ts_spacing_end == null) throw new Exception();

                dx_diameter = par_dx_diameter.AsString();
                dx_spacing = Math.Round(par_dx_spacing.AsDouble().ToMillimeters(), 0);

                dy_diameter = par_dy_diameter.AsString();
                dy_spacing = Math.Round(par_dy_spacing.AsDouble().ToMillimeters(), 0);

                ts_diameter = par_ts_diameter.AsString();
                ts_spacing = Math.Round(par_ts_spacing.AsDouble().ToMillimeters(), 0);
                ts_spacing_end = Math.Round(par_ts_spacing_end.AsDouble().ToMillimeters(), 0);

                if (string.IsNullOrEmpty(dx_diameter)) throw new Exception();
                if (dx_spacing < 10) throw new Exception();

                if (string.IsNullOrEmpty(dy_diameter)) throw new Exception();
                if (dy_spacing < 10) throw new Exception();

                if (string.IsNullOrEmpty(ts_diameter)) throw new Exception();
                if (ts_spacing < 10) throw new Exception();
                if (ts_spacing_end < 10) throw new Exception();
            }
            catch (Exception)
            {
                dx_diameter = "D10";
                dx_spacing = 5;
                dy_diameter = "D10";
                dy_spacing = 5;
                ts_diameter = "D10";
                ts_spacing = 100;
                ts_spacing_end = 100;
            }
        }
        private void GetFaceColumn(
            FamilyInstance cl,
            XYZ vtx,
            XYZ vty,
            XYZ vtz,
            XYZ center,
            double width,
            double height,
            double length,
            out ColumnFaceModel fLeft,
            out ColumnFaceModel fTop,
            out ColumnFaceModel fRight,
            out ColumnFaceModel fBottom)
        {
            fLeft = null;
            fTop = null;
            fRight = null;
            fBottom = null;
            try
            {
                var p1b = center
                    - vtx * width.FromMillimeters() / 2
                    - vty * height.FromMillimeters() / 2
                    - vtz * length.FromMillimeters() / 2;
                var p2b = center
                    + vtx * width.FromMillimeters() / 2
                    - vty * height.FromMillimeters() / 2
                    - vtz * length.FromMillimeters() / 2;
                var p3b = center
                    + vtx * width.FromMillimeters() / 2
                    + vty * height.FromMillimeters() / 2
                    - vtz * length.FromMillimeters() / 2;
                var p4b = center
                    - vtx * width.FromMillimeters() / 2
                    + vty * height.FromMillimeters() / 2
                    - vtz * length.FromMillimeters() / 2;

                var p1t = center
                    - vtx * width.FromMillimeters() / 2
                    - vty * height.FromMillimeters() / 2
                    + vtz * length.FromMillimeters() / 2;
                var p2t = center
                    + vtx * width.FromMillimeters() / 2
                    - vty * height.FromMillimeters() / 2
                    + vtz * length.FromMillimeters() / 2;
                var p3t = center
                    + vtx * width.FromMillimeters() / 2
                    + vty * height.FromMillimeters() / 2
                    + vtz * length.FromMillimeters() / 2;
                var p4t = center
                    - vtx * width.FromMillimeters() / 2
                    + vty * height.FromMillimeters() / 2
                    + vtz * length.FromMillimeters() / 2;

                fLeft = new ColumnFaceModel()
                {
                    HostId = cl.UniqueId,
                    FaceType = (int)ColumnFaceType.Left,
                    Pb1 = p4b,
                    Pb2 = p1b,
                    Pt1 = p4t,
                    Pt2 = p1t,
                    Plane = Plane.CreateByNormalAndOrigin(-vtx, p4b)
                };
                fTop = new ColumnFaceModel()
                {
                    HostId = cl.UniqueId,
                    FaceType = (int)ColumnFaceType.Top,
                    Pb1 = p3b,
                    Pb2 = p4b,
                    Pt1 = p3t,
                    Pt2 = p4t,
                    Plane = Plane.CreateByNormalAndOrigin(vty, p3b)
                };
                fRight = new ColumnFaceModel()
                {
                    HostId = cl.UniqueId,
                    FaceType = (int)ColumnFaceType.Right,
                    Pb1 = p2b,
                    Pb2 = p3b,
                    Pt1 = p2t,
                    Pt2 = p3t,
                    Plane = Plane.CreateByNormalAndOrigin(vtx, p2b)
                };
                fBottom = new ColumnFaceModel()
                {
                    HostId = cl.UniqueId,
                    FaceType = (int)ColumnFaceType.Bottom,
                    Pb1 = p1b,
                    Pb2 = p2b,
                    Pt1 = p1t,
                    Pt2 = p2t,
                    Plane = Plane.CreateByNormalAndOrigin(-vty, p1b)
                };
            }
            catch (Exception)
            {
                fLeft = null;
                fTop = null;
                fRight = null;
                fBottom = null;
            }
        }
        private void GetDistanceColumn(
            FamilyInstance cl,
            XYZ vtx,
            XYZ vty,
            XYZ vtz,
            out XYZ center,
            out double width,
            out double height,
            out double length)
        {
            center = null;
            width = 0;
            height = 0;
            length = 0;
            try
            {
                var ps = cl.GetSolid()
                        .Select(x => x.GetPoints())
                        .Aggregate((a, b) => a.Concat(b).ToList())
                        .ToList();
                center = ps.GetCenter();
                var fx = Plane.CreateByNormalAndOrigin(vty, center);
                var fy = Plane.CreateByNormalAndOrigin(vtx, center);
                var fS = Plane.CreateByNormalAndOrigin(vtz, center);

                var minz = ps.Min(x => x.Z);
                var maxz = ps.Max(x => x.Z);
                length = Math.Round(Math.Abs(maxz - minz).ToMillimeters(), 0);

                var psSections = ps
                    .Select(p => p.RayIntersectPlane(fS.Normal, fS))
                    .ToList();
                var psX = psSections
                    .Select(p => p.RayIntersectPlane(fx.Normal, fx))
                    .Distinct(new ComparePoint())
                    .OrderBy(p => p.DotProduct(vtx))
                    .ToList();
                var psY = psSections
                    .Select(p => p.RayIntersectPlane(fy.Normal, fy))
                    .Distinct(new ComparePoint())
                    .OrderBy(p => p.DotProduct(vty))
                    .ToList();
                if (!psX.Any()) throw new Exception();
                if (!psY.Any()) throw new Exception();

                width = Math.Round(psX.FirstOrDefault().DistanceTo(psX.LastOrDefault()).ToMillimeters(), 0);
                height = Math.Round(psY.FirstOrDefault().DistanceTo(psY.LastOrDefault()).ToMillimeters(), 0);

                if (width < 50) throw new Exception("ccM.Width < 50");
                if (height < 50) throw new Exception("ccM.Height < 50");
            }
            catch (Exception)
            {
                center = null;
                width = 0;
                height = 0;
                length = 0;
            }
        }
        private bool _columnSelectedFilter(Element element)
        {
            if (element is not FamilyInstance fa) return false;
            if (fa.Category.BuiltInCategory != BuiltInCategory.OST_StructuralColumns) return false;
            return true;
        }
        private List<FamilyInstance> ValidateColumns(List<Element> elements)
        {
            if (elements == null)
                throw new Exception("Element is not found");
            if (!elements.Any())
                throw new Exception("Element is not found");
            if (elements.Any(x => x is not FamilyInstance))
                throw new Exception("Element is not found");
            var columns = elements
                .Cast<FamilyInstance>()
                .ToList();
            foreach (var column in columns)
            {
                if (!column.GetTransform().BasisZ.IsParallel(XYZ.BasisZ))
                    throw new Exception("Tool chỉ hỗ trợ cột đứng");
            }
            return columns;
        }
    }
}
