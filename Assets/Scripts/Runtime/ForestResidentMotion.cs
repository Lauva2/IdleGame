using UnityEngine;

namespace ForestShop
{
    public sealed class ForestResidentMotion : MonoBehaviour
    {
        public Transform leftFoot, rightFoot, leftArm, rightArm;
        public readonly Transform[] eyes = new Transform[2];
        public readonly Transform[] thighs = new Transform[2], shins = new Transform[2], knees = new Transform[2];
        public readonly Transform[] upperArms = new Transform[2], forearms = new Transform[2], elbows = new Transform[2], hands = new Transform[2];
        public int resident;
        Transform body;
        bool routeStarted;
        Vector3 lastRoute;
        float lastPhase, routeDistance;

        public void Pose(float stride, float reach, float glance = 0, bool animate = false)
        {
            if (!animate) stride = reach = glance = 0;
            float step = resident == 3 ? 26 : resident == 4 ? 16 : 21;
            leftFoot.localEulerAngles = new Vector3(stride * step, 0, 0);
            rightFoot.localEulerAngles = new Vector3(-stride * step, 0, 0);
            leftFoot.localPosition = new Vector3(-.27f, .27f + Mathf.Max(0, stride) * .025f, -.05f);
            rightFoot.localPosition = new Vector3(.27f, .27f + Mathf.Max(0, -stride) * .025f, -.05f);
            PoseUpperBody(stride, reach, glance, animate, 0, 0);
            JoinLegs();
        }

        // Route coordinates belong to the shop, before its 1.08 scale. Feet are
        // posed against the actual stair treads, not the old diagonal ramp.
        // This only presents the route: sales and the 52 second visit stay in World3D.
        public void PoseRoute(Vector3 routePosition, Quaternion facing, float phase, bool walking,
            float reach, float glance, bool animate)
        {
            transform.localRotation = Quaternion.Euler(0, facing.eulerAngles.y, 0);
            if (!animate)
            {
                routeStarted = false;
                transform.localPosition = routePosition;
                Pose(0, 0, 0, false);
                return;
            }

            float parentScale = Mathf.Abs(transform.parent.localScale.y);
            float modelScale = Mathf.Abs(transform.localScale.y);
            float worldScale = Mathf.Max(.01f, parentScale * modelScale);
            float distance = routeStarted ? Vector2.Distance(new Vector2(routePosition.x, routePosition.z), new Vector2(lastRoute.x, lastRoute.z)) * parentScale : 0;
            if (!routeStarted || phase < lastPhase || distance > 2) routeDistance = 0;
            else if (walking) routeDistance += distance;
            routeStarted = true; lastRoute = routePosition; lastPhase = phase;

            var shopOrigin = transform.parent.localPosition;
            float stageZ = shopOrigin.z + routePosition.z * parentScale;
            float cycle = ForestVisitorGait.FootCycle(phase, stageZ, routeDistance);
            bool stairs = stageZ > -16.5f && stageZ < -13.65f && (phase < 13 || phase >= 39);
            float stride = walking ? Mathf.Sin(cycle * Mathf.PI * 2) : 0;
            var left = ForestVisitorGait.Footfall(cycle, walking, stairs, worldScale);
            var right = ForestVisitorGait.Footfall(cycle + .5f, walking, stairs, worldScale);
            Vector3 leftPoint = ContactPoint(routePosition, left, -.27f, modelScale);
            Vector3 rightPoint = ContactPoint(routePosition, right, .27f, modelScale);
            float leftGround = ContactHeight(leftPoint, left, routePosition, shopOrigin, parentScale);
            float rightGround = ContactHeight(rightPoint, right, routePosition, shopOrigin, parentScale);
            float leftWeight = walking ? .5f + .5f * Mathf.Sin(cycle * Mathf.PI * 2) : .5f;
            float support = Mathf.Lerp(rightGround, leftGround, leftWeight);
            float floorUnderBody = ForestVisitorGait.ShopSurfaceHeight(routePosition, shopOrigin, parentScale);
            // A rounded belly must stay above the leading stair even while the
            // trailing foot remains lower. The feet retain the height difference.
            support = Mathf.Max(support, floorUnderBody - .065f / parentScale);
            routePosition.y = support - .02f * modelScale;
            transform.localPosition = routePosition;
            SetFoot(leftFoot, leftPoint, leftGround + left.lift / parentScale, left, -.27f, routePosition, modelScale);
            SetFoot(rightFoot, rightPoint, rightGround + right.lift / parentScale, right, .27f, routePosition, modelScale);
            float nod = reach > .01f && phase >= 36 && phase < 37.4f ? Mathf.Sin((phase - 36) * Mathf.PI / 1.4f) : 0;
            PoseUpperBody(stride, reach, glance, true, nod, stairs && walking ? 3 : walking ? 1.5f : 0);
            JoinLegs();
        }

        void JoinLegs()
        {
            if (body == null) return;
            for (int i = 0; i < 2; i++)
            {
                if (thighs[i] == null) continue;
                var foot = i == 0 ? leftFoot : rightFoot;
                Vector3 hip = transform.InverseTransformPoint(body.TransformPoint(new Vector3(i == 0 ? -.26f : .26f, .51f, .015f)));
                Vector3 ankle = foot.localPosition + foot.localRotation * new Vector3(0, -.04f, 0);
                Vector3 knee = Vector3.Lerp(hip, ankle, .52f) + Vector3.back * Mathf.Clamp(.19f - Vector3.Distance(hip, ankle) * .2f, .045f, .14f);
                LegSegment(thighs[i], hip, knee, .26f);
                LegSegment(shins[i], knee, ankle, .21f);
                knees[i].localPosition = knee;
            }
        }

        static void LegSegment(Transform segment, Vector3 a, Vector3 b, float width)
        {
            segment.localPosition = (a + b) * .5f;
            segment.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
            segment.localScale = new Vector3(width, Vector3.Distance(a, b) + width * .25f, width);
        }

        public Vector3 RightHand => hands[1].position;
        public Vector3 HandTarget(Vector3 local) => transform.TransformPoint(local);
        public void AimRightHand(Vector3 target, float weight)
        {
            PoseArm(1, Vector3.Lerp(RightHand, target, Mathf.Clamp01(weight)));
        }

        public void LeanOverCounter(float weight)
        {
            if (body == null) return;
            body.localRotation *= Quaternion.Euler(-10 * weight, 0, 0);
            PlaceShoulders(); JoinArms(); JoinLegs();
        }

        void PlaceShoulders()
        {
            leftArm.localPosition = transform.InverseTransformPoint(body.TransformPoint(new Vector3(-.45f, 1.05f, 0)));
            rightArm.localPosition = transform.InverseTransformPoint(body.TransformPoint(new Vector3(.45f, 1.05f, 0)));
        }

        void JoinArms()
        {
            if (hands[0] == null) return;
            PoseArm(0, leftArm.TransformPoint(new Vector3(0, -.51f, -.12f)));
            PoseArm(1, rightArm.TransformPoint(new Vector3(0, -.51f, -.12f)));
        }

        void PoseArm(int index, Vector3 worldTarget)
        {
            var pivot = index == 0 ? leftArm : rightArm;
            Vector3 shoulder = pivot.localPosition;
            Vector3 offset = transform.InverseTransformPoint(worldTarget) - shoulder;
            float distance = Mathf.Clamp(offset.magnitude, .08f, .56f);
            Vector3 direction = offset.sqrMagnitude > .000001f ? offset.normalized : Vector3.down;
            Vector3 palm = shoulder + direction * distance;
            Vector3 bend = new Vector3(index == 0 ? -1 : 1, 0, -.3f);
            bend = Vector3.ProjectOnPlane(bend, direction).normalized;
            float half = distance * .5f;
            Vector3 elbow = shoulder + direction * half + bend * Mathf.Sqrt(Mathf.Max(0, .285f * .285f - half * half));
            LegSegment(upperArms[index], shoulder, elbow, .25f);
            LegSegment(forearms[index], elbow, palm, .23f);
            elbows[index].localPosition = elbow;
            hands[index].localPosition = palm;
            hands[index].localRotation = pivot.localRotation;
        }

        Vector3 ContactPoint(Vector3 routePosition, ForestVisitorGait.Step foot, float side, float modelScale)
        {
            var offset = new Vector3(side * modelScale, 0, -foot.forward / Mathf.Max(.01f, transform.parent.localScale.y));
            return routePosition + transform.localRotation * offset;
        }

        float ContactHeight(Vector3 contact, ForestVisitorGait.Step foot, Vector3 routePosition,
            Vector3 origin, float parentScale)
        {
            if (!foot.swing) return ForestVisitorGait.ShopSurfaceHeight(contact, origin, parentScale);
            Vector3 forward = transform.localRotation * Vector3.back;
            float sinceLift = ForestVisitorGait.StepLength * foot.transfer;
            Vector3 launch = routePosition - forward * ((sinceLift + ForestVisitorGait.StepLength * .5f) / parentScale);
            Vector3 landing = routePosition + forward * ((ForestVisitorGait.StepLength * 1.5f - sinceLift) / parentScale);
            float a = ForestVisitorGait.ShopSurfaceHeight(launch, origin, parentScale);
            float b = ForestVisitorGait.ShopSurfaceHeight(landing, origin, parentScale);
            float carried = Mathf.Lerp(a, b, Mathf.SmoothStep(0, 1, foot.transfer));
            float underneath = ForestVisitorGait.ShopSurfaceHeight(contact, origin, parentScale);
            // Keep the sole above the riser throughout a swing, including descent.
            float clearance = .018f * Mathf.Sin(foot.transfer * Mathf.PI) / parentScale;
            return Mathf.Max(carried, underneath - foot.lift / parentScale + clearance);
        }

        void SetFoot(Transform foot, Vector3 contact, float soleHeight, ForestVisitorGait.Step step,
            float side, Vector3 rootPosition, float modelScale)
        {
            Vector3 local = Quaternion.Inverse(transform.localRotation) * (contact - rootPosition) / Mathf.Max(.01f, modelScale);
            // The mesh sole is .25 below its pivot and .12 in front of it.
            // Correct the pivot for the toe roll so a planted sole stays planted.
            Quaternion roll = Quaternion.Euler(step.pitch, 0, 0);
            Vector3 sole = roll * new Vector3(0, -.25f, -.12f);
            foot.localPosition = new Vector3(side, (soleHeight - rootPosition.y) / modelScale - sole.y, local.z - sole.z);
            foot.localRotation = roll;
        }

        void PoseUpperBody(float stride, float reach, float glance, bool animate, float nod, float lean)
        {
            if (body == null) body = transform.Find("Sculpted body");
            if (body != null)
            {
                body.localPosition = new Vector3(-stride * .025f, -Mathf.Abs(stride) * .012f, 0);
                body.localRotation = Quaternion.Euler(-lean - nod * 7 - glance * 2.5f, glance * 3, stride * 1.4f);
            }
            leftArm.localEulerAngles = new Vector3(-stride * 15 + glance * 29, -glance * 5, -13 - glance * 8);
            rightArm.localEulerAngles = new Vector3(Mathf.Lerp(stride * 15 + glance * 6, 82, reach), -reach * 5, 13 + reach * 3);
            leftArm.localScale = rightArm.localScale = Vector3.one;
            if (body != null) PlaceShoulders();
            JoinArms();
            float blinkPhase = (Time.unscaledTime + resident * 1.17f) % (4.1f + resident * .23f);
            float openness = animate && blinkPhase < .18f
                ? Mathf.Lerp(.08f, 1, Mathf.Abs(blinkPhase - .09f) / .09f) : 1;
            foreach (var eye in eyes) if (eye != null) eye.localScale = new Vector3(1, openness, 1);
        }
    }
}
