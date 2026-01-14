using System;

public interface IGameManager
{
    event Action<int> OnGameTimerUpdated;
    RaceContext Context { get; }
}
