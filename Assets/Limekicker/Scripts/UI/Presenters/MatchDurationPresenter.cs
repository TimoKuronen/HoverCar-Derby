using System.Text;

public class MatchDurationPresenter : BasePresenter
{
    private readonly IMatchDurationDisplayView view;
    private readonly IntVariable matchDurationLeft;
    private readonly StringBuilder stringBuilder = new StringBuilder(8);

    public MatchDurationPresenter(IMatchDurationDisplayView view, IntVariable matchDurationLeft)
    {
        this.view = view;
        this.matchDurationLeft = matchDurationLeft;
    }

    protected override void SubscribeToModels()
    {
        matchDurationLeft.OnValueChanged += OnTimeValueChanged;
        OnTimeValueChanged(matchDurationLeft.Value);
    }

    protected override void UnsubscribeFromModels()
    {
        if (matchDurationLeft != null)
            matchDurationLeft.OnValueChanged -= OnTimeValueChanged;
    }

    private void OnTimeValueChanged(int seconds)
    {
        int minutes = seconds / 60;
        int secs = seconds % 60;
        stringBuilder.Clear();
        stringBuilder.AppendFormat("{0:00}:{1:00}", minutes, secs);
        view.SetTime(stringBuilder.ToString());
    }
}
