using UnityEngine;

namespace ForestShop
{
    // Distances below use the same stage units as CourtyardPaths. One footstep
    // is one .45-wide tread; the two feet alternate rather than following a ramp.
    public static class ForestVisitorGait
    {
        public const float StepLength = .45f;
        public struct Step
        {
            public float forward, lift, pitch, transfer;
            public bool swing;
        }

        public static float FootCycle(float visitPhase, float stageZ, float indoorDistance)
        {
            // Calibrate the landings to tread centres -15.8, -15.35, ... -14.
            // The approach and departure share that spacing before reaching stairs.
            float distance = visitPhase < 13 ? stageZ + 16.025f
                : visitPhase >= 39 ? -13.775f - stageZ : indoorDistance;
            return Mathf.Repeat(distance / (StepLength * 2), 1);
        }

        public static Step Footfall(float cycle, bool walking, bool stairs, float modelWorldScale)
        {
            if (!walking) return new Step { forward = .17f * modelWorldScale };
            float u = Mathf.Repeat(cycle, 1);
            if (u < .5f) return new Step { forward = StepLength * (.5f - u * 2) };
            float t = (u - .5f) * 2;
            float arc = Mathf.Sin(t * Mathf.PI);
            return new Step
            {
                swing = true, transfer = t,
                forward = StepLength * (Mathf.SmoothStep(0, 1, t) - .5f),
                lift = (stairs ? .24f : .085f) * arc,
                pitch = (14 - 18 * t) * arc
            };
        }

        // Return the supporting surface in shop-local coordinates. No physics,
        // state, sales, cadence timer, or saved player data is changed here.
        public static float ShopSurfaceHeight(Vector3 local, Vector3 origin, float shopScale)
        {
            shopScale = Mathf.Max(.01f, shopScale);
            float x = origin.x + local.x * shopScale;
            float z = origin.z + local.z * shopScale;
            float stageHeight = .168f;
            if (Mathf.Abs(x + 5.4f) <= 1.25f)
                for (int i = 0; i < 5; i++)
                    if (Mathf.Abs(z - (-15.8f + i * .45f)) <= .265f)
                        stageHeight = Mathf.Max(stageHeight, .305f + i * .23f);
            float localHeight = (stageHeight - origin.y) / shopScale;
            if (Mathf.Abs(local.x) <= 6.24f && local.z >= -3.9f && local.z <= 3.3f)
                localHeight = Mathf.Max(localHeight, 4.625f);
            if (Mathf.Abs(local.x + 5) <= .65f && local.z >= -3.905f && local.z <= -3.455f)
                localHeight = Mathf.Max(localHeight, 4.685f);
            return localHeight;
        }
    }
}
