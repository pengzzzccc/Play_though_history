using System;
using UnknownTechnology.Core.Events;
using UnknownTechnology.Core.Settings;
using UnknownTechnology.Core.State;
using UnityEngine;
using UnityEngine.UI;

namespace UnknownTechnology.Presentation
{
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class UiScaleSettingsApplier : MonoBehaviour
    {
        private CanvasScaler canvasScaler;
        private IDisposable settingsSubscription;

        private void Awake()
        {
            canvasScaler = GetComponent<CanvasScaler>();
        }

        private void Start()
        {
            if (!GameContextProvider.IsReady)
            {
                return;
            }

            var context = GameContextProvider.Current;
            Apply(context.Settings.Current);
            settingsSubscription = context.EventBus.Subscribe<SettingsChanged>(message => Apply(message.Settings));
        }

        private void Apply(GameSettingsSnapshot settings)
        {
            canvasScaler.scaleFactor = settings.UiScale;
        }

        private void OnDestroy()
        {
            settingsSubscription?.Dispose();
        }
    }
}
