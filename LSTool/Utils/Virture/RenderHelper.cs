using Autodesk.Revit.DB.DirectContext3D;

namespace LSTool.Utils
{
    public static class RenderHelper
    {
        public static void MapSurfaceBuffer(RenderingBufferStorage buffer, Mesh mesh, double offset)
        {
            var vertexCount = mesh.Vertices.Count;
            var triangleCount = mesh.NumTriangles;

            buffer.VertexBufferCount = vertexCount;
            buffer.PrimitiveCount = triangleCount;

            var vertexBufferSizeInFloats = VertexPosition.GetSizeInFloats() * buffer.VertexBufferCount;
            buffer.FormatBits = VertexFormatBits.Position;
            buffer.VertexBuffer = new VertexBuffer(vertexBufferSizeInFloats);
            buffer.VertexBuffer.Map(vertexBufferSizeInFloats);

            var vertexStream = buffer.VertexBuffer.GetVertexStreamPosition();
            var normals = new List<XYZ>(mesh.NumberOfNormals);

            for (var i = 0; i < mesh.Vertices.Count; i++)
            {
                var normal = RenderGeometryHelper.GetMeshVertexNormal(mesh, i, mesh.DistributionOfNormals);
                normals.Add(normal);
            }

            for (var i = 0; i < mesh.Vertices.Count; i++)
            {
                var vertex = mesh.Vertices[i];
                var normal = normals[i];
                var offsetVertex = vertex + normal * offset;
                var vertexPosition = new VertexPosition(offsetVertex);
                vertexStream.AddVertex(vertexPosition);
            }

            buffer.VertexBuffer.Unmap();
            buffer.IndexBufferCount = triangleCount * IndexTriangle.GetSizeInShortInts();
            buffer.IndexBuffer = new IndexBuffer(buffer.IndexBufferCount);
            buffer.IndexBuffer.Map(buffer.IndexBufferCount);

            var indexStream = buffer.IndexBuffer.GetIndexStreamTriangle();

            for (var i = 0; i < triangleCount; i++)
            {
                var meshTriangle = mesh.get_Triangle(i);
                var index0 = (int)meshTriangle.get_Index(0);
                var index1 = (int)meshTriangle.get_Index(1);
                var index2 = (int)meshTriangle.get_Index(2);
                indexStream.AddTriangle(new IndexTriangle(index0, index1, index2));
            }

            buffer.IndexBuffer.Unmap();
            buffer.VertexFormat = new VertexFormat(buffer.FormatBits);
        }

        public static void MapCurveBuffer(RenderingBufferStorage buffer, IList<XYZ> vertices)
        {
            var vertexCount = vertices.Count;

            buffer.VertexBufferCount = vertexCount;
            buffer.PrimitiveCount = vertexCount - 1;

            var vertexBufferSizeInFloats = VertexPosition.GetSizeInFloats() * buffer.VertexBufferCount;
            buffer.FormatBits = VertexFormatBits.Position;
            buffer.VertexBuffer = new VertexBuffer(vertexBufferSizeInFloats);
            buffer.VertexBuffer.Map(vertexBufferSizeInFloats);

            var vertexStream = buffer.VertexBuffer.GetVertexStreamPosition();

            foreach (var vertex in vertices)
            {
                var vertexPosition = new VertexPosition(vertex);
                vertexStream.AddVertex(vertexPosition);
            }

            buffer.VertexBuffer.Unmap();
            buffer.IndexBufferCount = (vertexCount - 1) * IndexLine.GetSizeInShortInts();
            buffer.IndexBuffer = new IndexBuffer(buffer.IndexBufferCount);
            buffer.IndexBuffer.Map(buffer.IndexBufferCount);

            var indexStream = buffer.IndexBuffer.GetIndexStreamLine();

            for (var i = 0; i < vertexCount - 1; i++)
            {
                indexStream.AddLine(new IndexLine(i, i + 1));
            }

            buffer.IndexBuffer.Unmap();
            buffer.VertexFormat = new VertexFormat(buffer.FormatBits);
            buffer.EffectInstance = new EffectInstance(VertexFormatBits.Position);
        }
    }
}
