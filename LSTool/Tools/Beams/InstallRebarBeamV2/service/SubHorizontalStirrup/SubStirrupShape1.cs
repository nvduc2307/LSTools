using Autodesk.Revit.DB.Structure;
using LSTool.Compatibility;
using LSTool.Tools.Beams.InstallRebarBeamV2.models.SecondaryStirrups;
using RIMT.Utils.RevRebars;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.service.SubHorizontalStirrup
{
    public class SubStirrupShape1(SubHorizontalStirrupCollectionDto mainStirrupDto) : InstallSubStirrupRebarBeam(mainStirrupDto)
    {
        protected override void PlaceRebar(LineHorizontalDto lineDto, int chanLe)
        {
            var point1 = lineDto.Transform.OfPoint(lineDto.Left);
            var point2 = lineDto.Transform.OfPoint(lineDto.Right);
            var origin = point1;
            var xVec = point2 - point1;
            var yVec = lineDto.DirectionToInside;
            if (SubStirrupDto.StaggerHooks && chanLe % 2 == 1)
            {
                origin = point2;
                xVec = point1 - point2;
            }

            var rebar = Rebar.CreateFromRebarShape(SubStirrupDto.Document, SecondaryShapeStirrup.GetRebarShape90_135(),
                SubStirrupDto.RebarBarTypeCustom.RebarBarType, SubStirrupDto.Host
                , origin, xVec, yVec);
            rebar.RebarScaleToBox(origin, xVec, yVec);
            rebar.SetSolidRebar3DView(SubStirrupDto.Document.ActiveView);
            Rebars.Add(rebar);
        }
    }
}


