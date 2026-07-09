namespace LSTool.Utils
{
    public static class FaceHelper
    {
        public static XYZ GetCenter(this Face face)
        {
            XYZ result = null;
            try
            {
                var ps = face.GetPoints();
                var minx = ps.Min(p => p.X);
                var miny = ps.Min(p => p.Y);
                var minz = ps.Min(p => p.Z);

                var maxx = ps.Max(p => p.X);
                var maxy = ps.Max(p => p.Y);
                var maxz = ps.Max(p => p.Z);

                var min = new XYZ(minx, miny, minz);
                var max = new XYZ(maxx, maxy, maxz);
                result = min.MidPoint(max);
            }
            catch (Exception)
            {
            }
            return result;
        }
    }
}
