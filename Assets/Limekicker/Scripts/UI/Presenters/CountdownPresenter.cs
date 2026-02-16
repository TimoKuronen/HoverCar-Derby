public class CountdownPresenter : BasePresenter
{
    private readonly ICountdownDisplayView view;
    private readonly IntVariable countdownValue;

    public CountdownPresenter(ICountdownDisplayView view, IntVariable countdownValue)
    {
        this.view = view;
        this.countdownValue = countdownValue;
    }

    protected override void SubscribeToModels()
    {
        countdownValue.OnValueChanged += OnCountdownValueChanged;
    }

    protected override void UnsubscribeFromModels()
    {
        if (countdownValue != null)
        {
            countdownValue.OnValueChanged -= OnCountdownValueChanged;
        }
    }

    private void OnCountdownValueChanged(int value)
    {
        if (value < 0 || value > 3)
        {
            view.Hide();
            return;
        }

        if (value == 0)
        {
            view.ShowGo();
        }
        else
        {
            view.ShowCountdown(value);
        }
    }
}
