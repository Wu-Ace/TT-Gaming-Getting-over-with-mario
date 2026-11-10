using UnityEngine;
using UnityEngine.UI;

namespace Character.Lifecycle
{
    // An aperture centered on the departure point; geometry avoids a full-screen shader dependency.
    public sealed class FallIrisGraphic : MaskableGraphic
    {
        float closure;
        Vector2 center;
        public float Closure => closure;
        public void Set(float value, Vector2 position) { closure = value; center = position; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (closure <= 0) return;
            float outer = (rectTransform.rect.width + rectTransform.rect.height) * 2;
            float inner = (rectTransform.rect.size.magnitude + center.magnitude) * (1 - closure);
            Color32 black = new Color32(3, 7, 12, 255);
            const int segments = 96;
            for (int i = 0; i <= segments; i++)
            {
                float a = i * 2 * Mathf.PI / segments;
                Vector2 direction = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                vh.AddVert(center + direction * inner, black, Vector2.zero);
                vh.AddVert(center + direction * outer, black, Vector2.zero);
                if (i == 0) continue;
                int k = i * 2; vh.AddTriangle(k - 2, k - 1, k); vh.AddTriangle(k, k - 1, k + 1);
            }
        }
    }
}
