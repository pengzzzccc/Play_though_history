using System;
using UnknownTechnology.Core.Events;
using UnknownTechnology.Core.Settings;
using UnknownTechnology.Core.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnknownTechnology.Presentation
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class UiScaleSettingsApplier : MonoBehaviour
    {
        private PanelSettings panelSettings;
        private IDisposable settingsSubscription;

        private void Start()
        {
            panelSettings = GetComponent<UIDocument>().panelSettings;
            if (panelSettings == null || !GameContextProvider.IsReady)
            {
                return;
            }

            var context = GameContextProvider.Current;
            Apply(context.Settings.Current);
            settingsSubscription = context.EventBus.Subscribe<SettingsChanged>(message => Apply(message.Settings));
        }

        private void Apply(GameSettingsSnapshot settings)
        {
            if (panelSettings != null)
            {
                panelSettings.scale = settings.UiScale;
            }
        }

        private void OnDestroy()
        {
            settingsSubscription?.Dispose();
        }
    }
}
