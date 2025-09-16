using Unity.Netcode;
using UnityEngine;

public class RaceSetupManager : MonoBehaviour
{
    private GameObject[] startingPositions;
    private int nextSpawnIndex = 0;

    private void Awake()
    {
        SetupStartingPositions();
    }

    private void SetupStartingPositions()
    {
        startingPositions = GameObject.FindGameObjectsWithTag("StartPosition");
        startingPositions = MathMethods.ShuffleArray(startingPositions);
    }

    public void AssignSpawnPosition(ulong clientId, PlayerController player)
    {
        Transform spawn = startingPositions[nextSpawnIndex % startingPositions.Length].transform;
        player.transform.position = spawn.position;
        player.transform.rotation = spawn.rotation;
        nextSpawnIndex++;
    }

}