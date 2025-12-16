using UnityEngine;

public class GameHUD : MonoBehaviour
{
    public void LeaveGame()
    {
        NetworkSession.LeaveGame();
    }
}
