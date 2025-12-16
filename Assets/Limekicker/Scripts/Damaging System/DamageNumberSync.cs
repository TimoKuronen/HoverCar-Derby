using Unity.Netcode;
using UnityEngine;

/// <summary>
/// NetworkBehaviour component that syncs damage events from server to clients
/// for displaying damage numbers. Attach this to player prefabs.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class DamageNumberSync : NetworkBehaviour
{
    /// <summary>
    /// Called by server to display a damage number on all relevant clients.
    /// </summary>
    /// <param name="worldPosition">World position where damage occurred</param>
    /// <param name="damageAmount">Amount of damage dealt</param>
    /// <param name="attackerClientId">Client ID of attacker (ulong.MaxValue for non-player sources)</param>
    /// <param name="victimClientId">Client ID of victim (ulong.MaxValue for non-player sources)</param>
    public void ShowDamageNumberRpc(Vector3 worldPosition, float damageAmount, ulong attackerClientId, ulong victimClientId)
    {
        if (!IsServer)
            return;

        ShowDamageNumberClientRpc(worldPosition, damageAmount, attackerClientId, victimClientId);
    }

    [ClientRpc]
    private void ShowDamageNumberClientRpc(Vector3 worldPosition, float damageAmount, ulong attackerClientId, ulong victimClientId)
    {
        DamageNumberPool pool = DamageNumberPool.Instance;
        if (pool != null)
        {
            pool.ShowDamageNumber(worldPosition, damageAmount, attackerClientId, victimClientId);
        }
    }
}






