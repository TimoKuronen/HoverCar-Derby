/// <summary>
/// One row of the session browser: what the view needs to show and join a session.
/// </summary>
public readonly struct SessionListEntry
{
    public string Id { get; }
    public string Name { get; }
    public int PlayerCount { get; }
    public int MaxPlayers { get; }

    public SessionListEntry(string id, string name, int playerCount, int maxPlayers)
    {
        Id = id;
        Name = name;
        PlayerCount = playerCount;
        MaxPlayers = maxPlayers;
    }
}
