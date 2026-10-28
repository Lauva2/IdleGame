using System.Collections.Generic;
using UnityEngine;
using static ForestShop.ForestGeometry;

namespace ForestShop
{
    public sealed partial class ForestWorld3D
    {
        readonly List<Transform> breezeTufts = new List<Transform>();
        readonly List<int> breezeRegions = new List<int>();

        void BuildAmbientLife()
        {
            AddBreezeTuft(worldRoot, V(-17, .14f, 3), 0);
            AddBreezeTuft(worldRoot, V(17, .14f, -5), 0);
            AddBreezeTuft(worldRoot, V(5, .14f, 18), 0);
            for (int region = 0; region < 3; region++)
            {
                AddBreezeTuft(forestScene, ForestCenters[region] + V(-6.2f, .18f, -2.4f), region + 4);
                AddBreezeTuft(forestScene, ForestCenters[region] + V(6.4f, .18f, 3.8f), region + 4);
            }
        }

        void AddBreezeTuft(Transform parent, Vector3 at, int region)
        {
            var tuft = Group(parent, "Breeze grass " + breezeTufts.Count, at);
            for (int blade = 0; blade < 5; blade++)
            {
                float angle = blade * 2.4f, length = .45f + blade % 3 * .09f;
                Vector3 end = V(Mathf.Cos(angle) * .28f, length, Mathf.Sin(angle) * .28f);
                Shape(tuft, "Curved grass blade", Sweep(new[] { Vector3.zero, end * .6f + Vector3.up * .05f, end }, new[] { .035f, .045f, 0 }, .25f), V((blade % 2) * .08f, 0, 0), Vector3.one, blade % 2 == 0 ? Leaf : Moss);
            }
            Combine(tuft);
            foreach (var renderer in tuft.GetComponentsInChildren<MeshRenderer>())
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            breezeTufts.Add(tuft); breezeRegions.Add(region);
        }

        void UpdateAmbientLife()
        {
            bool animate = state != null && !state.simpleAnimation && worldCamera.enabled;
            for (int i = 0; i < breezeTufts.Count; i++)
            {
                bool visibleRegion = breezeRegions[i] == 0 ? Region < 4 : Region == breezeRegions[i];
                float sway = animate && visibleRegion ? Mathf.Sin(Time.unscaledTime * .78f + i * 1.3f) * 3.5f : 0;
                breezeTufts[i].localRotation = Quaternion.Euler(sway * .45f, 0, sway);
            }
        }
    }
}
