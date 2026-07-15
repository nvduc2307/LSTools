using Autodesk.Revit.UI;
using LSTool.MVVM.Models;
using LSTool.Tools.Columns.ColumnRebar.models;
using LSTool.Utils;

namespace LSTool.Tools.Columns.ColumnRebar.actions
{
    public class ColumnRebarStirrupAction
    {
        private UIDocument _uidocument;
        private Document _document;
        public ColumnRebarStirrupAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
        }
        public void CreateStirrupMain(List<ColumnConcreteModel> ccRInfos)
        {
            using (var ts = new SubTransaction(_document))
            {
                ts.Start();
                foreach (var ccRInfo in ccRInfos)
                {
                    var ps = new List<XYZ>()
                    {
                        ccRInfo.FaceLeft.Pb1,
                        ccRInfo.FaceTop.Pb1,
                        ccRInfo.FaceRight.Pb1,
                        ccRInfo.FaceBottom.Pb1,
                    };
                    var shapes = CurveLoop.CreateViaOffset(ps
                            .PointsToCurveLoop(), ccRInfo.Cover.FromMillimeters(), -ccRInfo.VTZ)
                            .Select(x => x.GetEndPoint(1))
                            .ToList().PointsToCurves(true);
                    _document.CreateCurves(ps.PointsToCurves(true));
                }
                ts.Commit();
            }
        }
    }
}
