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
        if (gameManager == null)
        {
            Debug.LogError("GameManager reference is null in RaceCompletionState.");
            return;
        }

        var leadingPlayer = gameManager.GetLeadingPlayer();
        if (leadingPlayer == null)
        {
            Debug.LogError("Leading player is null in RaceCompletionState. Cannot set up victory cinematic.");
            return;
        }

        Transform winningPlayer = leadingPlayer.transform;

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
