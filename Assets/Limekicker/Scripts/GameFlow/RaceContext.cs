using Cinemachine;
using UnityEngine;

/// <summary>
/// Scene configuration for match timing, cameras, and player requirements. Owns the race camera
/// hand-over so every state raises it the same way.
/// </summary>
public class RaceContext : MonoBehaviour
{
    /// <summary>
    /// Ordered phases from lobby wait through match completion.
    /// </summary>
    public enum MatchPhase : byte
    {
        WaitingForPlayers = 0,
        Cinematic = 1,
        Countdown = 2,
        Playing = 3,
        Completed = 4
    }

    [SerializeField] private DollyCameraMover introDollyCamera;
    [SerializeField] private DollyCameraMover endingDollyCamera;
    [SerializeField] private CinemachineVirtualCamera raceCamera;

    [SerializeField] private Transform victoryDollyTrack;
    [SerializeField] private int roundDurationInSeconds = 120;
    [SerializeField] private int requiredPlayerCount = 2;

    [Header("Match Flow Timing")]
    [Tooltip("Minimum time after cinematic begins before countdown can start, even if all players are ready.")]
    [SerializeField] private float cinematicDurationSeconds = 3f;
    [SerializeField] private float countdownIntervalSeconds = 0.75f;
    [SerializeField] private float countdownGoDelaySeconds = 0.5f;
    [SerializeField] private float phaseReplicationBufferSeconds = 1f;

    public DollyCameraMover IntroDollyCamera => introDollyCamera;
    public DollyCameraMover EndingDollyCamera => endingDollyCamera;
    public Transform VictoryDollyTrack => victoryDollyTrack;
    public int RoundDurationInSeconds => roundDurationInSeconds;
    public int RequiredPlayerCount => requiredPlayerCount;
    public float CinematicDurationSeconds => cinematicDurationSeconds;
    public float CountdownIntervalSeconds => countdownIntervalSeconds;
    public float CountdownGoDelaySeconds => countdownGoDelaySeconds;
    public float PhaseReplicationBufferSeconds => phaseReplicationBufferSeconds;

    private void Awake()
    {
        if (introDollyCamera == null || endingDollyCamera == null || raceCamera == null || victoryDollyTrack == null)
            Debug.LogError($"{nameof(RaceContext)} is missing a camera or track reference.", this);
    }

    /// <summary>
    /// Parks the intro dolly camera at the start of its track so it holds the view while waiting.
    /// </summary>
    public void ShowIntroCamera()
    {
        introDollyCamera.Show(CameraPriorities.IntroDolly);
    }

    /// <summary>
    /// Hands the view to the race chase camera and retires the intro dolly camera.
    /// </summary>
    public void ActivateRaceCamera()
    {
        if (raceCamera.Priority < CameraPriorities.RaceChase)
            raceCamera.Priority = CameraPriorities.RaceChase;

        introDollyCamera.Hide();
    }
}
