using UnityEngine;
using static ForestShop.ForestGeometry;

namespace ForestShop
{
    // All world props and residents are solid meshes; no picture planes stand in for them.
    public static class ForestModels
    {
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        public static Transform Resident(Transform parent, int id)
        {
            var residentRoot = Group(parent, "Resident " + id);
            var p = Group(residentRoot, "Sculpted body");
            var motion = residentRoot.gameObject.AddComponent<ForestResidentMotion>();
            motion.resident = id;
            Color fur = id == 0 ? Hex(0xAF8453) : id == 1 ? Hex(0xC19A73) : id == 2 ? Hex(0xC57A3A) : id == 3 ? Hex(0xEEE0C0) : Hex(0xB98550);
            Color coat = id == 3 ? Hex(0x688B9D) : id == 2 ? Hex(0xC69A48) : Leaf;
            Ball(p, "Rounded body", V(0, .76f, 0), V(.94f, 1.24f, .77f), fur);
            Ball(p, "Head", V(0, 1.55f, -.05f), id == 0 ? V(1.2f, 1.03f, .86f) : id == 1 ? V(.88f, .85f, .85f) : V(1.02f, .94f, .84f), fur);
            for (int s = -1; s <= 1; s += 2)
            {
                int leg = s < 0 ? 0 : 1;
                motion.thighs[leg] = Ball(residentRoot, "Joined thigh " + s, Vector3.zero, Vector3.one, id == 0 ? fur : coat);
                motion.shins[leg] = Ball(residentRoot, "Joined lower leg " + s, Vector3.zero, Vector3.one, id == 0 ? Gold : fur);
                motion.knees[leg] = Ball(residentRoot, "Rounded knee " + s, Vector3.zero, Vector3.one * .23f, id == 0 ? Gold : fur);
                var foot = Group(residentRoot, "Foot pivot " + s, V(s * .27f, .27f, -.05f));
                Ball(foot, "Foot", V(0, -.13f, -.12f), V(.37f, .24f, .51f), id == 0 ? Gold : fur); Combine(foot);
                var armPivot = Group(residentRoot, "Arm pivot " + s, V(s * .45f, 1.05f, 0));
                Color sleeve = id == 0 ? fur : coat;
                motion.upperArms[leg] = Ball(residentRoot, "Upper sleeve " + s, Vector3.zero, Vector3.one, sleeve);
                motion.forearms[leg] = Ball(residentRoot, "Lower sleeve " + s, Vector3.zero, Vector3.one, sleeve);
                motion.elbows[leg] = Ball(residentRoot, "Rounded elbow " + s, Vector3.zero, Vector3.one * .24f, sleeve);
                motion.hands[leg] = Ball(residentRoot, "Hand " + s, Vector3.zero, V(.25f, .21f, .26f), fur);
                if (s < 0) { motion.leftFoot = foot; motion.leftArm = armPivot; } else { motion.rightFoot = foot; motion.rightArm = armPivot; }
            }
            if (id == 0)
            {
                Ball(p, "Feather bib", V(0, .75f, -.33f), V(.7f, .85f, .22f), Cream);
                for (int row = 0; row < 3; row++) for (int col = 0; col < 3; col++) Ball(p, "Feather scallop", V(-.22f + col * .22f, .48f + row * .18f, -.44f), V(.19f, .25f, .055f), Hex(0xE4CD9D), V(0, 0, (col - 1) * 15));
                for (int s = -1; s <= 1; s += 2)
                {
                    Ball(p, "Face disk", V(s * .25f, 1.58f, -.42f), V(.54f, .62f, .17f), Cream);
                    Tube(p, "Ear feather", V(s * .42f, 1.9f, .02f), V(s * .54f, 2.16f, .04f), .16f, fur, .03f);
                    Ring(p, "Round spectacles", V(s * .25f, 1.62f, -.54f), .23f, .025f, DarkWood, V(90, 0, 0));
                }
                Tube(p, "Spectacle bridge", V(-.03f, 1.63f, -.55f), V(.03f, 1.63f, -.55f), .025f, DarkWood);
                Tube(p, "Small beak", V(0, 1.46f, -.47f), V(0, 1.33f, -.62f), .1f, Gold, .02f);
                Ring(p, "Sage scarf", V(0, 1.14f, -.01f), .42f, .09f, Deep);
                Box(p, "Old book", V(0, .67f, -.54f), V(.55f, .65f, .17f), Hex(0x86503A), V(0, -8, 8));
                Box(p, "Book paper edge", V(.02f, .67f, -.555f), V(.44f, .56f, .13f), Cream, V(0, -8, 8));
                Box(p, "Book cover", V(0, .67f, -.64f), V(.56f, .66f, .04f), Hex(0x86503A), V(0, -8, 8));
                Ring(p, "Book emblem", V(0, .72f, -.68f), .11f, .015f, Gold, V(90, 0, 0));
            }
            else
            {
                if (id == 1) Tube(p, "Pointed hedgehog snout", V(0, 1.4f, -.32f), V(0, 1.37f, -.85f), .26f, Cream, .24f);
                else Ball(p, "Soft muzzle", V(0, 1.35f, -.43f), V(.61f, .4f, .26f), Cream);
                Ball(p, "Nose", V(0, id == 1 ? 1.37f : 1.44f, id == 1 ? -.87f : -.57f), V(.15f, .105f, .09f), Ink);
                Tube(p, "Smile left", V(-.12f, 1.29f, -.55f), V(0, 1.25f, -.566f), .015f, DarkWood);
                Tube(p, "Smile right", V(0, 1.25f, -.566f), V(.12f, 1.29f, -.55f), .015f, DarkWood);
                Ball(p, "Clothing", V(0, .72f, -.015f), V(.94f, .98f, .79f), coat);
                for (int i = 0; i < 3; i++) Ball(p, "Brass button", V(.08f, .56f + i * .18f, -.419f), Vector3.one * .055f, Gold);
                for (int s = -1; s <= 1; s += 2)
                {
                    if (id == 3)
                    {
                        Ball(p, "Long ear", V(s * .26f, 2.04f, 0), V(.29f, 1.03f, .24f), fur, V(0, 0, -s * 12));
                        Ball(p, "Inner ear", V(s * .275f, 2.05f, -.108f), V(.13f, .77f, .035f), Hex(0xDAA694), V(0, 0, -s * 12));
                    }
                    else if (id == 2)
                    {
                        Tube(p, "Pointed ear", V(s * .36f, 1.85f, .02f), V(s * .44f, 2.17f, .01f), .18f, fur, .04f);
                        Ball(p, "Ear inset", V(s * .4f, 2.0f, -.12f), V(.12f, .22f, .035f), Cream, V(0, 0, -s * 10));
                    }
                    else { float ear = id == 1 ? .21f : .34f; Ball(p, "Round ear", V(s * .43f, id == 1 ? 1.7f : 1.9f, .02f), Vector3.one * ear, fur); Ball(p, "Ear inset", V(s * .43f, id == 1 ? 1.7f : 1.9f, -.1f), V(ear * .55f, ear * .58f, .05f), Hex(0xD8AF87)); }
                }
                if (id == 1)
                {
                    for (int j = 0; j < 13; j++)
                    {
                        float a = j * Mathf.PI / 12;
                        Vector3 basePoint = V(Mathf.Cos(a) * .41f, 1.5f + Mathf.Sin(a) * .4f, .08f);
                        Tube(p, "Crown of chestnut spines", basePoint, basePoint + V(Mathf.Cos(a) * .23f, Mathf.Sin(a) * .27f + .06f, -.01f), .105f, j % 2 == 0 ? DarkWood : Wood, .025f);
                    }
                    for (int row = 0; row < 5; row++) for (int j = 0; j < 9; j++)
                    {
                        float a = -.25f + j * Mathf.PI / 8, y = .55f + row * .25f;
                        Vector3 a0 = V(Mathf.Cos(a) * .49f, y, Mathf.Sin(a) * .4f + .06f);
                        Tube(p, "Chestnut spine", a0, a0 + V(Mathf.Cos(a) * .22f, .17f, Mathf.Sin(a) * .26f), .1f, row % 2 == 0 ? DarkWood : Wood, .03f);
                    }
                    Box(p, "Apron pocket", V(-.18f, .6f, -.435f), V(.23f, .28f, .04f), Moss);
                    Tube(p, "Daisy stem", V(.35f, .7f, -.4f), V(.36f, 1.24f, -.45f), .02f, Deep);
                    Flower(p, V(.36f, 1.25f, -.46f), .17f, Cream);
                }
                if (id == 2)
                {
                    Shape(p, "Continuous curled squirrel tail", Sweep(new[] { V(0, .3f, .32f), V(.52f, .63f, .58f), V(.83f, 1.16f, .55f), V(.81f, 1.73f, .45f), V(.37f, 2.05f, .3f), V(.03f, 1.87f, .28f), V(.12f, 1.63f, .25f) }, new[] { .1f, .28f, .39f, .34f, .23f, .12f, 0 }), Vector3.zero, Vector3.one, fur);
                    Shape(p, "Tail inner curl", Sweep(new[] { V(.39f, .6f, .35f), V(.68f, 1.1f, .27f), V(.66f, 1.62f, .21f), V(.33f, 1.85f, .16f), V(.1f, 1.72f, .17f) }, new[] { 0, .11f, .12f, .07f, 0 }, .2f), Vector3.zero, Vector3.one, Honey);
                    Tube(p, "Bag strap", V(-.34f, 1.12f, -.41f), V(.36f, .45f, -.45f), .045f, DarkWood);
                    Ball(p, "Acorn satchel", V(.4f, .4f, -.37f), V(.42f, .46f, .22f), DarkWood);
                    Ball(p, "Satchel flap", V(.4f, .53f, -.49f), V(.43f, .2f, .06f), Honey);
                }
                if (id == 3)
                {
                    Ball(p, "Postal cap", V(0, 1.96f, -.04f), V(.96f, .3f, .76f), coat);
                    Box(p, "Cap visor", V(.06f, 1.91f, -.44f), V(.85f, .07f, .47f), Deep, V(0, 0, -5));
                    Ball(p, "Cap badge", V(0, 2.01f, -.4f), V(.13f, .13f, .035f), Gold);
                    Tube(p, "Postal satchel strap", V(.35f, 1.11f, -.42f), V(-.4f, .46f, -.4f), .05f, DarkWood);
                    Box(p, "Postal satchel", V(-.44f, .44f, -.24f), V(.45f, .49f, .3f), Hex(0x608496));
                    Box(p, "Letter", V(-.44f, .47f, -.401f), V(.28f, .19f, .022f), Cream);
                    Tube(p, "Envelope fold", V(-.57f, .55f, -.42f), V(-.44f, .46f, -.42f), .008f, Wood);
                    Tube(p, "Envelope fold", V(-.44f, .46f, -.42f), V(-.31f, .55f, -.42f), .008f, Wood);
                }
                if (id == 4)
                {
                    Box(p, "Baker apron", V(0, .66f, -.42f), V(.65f, .76f, .08f), Cream);
                    Box(p, "Apron pocket", V(0, .42f, -.48f), V(.38f, .23f, .05f), Hex(0xDDCDA8));
                    Ball(p, "Chef hat band", V(0, 1.98f, 0), V(.83f, .25f, .67f), Cream);
                    for (int i = 0; i < 5; i++) { float a = i * Mathf.PI * 2 / 5; Ball(p, "Chef hat puff", V(Mathf.Cos(a) * .21f, 2.15f, Mathf.Sin(a) * .15f), V(.4f, .43f, .4f), Cream); }
                    Bread(p, V(0, .91f, -.58f), .75f);
                }
            }
            for (int s = -1; s <= 1; s += 2)
            {
                float x = s * (id == 0 ? .25f : .235f), y = id == 0 ? 1.64f : 1.62f, z = id == 0 ? -.52f : -.423f;
                var eye = Group(residentRoot, "Blinking eye " + s, V(x, y, z));
                Ball(eye, "Eye", Vector3.zero, V(id == 0 ? .15f : .095f, id == 0 ? .19f : .12f, .065f), Ink);
                Ball(eye, "Eye catchlight", V(-.022f, .032f, -.033f), Vector3.one * .032f, Cream);
                Combine(eye); motion.eyes[s < 0 ? 0 : 1] = eye;
                if (id != 0) Ball(p, "Rosy cheek", V(s * .35f, 1.43f, -.435f), V(.15f, .07f, .02f), Hex(0xCC9A7E));
            }
            Combine(p);
            // Keep blinking meshes separate from the combined body, then parent
            // them to its pose so a lean cannot leave the eyes behind the face.
            foreach (var eye in motion.eyes) eye.SetParent(p, false);
            motion.Pose(0, 0); return residentRoot;
        }
        static void Bread(Transform p, Vector3 position, float scale)
        {
            var g = Group(p, "Fresh bread", position); g.localScale = Vector3.one * scale;
            Ball(g, "Loaf", V(0, 0, 0), V(.92f, .4f, .42f), Honey);
            for (int i = 0; i < 3; i++) Box(g, "Loaf scoring", V(-.25f + i * .25f, .18f, -.06f), V(.055f, .025f, .23f), Cream, V(0, -20, 0));
        }
        public static Transform TeaParty(Transform parent)
        {
            var p = Group(parent, "Opening day woodland tea party");
            Ball(p, "Woven picnic rug", V(0, -.05f, 0), V(4.9f, .08f, 3), Moss);
            for (int i = 0; i < 5; i++)
            {
                var resident = Resident(p, i); resident.localPosition = V(-1.8f + i * .9f, 0, .45f + Mathf.Sin(i * Mathf.PI / 4) * .35f); resident.localScale = Vector3.one * .66f;
                resident.localEulerAngles = V(0, (2 - i) * -7, 0);
            }
            Box(p, "Tea table", V(0, .6f, -.67f), V(3.7f, .16f, 1.13f), Honey);
            for (int s = -1; s <= 1; s += 2) Box(p, "Tea table foot", V(s * 1.4f, .27f, -.67f), V(.16f, .6f, .75f), DarkWood);
            Box(p, "Linen table runner", V(0, .69f, -.67f), V(2.9f, .015f, .43f), Cream);
            for (int i = 0; i < 5; i++) { var cup = Product(p, 0); cup.localPosition = V(-1.4f + i * .7f, .7f, -.76f); cup.localScale = Vector3.one * .55f; }
            Bread(p, V(-.8f, .79f, -.4f), .6f); Pot(p, V(.85f, .7f, -.4f), .45f, true);
            for (int s = -1; s <= 1; s += 2) Tube(p, "Bunting post", V(s * 2.2f, 0, .9f), V(s * 2.2f, 2.3f, .9f), .035f, Honey);
            for (int i = 0; i < 8; i++)
            {
                float x = -2.2f + i * .55f, y = 2.18f - Mathf.Sin(i * Mathf.PI / 8) * .22f;
                Tube(p, "Bunting string", V(x, y, .9f), V(x + .55f, 2.18f - Mathf.Sin((i + 1) * Mathf.PI / 8) * .22f, .9f), .01f, DarkWood);
                Ball(p, "Celebration leaf pennant", V(x + .25f, y - .14f, .9f), V(.23f, .31f, .035f), i % 2 == 0 ? Cream : Gold, V(0, 0, -12));
            }
            Combine(p); return p;
        }
        public static void Flower(Transform parent, Vector3 position, float scale, Color petal)
        {
            var p = Group(parent, "Flower", position);
            for (int i = 0; i < 6; i++) { float a = i * Mathf.PI / 3; Ball(p, "Petal", V(Mathf.Sin(a) * scale * .68f, Mathf.Cos(a) * scale * .68f, 0), V(scale, scale, scale * .35f), petal); }
            Ball(p, "Pollen", V(0, 0, -.045f), Vector3.one * scale * .7f, Gold);
        }
        public static Transform Crop(Transform parent, int id)
        {
            if (id >= 4) return ForestExpansionModels.Crop(parent, id);
            var p = Group(parent, "Crop " + id);
            if (id == 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    float a = i * 2.3f, x = Mathf.Sin(a) * .25f, z = Mathf.Cos(a) * .20f, height = .68f + i % 2 * .23f;
                    Tube(p, "Mint stem", V(x, 0, z), V(x * .8f, height, z), .022f, Deep);
                    for (int j = 0; j < 3; j++) for (int s = -1; s <= 1; s += 2) Ball(p, "Mint leaf", V(x + s * .13f, (.22f + j * .23f) * height / .91f, z), V(.39f, .09f, .25f), j == 2 ? Hex(0x98B88B) : Leaf, V(-s * 32, a * 57 + j * 35, s * 32));
                }
            }
            if (id == 1)
                for (int i = 0; i < 3; i++)
                {
                    var at = V((i - 1) * .25f, 0, (i % 2) * .18f); var g = Group(p, "Lantern mushroom", at); float scale = i == 1 ? 1 : .72f; g.localScale = Vector3.one * scale;
                    Shape(g, "Ivory stalk", Lathe(new[] { V2(0, 0), V2(.12f, 0), V2(.08f, .42f), V2(0, .45f) }), Vector3.zero, Vector3.one, Cream);
                    Shape(g, "Luminous cap", Lathe(new[] { V2(0, .39f), V2(.28f, .39f), V2(.32f, .45f), V2(.26f, .58f), V2(.12f, .67f), V2(0, .69f) }), Vector3.zero, Vector3.one, Gold, glow: true);
                    for (int j = 0; j < 4; j++) { float a = j * 1.7f; Ball(g, "Cap fleck", V(Mathf.Cos(a) * .19f, .6f, Mathf.Sin(a) * .19f), V(.08f, .03f, .08f), Cream); }
                }
            if (id == 2)
                for (int i = 0; i < 4; i++)
                {
                    float x = (i - 1.5f) * .16f, z = i % 2 * .16f;
                    Tube(p, "Bell stem", V(x, 0, z), V(x, .65f, z), .023f, Deep);
                    Tube(p, "Drooping stem", V(x, .65f, z), V(x + .13f, .65f, z), .023f, Deep);
                    Shape(p, "Bellflower", Lathe(new[] { V2(.05f, 0), V2(.16f, .01f), V2(.13f, .12f), V2(.07f, .22f), V2(0, .23f) }, 12), V(x + .13f, .36f, z), Vector3.one, Hex(0xA79AC0));
                    Ball(p, "Bell leaf", V(x - .08f, .22f, z), V(.27f, .055f, .13f), Leaf, V(0, 0, -30));
                }
            if (id == 3)
            {
                Shape(p, "Water", Lathe(new[] { V2(0, .04f), V2(.48f, .04f), V2(.48f, .08f), V2(0, .08f) }), Vector3.zero, Vector3.one, Blue);
                for (int i = 0; i < 3; i++) { float a = i * 2.2f; Ball(p, "Lotus pad", V(Mathf.Sin(a) * .22f, .1f + i * .025f, Mathf.Cos(a) * .22f), V(.48f, .035f, .4f), Moss, V(0, i * 70, 0)); }
                Tube(p, "Lotus stem", V(.1f, 0, 0), V(.1f, .33f, 0), .02f, Deep);
                for (int i = 0; i < 7; i++) { float a = i * Mathf.PI * 2 / 7; Ball(p, "Lotus petal", V(.1f + Mathf.Sin(a) * .1f, .35f, Mathf.Cos(a) * .1f), V(.13f, .21f, .1f), Cream, V(Mathf.Cos(a) * 24, 0, Mathf.Sin(a) * 24)); }
            }
            Combine(p); return p;
        }
        static Vector2 V2(float x, float y) => new Vector2(x, y);
        public static Transform Product(Transform parent, int id)
        {
            if (id >= 6) return ForestExpansionModels.Product(parent, id);
            var p = Group(parent, "Product " + id);
            if (id == 0)
            {
                Shape(p, "Ceramic tea cup", Lathe(new[] { V2(0, 0), V2(.22f, 0), V2(.29f, .07f), V2(.3f, .37f), V2(.25f, .39f), V2(.23f, .1f), V2(0, .1f) }), Vector3.zero, Vector3.one, Cream);
                Ring(p, "Cup handle", V(.31f, .23f, 0), .14f, .035f, Honey, V(90, 0, 0));
                Shape(p, "Tea", Lathe(new[] { V2(0, .34f), V2(.25f, .34f), V2(0, .345f) }), Vector3.zero, Vector3.one, Hex(0x8FAD78));
                Ball(p, "Mint garnish", V(-.12f, .36f, 0), V(.21f, .025f, .1f), Leaf, V(0, 20, 0));
                Ring(p, "Saucer", V(0, -.025f, 0), .34f, .055f, Cream);
            }
            else if (id == 2)
            {
                Shape(p, "Powder pouch", Lathe(new[] { V2(0, 0), V2(.23f, 0), V2(.31f, .16f), V2(.24f, .38f), V2(.11f, .46f), V2(.18f, .55f), V2(0, .55f) }), Vector3.zero, Vector3.one, Hex(0xD9C395));
                Ring(p, "Tied cord", V(0, .43f, 0), .115f, .035f, DarkWood);
                Flower(p, V(0, .23f, -.3f), .11f, Hex(0xAD9CBD));
            }
            else if (id == 5)
            {
                Box(p, "Glazed night incense tray", V(0, .035f, 0), V(.66f, .08f, .4f), Hex(0x697A8D));
                for (int i = 0; i < 5; i++)
                {
                    Tube(p, "Lavender incense", V(-.18f + i * .09f, .08f, 0), V(-.16f + i * .08f, .72f + (i % 2) * .06f, .02f), .034f, Hex(0xA79AC0));
                    Shape(p, "Warm incense ember", Sphere(), V(-.16f + i * .08f, .72f + i % 2 * .06f, .02f), Vector3.one * .055f, Hex(0xDEAA70), glow: true);
                }
                Box(p, "Night violet paper sleeve", V(0, .27f, -.015f), V(.44f, .35f, .18f), Hex(0x9184AA));
                Shape(p, "Carved crescent seal", Sweep(new[] { V(.06f, .38f, -.13f), V(-.06f, .36f, -.13f), V(-.09f, .27f, -.13f), V(-.04f, .19f, -.13f), V(.07f, .2f, -.13f) }, new[] { 0, .025f, .027f, .025f, 0 }, .45f), Vector3.zero, Vector3.one, Cream);
                Tube(p, "Lavender sprig stem", V(.25f, .08f, -.1f), V(.29f, .47f, -.1f), .012f, Deep);
                for (int i = 0; i < 4; i++) Ball(p, "Lavender buds", V(.29f + (i % 2 == 0 ? -.02f : .02f), .36f + i * .045f, -.1f), V(.085f, .07f, .065f), Hex(0xA79AC0));
            }
            else
            {
                Color liquid = id == 1 ? Gold : id == 3 ? Blue : Hex(0xD69568);
                float r = id == 1 ? .34f : id == 3 ? .23f : .29f;
                Vector2[] profile = id == 1
                    ? new[] { V2(0, 0), V2(.3f, 0), V2(.34f, .08f), V2(.34f, .4f), V2(.3f, .46f), V2(.22f, .48f), V2(.22f, .56f), V2(0, .56f) }
                    : id == 3 ? new[] { V2(0, 0), V2(.12f, 0), V2(.24f, .13f), V2(.23f, .29f), V2(.11f, .5f), V2(.08f, .7f), V2(0, .7f) }
                    : new[] { V2(0, 0), V2(.25f, 0), V2(.29f, .08f), V2(.29f, .36f), V2(.23f, .45f), V2(.13f, .5f), V2(.13f, .58f), V2(0, .58f) };
                Shape(p, "Glazed potion bottle", Lathe(profile), Vector3.zero, Vector3.one, liquid, glow: id == 1);
                Shape(p, "Cork", Lathe(new[] { V2(0, .56f), V2(.105f, .56f), V2(.11f, .68f), V2(0, .68f) }), Vector3.zero, Vector3.one, Wood);
                Ring(p, "Bottle neck tie", V(0, .51f, 0), .115f, .022f, DarkWood);
                if (id == 1)
                {
                    Ring(p, "Wide lantern lid", V(0, .57f, 0), .25f, .055f, DarkWood);
                    Ring(p, "Carrying loop", V(0, .74f, 0), .22f, .025f, Gold, V(90, 0, 0));
                    for (int s = -1; s <= 1; s += 2) Tube(p, "Lantern cage", V(s * .3f, .05f, -.15f), V(s * .3f, .49f, -.15f), .018f, DarkWood);
                }
                if (id == 3) { Shape(p, "Tall rain stopper", Lathe(new[] { V2(0, .68f), V2(.1f, .68f), V2(.1f, .8f), V2(0, .8f) }), Vector3.zero, Vector3.one, Deep); }
                if (id == 4) { Ring(p, "Warm flask handle", V(.3f, .27f, 0), .18f, .045f, DarkWood, V(90, 0, 0)); Box(p, "Woven flask sleeve", V(0, .2f, -.01f), V(.59f, .25f, .53f), Honey); }
                Box(p, "Cream label", V(0, .23f, -r + .005f), V(.23f, .22f, .025f), Cream);
                if (id == 1) { Ball(p, "Little firefly", V(0, .24f, -r - .02f), Vector3.one * .08f, Gold); }
                if (id == 3) { Ball(p, "Rain drop label", V(0, .24f, -r - .02f), V(.075f, .12f, .025f), Deep); }
                if (id == 4) { Flower(p, V(0, .23f, -r - .025f), .07f, Copper); }
                Ball(p, "Glaze reflection", V(-r * .6f, .32f, -r * .7f), V(.035f, .16f, .02f), Cream, V(0, 10, -12));
            }
            Combine(p); return p;
        }
        public static Transform Cauldron(Transform parent)
        {
            var p = Group(parent, "Copper magic cauldron");
            Shape(p, "Hammered bowl", Lathe(new[] { V2(0, .25f), V2(.6f, .25f), V2(.92f, .48f), V2(1.02f, .95f), V2(.94f, 1.3f), V2(.85f, 1.33f), V2(.83f, 1.19f), V2(0, 1.14f) }), Vector3.zero, Vector3.one, Copper);
            Ring(p, "Rolled rim", V(0, 1.3f, 0), .91f, .085f, Gold);
            Shape(p, "Potion surface", Lathe(new[] { V2(0, 1.21f), V2(.84f, 1.21f), V2(0, 1.22f) }), Vector3.zero, Vector3.one, Blue, glow: true);
            Ring(p, "Potion ripple", V(-.12f, 1.23f, -.1f), .38f, .018f, Cream);
            for (int i = 0; i < 3; i++) { float a = i * Mathf.PI * 2 / 3; Tube(p, "Curved foot", V(Mathf.Cos(a) * .6f, .35f, Mathf.Sin(a) * .6f), V(Mathf.Cos(a) * .78f, .04f, Mathf.Sin(a) * .78f), .13f, DarkWood); }
            for (int s = -1; s <= 1; s += 2) Ring(p, "Side handle", V(s * 1.02f, 1.03f, 0), .28f, .07f, DarkWood, V(90, 0, 0));
            for (int i = 0; i < 7; i++) { float a = (i - 3) * .22f; Ball(p, "Brass rivet", V(Mathf.Sin(a) * .98f, 1.05f, -Mathf.Cos(a) * .98f), Vector3.one * .065f, Gold); }
            for (int i = 0; i < 5; i++) { float a = i * Mathf.PI * 2 / 5; Ball(p, "Embossed leaf", V(Mathf.Sin(a) * .27f, .69f + Mathf.Cos(a) * .25f, -.94f), V(.13f, .28f, .04f), Gold, V(0, 0, -i * 72)); }
            Combine(p);
            var spoon = Group(p, "Stirring spoon"); spoon.localPosition = V(.1f, 1.15f, 0); spoon.localEulerAngles = V(-18, 0, -28);
            Tube(spoon, "Spoon handle", Vector3.zero, V(0, 1.3f, 0), .055f, DarkWood);
            Ball(spoon, "Spoon bowl", V(0, .05f, 0), V(.21f, .35f, .07f), Honey); return p;
        }
        public static Transform Bench(Transform parent)
        {
            var p = Group(parent, "Enchanted workbench");
            for (int s = -1; s <= 1; s += 2) for (int z = -1; z <= 1; z += 2) Box(p, "Carved bench leg", V(s * .85f, .48f, z * .39f), V(.16f, .96f, .16f), Deep, V(0, 0, -s * 5));
            Box(p, "Worktop", V(0, .99f, 0), V(2.16f, .18f, 1.16f), Honey);
            Box(p, "Bench lower shelf", V(0, .28f, 0), V(1.8f, .1f, .9f), Wood);
            Box(p, "Sage cutting mat", V(-.28f, 1.1f, -.05f), V(.95f, .025f, .78f), Moss);
            Shape(p, "Porcelain mortar", Lathe(new[] { V2(0, 0), V2(.2f, 0), V2(.3f, .32f), V2(.24f, .34f), V2(.1f, .1f), V2(0, .1f) }), V(-.45f, 1.1f, -.08f), Vector3.one, Cream);
            Tube(p, "Pestle", V(-.43f, 1.22f, -.1f), V(-.27f, 1.62f, -.1f), .07f, Honey);
            var a = Product(p, 1); a.localPosition = V(.6f, 1.1f, .18f); a.localScale = Vector3.one * .7f;
            var b = Product(p, 2); b.localPosition = V(.55f, 1.1f, -.31f); b.localScale = Vector3.one * .65f;
            Box(p, "Recipe paper", V(.09f, 1.104f, .26f), V(.42f, .015f, .48f), Cream, V(0, 12, 0));
            for (int i = 0; i < 4; i++) Box(p, "Recipe ink line", V(.08f, 1.119f, .1f + i * .08f), V(.29f, .006f, .011f), Leaf, V(0, 12, 0));
            Ring(p, "Little brass scale", V(.76f, 1.5f, .32f), .15f, .02f, Gold, V(90, 0, 0));
            Combine(p); return p;
        }
        public static Transform Shelf(Transform parent)
        {
            var p = Group(parent, "Display shelf");
            for (int s = -1; s <= 1; s += 2) Box(p, "Shelf side", V(s * .51f, .76f, .13f), V(.13f, 1.52f, .55f), Wood);
            for (int j = 0; j < 3; j++) Box(p, "Shelf tier", V(0, .17f + j * .57f, .13f), V(1.14f, .11f, .61f), Honey);
            Box(p, "Shelf crown", V(0, 1.54f, .15f), V(1.18f, .18f, .59f), Wood);
            Box(p, "Shelf backing", V(0, .8f, .39f), V(1.05f, 1.31f, .06f), DarkWood);
            Combine(p); return p;
        }
        public static Transform Pot(Transform parent, Vector3 at, float size, bool flowers)
        {
            var p = Group(parent, "Terracotta planter", at); p.localScale = Vector3.one * size;
            Shape(p, "Pot", Lathe(new[] { V2(0, 0), V2(.24f, 0), V2(.34f, .45f), V2(.38f, .46f), V2(.38f, .55f), V2(.29f, .55f), V2(.27f, .45f), V2(0, .45f) }), Vector3.zero, Vector3.one, Copper);
            if (flowers)
                for (int i = 0; i < 4; i++) { float a = i * 2; var v = V(Mathf.Sin(a) * .19f, .92f + i % 2 * .17f, Mathf.Cos(a) * .18f); Tube(p, "Flower stalk", V(0, .45f, 0), v, .021f, Deep); Flower(p, v, .13f, i % 2 == 0 ? Cream : Hex(0xC99689)); }
            else { var crop = Crop(p, 0); crop.localPosition = V(0, .48f, 0); crop.localScale = Vector3.one * .65f; }
            Combine(p); return p;
        }
    }
}
