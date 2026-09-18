using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnknownTechnology
{
    /// <summary>
    /// Binds the shared SettingPage.uxml template to GameSettings. Both the main
    /// menu and the pause overlay embed the same template, so all binding logic
    /// lives here; host controllers only decide when the panel is visible.
    /// </summary>
    public class SettingsPanelBinder
    {
        public const string HiddenClass = "hidden";

        private static readonly string[] TabPageNames = { "control", "graphic", "audio", "general" };

        private readonly VisualElement panel;
        private readonly Button[] tabButtons = new Button[TabPageNames.Length];
        private readonly VisualElement[] pages = new VisualElement[TabPageNames.Length];
        private readonly Slider mouseSensitivity;
        private readonly Slider gamepadSensitivity;
        private readonly Slider uiScale;
        private readonly Slider masterVolume;
        private readonly Slider musicVolume;
        private readonly Slider sfxVolume;
        private readonly Slider uiVolume;
        private readonly Toggle invertY;
        private readonly Toggle reducedMotion;
        private readonly Toggle fullscreen;
        private readonly DropdownField qualityDropdown;
        private readonly Button settingsBackButton;
        private readonly Action[] tabClickHandlers = new Action[TabPageNames.Length];
        private readonly Action backClickHandler;
        private bool bound;
        private bool visible;

        /// <summary>Raised when the user presses the panel's Back button.</summary>
        public event Action BackRequested;

        public bool IsVisible => visible;

        public SettingsPanelBinder(VisualElement root)
        {
            // The host UXML instantiates the template as <ui:Instance name="settings">;
            // the visibility class lives on that container, not on the template root.
            panel = root.Q<VisualElement>("settings");
            tabButtons[0] = root.Q<Button>("tab-control");
            tabButtons[1] = root.Q<Button>("tab-graphic");
            tabButtons[2] = root.Q<Button>("tab-audio");
            tabButtons[3] = root.Q<Button>("tab-general");
            pages[0] = root.Q<VisualElement>("page-control");
            pages[1] = root.Q<VisualElement>("page-graphic");
            pages[2] = root.Q<VisualElement>("page-audio");
            pages[3] = root.Q<VisualElement>("page-general");
            mouseSensitivity = root.Q<Slider>("mouse-sensitivity");
            gamepadSensitivity = root.Q<Slider>("gamepad-sensitivity");
            uiScale = root.Q<Slider>("ui-scale");
            masterVolume = root.Q<Slider>("master-volume");
            musicVolume = root.Q<Slider>("music-volume");
            sfxVolume = root.Q<Slider>("sfx-volume");
            uiVolume = root.Q<Slider>("ui-volume");
            invertY = root.Q<Toggle>("invert-y-toggle");
            reducedMotion = root.Q<Toggle>("reduced-motion-toggle");
            fullscreen = root.Q<Toggle>("fullscreen-toggle");
            qualityDropdown = root.Q<DropdownField>("quality-dropdown");
            settingsBackButton = root.Q<Button>("settings-back-button");

            mouseSensitivity.lowValue = GameSettings.MinimumMouseSensitivity;
            mouseSensitivity.highValue = GameSettings.MaximumMouseSensitivity;
            gamepadSensitivity.lowValue = GameSettings.MinimumGamepadSensitivity;
            gamepadSensitivity.highValue = GameSettings.MaximumGamepadSensitivity;
            uiScale.lowValue = GameSettings.MinimumUiScale;
            uiScale.highValue = GameSettings.MaximumUiScale;
            masterVolume.lowValue = 0f;
            masterVolume.highValue = 1f;
            musicVolume.lowValue = 0f;
            musicVolume.highValue = 1f;
            sfxVolume.lowValue = 0f;
            sfxVolume.highValue = 1f;
            uiVolume.lowValue = 0f;
            uiVolume.highValue = 1f;
            fullscreen.EnableInClassList(HiddenClass, !GameSettings.SupportsDisplaySettings);

            qualityDropdown.choices = new List<string>(QualitySettings.names);

            Refresh(Game.Settings);
            SelectTab(TabPageNames[0]);

            for (var index = 0; index < tabButtons.Length; index++)
            {
                var tabIndex = index;
                tabClickHandlers[index] = () => SelectTab(TabPageNames[tabIndex]);
                tabButtons[index].clicked += tabClickHandlers[index];
            }

            mouseSensitivity.RegisterValueChangedCallback(OnFloatChanged);
            gamepadSensitivity.RegisterValueChangedCallback(OnFloatChanged);
            uiScale.RegisterValueChangedCallback(OnFloatChanged);
            masterVolume.RegisterValueChangedCallback(OnFloatChanged);
            musicVolume.RegisterValueChangedCallback(OnFloatChanged);
            sfxVolume.RegisterValueChangedCallback(OnFloatChanged);
            uiVolume.RegisterValueChangedCallback(OnFloatChanged);
            invertY.RegisterValueChangedCallback(OnBoolChanged);
            reducedMotion.RegisterValueChangedCallback(OnBoolChanged);
            fullscreen.RegisterValueChangedCallback(OnBoolChanged);
            qualityDropdown.RegisterValueChangedCallback(OnQualityChanged);
            backClickHandler = () => BackRequested?.Invoke();
            settingsBackButton.clicked += backClickHandler;
            bound = true;
        }

        public void SetVisible(bool value)
        {
            if (visible == value)
            {
                return;
            }

            visible = value;
            panel.EnableInClassList(HiddenClass, !value);
            if (value)
            {
                panel.schedule.Execute(() => tabButtons[0].Focus());
            }
        }

        public void Unbind()
        {
            if (!bound)
            {
                return;
            }

            for (var index = 0; index < tabButtons.Length; index++)
            {
                tabButtons[index].clicked -= tabClickHandlers[index];
            }

            mouseSensitivity.UnregisterValueChangedCallback(OnFloatChanged);
            gamepadSensitivity.UnregisterValueChangedCallback(OnFloatChanged);
            uiScale.UnregisterValueChangedCallback(OnFloatChanged);
            masterVolume.UnregisterValueChangedCallback(OnFloatChanged);
            musicVolume.UnregisterValueChangedCallback(OnFloatChanged);
            sfxVolume.UnregisterValueChangedCallback(OnFloatChanged);
            uiVolume.UnregisterValueChangedCallback(OnFloatChanged);
            invertY.UnregisterValueChangedCallback(OnBoolChanged);
            reducedMotion.UnregisterValueChangedCallback(OnBoolChanged);
            fullscreen.UnregisterValueChangedCallback(OnBoolChanged);
            qualityDropdown.UnregisterValueChangedCallback(OnQualityChanged);
            settingsBackButton.clicked -= backClickHandler;
            bound = false;
        }

        private void SelectTab(string tabName)
        {
            for (var index = 0; index < TabPageNames.Length; index++)
            {
                var selected = TabPageNames[index] == tabName;
                pages[index].EnableInClassList(HiddenClass, !selected);
                tabButtons[index].EnableInClassList("tab-button--active", selected);
            }
        }

        private void Refresh(GameSettings settings)
        {
            mouseSensitivity.SetValueWithoutNotify(settings.mouseSensitivity);
            gamepadSensitivity.SetValueWithoutNotify(settings.gamepadSensitivity);
            uiScale.SetValueWithoutNotify(settings.uiScale);
            masterVolume.SetValueWithoutNotify(settings.masterVolume);
            musicVolume.SetValueWithoutNotify(settings.musicVolume);
            sfxVolume.SetValueWithoutNotify(settings.sfxVolume);
            uiVolume.SetValueWithoutNotify(settings.uiVolume);
            invertY.SetValueWithoutNotify(settings.invertY);
            reducedMotion.SetValueWithoutNotify(settings.reducedMotion);
            fullscreen.SetValueWithoutNotify(settings.fullscreen);
            qualityDropdown.index = Mathf.Clamp(settings.qualityLevel, 0, qualityDropdown.choices.Count - 1);
        }

        private void ApplyAll()
        {
            var settings = Game.Settings;
            settings.mouseSensitivity = mouseSensitivity.value;
            settings.gamepadSensitivity = gamepadSensitivity.value;
            settings.uiScale = uiScale.value;
            settings.masterVolume = masterVolume.value;
            settings.musicVolume = musicVolume.value;
            settings.sfxVolume = sfxVolume.value;
            settings.uiVolume = uiVolume.value;
            settings.invertY = invertY.value;
            settings.reducedMotion = reducedMotion.value;
            settings.fullscreen = fullscreen.value;
            settings.qualityLevel = qualityDropdown.index;
            settings.Save();
        }

        private void OnFloatChanged(ChangeEvent<float> evt)
        {
            ApplyAll();
        }

        private void OnBoolChanged(ChangeEvent<bool> evt)
        {
            ApplyAll();
        }

        private void OnQualityChanged(ChangeEvent<string> evt)
        {
            ApplyAll();
        }
    }
}
