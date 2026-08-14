using Unity.Netcode.Components;

/// <summary>
/// Client-authoritative NetworkTransform that commits position from the owning client.
/// </summary>
public class ClientNetworkTransform : NetworkTransform
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        CanCommitToTransform = true;
    }

    protected override void Update()
    {
        CanCommitToTransform = IsOwner;
        base.Update();

        if (NetworkManager != null)
        {
            if (NetworkManager.IsConnectedClient || NetworkManager.IsListening)
            {
                if (CanCommitToTransform)
                {
                    TryCommitTransformToServer(transform, NetworkManager.LocalTime.Time);
                }
            }
        }
    }

    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
