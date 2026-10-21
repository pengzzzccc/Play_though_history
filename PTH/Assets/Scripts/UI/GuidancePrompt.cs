using UnityEngine;

namespace UnknownTechnology
{
    internal sealed class GuidancePrompt
    {
        private string text;
        private GUIStyle style;
        public float Remaining { get; private set; }

        public void Show(string message, float duration = 3f)
        {
            text = message;
            Remaining = duration;
        }

        public void Clear() => Remaining = 0f;

        public void Tick(float deltaTime) => Remaining = Mathf.Max(0f, Remaining - deltaTime);

        public void Draw(string statusText = null)
        {
            if (Remaining <= 0f && statusText == null) return;
            style ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 22 };
            var width = Mathf.Min(600, Screen.width - 32);
            GUI.Box(new Rect((Screen.width - width) / 2, Screen.height * 0.12f, width, 48),
                statusText ?? text, style);
        }
    }
}
