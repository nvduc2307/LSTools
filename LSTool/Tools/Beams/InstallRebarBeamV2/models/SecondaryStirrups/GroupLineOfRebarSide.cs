using Autodesk.Revit.DB;
using HcBimUtils;
using HcBimUtils.GeometryUtils;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.models.SecondaryStirrups
{
    public class GroupLineOfRebarSide(XYZ vectorY) : IEqualityComparer<Line>
    {
        public bool Equals(Line x, Line y)
        {
            var sp1 = x.SP();
            var ep1 = x.EP();
            var sp2 = y.SP();
            var ep2 = y.EP();

            if (!x.Direction.IsAlmostEqualTo(y.Direction))
            {
                (sp2, ep2) = (ep2, sp2);
            }
            var plane = BPlane.CreateByNormalAndOrigin(vectorY, XYZ.Zero);
            var spX = sp1.ProjectOnto(plane);
            var spY = sp2.ProjectOnto(plane);
            var epX = ep1.ProjectOnto(plane);
            var epY =ep2.ProjectOnto(plane);
            return spX.DistanceTo(spY) < 1.MmToFoot() && epX.DistanceTo(epY)  < 1.MmToFoot();
        }

        public int GetHashCode(Line x)
        {
            return 0;
        }
    }
}


