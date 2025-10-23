public enum Map
{
    Default
}

public enum  GameMode
{
    Default
}

public enum GameQue
{
    Solo,
    Team
}

[System.Serializable]
public class UserData 
{
    public string userName;
    public string userAuthId;
    public GameInfo userGamePreferences;
}

[System.Serializable]
public class GameInfo
{
    public Map map;
    public GameMode gameMode;
    public GameQue gameQue;

    public string ToMultiplayQueue()
    {
        return "";
    }
}