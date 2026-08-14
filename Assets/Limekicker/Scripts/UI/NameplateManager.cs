using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Spawns and tracks world-space nameplates for remote players.
/// </summary>
public class NameplateManager : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private RectTransform nameplateContainer;
    [SerializeField] private GameObject nameplatePrefab;

    private Dictionary<ulong, RectTransform> plates = new();
    private EventBinding<PlayerSpawnedEvent> playerSpawnedEvent;

    private void Start()
    {
        playerSpawnedEvent = new EventBinding<PlayerSpawnedEvent>(RegisterPlayer);
        EventBus<PlayerSpawnedEvent>.Register(playerSpawnedEvent);
    }

    private void RegisterPlayer(PlayerSpawnedEvent playerSpawnedEvent)
    {
        var userData = playerSpawnedEvent.UserData;
        var playerObject = playerSpawnedEvent.NetworkObject;
        // Skip local player's own nameplate
        if (playerObject.NetworkManager.LocalClientId == NetworkManager.Singleton.LocalClientId)
        {
            return;
        }

        var plate = Instantiate(nameplatePrefab, nameplateContainer).GetComponent<RectTransform>();
        plate.GetComponentInChildren<TMP_Text>().text = userData.userName;
        plates[playerObject.NetworkManager.LocalClientId] = plate;

        StartCoroutine(UpdatePosition(plate, playerObject.transform));
    }

    /// <summary>
    /// Continuously updates nameplate screen position to follow target transform.
    /// Positions nameplate above the target using world-to-screen conversion.
    /// </summary>
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

    private void OnDestroy()
    {
        EventBus<PlayerSpawnedEvent>.Unregister(playerSpawnedEvent);
    }
}
