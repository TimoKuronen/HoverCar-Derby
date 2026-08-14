/// <summary>
/// Contract for match orchestration, pause control, and participant registration.
/// </summary>
public interface IGameManager
{
    RaceContext Context { get; }
    PlayerTracker PlayerTracker { get; }
    IGameState CurrentGameState { get; }
    bool CanPause { get; }
    void ReturnToPreviousState();
    void TogglePause();

    void RegisterParticipant(ulong networkObjectId);
    void MarkParticipantReady(ulong networkObjectId);
    void UnregisterParticipant(ulong networkObjectId);
}
