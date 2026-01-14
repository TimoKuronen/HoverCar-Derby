using Cinemachine;
using TMPro;
using UnityEngine;

public class RaceContext : MonoBehaviour
{
    public DollyCameraMover introDollyCamera;
    public DollyCameraMover endingDollyCamera;
    public TextMeshProUGUI startCounterText;
    public CinemachineVirtualCamera raceCamera;

    public Transform victoryDollyTrack;
    public int roundDurationInSeconds = 120;
}