using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectibleSpawner : MonoBehaviour
{
    [SerializeField] private bool randomizeSpawnType;
    [SerializeField] private float spawnInterval;
    
    private float timer;

    private IGameStateHandler gameStateHandler;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => DIBootstrapper.Container.Resolve<IGameManager>().GameSetupCompleted);
    }

    private void Update()
    {
        if (gameStateHandler.GetCurrentGameState != GameState.Normal)
            return;

        timer += Time.deltaTime;
    }
}
