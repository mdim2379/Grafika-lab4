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
        using var objStream = typeof(ObjectResourceReader).Assembly.GetManifestResourceStream(fullResourceName);
        using var objReader = new StreamReader(objStream);

        List<(int vIdx, int nIdx)> faceVertexInfo = new();

        while (!objReader.EndOfStream)
        {
            var line = objReader.ReadLine();
            line = Regex.Replace(line, @"\s+", " ");

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            var tokens = line.Trim().Split(' ');
            switch (tokens[0])
            {
                case "v":
                    objVertices.Add(tokens.Skip(1).Take(3).Select(s => float.Parse(s, CultureInfo.InvariantCulture))
                        .ToArray());
                    break;

                case "vn":
                    objNormals.Add(tokens.Skip(1).Take(3).Select(s => float.Parse(s, CultureInfo.InvariantCulture))
                        .ToArray());
                    break;

                case "f":
                    foreach (var vert in tokens.Skip(1))
                    {
                        var parts = vert.Split('/');
                        var vIdx = int.Parse(parts[0], CultureInfo.InvariantCulture) - 1;
                        var nIdx = parts.Length == 3 && !string.IsNullOrEmpty(parts[2])
                            ? int.Parse(parts[2], CultureInfo.InvariantCulture) - 1
                            : -1;
                        faceVertexInfo.Add((vIdx, nIdx));
                    }

                    break;
            }
        }

        Dictionary<(int, int), uint> uniqueVertices = new();
        List<float> glVertices = new();
        List<float> glColors = new();
        List<uint> glIndices = new();
        uint index = 0;

        foreach (var (vIdx, nIdx) in faceVertexInfo)
        {
            var key = (vIdx, nIdx);
            if (!uniqueVertices.TryGetValue(key, out var existingIndex))
            {
                var v = objVertices[vIdx];
                var n = nIdx >= 0 ? objNormals[nIdx] : new float[] { 0, 0, 0 };

                glVertices.AddRange(new[] { v[0], v[1], v[2], n[0], n[1], n[2] });
                glColors.AddRange(new[] { 0.0f, 1.0f, 0.0f, 1.0f });

                uniqueVertices[key] = index;
                glIndices.Add(index++);
            }
            else
            {
                glIndices.Add(existingIndex);
            }
        }

        var vao = Gl.GenVertexArray();
        Gl.BindVertexArray(vao);

        uint offsetPos = 0;
        var offsetNormals = offsetPos + 3 * sizeof(float);
        var vertexSize = offsetNormals + 3 * sizeof(float);

        var vbo = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        Gl.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)glVertices.ToArray().AsSpan(),
            BufferUsageARB.StaticDraw);
        Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
        Gl.EnableVertexAttribArray(0);
        Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, vertexSize, (void*)offsetNormals);
        Gl.EnableVertexAttribArray(2);

        var cbo = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, cbo);
        Gl.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)glColors.ToArray().AsSpan(),
            BufferUsageARB.StaticDraw);
        Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
        Gl.EnableVertexAttribArray(1);

        var ebo = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)glIndices.ToArray().AsSpan(),
            BufferUsageARB.StaticDraw);

        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        Gl.BindVertexArray(0);

        return new GlObject(vao, vbo, cbo, ebo, (uint)glIndices.Count, Gl);
    }
}