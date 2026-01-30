using Cinemachine;
using UnityEngine;

public class RaceContext : MonoBehaviour
{
    public DollyCameraMover introDollyCamera;
    public DollyCameraMover endingDollyCamera;
    public CinemachineVirtualCamera raceCamera;

    public Transform victoryDollyTrack;
    public int roundDurationInSeconds = 120;
}