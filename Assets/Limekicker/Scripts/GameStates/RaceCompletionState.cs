using UnityEngine;

public class RaceCompletionState : IGameState
{
    private Vector3 dollyTrackOffset = new Vector3(0, 10, 0);

    private readonly GameManager gameManager;

    public RaceCompletionState(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    /// <summary>
    /// Sets up the victory cinematic by positioning the dolly track at the winner's location
    /// and configuring the camera to follow them.
    /// </summary>
    public void Enter()
    {
        Transform winningPlayer = gameManager.GetLeadingPlayer().transform;

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
