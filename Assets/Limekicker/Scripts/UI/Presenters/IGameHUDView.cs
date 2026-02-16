public interface IGameHUDView
{
    void SetDebugText(string text);
    void ShowPauseMenu(bool show);
    void LeaveGame();
}
