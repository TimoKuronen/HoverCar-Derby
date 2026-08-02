public interface IGameManager
{
    RaceContext Context { get; }
    PlayerTracker PlayerTracker { get; }
    IGameState CurrentGameState { get; }
    void ReturnToPreviousState();

    void RegisterParticipant(ulong networkObjectId);
    void MarkParticipantReady(ulong networkObjectId);
    void UnregisterParticipant(ulong networkObjectId);
}
