using System.Text;

public class MatchDurationPresenter : BasePresenter
{
    private readonly IMatchDurationDisplayView view;
    private readonly IntVariable matchDurationLeft;
    private readonly StringBuilder stringBuilder = new StringBuilder(8);
    private EventBinding<GameStateChangeEvent> gameStateChangeBinding;

    public MatchDurationPresenter(IMatchDurationDisplayView view, IntVariable matchDurationLeft)
    {
        this.view = view;
        this.matchDurationLeft = matchDurationLeft;
    }

    protected override void SubscribeToModels()
    {
        matchDurationLeft.OnValueChanged += OnTimeValueChanged;
        OnTimeValueChanged(matchDurationLeft.Value);

        gameStateChangeBinding = new EventBinding<GameStateChangeEvent>(HandleGameStateChange);
        EventBus<GameStateChangeEvent>.Register(gameStateChangeBinding);
    }

    protected override void UnsubscribeFromModels()
    {
        if (matchDurationLeft != null)
        {
            matchDurationLeft.OnValueChanged -= OnTimeValueChanged;
        }

        if (gameStateChangeBinding != null)
        {
            EventBus<GameStateChangeEvent>.Unregister(gameStateChangeBinding);
        }
    }

    private void OnTimeValueChanged(int seconds)
    {
        int minutes = seconds / 60;
        int secs = seconds % 60;
        stringBuilder.Clear();
        stringBuilder.AppendFormat("{0:00}:{1:00}", minutes, secs);
        view.SetTime(stringBuilder.ToString());
    }

    private void HandleGameStateChange(GameStateChangeEvent @event)
    {
        if (@event.NewState is PlayState || @event.NewState is CountdownState)
        {
            view.Show();
        }
        else
        {
            view.Hide();
        }
    }
}
