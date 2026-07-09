namespace LSTool.Utils
{
    public static class RenderGeometryHelper
    {
        public static List<List<XYZ>> GetSegmentationTube(IList<XYZ> vertices, double diameter)
        {
            var points = new List<List<XYZ>>();

            for (var i = 0; i < vertices.Count; i++)
            {
                var center = vertices[i];
                XYZ normal;
                if (i == 0)
                {
                    normal = (vertices[i + 1] - center).Normalize();
                }
                else if (i == vertices.Count - 1)
                {
                    normal = (center - vertices[i - 1]).Normalize();
                }
                else
                {
                    normal = ((vertices[i + 1] - vertices[i - 1]) / 2.0).Normalize();
                }

                points.Add(TessellateCircle(center, normal, diameter / 2));
            }

            return points;
        }

        public static XYZ GetMeshVertexNormal(Mesh mesh, int index, DistributionOfNormals normalDistribution)
        {
            switch (normalDistribution)
            {
                case DistributionOfNormals.AtEachPoint:
                    return mesh.GetNormal(index);
                case DistributionOfNormals.OnEachFacet:
                    var vertex = mesh.Vertices[index];
                    for (var i = 0; i < mesh.NumTriangles; i++)
                    {
                        var triangle = mesh.get_Triangle(i);
                        var triangleVertex = triangle.get_Vertex(0);
                        if (triangleVertex.IsAlmostEqualTo(vertex)) return mesh.GetNormal(i);
                        triangleVertex = triangle.get_Vertex(1);
                        if (triangleVertex.IsAlmostEqualTo(vertex)) return mesh.GetNormal(i);
                        triangleVertex = triangle.get_Vertex(2);
                        if (triangleVertex.IsAlmostEqualTo(vertex)) return mesh.GetNormal(i);
                    }

                    return XYZ.Zero;
                case DistributionOfNormals.OnePerFace:
                    return mesh.GetNormal(0);
                default:
                    throw new ArgumentOutOfRangeException(nameof(normalDistribution), normalDistribution, null);
            }
        }

        public static List<XYZ> TessellateCircle(XYZ center, XYZ normal, double radius)
        {
            var vertices = new List<XYZ>();
            var segmentCount = InterpolateSegmentsCount(radius);
            var xDirection = normal.CrossProduct(XYZ.BasisZ).Normalize() * radius;
            if (xDirection.IsZeroLength())
            {
                xDirection = normal.CrossProduct(XYZ.BasisX).Normalize() * radius;
            }

            var yDirection = normal.CrossProduct(xDirection).Normalize() * radius;

            for (var i = 0; i < segmentCount; i++)
            {
                var angle = 2 * Math.PI * i / segmentCount;
                var vertex = center + xDirection * Math.Cos(angle) + yDirection * Math.Sin(angle);
                vertices.Add(vertex);
            }

            return vertices;
        }

        public static int InterpolateSegmentsCount(double diameter)
        {
            const int minSegments = 6;
            const int maxSegments = 33;
            const double minDiameter = 0.1 / 12d;
            const double maxDiameter = 3 / 12d;

            if (diameter <= minDiameter) return minSegments;
            if (diameter >= maxDiameter) return maxSegments;

            var normalDiameter = (diameter - minDiameter) / (maxDiameter - minDiameter);
            return (int)(minSegments + normalDiameter * (maxSegments - minSegments));
        }

    }
}
