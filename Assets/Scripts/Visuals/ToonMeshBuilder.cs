using System;
using System.Collections.Generic;
using UnityEngine;

namespace GazeLab
{
    public sealed class ToonMeshBuilder
    {
        readonly List<Vector3> positions = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Color> colors = new List<Color>();
        readonly List<Vector2> glows = new List<Vector2>();
        Matrix4x4 part = Matrix4x4.identity;
        Matrix4x4 partNormals = Matrix4x4.identity;

        public Color Color = Color.white;
        public float Glow;
        public float Gloss = 1f;
        public Func<Vector3, Color> Paint;

        public ToonMeshBuilder Part(Vector3 position, Vector3 euler, Vector3 scale)
        {
            return Part(Matrix4x4.TRS(position, Quaternion.Euler(euler), scale));
        }

        public ToonMeshBuilder Part(Vector3 position) { return Part(Matrix4x4.Translate(position)); }

        public ToonMeshBuilder Part(Matrix4x4 placement)
        {
            part = placement;
            partNormals = placement.inverse.transpose;
            return this;
        }

        public ToonMeshBuilder Style(Color color, float glow = 0f)
        {
            Color = color;
            Glow = glow;
            Paint = null;
            return this;
        }

        public void Box(Vector3 size)
        {
            Vector3 h = size * 0.5f;
            Vector3[] c =
            {
                new Vector3(-h.x, -h.y, -h.z), new Vector3(h.x, -h.y, -h.z), new Vector3(h.x, h.y, -h.z), new Vector3(-h.x, h.y, -h.z),
                new Vector3(-h.x, -h.y, h.z), new Vector3(h.x, -h.y, h.z), new Vector3(h.x, h.y, h.z), new Vector3(-h.x, h.y, h.z)
            };
            int[,] faces = { { 0, 1, 2, 3 }, { 5, 4, 7, 6 }, { 4, 0, 3, 7 }, { 1, 5, 6, 2 }, { 3, 2, 6, 7 }, { 4, 5, 1, 0 } };
            for (int f = 0; f < 6; f++) Quad(c[faces[f, 0]], c[faces[f, 1]], c[faces[f, 2]], c[faces[f, 3]], Vector3.zero);
        }

        public void Frustum(int sides, float y0, float r0, float y1, float r1, bool smooth = true, bool capBottom = true, bool capTop = true)
        {
            float height = y1 - y0;
            var inside = new Vector3(0f, (y0 + y1) * 0.5f, 0f);
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides, b = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 da = Ring(a), db = Ring(b);
                Vector3 p0 = da * r0 + Vector3.up * y0, p1 = db * r0 + Vector3.up * y0;
                Vector3 p2 = db * r1 + Vector3.up * y1, p3 = da * r1 + Vector3.up * y1;
                if (smooth)
                {
                    Vector3 na = (da * height + Vector3.up * (r0 - r1)).normalized;
                    Vector3 nb = (db * height + Vector3.up * (r0 - r1)).normalized;
                    if (r0 > 0f) Triangle(p0, p1, p2, inside, na, nb, nb);
                    if (r1 > 0f) Triangle(p0, p2, p3, inside, na, nb, na);
                }
                else
                {
                    if (r0 > 0f) Triangle(p0, p1, p2, inside);
                    if (r1 > 0f) Triangle(p0, p2, p3, inside);
                }
                if (capBottom && r0 > 0f) Triangle(Vector3.up * y0, p1, p0, inside);
                if (capTop && r1 > 0f) Triangle(Vector3.up * y1, p3, p2, inside);
            }
        }

        public void Dome(int sides, int rings, float radius)
        {
            for (int r = 0; r < rings; r++)
            {
                float t0 = r * Mathf.PI * 0.5f / rings, t1 = (r + 1) * Mathf.PI * 0.5f / rings;
                for (int i = 0; i < sides; i++)
                {
                    float a = i * Mathf.PI * 2f / sides, b = (i + 1) * Mathf.PI * 2f / sides;
                    Vector3 p0 = Spherical(a, t0), p1 = Spherical(b, t0), p2 = Spherical(b, t1), p3 = Spherical(a, t1);
                    Triangle(p0 * radius, p1 * radius, p2 * radius, Vector3.zero, p0, p1, p2);
                    if (r < rings - 1) Triangle(p0 * radius, p2 * radius, p3 * radius, Vector3.zero, p0, p2, p3);
                }
            }
        }

        public void Slab(Vector2[] outline, float thickness)
        {
            float z = thickness * 0.5f;
            Vector2 centre = Vector2.zero;
            foreach (Vector2 p in outline) centre += p;
            centre /= outline.Length;
            var inside = new Vector3(centre.x, centre.y, 0f);
            for (int i = 0; i < outline.Length; i++)
            {
                Vector2 a = outline[i], b = outline[(i + 1) % outline.Length];
                Quad(new Vector3(a.x, a.y, -z), new Vector3(b.x, b.y, -z), new Vector3(b.x, b.y, z), new Vector3(a.x, a.y, z), inside);
                if (i == 0 || i == outline.Length - 1) continue;
                Vector2 o = outline[0];
                Triangle(new Vector3(o.x, o.y, -z), new Vector3(a.x, a.y, -z), new Vector3(b.x, b.y, -z), inside);
                Triangle(new Vector3(o.x, o.y, z), new Vector3(a.x, a.y, z), new Vector3(b.x, b.y, z), inside);
            }
        }

        public void Sphere(float radius, int subdivisions = 1)
        {
            List<Vector3> dirs;
            List<int> faces;
            Icosphere(subdivisions, out dirs, out faces);
            for (int f = 0; f < faces.Count; f += 3)
            {
                Vector3 a = dirs[faces[f]], b = dirs[faces[f + 1]], c = dirs[faces[f + 2]];
                Triangle(a * radius, b * radius, c * radius, Vector3.zero, a, b, c);
            }
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 inside)
        {
            Triangle(a, b, c, inside);
            Triangle(a, c, d, inside);
        }

        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 inside)
        {
            AddTriangle(a, b, c, inside, null, null, null);
        }

        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 inside, Vector3 na, Vector3 nb, Vector3 nc)
        {
            AddTriangle(a, b, c, inside, na, nb, nc);
        }

        void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 inside, Vector3? na, Vector3? nb, Vector3? nc)
        {
            a = part.MultiplyPoint3x4(a);
            b = part.MultiplyPoint3x4(b);
            c = part.MultiplyPoint3x4(c);
            inside = part.MultiplyPoint3x4(inside);
            Vector3 face = Vector3.Cross(b - a, c - a);
            if (face.sqrMagnitude < 1e-12f) return;
            Vector3 centre = (a + b + c) / 3f;
            Vector3 normalA = face, normalB = face, normalC = face;
            if (na.HasValue)
            {
                normalA = partNormals.MultiplyVector(na.Value);
                normalB = partNormals.MultiplyVector(nb.Value);
                normalC = partNormals.MultiplyVector(nc.Value);
            }
            if (Vector3.Dot(face, centre - inside) < 0f)
            {
                Vector3 t = b; b = c; c = t;
                t = normalB; normalB = normalC; normalC = t;
                if (!na.HasValue) normalA = normalB = normalC = -face;
            }
            Color color = Paint != null ? Paint(centre) : Color;
            color.a = Gloss;
            Add(a, normalA, color);
            Add(b, normalB, color);
            Add(c, normalC, color);
        }

        void Add(Vector3 position, Vector3 normal, Color color)
        {
            positions.Add(position);
            normals.Add(normal.normalized);
            colors.Add(color);
            glows.Add(new Vector2(Glow, 0f));
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name };
            if (positions.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetUVs(1, glows);
            mesh.SetUVs(2, OutlineNormals());
            var indices = new int[positions.Count];
            for (int i = 0; i < indices.Length; i++) indices[i] = i;
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        List<Vector3> OutlineNormals()
        {
            var sums = new Dictionary<Vector3Int, Vector3>();
            var keys = new Vector3Int[positions.Count];
            for (int i = 0; i < positions.Count; i += 3)
            {
                Vector3 face = Vector3.Cross(positions[i + 1] - positions[i], positions[i + 2] - positions[i]).normalized;
                for (int k = i; k < i + 3; k++)
                {
                    Vector3 p = positions[k] * 1000f;
                    keys[k] = new Vector3Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Mathf.RoundToInt(p.z));
                    Vector3 sum;
                    sums.TryGetValue(keys[k], out sum);
                    sums[keys[k]] = sum + face;
                }
            }
            var result = new List<Vector3>(positions.Count);
            for (int k = 0; k < positions.Count; k++) result.Add(sums[keys[k]].normalized);
            return result;
        }

        static Vector3 Ring(float angle) { return new Vector3(Mathf.Sin(angle), 0f, -Mathf.Cos(angle)); }

        static Vector3 Spherical(float angle, float elevation)
        {
            return Ring(angle) * Mathf.Cos(elevation) + Vector3.up * Mathf.Sin(elevation);
        }

        public static void Icosphere(int subdivisions, out List<Vector3> dirs, out List<int> faces)
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            dirs = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            for (int i = 0; i < dirs.Count; i++) dirs[i] = dirs[i].normalized;
            faces = new List<int>
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
            };
            for (int s = 0; s < subdivisions; s++)
            {
                var midpoints = new Dictionary<long, int>();
                var next = new List<int>(faces.Count * 4);
                for (int f = 0; f < faces.Count; f += 3)
                {
                    int a = faces[f], b = faces[f + 1], c = faces[f + 2];
                    int ab = Midpoint(dirs, midpoints, a, b), bc = Midpoint(dirs, midpoints, b, c), ca = Midpoint(dirs, midpoints, c, a);
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                faces = next;
            }
        }

        static int Midpoint(List<Vector3> dirs, Dictionary<long, int> cache, int a, int b)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            int index;
            if (cache.TryGetValue(key, out index)) return index;
            dirs.Add(((dirs[a] + dirs[b]) * 0.5f).normalized);
            cache[key] = dirs.Count - 1;
            return dirs.Count - 1;
        }
    }
}
