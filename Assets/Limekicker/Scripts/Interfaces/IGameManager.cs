using System;

public interface IGameManager
{
    event Action<int> OnGameTimerUpdated;
    RaceContext Context { get; }
    float GameTimeLeft { get; }
    PlayerTracker PlayerTracker { get; }
}
