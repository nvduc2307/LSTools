using Autodesk.Revit.UI;
using LSTool.MVVM.Models;
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
        public ConcreteModel GetConcreteModel()
        {
            return null;
        }
        public List<FamilyInstance> SelectBeams()
        {
            //selete beams
            var elements = _uidocument.Selection.PickElements(_document, null, _beamSelectedFilter);
            //valid beams
            var beams = ValidateBeams(elements);
            return beams;
        }

        private bool _beamSelectedFilter(Element element)
        {
            if (element is not FamilyInstance fa) return false;
            if (fa.Category.BuiltInCategory != BuiltInCategory.OST_StructuralFraming) return false;
            return true;
        }
        private List<FamilyInstance> ValidateBeams(List<Element> elements)
        {
            var result = new List<FamilyInstance>();
            var toole = 300;
            if (elements == null)
                throw new Exception("Element is not found");
            if (!elements.Any())
                throw new Exception("Element is not found");
            if(elements.Any(x=>x is not FamilyInstance))
                throw new Exception("Element is not found");
            var beams = elements
                .Select(x => x as FamilyInstance)
                .Where(x=>x!= null)
                .ToList();
            if (beams == null) throw new Exception("Element is not found");
            //validate direction
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
                .OrderBy(x=>x.GetTransform().Origin.DotProduct(dirbf))
                .ToList();
            return result;
        }
    }
}
