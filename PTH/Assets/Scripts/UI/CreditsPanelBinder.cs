using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnknownTechnology.Audio;

namespace UnknownTechnology
{
    /// <summary>
    /// Binds CreditsPage.uxml: auto-scrolls the film-style credits from bottom to top,
    /// supports hold/toggle fast-forward, quick close, and seamless Main Menu music.
    /// </summary>
    public class CreditsPanelBinder
    {
        public const string HiddenClass = "hidden";

        private const float BaseScrollSpeed = 55f;
        private const float FastForwardMultiplier = 4f;
        private const float EndPauseDuration = 1f;

        private readonly AudioManager audioManager;
        private readonly VisualElement panel;
        private readonly VisualElement viewport;
        private readonly VisualElement content;
        private readonly Button speedButton;
        private readonly Button closeButton;

        private readonly Action speedClickHandler;
        private readonly Action closeClickHandler;
        private readonly EventCallback<PointerDownEvent> viewportPointerDownHandler;
        private readonly EventCallback<PointerUpEvent> viewportPointerUpHandler;
        private readonly EventCallback<PointerCancelEvent> viewportPointerCancelHandler;

        private bool bound;
        private bool visible;
        private bool speedToggleActive;
        private bool pointerFastForward;
        private bool scrollInitialized;
        private float scrollY;
        private float endTimer;

        /// <summary>Raised when the player closes the credits or they finish scrolling.</summary>
        public event Action CloseRequested;

        public bool IsVisible => visible;

        public CreditsPanelBinder(VisualElement root)
        {
            panel = root.Q<VisualElement>("credits");
            viewport = root.Q<VisualElement>("credits-viewport");
            content = root.Q<VisualElement>("credits-content");
            speedButton = root.Q<Button>("credits-speed-button");
            closeButton = root.Q<Button>("credits-close-button");
            audioManager = UnityEngine.Object.FindAnyObjectByType<AudioManager>();

            speedClickHandler = ToggleSpeed;
            closeClickHandler = RequestClose;
            viewportPointerDownHandler = _ => pointerFastForward = true;
            viewportPointerUpHandler = _ => pointerFastForward = false;
            viewportPointerCancelHandler = _ => pointerFastForward = false;

            if (speedButton != null)
            {
                speedButton.clicked += speedClickHandler;
            }

            if (closeButton != null)
            {
                closeButton.clicked += closeClickHandler;
            }

            if (viewport != null)
            {
                viewport.focusable = true;
                viewport.RegisterCallback(viewportPointerDownHandler);
                viewport.RegisterCallback(viewportPointerUpHandler);
                viewport.RegisterCallback(viewportPointerCancelHandler);
            }

            bound = true;
            UpdateSpeedButtonLabel(false);
        }

        public void SetVisible(bool show)
        {
            visible = show;
            if (panel != null)
            {
                panel.EnableInClassList(HiddenClass, !show);
            }

            pointerFastForward = false;

            if (!show)
            {
                return;
            }

            speedToggleActive = false;
            scrollInitialized = false;
            endTimer = 0f;
            if (content != null)
            {
                // Keep content off-screen until layout provides the viewport height.
                content.style.top = 9999f;
            }

            UpdateSpeedButtonLabel(false);
            audioManager?.EnsureMainMenuMusic();
            viewport?.Focus();
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (!visible || viewport == null || content == null)
            {
                return;
            }

            var viewportHeight = viewport.resolvedStyle.height;
            var contentHeight = content.resolvedStyle.height;
            if (float.IsNaN(viewportHeight) || float.IsNaN(contentHeight) || viewportHeight <= 0f || contentHeight <= 0f)
            {
                return;
            }

            if (!scrollInitialized)
            {
                scrollInitialized = true;
                scrollY = viewportHeight;
                content.style.top = scrollY;
                return;
            }

            var fastForward = speedToggleActive || pointerFastForward || IsFastForwardKeyHeld();
            UpdateSpeedButtonLabel(fastForward);

            var multiplier = fastForward ? FastForwardMultiplier : 1f;
            scrollY -= BaseScrollSpeed * multiplier * unscaledDeltaTime;
            content.style.top = scrollY;

            if (scrollY + contentHeight <= 0f)
            {
                endTimer += unscaledDeltaTime * multiplier;
                if (endTimer >= EndPauseDuration)
                {
                    CloseRequested?.Invoke();
                }
            }
        }

        public void Unbind()
        {
            if (!bound)
            {
                return;
            }

            if (speedButton != null)
            {
                speedButton.clicked -= speedClickHandler;
            }

            if (closeButton != null)
            {
                closeButton.clicked -= closeClickHandler;
            }

            if (viewport != null)
            {
                viewport.UnregisterCallback(viewportPointerDownHandler);
                viewport.UnregisterCallback(viewportPointerUpHandler);
                viewport.UnregisterCallback(viewportPointerCancelHandler);
            }

            bound = false;
        }

        private void ToggleSpeed()
        {
            audioManager?.PlayButtonClickSound();
            speedToggleActive = !speedToggleActive;
            UpdateSpeedButtonLabel(speedToggleActive);
        }

        private void RequestClose()
        {
            audioManager?.PlayButtonClickSound();
            CloseRequested?.Invoke();
        }

        private void UpdateSpeedButtonLabel(bool fastForward)
        {
            if (speedButton != null)
            {
                speedButton.text = fastForward ? "4x" : "1x";
            }
        }

        private static bool IsFastForwardKeyHeld()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.isPressed || keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed))
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonSouth.isPressed;
        }
    }
}
