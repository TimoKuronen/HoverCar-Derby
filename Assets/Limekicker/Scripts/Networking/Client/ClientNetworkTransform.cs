using Unity.Netcode.Components;

/// <summary>
/// Owner-authoritative NetworkTransform: the owning client commits its transform and the server relays it.
/// </summary>
public class ClientNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
