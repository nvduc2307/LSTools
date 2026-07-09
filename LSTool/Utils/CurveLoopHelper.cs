namespace LSTool.Utils
{
    public static class CurveLoopHelper
    {
        public static List<Curve> GetCurves(this CurveLoop curveLoop)
        {
            var result = new List<Curve>();
            foreach (var item in curveLoop)
            {
                result.Add(item);
            }
            return result;
        }
        public static List<List<Curve>> GetCurveLoops(this EdgeArrayArray edgeArrayArray)
        {
            var result = new List<List<Curve>>();
            foreach (var edgeArray in edgeArrayArray)
            {
                try
                {
                    if (edgeArray is not EdgeArray edArr) continue;
                    var cs = new List<Curve>();
                    foreach (var ed in edArr)
                    {
                        if (ed is not Edge edge) continue;
                        var cv = edge.AsCurve();
                        cs.Add(cv);
                    }
                    if (cs.Any())
                        result.Add(cs);
                }
                catch (Exception)
                {
                }
            }
            return result;
        }
    }
}
