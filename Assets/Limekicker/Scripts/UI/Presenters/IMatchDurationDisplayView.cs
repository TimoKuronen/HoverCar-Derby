/// <summary>
/// Contract for showing formatted match time remaining.
/// </summary>
public interface IMatchDurationDisplayView
{
    void SetTime(string timeString);
    void Show();
    void Hide();
}
