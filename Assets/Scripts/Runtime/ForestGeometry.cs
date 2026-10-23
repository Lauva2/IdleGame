using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace ForestShop
{
    // Authored mesh construction shared by the world and its catalogue portraits.
    public static class ForestGeometry
    {
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        static readonly HashSet<Mesh> ownedMeshes = new HashSet<Mesh>();
        public static readonly Color Wood = Hex(0xA66E43), Honey = Hex(0xC99256), DarkWood = Hex(0x65472F), Cream = Hex(0xF5E6C3), Leaf = Hex(0x64885A), Moss = Hex(0x829C62), Deep = Hex(0x385D4C), Gold = Hex(0xDEB760), Copper = Hex(0xB5754A), Blue = Hex(0x80B7AF), Ink = Hex(0x302D2B);
        public static Color Hex(uint x) => new Color(((x >> 16) & 255) / 255f, ((x >> 8) & 255) / 255f, (x & 255) / 255f);
        public static Material Mat(Color color, bool glow = false)
        {
            string key = ColorUtility.ToHtmlStringRGBA(color) + glow;
            if (materials.TryGetValue(key, out var material)) return material;
            material = new Material(Resources.Load<Shader>("ForestMatte")) { name = "Forest " + key };
            material.color = color; material.SetFloat("_Glow", glow ? .32f : 0);
            materials[key] = material; return material;
        }
        public static Transform Group(Transform parent, string name, Vector3 position = default(Vector3))
        { var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = position; g.layer = parent == null ? 10 : parent.gameObject.layer; return g.transform; }
        public static Transform Shape(Transform parent, string name, Mesh mesh, Vector3 position, Vector3 scale, Color color, Vector3 rotation = default(Vector3), bool glow = false)
        {
            var g = Group(parent, name, position); g.localScale = scale; g.localEulerAngles = rotation;
            g.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = g.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = Mat(color, glow);
            renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true; return g;
        }
        public static Transform Ball(Transform p, string n, Vector3 v, Vector3 scale, Color c, Vector3 r = default(Vector3)) => Shape(p, n, Sphere(), v, scale, c, r);
        public static Transform Box(Transform p, string n, Vector3 v, Vector3 scale, Color c, Vector3 r = default(Vector3)) => Shape(p, n, BevelBox(), v, scale, c, r);
        public static Transform Tube(Transform p, string n, Vector3 a, Vector3 b, float radius, Color c, float taper = 1)
        {
            var g = Shape(p, n, Lathe(new[] { new Vector2(0, 0), new Vector2(radius, 0), new Vector2(radius * taper, (b - a).magnitude), new Vector2(0, (b - a).magnitude) }, 12), a, Vector3.one, c);
            g.localRotation = Quaternion.FromToRotation(Vector3.up, b - a); return g;
        }
        public static Transform Ring(Transform p, string n, Vector3 at, float radius, float thickness, Color c, Vector3 rotation = default(Vector3))
        {
            string key = "ring" + radius + ":" + thickness;
            if (!meshes.TryGetValue(key, out var mesh))
            {
                var vertices = new List<Vector3>(); var triangles = new List<int>(); const int sides = 24, cross = 6;
                for (int i = 0; i <= sides; i++) for (int j = 0; j <= cross; j++)
                { float a = i * Mathf.PI * 2 / sides, b = j * Mathf.PI * 2 / cross; vertices.Add(new Vector3(Mathf.Cos(a) * (radius + Mathf.Cos(b) * thickness), Mathf.Sin(b) * thickness, Mathf.Sin(a) * (radius + Mathf.Cos(b) * thickness))); }
                for (int i = 0; i < sides; i++) for (int j = 0; j < cross; j++) { int k = i * (cross + 1) + j; Quad(triangles, k, k + cross + 1, k + cross + 2, k + 1); }
                mesh = Make(vertices, triangles); meshes[key] = mesh;
            }
            return Shape(p, n, mesh, at, Vector3.one, c, rotation);
        }
        public static Mesh Lathe(Vector2[] profile, int segments = 20)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int i = 0; i < profile.Length; i++) for (int j = 0; j <= segments; j++)
            { float a = j * Mathf.PI * 2 / segments; vertices.Add(new Vector3(profile[i].x * Mathf.Cos(a), profile[i].y, profile[i].x * Mathf.Sin(a))); }
            for (int i = 0; i < profile.Length - 1; i++) for (int j = 0; j < segments; j++)
            { int k = i * (segments + 1) + j; Quad(triangles, k, k + segments + 1, k + segments + 2, k + 1); }
            return Make(vertices, triangles);
        }
        public static Mesh Sphere()
        {
            if (meshes.TryGetValue("sphere", out var mesh)) return mesh;
            var profile = new Vector2[13]; for (int i = 0; i < profile.Length; i++) { float a = i * Mathf.PI / 12; profile[i] = new Vector2(Mathf.Sin(a) * .5f, -.5f * Mathf.Cos(a)); }
            mesh = Lathe(profile, 18); meshes["sphere"] = mesh; return mesh;
        }
        public static Mesh Sweep(Vector3[] path, float[] radii, float depth = .7f)
        {
            // Interpolate the authored guide points before skinning the tube. Silhouettes
            // stay curved at portrait size instead of exposing the control polygon.
            var curved = new List<Vector3>(); var widths = new List<float>();
            for (int i = 0; i < path.Length - 1; i++) for (int step = 0; step < 8; step++)
            {
                float t = step / 8f, t2 = t * t, t3 = t2 * t;
                Vector3 a = path[Mathf.Max(0, i - 1)], b = path[i], c = path[i + 1], d = path[Mathf.Min(path.Length - 1, i + 2)];
                curved.Add(.5f * ((2 * b) + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t2 + (-a + 3 * b - 3 * c + d) * t3));
                widths.Add(Mathf.Lerp(radii[i], radii[i + 1], Mathf.SmoothStep(0, 1, t)));
            }
            curved.Add(path[path.Length - 1]); widths.Add(radii[radii.Length - 1]); path = curved.ToArray(); radii = widths.ToArray();
            var vertices = new List<Vector3>(); var triangles = new List<int>(); const int sides = 16;
            for (int i = 0; i < path.Length; i++)
            {
                Vector3 tangent = (path[Mathf.Min(path.Length - 1, i + 1)] - path[Mathf.Max(0, i - 1)]).normalized;
                Vector3 side = Vector3.Cross(Vector3.forward, tangent).normalized, back = Vector3.Cross(side, tangent).normalized;
                for (int j = 0; j <= sides; j++)
                { float angle = j * Mathf.PI * 2 / sides; vertices.Add(path[i] + radii[i] * (side * Mathf.Cos(angle) + back * Mathf.Sin(angle) * depth)); }
            }
            for (int i = 0; i < path.Length - 1; i++) for (int j = 0; j < sides; j++)
            { int k = i * (sides + 1) + j; Quad(triangles, k, k + sides + 1, k + sides + 2, k + 1); }
            return Make(vertices, triangles);
        }
        public static Mesh Foliage(int seed)
        {
            // One closed sculpted volume; periodic deformation and analytic normals
            // avoid both the split lathe seam and the pinched radial pole of d05.
            const int rings = 18, sides = 40;
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int i = 0; i <= rings; i++) for (int j = 0; j <= sides; j++)
            {
                float latitude = Mathf.PI * i / rings, longitude = Mathf.PI * 2 * j / sides;
                float rim = Mathf.Sin(latitude), bulge = 1 + .085f * Mathf.Sin(5 * longitude + seed) * rim * rim + .045f * Mathf.Cos(3 * longitude - latitude * 4 + seed) * rim;
                vertices.Add(new Vector3(Mathf.Cos(longitude) * rim * bulge, -Mathf.Cos(latitude) * .65f, Mathf.Sin(longitude) * rim * bulge));
            }
            for (int i = 0; i < rings; i++) for (int j = 0; j < sides; j++)
            { int k = i * (sides + 1) + j; Quad(triangles, k, k + sides + 1, k + sides + 2, k + 1); }
            var mesh = Make(vertices, triangles); var normals = mesh.normals;
            for (int i = 0; i <= rings; i++)
            {
                int first = i * (sides + 1), last = first + sides;
                Vector3 normal = (normals[first] + normals[last]).normalized; normals[first] = normals[last] = normal;
                if (i == 0 || i == rings) for (int j = 0; j <= sides; j++) normals[first + j] = i == 0 ? Vector3.down : Vector3.up;
            }
            mesh.normals = normals; return mesh;
        }
        public static Transform Sculpture(Transform parent, string name, string asset, Vector3 position, Vector3 scale, Color tint)
        {
            string key = "sculpture " + asset;
            if (!meshes.TryGetValue(key, out var mesh))
            {
                var data = Resources.Load<TextAsset>("Models/" + asset);
                using (var reader = new BinaryReader(new MemoryStream(data.bytes)))
                {
                    if (reader.ReadUInt32() != 0x4D545346) throw new InvalidDataException("Invalid forest mesh " + asset);
                    int count = reader.ReadInt32(), indexCount = reader.ReadInt32();
                    var vertices = new Vector3[count]; var normals = new Vector3[count]; var colors = new Color32[count]; var indices = new int[indexCount];
                    for (int i = 0; i < count; i++) vertices[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    for (int i = 0; i < count; i++) normals[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    for (int i = 0; i < count; i++) colors[i] = new Color32(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
                    for (int i = 0; i < indexCount; i++) indices[i] = reader.ReadInt32();
                    mesh = new Mesh { name = asset, indexFormat = count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                    mesh.vertices = vertices; mesh.normals = normals; mesh.colors32 = colors; mesh.triangles = indices; mesh.RecalculateBounds();
                    meshes[key] = mesh; ownedMeshes.Add(mesh);
                }
            }
            string materialKey = "sculpture " + ColorUtility.ToHtmlStringRGBA(tint);
            if (!materials.TryGetValue(materialKey, out var material))
            {
                material = new Material(Resources.Load<Shader>("ForestSculpture")) { name = materialKey, color = tint };
                materials[materialKey] = material;
            }
            var shape = Shape(parent, name, mesh, position, scale, tint);
            shape.GetComponent<MeshRenderer>().sharedMaterial = material; return shape;
        }
        public static Transform MeadowPatch(Transform parent, Vector3 at, Vector2 radius, Color centre, float phase)
        {
            const int sides = 32;
            var vertices = new Vector3[1 + sides * 2]; var colors = new Color[vertices.Length]; var triangles = new List<int>();
            vertices[0] = Vector3.zero; colors[0] = centre;
            Color edge = Hex(0x91A575);
            for (int ring = 0; ring < 2; ring++) for (int j = 0; j < sides; j++)
            {
                float a = j * Mathf.PI * 2 / sides, r = (ring == 0 ? .57f : 1) * (1 + .10f * Mathf.Sin(a * 3 + phase) + .06f * Mathf.Cos(a * 5 - phase));
                int k = 1 + ring * sides + j;
                vertices[k] = new Vector3(Mathf.Cos(a) * r * radius.x, 0, Mathf.Sin(a) * r * radius.y);
                colors[k] = ring == 0 ? Color.Lerp(centre, edge, .32f) : edge;
                int next = (j + 1) % sides;
                if (ring == 0) triangles.AddRange(new[] { 0, 1 + next, 1 + j });
                else triangles.AddRange(new[] { 1 + j, 1 + next, 1 + sides + next, 1 + j, 1 + sides + next, 1 + sides + j });
            }
            for (int i = 0; i < colors.Length; i++) colors[i] *= .88f / .92f;
            var mesh = new Mesh { name = "Soft meadow wash" }; mesh.vertices = vertices; mesh.colors = colors; mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); ownedMeshes.Add(mesh);
            const string key = "Meadow vertex paint";
            if (!materials.TryGetValue(key, out var material)) { material = new Material(Resources.Load<Shader>("ForestSculpture")) { color = Color.white }; materials[key] = material; }
            var result = Shape(parent, "Soft meadow colour wash", mesh, at, Vector3.one, Color.white);
            var renderer = result.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
            return result;
        }
        public static Mesh OakLeaf()
        {
            if (meshes.TryGetValue("oak leaf", out var mesh)) return mesh;
            var vertices = new List<Vector3> { new Vector3(0, .075f, 0), new Vector3(0, -.025f, 0) }; var triangles = new List<int>();
            const int outline = 28;
            for (int side = 0; side < 2; side++) for (int i = 0; i < outline; i++)
            {
                float a = i * Mathf.PI * 2 / outline, z = Mathf.Cos(a) * .5f;
                float width = Mathf.Sin(a) * (.27f + .04f * Mathf.Cos(z * Mathf.PI * 10));
                vertices.Add(new Vector3(width, .015f - side * .03f - .02f * Mathf.Abs(z), z));
            }
            for (int i = 0; i < outline; i++)
            { int a = i + 2, b = (i + 1) % outline + 2; triangles.AddRange(new[] { 0, a, b, 1, b + outline, a + outline }); Quad(triangles, a, a + outline, b + outline, b); }
            mesh = Make(vertices, triangles); meshes["oak leaf"] = mesh; return mesh;
        }
        public static Mesh BevelBox()
        {
            if (meshes.TryGetValue("bevel", out var mesh)) return mesh;
            var verts = new List<Vector3>(); var tris = new List<int>();
            // A rounded superellipse block, with real bevel geometry on all edges.
            const int steps = 8;
            Vector3[] normal = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var n in normal)
            {
                var u = n.y != 0 ? Vector3.right : Vector3.up; var v = Vector3.Cross(n, u); int start = verts.Count;
                for (int y = 0; y <= steps; y++) for (int x = 0; x <= steps; x++)
                {
                    var q = n * .5f + u * (x / (float)steps - .5f) + v * (y / (float)steps - .5f);
                    var core = new Vector3(Mathf.Clamp(q.x, -.39f, .39f), Mathf.Clamp(q.y, -.39f, .39f), Mathf.Clamp(q.z, -.39f, .39f));
                    verts.Add(core + (q - core).normalized * .11f);
                }
                for (int y = 0; y < steps; y++) for (int x = 0; x < steps; x++) { int k = start + y * (steps + 1) + x; Quad(tris, k, k + 1, k + steps + 2, k + steps + 1); }
            }
            mesh = Make(verts, tris); meshes["bevel"] = mesh; return mesh;
        }
        static void Quad(List<int> list, int a, int b, int c, int d) { list.Add(a); list.Add(b); list.Add(c); list.Add(a); list.Add(c); list.Add(d); }
        static Mesh Make(List<Vector3> vertices, List<int> triangles)
        { var mesh = new Mesh { indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); ownedMeshes.Add(mesh); return mesh; }
        public static void Combine(Transform root)
        {
            var groups = new Dictionary<Material, List<CombineInstance>>(); var renderers = root.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                if (!r.gameObject.activeInHierarchy || !r.enabled) continue; var f = r.GetComponent<MeshFilter>(); if (f == null || f.sharedMesh == null) continue;
                if (!groups.TryGetValue(r.sharedMaterial, out var list)) groups[r.sharedMaterial] = list = new List<CombineInstance>();
                list.Add(new CombineInstance { mesh = f.sharedMesh, transform = root.worldToLocalMatrix * r.localToWorldMatrix });
            }
            foreach (var pair in groups)
            {
                var mesh = new Mesh { indexFormat = IndexFormat.UInt32 }; mesh.CombineMeshes(pair.Value.ToArray()); ownedMeshes.Add(mesh);
                Shape(root, "Combined " + pair.Key.name, mesh, Vector3.zero, Vector3.one, pair.Key.color).GetComponent<MeshRenderer>().sharedMaterial = pair.Key;
            }
            foreach (var renderer in renderers)
            {
                renderer.enabled = false; var filter = renderer.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null && !meshes.ContainsValue(filter.sharedMesh) && ownedMeshes.Remove(filter.sharedMesh)) UnityEngine.Object.Destroy(filter.sharedMesh);
                UnityEngine.Object.Destroy(filter); UnityEngine.Object.Destroy(renderer);
            }
        }
        public static void Release()
        {
            foreach (var mesh in ownedMeshes) if (mesh != null) UnityEngine.Object.Destroy(mesh);
            foreach (var material in materials.Values) if (material != null) UnityEngine.Object.Destroy(material);
            ownedMeshes.Clear(); meshes.Clear(); materials.Clear();
        }
    }
}
