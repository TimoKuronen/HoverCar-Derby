public interface IEvent { }

public class Events : IEvent { }

public struct GameStateChangeEvent : IEvent
{
    public IGameState NewState;

}
