using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using VContainer;

public class NameplateManager : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private RectTransform nameplateContainer;
    [SerializeField] private GameObject nameplatePrefab;

    private Dictionary<ulong, RectTransform> plates = new();
    private IPlayerSpawnManager playerSpawnManager;

    [Inject]
    public void Construct(IPlayerSpawnManager playerSpawnManager)
    {
        this.playerSpawnManager = playerSpawnManager;
        this.playerSpawnManager.OnPlayerSpawned += RegisterPlayer;
        this.playerSpawnManager.OnPlayerDespawned += UnregisterPlayer;
    }

    private void RegisterPlayer(UserData userData, NetworkObject playerObject)
    {
        // Skip local player's own nameplate
        if (playerObject.NetworkManager.LocalClientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("not showing own nameplate");
            return;
        }

        var plate = Instantiate(nameplatePrefab, nameplateContainer).GetComponent<RectTransform>();
        plate.GetComponentInChildren<TMP_Text>().text = userData.userName;
        plates[playerObject.NetworkManager.LocalClientId] = plate;

        Debug.Log(userData.userName + " nameplate instantiated ");

        StartCoroutine(UpdatePosition(plate, playerObject.transform));
    }

    private IEnumerator UpdatePosition(RectTransform plate, Transform target)
    {
        while (target != null)
        {
            var screenPos = mainCamera.WorldToScreenPoint(target.position + Vector3.up * 2f);
            plate.position = screenPos;
            yield return null;
        }
    }

    private void UnregisterPlayer(UserData data, NetworkObject @object)
    {
        StopAllCoroutines();
        if (plates.TryGetValue(@object.NetworkManager.LocalClientId, out var plate))
        {
            Destroy(plate.gameObject);
            plates.Remove(@object.NetworkManager.LocalClientId);
        }
    }
}
