using System;
using UnityEngine;

namespace UnknownTechnology.Core.Input
{
    public enum ControlScheme
    {
        KeyboardMouse = 0,
        Gamepad = 1
    }

    public interface IInputReader
    {
        Vector2 Move { get; }
        Vector2 Look { get; }
        bool ToolHeld { get; }
        ControlScheme CurrentControlScheme { get; }
        event Action InteractPerformed;
        event Action JumpPerformed;
        event Action ToolPerformed;
        event Action NotebookPerformed;
        event Action PausePerformed;
        event Action CancelPerformed;
    }
}
