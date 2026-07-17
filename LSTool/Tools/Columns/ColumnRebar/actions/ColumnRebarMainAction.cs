using Autodesk.Revit.UI;
using LSTool.Tools.Columns.ColumnRebar.models;
using LSTool.Tools.Generals.SettingRebarStandard.models;
using LSTool.Utils;

namespace LSTool.Tools.Columns.ColumnRebar.actions
{
    public class ColumnRebarMainAction
    {
        private UIDocument _uidocument;
        private Document _document;
        private ColumnRebarAnchorModel _columnRebarAnchorModel;
        private SettingRebarStandardModel _settingRebarStandardModel;
        public ColumnRebarMainAction(
            UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            //_columnRebarAnchorModel = columnRebarAnchorModel;
            //_settingRebarStandardModel = settingRebarStandardModel;
        }
        public void CreateRebarMain(List<ColumnConcreteModel> cCols)
        {
            foreach (ColumnConcreteModel cModel in cCols)
            {
                var qtyX = cModel.SpacingDX;
                var qtyY = cModel.SpacingDY;

                cModel.FaceLeft.RebarQty = qtyY;
                cModel.FaceLeft.RebarQtyNext = qtyX;

                cModel.FaceTop.RebarQty = qtyX;
                cModel.FaceTop.RebarQtyNext = qtyY;

                cModel.FaceRight.RebarQty = qtyY;
                cModel.FaceRight.RebarQtyNext = qtyX;

                cModel.FaceBottom.RebarQty = qtyX;
                cModel.FaceBottom.RebarQtyNext = qtyY;
            }
            var faceLefts = cCols.Select(x => x.FaceLeft).ToList();
            var faceTops = cCols.Select(x => x.FaceTop).ToList();
            var faceRights = cCols.Select(x => x.FaceRight).ToList();
            var faceBots = cCols.Select(x => x.FaceBottom).ToList();
            InstallRebarFace(faceLefts);
            InstallRebarFace(faceTops);
            InstallRebarFace(faceRights);
            InstallRebarFace(faceBots);
        }
        private void InstallRebarFace(List<ColumnFaceModel> faces)
        {
            var qtyMax = faces.Max(x => x.RebarQty);
            foreach (var face in faces)
            {
                var ps = SolvePositionInstallRebar(face.Pb1, face.Pb2, 
                    int.Parse(Math.Round(face.RebarQty, 0).ToString()),
                    int.Parse(Math.Round(qtyMax, 0).ToString()));
                //var cs = ps
                //    .Select(x=> Line.CreateBound(x, x+ XYZ.BasisZ * 100.0.FromMillimeters()))
                //    .Cast<Curve>()
                //    .Where(x=> x!= null)
                //    .ToList();
                //_document.CreateCurves(cs);
            }
        }
        private List<ColumnRebarPositionModel> SolvePositionInstallRebar(XYZ start, XYZ end, int qty, int maxQty)
        {
            var results = new List<ColumnRebarPositionModel>();
            try
            {
                var vt = (end - start).Normalize();
                var distance = start.DistanceTo(end);
                var spacing = (distance / maxQty);
                var qtyDu = qty % 2;
                var haft = (qty - qtyDu) / 2;
                for (int i = 0; i < haft; i++)
                {
                    var p = start + i * spacing * vt;
                    results.Add(new ColumnRebarPositionModel() { Index = i + 1, Position = p });
                }
                if(qtyDu == 1)
                {
                    var p = start.MidPoint(end);
                    results.Add(new ColumnRebarPositionModel() { Index = 1 + maxQty / 2, Position = p });
                }
                for (int i = 0; i < haft; i++)
                {
                    var p = end - i * spacing * vt;
                    results.Add(new ColumnRebarPositionModel() { Index = maxQty - i, Position = p });
                }
            }
            catch (Exception)
            {
            }
            return results;
        }
    }
}
