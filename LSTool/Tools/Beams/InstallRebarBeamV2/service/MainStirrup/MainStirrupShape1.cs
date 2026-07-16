using Autodesk.Revit.DB.Structure;
using HcBimUtils.RebarUtils;
using LSTool.Tools.Beams.InstallRebarBeamV2.models.MainStirrups;
using RIMT.Utils.RevRebars;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.service.MainStirrup
{
    public class MainStirrupShape1(MainStirrupCollectionDto mainStirrupDto) : InstallMainStirrupRebarBeam(mainStirrupDto)
    {
        protected override void PlaceRebar(RectangleDto rectangleDto, int chanLe)
        {
            try
            {
                var point1 = rectangleDto.Transform.OfPoint(rectangleDto.BottomLeft);
                var point2 = rectangleDto.Transform.OfPoint(rectangleDto.BottomRight);
                var point3 = rectangleDto.Transform.OfPoint(rectangleDto.TopRight);
                var point4 = rectangleDto.Transform.OfPoint(rectangleDto.TopLeft);
                var origin = point1;
                var xVec = point2 - point1;
                var yVec = point4 - point1;

                if (chanLe % 2 == 1)
                {
                    origin = point2;
                    xVec = point1 - point2;
                    yVec = point3 - point2;
                }

                var rebar = Rebar.CreateFromRebarShape(MainStirrupDto.Document, ShapeMainStirrup.GetRebarShape1(),
                    MainStirrupDto.RebarBarTypeCustom.RebarBarType, MainStirrupDto.Host
                    , origin, xVec, yVec);
                rebar.RebarScaleToBox(origin, xVec, yVec);
                rebar.SetSolidRebar3DView(MainStirrupDto.Document.ActiveView);
                Rebars.Add(rebar);
            }
            catch (Exception)
            {
            }
        }
    }
}


