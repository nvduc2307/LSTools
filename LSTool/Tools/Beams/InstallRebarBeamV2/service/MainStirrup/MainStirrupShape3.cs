using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HcBimUtils.RebarUtils;
using LSTool.Tools.Beams.InstallRebarBeamV2.models.MainStirrups;
using LSTool.Tools.Beams.InstallRebarBeamV2.models.SecondaryStirrups;
using RIMT.Utils.RevRebars;
using RebarUtils = RIMT.Utils.Revit.RebarUtils;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.service.MainStirrup
{
    public class MainStirrupShape3(MainStirrupCollectionDto mainStirrupDto) : InstallMainStirrupRebarBeam(mainStirrupDto)
    {
        protected override void PlaceRebar(RectangleDto rectangleDto, int chanLe)
        {
            try
            {
                {
                    var point1 = rectangleDto.Transform.OfPoint(rectangleDto.TopLeft);
                    var point2 = rectangleDto.Transform.OfPoint(rectangleDto.BottomLeft);
                    var point3 = rectangleDto.Transform.OfPoint(rectangleDto.BottomRight);
                    var origin = point2;
                    var xVec = point3 - point2;
                    var yVec = point1 - point2;

                    var rebar = Rebar.CreateFromRebarShape(MainStirrupDto.Document, ShapeMainStirrup.GetRebarShape3(),
                        MainStirrupDto.RebarBarTypeCustom.RebarBarType, MainStirrupDto.Host
                        , origin, xVec, yVec);
                    rebar.RebarScaleToBox(origin, xVec, yVec);
                    rebar.SetSolidRebar3DView(MainStirrupDto.Document.ActiveView);
                    Rebars.Add(rebar);
                }

                {
                    var transform = rectangleDto.Transform.Multiply(Transform.CreateTranslation(MainStirrupDto.Direction *
                        RebarUtils.GetRebarDiameter(MainStirrupDto.RebarBarTypeCustom.RebarBarType)));
                    var point1 = transform.OfPoint(rectangleDto.TopLeft);
                    var point2 = transform.OfPoint(rectangleDto.TopRight);
                    var point3 = transform.OfPoint(rectangleDto.BottomLeft);
                    var origin = point1;
                    var xVec = point2 - point1;
                    var yVec = point3 - point1;

                    if (chanLe % 2 == 1)
                    {
                        origin = point2;
                        xVec = point1 - point2;
                    }

                    var rebar = Rebar.CreateFromRebarShape(MainStirrupDto.Document, SecondaryShapeStirrup.GetRebarShape90_135(),
                        MainStirrupDto.RebarBarTypeCustom.RebarBarType, MainStirrupDto.Host
                        , origin, xVec, yVec);
                    rebar.RebarScaleToBox(origin, xVec, yVec);
                    rebar.SetSolidRebar3DView(MainStirrupDto.Document.ActiveView);
                    Rebars.Add(rebar);
                }

            }
            catch (Exception)
            {
            }
        }
    }
}


