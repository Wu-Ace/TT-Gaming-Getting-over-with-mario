using UnityEngine;

namespace Character.Control
{
    // Visual-only two-bone rig. Physics, joints and input remain on the original objects.
    [ExecuteAlways, DefaultExecutionOrder(200)]
    public sealed class PaddleCharacterVisual : MonoBehaviour
    {
        public Transform body, tool, tip;
        public SpriteRenderer torso, paddle, leftUpper, leftLower, rightUpper, rightLower;
        public SpriteRenderer[] legacy;
        const float Pixel = 1.8f / 311f;
        public float HandError { get; private set; }
        void LateUpdate() { Pose(); }
        public void Pose()
        {
            if (!body || !tool || !tip || !torso) return;
            foreach (var r in legacy) if (r) r.enabled = false;
            float growth = Mathf.Abs(body.lossyScale.x) / 7.5f;
            float px = Pixel * growth;
            Vector3 up = body.up, right = body.right;
            Vector3 origin = body.position + up * (.3f * growth);
            Place(torso, origin, Vector2.zero, Vector2.up, up, px);
            Vector3 blade = tip.position;
            Vector3 axis = -tool.right;
            // Bottom of blade stays on the existing physical contact point.
            Place(paddle, blade, new Vector2(24, 240), Vector2.up, axis, 2.15f / 230f * growth);
            Vector3 shoulderL = body.position + up * (.66f * growth) - right * (.245f * growth);
            Vector3 shoulderR = body.position + up * (.66f * growth) + right * (.245f * growth);
            float slide = Mathf.Clamp(Vector3.Dot((shoulderL + shoulderR) * .5f - blade, axis), .90f * growth, 1.98f * growth);
            Vector3 handL = blade + axis * (slide - .085f * growth);
            Vector3 handR = blade + axis * (slide + .085f * growth);
            float a = Arm(leftUpper, leftLower, shoulderL, handL, -1, px * 1.3f,
                new Vector2(16,16), new Vector2(68,70), new Vector2(13,17), new Vector2(94,16));
            float b = Arm(rightUpper, rightLower, shoulderR, handR, 1, px * 1.3f,
                new Vector2(66,16), new Vector2(12,74), new Vector2(12,28), new Vector2(99,15));
            HandError = Mathf.Max(a,b);
        }
        static float Arm(SpriteRenderer upper, SpriteRenderer lower, Vector3 shoulder, Vector3 hand, float bend,
            float px, Vector2 s, Vector2 e, Vector2 elbowPixel, Vector2 wristPixel)
        {
            // Equal bone reach permits a fully folded elbow without a dead zone at the shoulder.
            // Each source sprite still receives a uniform scale (no aspect distortion).
            float l1 = Mathf.Max(Vector2.Distance(s,e), Vector2.Distance(elbowPixel,wristPixel))*px, l2 = l1;
            float upperPx = l1 / Vector2.Distance(s,e);
            float lowerPx = l2 / Vector2.Distance(elbowPixel,wristPixel);
            Vector3 delta = hand-shoulder;
            float distance = delta.magnitude;
            Vector3 dir = distance > .00001f ? delta/distance : Vector3.right;
            float d = Mathf.Clamp(distance, Mathf.Abs(l1-l2)+.0001f, l1+l2-.0001f);
            float along = (l1*l1-l2*l2+d*d)/(2*d);
            float height = Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
            Vector3 elbow = shoulder+dir*along+new Vector3(-dir.y,dir.x,0)*height*bend;
            Vector3 wrist = shoulder+dir*d;
            Place(upper,shoulder,s,ImageDirection(e-s),elbow-shoulder,upperPx);
            Place(lower,elbow,elbowPixel,ImageDirection(wristPixel-elbowPixel),wrist-elbow,lowerPx);
            return Vector3.Distance(wrist,hand);
        }
        static Vector2 ImageDirection(Vector2 v) { return new Vector2(v.x,-v.y); }
        static void Place(SpriteRenderer r, Vector3 anchor, Vector2 pixel, Vector2 sourceAxis, Vector3 direction, float scale)
        {
            if (!r || !r.sprite) return;
            float angle = Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-Mathf.Atan2(sourceAxis.y,sourceAxis.x)*Mathf.Rad2Deg;
            var rotation=Quaternion.Euler(0,0,angle);
            // A zero pixel denotes the center anchor used by the torso.
            Vector2 local = pixel == Vector2.zero ? Vector2.zero : new Vector2(pixel.x-r.sprite.pivot.x,r.sprite.rect.height-pixel.y-r.sprite.pivot.y);
            r.transform.SetPositionAndRotation(anchor-rotation*(Vector3)(local*scale),rotation);
            float parentScale = r.transform.parent.lossyScale.x;
            r.transform.localScale=Vector3.one*(scale*r.sprite.pixelsPerUnit/parentScale);
        }
    }
}
