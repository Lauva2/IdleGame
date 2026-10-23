using UnityEngine;
using UnityEngine.UI;

namespace ForestShop
{
    public enum ForestSymbol
    {
        Coin, Reputation, Settings, Home, Garden, Workshop, Shop, Orders,
        Collection, Warehouse, Expand, Decoration, Harvest, Shelf, Timer,
        Loop, Check, Lock, Reserved, Empty, Info, Arrow, Close, Tea
    }

    // Small, original woodland UI illustrations. Native geometry stays crisp
    // at different canvas scales and does not depend on emoji or an icon font.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ForestSymbolGraphic : MaskableGraphic
    {
        [SerializeField] ForestSymbol symbol;
        [SerializeField] bool lightPalette;
        VertexHelper mesh;
        Color edge, green, gold, paper, blue;
        public ForestSymbol Symbol { get => symbol; set { if (symbol == value) return; symbol = value; SetVerticesDirty(); } }
        public bool Light { get => lightPalette; set { if (lightPalette == value) return; lightPalette = value; SetVerticesDirty(); } }
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            mesh = helper; mesh.Clear();
            edge = lightPalette ? C(255, 249, 228) : C(72, 78, 55);
            green = lightPalette ? C(224, 237, 204) : C(92, 130, 87);
            gold = lightPalette ? C(240, 205, 122) : C(213, 165, 76);
            paper = lightPalette ? C(93, 128, 89) : C(255, 245, 212);
            blue = lightPalette ? C(192, 220, 209) : C(122, 174, 165);
            switch (symbol)
            {
                case ForestSymbol.Coin:
                    Disk(28, 28, 20, edge); Disk(28, 29, 16, gold); Arc(28, 29, 11, 35, 305, 2.5f, paper);
                    Leaf(28, 29, 7, 11, -28, green); Line(28, 21, 28, 36, 2, paper); break;
                case ForestSymbol.Reputation:
                    Poly(edge, 32, 5, 13, 19, 8, 49, 32, 58, 56, 49, 51, 19);
                    Poly(gold, 32, 11, 18, 22, 14, 45, 32, 52, 50, 45, 46, 22);
                    Leaf(26, 34, 9, 17, -32, green); Leaf(39, 28, 7, 12, 42, green);
                    Line(26, 16, 35, 44, 3, paper); break;
                case ForestSymbol.Settings:
                    for (int i = 0; i < 8; i++) { float a = i * 45; Stroke(32, 32, a, 18, 26, 9, edge); }
                    Disk(32, 32, 21, edge); Disk(32, 32, 14, gold); Disk(32, 32, 7, paper); break;
                case ForestSymbol.Home:
                    Disk(48, 45, 12, green); Line(48, 37, 48, 21, 5, edge);
                    Box(12, 10, 40, 31, edge); Box(16, 10, 32, 28, paper);
                    Poly(green, 7, 37, 32, 56, 57, 37); Line(7, 37, 32, 56, 4, edge); Line(32, 56, 57, 37, 4, edge);
                    Box(27, 10, 11, 19, gold); Disk(22, 30, 5, blue); break;
                case ForestSymbol.Garden:
                    Poly(edge, 6, 17, 45, 11, 58, 20, 19, 27); Poly(gold, 10, 17, 44, 14, 53, 20, 19, 24);
                    Line(32, 22, 32, 42, 4, edge); Leaf(23, 39, 9, 14, -50, green); Leaf(41, 46, 9, 14, 45, green); break;
                case ForestSymbol.Workshop:
                    Arc(13, 31, 8, 70, 290, 4, edge); Arc(51, 31, 8, -110, 110, 4, edge);
                    Line(19, 11, 16, 5, 5, edge); Line(45, 11, 48, 5, 5, edge);
                    Poly(edge, 10, 39, 14, 17, 23, 9, 41, 9, 50, 17, 54, 39);
                    Poly(gold, 15, 36, 18, 20, 25, 14, 39, 14, 46, 20, 49, 36);
                    Oval(32, 39, 23, 7, edge); Oval(32, 40, 18, 4, blue);
                    Line(36, 39, 46, 58, 4, edge); Disk(25, 49, 3, blue); Disk(18, 55, 2, gold); break;
                case ForestSymbol.Shop:
                    Box(10, 9, 44, 31, edge); Box(14, 10, 36, 27, paper);
                    Poly(edge, 8, 39, 15, 55, 49, 55, 56, 39); Poly(gold, 12, 40, 18, 51, 46, 51, 52, 40);
                    for (int i = 0; i < 4; i++) { Box(13 + i * 10, 37, 8, 6, i % 2 == 0 ? green : gold); Disk(17 + i * 10, 37, 4, i % 2 == 0 ? green : gold); }
                    Box(19, 15, 14, 14, blue); Box(39, 10, 7, 20, green); break;
                case ForestSymbol.Orders:
                    Box(7, 13, 50, 36, edge); Box(11, 17, 42, 28, paper);
                    Line(11, 45, 32, 28, 3, edge); Line(53, 45, 32, 28, 3, edge);
                    Disk(32, 27, 7, gold); Leaf(32, 28, 3, 5, -30, green); break;
                case ForestSymbol.Collection:
                    Poly(edge, 5, 14, 5, 51, 21, 54, 32, 48, 43, 54, 59, 51, 59, 14, 43, 17, 32, 11, 21, 17);
                    Poly(paper, 9, 19, 9, 48, 21, 50, 30, 45, 30, 17, 21, 21);
                    Poly(gold, 34, 17, 34, 45, 43, 50, 55, 48, 55, 19, 43, 21);
                    Line(32, 14, 32, 47, 3, edge); Leaf(19, 35, 5, 9, -30, green);
                    Line(40, 38, 50, 40, 2, edge); Line(40, 31, 50, 33, 2, edge); break;
                case ForestSymbol.Warehouse:
                    Poly(edge, 7, 17, 32, 7, 57, 17, 57, 47, 32, 57, 7, 47);
                    Poly(gold, 11, 20, 30, 12, 30, 37, 11, 44); Poly(paper, 34, 12, 53, 20, 53, 44, 34, 37);
                    Poly(green, 12, 48, 32, 52, 52, 48, 32, 41); Line(16, 35, 26, 31, 3, edge); break;
                case ForestSymbol.Expand:
                    Line(16, 10, 43, 48, 8, edge); Line(17, 12, 42, 47, 4, gold);
                    Poly(edge, 30, 51, 38, 58, 56, 44, 49, 35); Poly(gold, 36, 51, 39, 53, 51, 44, 48, 40);
                    Line(14, 39, 14, 55, 4, green); Line(6, 47, 22, 47, 4, green); break;
                case ForestSymbol.Decoration:
                    Poly(edge, 15, 27, 20, 7, 44, 7, 49, 27); Poly(gold, 20, 24, 24, 11, 40, 11, 44, 24);
                    Line(32, 27, 32, 48, 4, green); Leaf(23, 35, 7, 11, -55, green);
                    for (int i = 0; i < 5; i++) { float a = i * Mathf.PI * 2 / 5; Disk(32 + Mathf.Cos(a) * 8, 49 + Mathf.Sin(a) * 8, 6, paper); }
                    Disk(32, 49, 5, gold); break;
                case ForestSymbol.Harvest:
                    Arc(32, 35, 18, 0, 180, 4, edge); Poly(edge, 9, 32, 15, 9, 49, 9, 55, 32); Poly(gold, 14, 28, 19, 14, 45, 14, 50, 28);
                    Line(22, 16, 22, 27, 2, paper); Line(32, 16, 32, 27, 2, paper); Line(42, 16, 42, 27, 2, paper);
                    Leaf(25, 39, 7, 13, -30, green); Leaf(38, 42, 7, 12, 35, green); break;
                case ForestSymbol.Shelf:
                    Box(8, 8, 5, 48, edge); Box(51, 8, 5, 48, edge);
                    for (int i = 0; i < 3; i++) Box(9, 10 + i * 20, 46, 5, gold);
                    Jar(22, 31); Jar(42, 31); Jar(32, 11); break;
                case ForestSymbol.Timer:
                    Disk(32, 30, 23, edge); Disk(32, 30, 18, paper);
                    Line(32, 30, 32, 43, 4, green); Line(32, 30, 43, 25, 4, green);
                    Line(27, 57, 37, 57, 4, gold); Line(32, 53, 32, 57, 4, edge); break;
                case ForestSymbol.Loop:
                    Arc(32, 32, 21, 40, 205, 5, green); Arc(32, 32, 21, 220, 385, 5, green);
                    Poly(gold, 8, 25, 21, 25, 12, 13); Poly(gold, 56, 39, 43, 39, 52, 51); break;
                case ForestSymbol.Check:
                    Disk(32, 32, 24, green); Line(17, 31, 28, 20, 6, paper); Line(28, 20, 48, 44, 6, paper); break;
                case ForestSymbol.Lock:
                    Arc(32, 38, 14, 0, 180, 6, edge); Box(15, 12, 34, 29, edge); Box(19, 16, 26, 21, gold);
                    Disk(32, 29, 3.5f, paper); Line(32, 26, 32, 22, 3, paper); break;
                case ForestSymbol.Reserved:
                    Poly(edge, 9, 14, 9, 42, 30, 57, 55, 40, 55, 14); Poly(gold, 14, 18, 14, 40, 30, 51, 50, 38, 50, 18);
                    Disk(30, 42, 3, paper); Line(24, 28, 30, 23, 3, green); Line(30, 23, 42, 35, 3, green); break;
                case ForestSymbol.Empty:
                    Poly(edge, 8, 12, 8, 40, 56, 40, 56, 12); Poly(paper, 13, 17, 13, 35, 51, 35, 51, 17);
                    Line(19, 26, 45, 26, 5, gold); Line(12, 43, 22, 54, 4, green); Line(52, 43, 42, 54, 4, green); break;
                case ForestSymbol.Info:
                    Disk(32, 32, 24, edge); Disk(32, 32, 19, paper); Disk(32, 43, 3, green); Line(32, 32, 32, 20, 5, green); break;
                case ForestSymbol.Arrow: Line(13, 32, 50, 32, 5, edge); Line(39, 45, 52, 32, 5, edge); Line(52, 32, 39, 19, 5, edge); break;
                case ForestSymbol.Close: Line(15, 15, 49, 49, 6, edge); Line(15, 49, 49, 15, 6, edge); break;
                case ForestSymbol.Tea:
                    Poly(edge, 10, 40, 15, 14, 38, 14, 44, 40); Poly(paper, 15, 36, 19, 19, 34, 19, 39, 36);
                    Arc(44, 29, 10, -90, 90, 5, gold); Line(8, 9, 48, 9, 4, green);
                    Line(23, 46, 20, 53, 3, green); Line(33, 46, 36, 57, 3, green); break;
            }
        }
        void Jar(float x, float y)
        { Box(x - 5, y + 2, 10, 12, blue); Box(x - 4, y + 14, 8, 4, edge); Disk(x, y + 8, 2, paper); }
        static Color C(byte r, byte g, byte b) => new Color32(r, g, b, 255);
        Vector2 Map(float x, float y)
        { var r = rectTransform.rect; float s = Mathf.Min(r.width, r.height) / 64; return r.center + new Vector2(x - 32, y - 32) * s; }
        void Vertex(float x, float y, Color tint)
        { var v = UIVertex.simpleVert; v.position = Map(x, y); v.color = tint * color; mesh.AddVert(v); }
        void Box(float x, float y, float w, float h, Color tint) => Poly(tint, x, y, x + w, y, x + w, y + h, x, y + h);
        void Poly(Color tint, params float[] xy)
        { int start = mesh.currentVertCount; for (int i = 0; i < xy.Length; i += 2) Vertex(xy[i], xy[i + 1], tint); for (int i = 1; i < xy.Length / 2 - 1; i++) mesh.AddTriangle(start, start + i, start + i + 1); }
        void Disk(float x, float y, float radius, Color tint) => Oval(x, y, radius, radius, tint);
        void Oval(float x, float y, float rx, float ry, Color tint)
        {
            const int count = 28; int start = mesh.currentVertCount; Vertex(x, y, tint);
            for (int i = 0; i <= count; i++) { float a = i * Mathf.PI * 2 / count; Vertex(x + Mathf.Cos(a) * rx, y + Mathf.Sin(a) * ry, tint); }
            for (int i = 0; i < count; i++) mesh.AddTriangle(start, start + i + 1, start + i + 2);
        }
        void Line(float x1, float y1, float x2, float y2, float width, Color tint)
        {
            Vector2 d = new Vector2(y1 - y2, x2 - x1).normalized * width / 2;
            Poly(tint, x1 + d.x, y1 + d.y, x1 - d.x, y1 - d.y, x2 - d.x, y2 - d.y, x2 + d.x, y2 + d.y);
            Disk(x1, y1, width / 2, tint); Disk(x2, y2, width / 2, tint);
        }
        void Stroke(float x, float y, float angle, float from, float to, float width, Color tint)
        { float a = angle * Mathf.Deg2Rad; Line(x + Mathf.Cos(a) * from, y + Mathf.Sin(a) * from, x + Mathf.Cos(a) * to, y + Mathf.Sin(a) * to, width, tint); }
        void Arc(float x, float y, float radius, float from, float to, float width, Color tint)
        {
            const int segments = 24; int start = mesh.currentVertCount;
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(from, to, i / (float)segments) * Mathf.Deg2Rad;
                Vertex(x + Mathf.Cos(a) * (radius - width / 2), y + Mathf.Sin(a) * (radius - width / 2), tint);
                Vertex(x + Mathf.Cos(a) * (radius + width / 2), y + Mathf.Sin(a) * (radius + width / 2), tint);
                if (i > 0) { int n = start + i * 2; mesh.AddTriangle(n - 2, n - 1, n); mesh.AddTriangle(n - 1, n + 1, n); }
            }
            float first = from * Mathf.Deg2Rad, last = to * Mathf.Deg2Rad;
            Disk(x + Mathf.Cos(first) * radius, y + Mathf.Sin(first) * radius, width / 2, tint);
            Disk(x + Mathf.Cos(last) * radius, y + Mathf.Sin(last) * radius, width / 2, tint);
        }
        void Leaf(float x, float y, float rx, float ry, float angle, Color tint)
        {
            float a = angle * Mathf.Deg2Rad; const int count = 18; int start = mesh.currentVertCount; Vertex(x, y, tint);
            for (int i = 0; i <= count; i++) { float t = i * Mathf.PI * 2 / count; float px = Mathf.Sin(t) * rx * Mathf.Abs(Mathf.Sin(t)), py = Mathf.Cos(t) * ry; Vertex(x + px * Mathf.Cos(a) - py * Mathf.Sin(a), y + px * Mathf.Sin(a) + py * Mathf.Cos(a), tint); }
            for (int i = 0; i < count; i++) mesh.AddTriangle(start, start + i + 1, start + i + 2);
        }
    }
}
