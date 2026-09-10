using UnityEngine;
using UnityEngine.UIElements;

namespace UnknownTechnology
{
    /// Applies the UI scale setting to the whole UI Toolkit panel.
    [RequireComponent(typeof(UIDocument))]
    public class UiScaleSettingsApplier : MonoBehaviour
    {
        private PanelSettings panelSettings;

        private void Start()
        {
            panelSettings = GetComponent<UIDocument>().panelSettings;
            Apply(Game.Settings);
            Game.Settings.Changed += Apply;
        }

        private void Apply(GameSettings settings)
        {
            if (panelSettings != null)
            {
                panelSettings.scale = settings.uiScale;
            }
        }

        private void OnDestroy()
        {
            Game.Settings.Changed -= Apply;
        }
    }
}
