using System.Collections;
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

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(1);
        GameSignals.MarkSessionLoaded();
    }
    private void SetupStartingPositions()
    {
        startingPositions = GameObject.FindGameObjectsWithTag("StartPosition");
        startingPositions = MathMethods.ShuffleArray(startingPositions);
    }

    private void GetAndSetPlayer()
    {
        NetworkServer networkServer = null;
    }

    public void AssignSpawnPosition(ulong clientId, PlayerController player)
    {
        Transform spawn = startingPositions[nextSpawnIndex % startingPositions.Length].transform;
        player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        nextSpawnIndex++;
    }

    private void SpawnPlayers()
    {
        //for (int i = 0; i < startingPositions.Length; i++)
        //{
        //    if (i == 0)
        //    {
        //        Instantiate(playerPrefab, startingPositions[i].transform.position, startingPositions[i].transform.rotation);
        //        players[i] = playerPrefab.GetComponent<PlayerController>().PlayerData;
        //    }
        //    else
        //    {
        //        var opponent = Instantiate(opponentPrefab, startingPositions[i].transform.position, startingPositions[i].transform.rotation);
        //        CarManager carManager = opponent.GetComponent<CarManager>();
        //        carManager.CarData.RandomiseCarValues(6);
        //        PlayerData playerData = new PlayerData();
        //        playerData.SetPlayerName(opponent.ToString() + i.ToString());
        //        players[i] = playerData;

        //    }

        //    DIBootstrapper.Container.Resolve<IScoreManager>().AddPlayer(players[i]);

        //}
    }
}