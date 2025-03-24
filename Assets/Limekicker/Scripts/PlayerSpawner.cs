using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject opponentPrefab;

    private GameObject[] startingPositions;
    private PlayerData[] players;

    private void Awake()
    {
        startingPositions = GameObject.FindGameObjectsWithTag("StartPosition");
        players = new PlayerData[startingPositions.Length];

        System.Random rng = new System.Random();
        startingPositions = startingPositions.OrderBy(x => rng.Next()).ToArray();
    }

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        SpawnPlayers();
    }

    private void SpawnPlayers()
    {
        for (int i = 0; i < startingPositions.Length; i++)
        {
            if (i == 0)
            {
                Instantiate(playerPrefab, startingPositions[i].transform.position, startingPositions[i].transform.rotation);
                players[i] = playerPrefab.GetComponent<PlayerController>().PlayerData;
            }
            else
            {
                var opponent = Instantiate(opponentPrefab, startingPositions[i].transform.position, startingPositions[i].transform.rotation);
                CarManager carManager = opponent.GetComponent<CarManager>();
                carManager.CarData.RandomiseCarValues(6);
                PlayerData playerData = new PlayerData();
                playerData.SetPlayerName(opponent.ToString() + i.ToString());
                players[i] = playerData;
                
            }

            Services.Get<IScoreManager>().AddPlayer(players[i]);

        }
    }
}
