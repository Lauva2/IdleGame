using UnityEngine;
using UnityEngine.Rendering;
using static ForestShop.ForestGeometry;

namespace ForestShop
{
    // Original meshes. Forest discoveries deliberately have different silhouettes:
    // red fruit clusters, tall blue leaves, and amber resin on a cut branch.
    public static class ForestExpansionModels
    {
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static Material glass;
        public static Transform Crop(Transform parent, int id)
        {
            var p = Group(parent, "Forest crop " + id);
            if (id == 4)
            {
                for (int branch = 0; branch < 4; branch++)
                {
                    float a = branch * 1.9f; var top = V(Mathf.Sin(a) * .34f, .55f + branch % 2 * .22f, Mathf.Cos(a) * .27f);
                    Tube(p, "Berry twig", V(0, .03f, 0), top, .027f, DarkWood);
                    for (int leaf = 0; leaf < 3; leaf++)
                    {
                        var at = top * (.46f + leaf * .22f);
                        Ball(p, "Oval berry leaf", at + V(.12f, .02f, 0), V(.33f, .08f, .20f), branch % 2 == 0 ? Moss : Leaf, V(-25, a * 57, 32));
                    }
                    for (int fruit = 0; fruit < 3; fruit++)
                    {
                        var at = top + V((fruit - 1) * .11f, fruit % 2 * .07f, -.08f);
                        Ball(p, "Ripe forest berry", at, Vector3.one * .18f, fruit % 2 == 0 ? Hex(0xA74C51) : Hex(0xC77768));
                        Ball(p, "Berry blossom end", at + V(0, .075f, -.02f), V(.07f, .018f, .07f), Gold);
                    }
                }
            }
            else if (id == 5)
            {
                for (int i = 0; i < 7; i++)
                {
                    float a = i * 2.4f;
                    Ball(p, "Silver blue long leaf", V(Mathf.Sin(a) * .18f, .33f, Mathf.Cos(a) * .18f), V(.14f, .74f, .11f), i % 2 == 0 ? Hex(0x8BBFB5) : Hex(0x5F9C98), V(Mathf.Sin(a) * 32, 0, Mathf.Cos(a) * 32));
                }
                for (int i = 0; i < 3; i++)
                {
                    var top = V((i - 1) * .20f, .82f + i % 2 * .15f, .06f);
                    Tube(p, "Dew herb stalk", V(0, 0, 0), top, .016f, Deep);
                    Ball(p, "Pearl dew flower", top, V(.16f, .21f, .16f), Cream);
                    Ring(p, "Blue flower calyx", top - V(0, .05f, 0), .10f, .022f, Blue);
                }
            }
            else
            {
                Tube(p, "Resin branch", V(-.36f, .08f, .1f), V(.33f, .38f, .03f), .18f, Wood, .75f);
                for (int i = 0; i < 4; i++)
                {
                    var at = V(-.22f + i * .14f, .25f + i % 2 * .09f, -.10f);
                    Ball(p, "Amber resin bead", at, V(.19f, .26f, .16f), i % 2 == 0 ? Hex(0xDBA45C) : Hex(0xC28745));
                    Ball(p, "Resin soft highlight", at + V(-.035f, .055f, -.06f), V(.035f, .07f, .015f), Cream);
                }
                Box(p, "Folded leaf beneath resin", V(0, .04f, -.02f), V(.75f, .045f, .44f), Moss, V(0, -14, 0));
            }
            Combine(p); return p;
        }
        public static Transform Product(Transform parent, int id)
        {
            var p = Group(parent, "Forest product " + id);
            if (id == 6)
            {
                Shape(p, "Berry glazed cup", Lathe(new[] { new Vector2(0, 0), new Vector2(.23f, 0), new Vector2(.32f, .12f), new Vector2(.34f, .42f), new Vector2(.28f, .45f), new Vector2(0, .45f) }), Vector3.zero, Vector3.one, Hex(0xB56B68));
                Ring(p, "Wide cream lip", V(0, .43f, 0), .31f, .035f, Cream);
                Ring(p, "Round tea handle", V(.33f, .25f, 0), .15f, .04f, Honey, V(90, 0, 0));
                Ball(p, "Berry tea surface", V(0, .44f, 0), V(.54f, .02f, .54f), Hex(0x8F4D59));
                for (int i = 0; i < 3; i++) Ball(p, "Berries on label", V(-.10f + i * .09f, .23f + i % 2 * .07f, -.31f), Vector3.one * .12f, Cream);
                Ball(p, "Fresh berry leaf", V(-.10f, .47f, -.09f), V(.27f, .025f, .12f), Leaf, V(0, -25, 0));
                Box(p, "Woodland square coaster", V(0, -.045f, 0), V(.78f, .07f, .65f), Honey, V(0, -8, 0));
            }
            else if (id == 7)
            {
                Box(p, "Tall teal mist bottle", V(0, .28f, 0), V(.39f, .54f, .30f), Hex(0x72AFA8));
                Tube(p, "Mist bottle neck", V(0, .5f, 0), V(0, .68f, 0), .08f, Cream);
                Box(p, "Brass pump", V(.02f, .71f, 0), V(.31f, .10f, .14f), Gold);
                Tube(p, "Curved spray nozzle", V(.1f, .71f, 0), V(.27f, .66f, 0), .04f, DarkWood);
                Box(p, "Dew paper label", V(0, .28f, -.16f), V(.25f, .25f, .025f), Cream);
                Ball(p, "Teardrop seal", V(0, .28f, -.185f), V(.085f, .16f, .025f), Blue, V(0, 0, -12));
                Ring(p, "Bottle collar", V(0, .54f, 0), .10f, .023f, DarkWood);
            }
            else
            {
                Box(p, "Low square wood balm box", V(0, .13f, 0), V(.64f, .25f, .49f), Honey);
                Box(p, "Carved dark lid", V(0, .29f, .03f), V(.68f, .075f, .53f), Deep, V(-8, 0, 0));
                Shape(p, "Pine leaf lid mark", OakLeaf(), V(0, .345f, .03f), V(.32f, .36f, .25f), Cream);
                Box(p, "Balm paper band", V(0, .14f, -.26f), V(.32f, .17f, .025f), Cream);
                Tube(p, "Small balm brush", V(-.24f, .09f, -.35f), V(.29f, .09f, -.35f), .025f, DarkWood);
                Box(p, "Brush soft bristles", V(.28f, .09f, -.35f), V(.16f, .075f, .10f), Gold);
            }
            Combine(p); return p;
        }
        public static Transform Glass(Transform parent, string name, Vector3 position, Vector3 size, Vector3 rotation = default(Vector3))
        {
            if (glass == null) glass = new Material(Resources.Load<Shader>("ForestGlass")) { color = new Color(.77f, .91f, .84f, .15f), name = "Soft greenhouse glass" };
            var p = Box(parent, name, position, size, Cream, rotation);
            var renderer = p.GetComponent<MeshRenderer>(); renderer.sharedMaterial = glass; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; return p;
        }
        public static void Greenhouse(Transform parent, Transform roof, bool restored)
        {
            var frame = Group(parent, restored ? "Repaired honey greenhouse" : "Greenhouse awaiting repair");
            Box(frame, "Greenhouse stone footing", V(0, .02f, 0), V(9.4f, .3f, 7.4f), Hex(0xA5AA8A));
            for (int i = 0; i < 18; i++) Box(frame, "Greenhouse walkway plank", V(0, .23f, -3.35f + i * .39f), V(1.5f, .10f, .37f), i % 3 == 0 ? Honey : Wood);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(frame, "Low brick wall", V(side * 4.4f, .47f, 0), V(.24f, .8f, 7), Hex(0xBB9874));
                for (int i = 0; i < 5; i++)
                {
                    float z = -3.3f + i * 1.65f;
                    Box(frame, "Glasshouse wall upright", V(side * 4.4f, 1.68f, z), V(.16f, 3, .17f), restored ? Wood : DarkWood, V(0, 0, restored ? 0 : side * 3));
                    Tube(roof, "Sloping roof rib", V(side * 4.5f, 3.18f, z), V(0, 4.7f, z), .08f, restored ? Honey : Wood);
                    if (restored && i < 4)
                    {
                        Glass(parent, "Side light pane", V(side * 4.4f, 1.98f, z + .8f), V(.025f, 2.15f, 1.48f));
                        Glass(roof, "Pitched roof pane", V(side * 2.24f, 3.92f, z + .8f), V(4.5f, .025f, 1.5f), V(0, 0, -side * 18.7f));
                    }
                }
                Box(frame, "Side wall honey rail", V(side * 4.4f, 2.6f, 0), V(.14f, .11f, 7), Honey);
                if (!restored) for (int i = 0; i < 3; i++) Box(frame, "Leaning repair timber", V(side * 2.1f, .55f, -2.2f + i * .5f), V(.22f, .17f, 2.1f), Wood, V(0, side * (12 + i * 9), 18));
            }
            Box(frame, "Rear planting wall", V(0, 1.35f, 3.45f), V(8.8f, 2.4f, .14f), restored ? Hex(0xE5DDB9) : Hex(0xBCBDA4));
            for (int i = -2; i <= 2; i++) Box(frame, "Rear greenhouse timber", V(i * 2.15f, 1.72f, 3.32f), V(.12f, 3.2f, .14f), Wood);
            Tube(roof, "Greenhouse warm ridge", V(0, 4.7f, -3.6f), V(0, 4.7f, 3.6f), .11f, Honey);
            for (int side = -1; side <= 1; side += 2) Box(frame, "Open front doorway post", V(side * 1.05f, 1.72f, -3.45f), V(.17f, 3.1f, .2f), Wood);
            Box(frame, "Greenhouse leaf sign", V(0, 3.10f, -3.57f), V(2.6f, .65f, .16f), Deep);
            Shape(frame, "Greenhouse sign leaf", OakLeaf(), V(0, 3.13f, -3.69f), V(.56f, .56f, .1f), Cream, V(90, 0, 0));
            var shelf = Group(frame, "Pots and gardening supplies");
            Box(shelf, "Potting shelf", V(0, 1.05f, 2.9f), V(3.3f, .14f, .7f), Honey);
            for (int i = 0; i < 4; i++) ForestModels.Pot(shelf, V(-1.1f + i * .7f, 1.14f, 2.9f), .7f, restored && i % 2 == 0);
            Box(shelf, "Seedling basket", V(3.6f, .7f, 2.5f), V(.8f, .6f, .7f), Honey);
            Combine(shelf); Combine(frame);
        }
        public static void Release() { if (glass != null) Object.Destroy(glass); glass = null; }
    }
}
