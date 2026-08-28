using Autodesk.Revit.UI;
using LSTool.MVVM.Models;
using LSTool.Tools.Beams.BeamRebar.models;
using LSTool.Tools.Beams.BeamRebar.Utils;
using LSTool.Utils;

namespace LSTool.Tools.Beams.BeamRebar.action
{
    public class BeamConcreteAction
    {
        private UIDocument _uidocument;
        private Document _document;
        public BeamConcreteAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
        }

        public List<FamilyInstance> SelectBeams()
        {
            var elements = _uidocument.Selection.PickElements(_document, null, _beamSelectedFilter);
            var beams = ValidateBeams(elements);
            return beams;
        }

        public List<FamilyInstance> ValidateBeams(List<Element> elements)
        {
            var result = new List<FamilyInstance>();
            var toole = 300;
            if (elements == null)
                throw new Exception("Element is not found");
            if (!elements.Any())
                throw new Exception("Element is not found");
            if (elements.Any(x => x is not FamilyInstance))
                throw new Exception("Element is not found");
            var beams = elements
                .Select(x => x as FamilyInstance)
                .Where(x => x != null)
                .ToList();
            if (beams == null) throw new Exception("Element is not found");

            var bf = beams.FirstOrDefault();
            var transfbf = bf?.GetTransform();
            var dirbf = transfbf?.BasisX;
            var fAlong = Plane.CreateByNormalAndOrigin(transfbf?.BasisY, transfbf?.Origin);
            foreach (var b in beams)
            {
                if (b.Id.ToString() == bf.Id.ToString()) continue;
                var transf = b?.GetTransform();
                var dir = transf?.BasisX;
                if (!dir.IsParallel(dirbf))
                    throw new Exception("Các dầm không cùng hướng với nhau");
                var distance = transf.Origin
                    .RayIntersectPlane(fAlong.Normal, fAlong)
                    .DistanceTo(transf.Origin)
                    .ToMillimeters();
                if (distance > toole)
                    throw new Exception($"Các dầm lệch nhau quá {toole}");
            }
            result = beams
                .OrderBy(x => x.GetTransform().Origin.DotProduct(dirbf))
                .ToList();
            return result;
        }

        public List<BeamRebarModel> GetConcreteModels(List<FamilyInstance> objs)
        {
            var results = new List<BeamRebarModel>();
            if (objs == null || !objs.Any()) return results;

            foreach (var beam in objs)
            {
                try
                {
                    var transform = beam.GetTransform();
                    var vtx = transform.BasisX;
                    var vtz = transform.BasisZ;
                    var vty = vtx.CrossProduct(vtz);
                    var index = objs.IndexOf(beam) + 1;

                    BeamRebarUtils.GetBeamDimensions(
                        beam, vtx, vty, vtz,
                        out XYZ center,
                        out double width,
                        out double height,
                        out double length);

                    if (center == null || width <= 0 || height <= 0 || length <= 0)
                        throw new InvalidOperationException($"Không đọc được kích thước dầm [{beam.Id}].");

                    BeamRebarUtils.GetRebarSetting(beam,
                        out RebarModel stirrupStart,
                        out RebarModel stirrupMid,
                        out RebarModel stirrupEnd,
                        out RebarModel top1, out RebarModel top2, out RebarModel top3,
                        out RebarModel bot1, out RebarModel bot2, out RebarModel bot3,
                        out RebarModel sideBar);

                    var model = new BeamRebarModel
                    {
                        Name = $"Dầm {index}",
                        Id = beam.UniqueId,
                        Cover = BeamRebarModel.COVER,
                        Center = center,
                        VTX = vtx,
                        VTY = vty,
                        VTZ = vtz,
                        Width = width,
                        Height = height,
                        Length = length,
                        SectionStart = new BeamRebarSectionModel
                        {
                            Stirrup = stirrupStart,
                            RebarTop1 = top1,
                            RebarTop2 = top2,
                            RebarTop3 = top3,
                            RebarBot1 = bot1,
                            RebarBot2 = bot2,
                            RebarBot3 = bot3,
                            SideBar = sideBar,
                        },
                        SectionMid = new BeamRebarSectionModel
                        {
                            Stirrup = stirrupMid,
                            RebarTop1 = top1,
                            RebarTop2 = top2,
                            RebarTop3 = top3,
                            RebarBot1 = bot1,
                            RebarBot2 = bot2,
                            RebarBot3 = bot3,
                            SideBar = sideBar,
                        },
                        SectionEnd = new BeamRebarSectionModel
                        {
                            Stirrup = stirrupEnd,
                            RebarTop1 = top1,
                            RebarTop2 = top2,
                            RebarTop3 = top3,
                            RebarBot1 = bot1,
                            RebarBot2 = bot2,
                            RebarBot3 = bot3,
                            SideBar = sideBar,
                        },
                    };
                    model.BeamBearingStart = BeamRebarUtils.GetBeamBearing(_document, model, BeamBearingType.Start);
                    model.BeamBearingEnd = BeamRebarUtils.GetBeamBearing(_document, model, BeamBearingType.End);
                    results.Add(model);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Lỗi xử lý dầm [{beam.Id}]: {ex.Message}", ex);
                }
            }

            return results;
        }
        private bool _beamSelectedFilter(Element element)
        {
            if (element is not FamilyInstance fa) return false;
            if (fa.Category.BuiltInCategory != BuiltInCategory.OST_StructuralFraming) return false;
            return true;
        }
    }
}
