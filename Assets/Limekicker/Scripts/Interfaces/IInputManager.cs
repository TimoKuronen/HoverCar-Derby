using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum InputMethod { Keyboard, Controller }
public interface IInputManager : IUpdateableService
{
    void AddInputReference(InputActionAsset inputAsset);
    Action OnCancel { get; set; }
    Action OnMoveDown { get; set; }
    Action OnMoveUp { get; set; }
    Action OnMoveLeft { get; set; }
    Action OnMoveRight { get; set; }
    Action OnSubmit { get; set; }

    Vector2 MousePosition { get; }
    Vector2 GetMoveInput();
    InputMethod CurrentInputMethod { get; }
}
