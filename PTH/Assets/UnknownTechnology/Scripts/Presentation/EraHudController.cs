using UnityEngine;
using UnityEngine.UIElements;

namespace UnknownTechnology.Presentation
{
    /// <summary>
    /// Displays the per-scene era heading of the greybox HUD. The heading text is
    /// scene data injected by PrototypeProjectBuilder; all elements come from the shared EraUI.uxml.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class EraHudController : MonoBehaviour
    {
        [SerializeField] private string heading;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null)
            {
                return;
            }

            var label = root.Q<Label>("era-heading");
            if (label != null)
            {
                label.text = heading;
            }
        }

#if UNITY_EDITOR
        public void Configure(string configuredHeading)
        {
            heading = configuredHeading;
        }
#endif
    }
}
