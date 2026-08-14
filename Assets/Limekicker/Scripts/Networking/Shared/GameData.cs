/// <summary>
/// Available match maps.
/// </summary>
public enum Map
{
    Default
}

/// <summary>
/// Available match rule sets.
/// </summary>
public enum GameMode
{
    Default
}

/// <summary>
/// Matchmaking queue type for solo or team play.
/// </summary>
public enum GameQue
{
    Solo,
    Team
}

/// <summary>
/// Authenticated player identity and match preferences sent at join time.
/// </summary>
[System.Serializable]
public class UserData
{
    public string userName;
    public string userAuthId;
    public GameInfo userGamePreferences = new GameInfo();
}

/// <summary>
/// Serializable map, mode, and queue selection for matchmaking.
/// </summary>
[System.Serializable]
public class GameInfo
{
    public Map map;
    public GameMode gameMode;
    public GameQue gameQue;

    public string ToMultiplayQueue()
    {
        return gameQue switch
        {
            GameQue.Solo => "solo-queue",
            GameQue.Team => "team-queue",
            _ => "solo-queue"
        };
    }
}
