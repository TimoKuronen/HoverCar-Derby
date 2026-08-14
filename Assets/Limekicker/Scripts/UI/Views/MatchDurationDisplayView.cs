using TMPro;
using UnityEngine;

/// <summary>
/// Displays remaining match time during gameplay.
/// </summary>
public class MatchDurationDisplayView : MonoBehaviour, IMatchDurationDisplayView
{
    [SerializeField] private TextMeshProUGUI matchDurationText;
    [SerializeField] private IntVariable matchDurationLeft;

    private MatchDurationPresenter presenter;

    private void Start()
    {
        presenter = new MatchDurationPresenter(this, matchDurationLeft);
        presenter.Initialize();
    }

    public void SetTime(string timeString)
    {
        matchDurationText.text = timeString;
    }

    public void Show()
    {
        matchDurationText.gameObject.SetActive(true);
    }

    public void Hide()
    {
        matchDurationText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        presenter?.Dispose();
    }
}
