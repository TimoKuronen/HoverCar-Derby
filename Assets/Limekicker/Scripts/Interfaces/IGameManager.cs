public interface IGameManager
{
    RaceContext Context { get; }
    PlayerTracker PlayerTracker { get; }
    IGameState CurrentGameState { get; }
}
