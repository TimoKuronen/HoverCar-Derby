using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaceCompletionState : IGameState
{
    private Vector3 dollyTrackOffset = new Vector3(0, 10, 0);

    private readonly GameManager gameManager;

    public RaceCompletionState(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public void Enter()
    {
        Transform winningPlayer = gameManager.playerTracker.GetPlayerByID(gameManager.scoreManager.GetLeadingPlayer().ClientId).transform;

        // move victory dolly track to winner position
        gameManager.Context.victoryDollyTrack.transform.position = winningPlayer.position + dollyTrackOffset;

        // cinematic camera to target the winner 
        gameManager.Context.endingDollyCamera.vcam.LookAt = winningPlayer;

        // make camera move
        gameManager.Context.endingDollyCamera.ToggleMovement();
    }

    public void Exit() { }

    public void Update() { }
}
