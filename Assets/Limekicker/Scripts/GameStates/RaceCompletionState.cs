using UnityEngine;

/// <summary>
/// Runs the victory dolly camera around the leading player when the match ends.
/// </summary>
public class RaceCompletionState : IGameState
{
    private static readonly Vector3 DollyTrackOffset = new Vector3(0f, 10f, 0f);

    private readonly GameManager gameManager;

    public RaceCompletionState(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public void Enter()
    {
        var leadingPlayer = gameManager.GetLeadingPlayer();
        if (leadingPlayer == null)
        {
            Debug.LogError("Leading player is null in RaceCompletionState. Cannot set up victory cinematic.");
            return;
        }

        RaceContext context = gameManager.Context;
        Transform winner = leadingPlayer.transform;

        context.VictoryDollyTrack.position = winner.position + DollyTrackOffset;
        context.EndingDollyCamera.SetLookAt(winner);
        context.EndingDollyCamera.Show(CameraPriorities.VictoryDolly);
        context.EndingDollyCamera.Play();
    }

    public void Exit()
    {
        gameManager.Context.EndingDollyCamera.Hide();
    }

    public void Update() { }
}
