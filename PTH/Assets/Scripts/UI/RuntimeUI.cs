using TMPro;
using UnityEngine;

namespace UnknownTechnology
{
    internal static class RuntimeUI
    {
        public static RectTransform CreateRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = parent.gameObject.layer;
            rect.SetParent(parent, false);
            return rect;
        }

        public static T Create<T>(string name, Transform parent) where T : Component
        {
            return CreateRect(name, parent).gameObject.AddComponent<T>();
        }

        public static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font,
            float fontSize, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
        {
            var text = Create<TextMeshProUGUI>(name, parent);
            text.font = font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        public static void Stretch(RectTransform rect, Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
