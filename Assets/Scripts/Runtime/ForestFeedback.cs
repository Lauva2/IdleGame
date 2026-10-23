using UnityEngine;

namespace ForestShop
{
    public sealed class ForestFeedback : MonoBehaviour
    {
        float elapsed;
        RectTransform rect;
        CanvasGroup group;
        void Awake() { rect = GetComponent<RectTransform>(); group = gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; }
        void Update()
        {
            elapsed += Time.unscaledDeltaTime; rect.anchoredPosition += Vector2.up * (35 * Time.unscaledDeltaTime);
            group.alpha = Mathf.Clamp01((1.5f - elapsed) * 2);
            if (elapsed >= 1.5f) Destroy(gameObject);
        }
    }
}
