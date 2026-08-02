using Cinemachine;
using UnityEngine;

public class RaceContext : MonoBehaviour
{
    public enum MatchPhase : byte
    {
        WaitingForPlayers = 0,
        Cinematic = 1,
        Countdown = 2,
        Playing = 3,
        Completed = 4
    }

    public DollyCameraMover introDollyCamera;
    public DollyCameraMover endingDollyCamera;
    public CinemachineVirtualCamera raceCamera;

    public Transform victoryDollyTrack;
    public int roundDurationInSeconds = 120;
    public int requiredPlayerCount = 2;

    [Header("Match Flow Timing")]
    [Tooltip("Minimum time after cinematic begins before countdown can start, even if all players are ready.")]
    public float cinematicDurationSeconds = 3f;
    public float countdownIntervalSeconds = 0.75f;
    public float countdownGoDelaySeconds = 0.5f;
    public float phaseReplicationBufferSeconds = 1f;
}