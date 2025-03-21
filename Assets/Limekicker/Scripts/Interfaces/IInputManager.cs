using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInputManager : IUpdateableService
{
    Vector2 CurrentTouchPosition { get; }
    Vector2 StartingTouchPosition { get; } 
    public bool InputGiven { get; }
}
