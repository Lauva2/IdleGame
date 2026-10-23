using UnityEngine;

namespace ForestShop
{
    public sealed class ForestResidentMotion : MonoBehaviour
    {
        public Transform leftFoot, rightFoot, leftArm, rightArm;
        public readonly Transform[] eyes = new Transform[2];
        public int resident;
        public void Pose(float stride, float reach, float glance = 0, bool animate = false)
        {
            float step = resident == 3 ? 26 : resident == 4 ? 16 : 21;
            leftFoot.localEulerAngles = new Vector3(stride * step, 0, 0);
            rightFoot.localEulerAngles = new Vector3(-stride * step, 0, 0);
            leftFoot.localPosition = new Vector3(-.27f, .27f + Mathf.Max(0, stride) * .025f, -.05f);
            rightFoot.localPosition = new Vector3(.27f, .27f + Mathf.Max(0, -stride) * .025f, -.05f);
            leftArm.localEulerAngles = new Vector3(-stride * 12 + glance * 13, 0, -13 - glance * 5);
            rightArm.localEulerAngles = new Vector3(Mathf.Lerp(stride * 15, 58, reach), 0, 13);
            float blinkPhase = (Time.unscaledTime + resident * 1.17f) % (4.1f + resident * .23f);
            float openness = animate && blinkPhase < .18f
                ? Mathf.Lerp(.08f, 1, Mathf.Abs(blinkPhase - .09f) / .09f) : 1;
            foreach (var eye in eyes) if (eye != null) eye.localScale = new Vector3(1, openness, 1);
        }
    }
}
