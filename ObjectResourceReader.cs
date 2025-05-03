using System.Globalization;
using System.Text.RegularExpressions;
using Silk.NET.OpenGL;

namespace Lab4;

internal class ObjectResourceReader
{
    public static unsafe GlObject CreateObjectFromResource(GL Gl, string resourceName)
    {
        List<float[]> objVertices = new();
        List<float[]> objNormals = new();
        List<int[]> objFaces = new();

        var fullResourceName = "Lab4.Resources." + resourceName;
        using (var objStream = typeof(ObjectResourceReader).Assembly.GetManifestResourceStream(fullResourceName))
        using (var objReader = new StreamReader(objStream))
        {
            while (!objReader.EndOfStream)
            {
                var line = objReader.ReadLine();
                line = Regex.Replace(line, @"\s+", " ");

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var lineClassifier = line.Substring(0, line.IndexOf(' '));
                var lineData = line.Substring(line.IndexOf(' ')).Trim().Split(' ');

                switch (lineClassifier)
                {
                    case "v":
                        var vertex = new float[3];
                        for (var i = 0; i < 3; ++i)
                            vertex[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                        objVertices.Add(vertex);
                        break;

                    case "f":
                        var face = new int[3];
                        for (var i = 0; i < 3; ++i)
                            face[i] = int.Parse(lineData[i], CultureInfo.InvariantCulture) - 1;
                        objFaces.Add(face);
                        break;
                    case "vn":
                        var normal = new float[3];
                        for (var i = 0; i < 3; ++i)
                            normal[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                        objNormals.Add(normal);
                        break;
                }
            }
        }

        if (objVertices.Count != objNormals.Count)
            throw new Exception("Mismatch between vertex count and normal count.");

        List<float> glVertices = new();
        List<float> glColors = new();
        for (var i = 0; i < objVertices.Count; i++)
        {
            var v = objVertices[i];
            var n = objNormals[i];

            glVertices.AddRange(new[] { v[0], v[1], v[2], n[0], n[1], n[2] });
            glColors.AddRange(new[] { 0.0f, 1.0f, 0.0f, 1.0f });
        }

        List<uint> glIndexArray = new();
        foreach (var face in objFaces)
        {
            glIndexArray.Add((uint)face[0]);
            glIndexArray.Add((uint)face[1]);
            glIndexArray.Add((uint)face[2]);
        }

        var vao = Gl.GenVertexArray();
        Gl.BindVertexArray(vao);

        uint offsetPos = 0;
        var offsetNormals = offsetPos + 3 * sizeof(float);
        var vertexSize = offsetNormals + 3 * sizeof(float);

        var vertices = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertices);
        Gl.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)glVertices.ToArray().AsSpan(),
            BufferUsageARB.StaticDraw);
        Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
        Gl.EnableVertexAttribArray(0);
        Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, vertexSize, (void*)offsetNormals);
        Gl.EnableVertexAttribArray(2);

        var colors = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, colors);
        Gl.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)glColors.ToArray().AsSpan(),
            BufferUsageARB.StaticDraw);
        Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
        Gl.EnableVertexAttribArray(1);

        var indices = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indices);
        Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)glIndexArray.ToArray().AsSpan(),
            BufferUsageARB.StaticDraw);

        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        var indexArrayLength = (uint)glIndexArray.Count;

        Gl.BindVertexArray(0);

        return new GlObject(vao, vertices, colors, indices, indexArrayLength, Gl);
    }
}