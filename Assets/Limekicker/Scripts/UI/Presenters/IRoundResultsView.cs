using System;

/// <summary>
/// Contract for round results display and rematch or menu navigation.
/// </summary>
public interface IRoundResultsView
{
    void ShowResults(string title);
    void HideResults();
    void ClearScoreRows();
    void AddScoreRow(ulong clientId, string playerName, int points);

    event Action OnRematchClicked;
    event Action OnMainMenuClicked;
}
