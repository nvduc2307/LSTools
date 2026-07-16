using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HcBimUtils;
using LSTool.Tools.Beams.InstallRebarBeamV2.models.MainStirrups;
using RIMT.Utils.Revit;
using RIMT.Utils.RevRebars;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.service.MainStirrup
{
    public class MainStirrupShape2(MainStirrupCollectionDto mainStirrupDto) : InstallMainStirrupRebarBeam(mainStirrupDto)
    {
        protected override void PlaceRebar(RectangleDto rectangleDto, int chanLe)
        {
            try
            {
                var point1 = rectangleDto.Transform.OfPoint(rectangleDto.BottomLeft);
                var point2 = rectangleDto.Transform.OfPoint(rectangleDto.BottomRight);
                var point3 = rectangleDto.Transform.OfPoint(rectangleDto.TopRight);
                var point4 = rectangleDto.Transform.OfPoint(rectangleDto.TopLeft);
                var direction12 = (point2 - point1).Normalize();

                var direction14 = (point4 - point1).Normalize();
                var diameter = MainStirrupDto.RebarBarTypeCustom.RebarBarType.GetRebarDiameter();

                //offset to the inside by half diameter
                point1 = point1 + direction12 * diameter / 2 + direction14 * diameter / 2;
                point2 = point2 - direction12 * diameter / 2 + direction14 * diameter / 2;
                point3 = point3 - direction12 * diameter / 2 - direction14 * diameter / 2;
                point4 = point4 + direction12 * diameter / 2 - direction14 * diameter / 2;

                var length12 = (point2 - point1).GetLength();
                var pointB = point1;
                var pointA = pointB + direction12 * length12 / 2;
                var pointC = point4;
                var pointD = point3;
                var pointE = point2;
                var pointF = point2 - direction12 * length12 / 2;

                var curves = new List<Curve>
                {
                    pointA.CreateLine(pointB),
                    pointB.CreateLine(pointC),
                    pointC.CreateLine(pointD),
                    pointD.CreateLine(pointE),
                    pointE.CreateLine(pointF)
                };

                var rebar = RebarCreationCompat.CreateFromCurvesAndShape(
                    MainStirrupDto.Document,
                    ShapeMainStirrup.GetRebarShape2(),
                    MainStirrupDto.RebarBarTypeCustom.RebarBarType,
                    MainStirrupDto.Host,
                    MainStirrupDto.Direction,
                    curves);
                RevRebarUtils.SetSolidRebar3DView(rebar, MainStirrupDto.Document.ActiveView);
                Rebars.Add(rebar);
            }
            catch (Exception)
            {
            }
        }
    }
}


