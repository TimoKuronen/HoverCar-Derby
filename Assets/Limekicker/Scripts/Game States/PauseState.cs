using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseState : IGameState
{
    public void Enter()
    {
        Time.timeScale = 0.00001f;
    }

    public void Exit()
    {
        Time.timeScale = 1f;
    }

    public void Update() { }
}
