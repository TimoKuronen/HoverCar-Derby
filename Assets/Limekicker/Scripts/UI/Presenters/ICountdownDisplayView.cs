/// <summary>
/// Contract for pre-race countdown number and go display.
/// </summary>
public interface ICountdownDisplayView
{
    void ShowCountdown(int number);
    void ShowGo();
    void Hide();
}
