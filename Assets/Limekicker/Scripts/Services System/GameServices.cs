public class GameServices : Services
{
    protected override void Initialize()
    {
        var gameStateHandler = new GameStateHandler();
        AddService<IGameStateHandler>(gameStateHandler);

        var inputManager = new InputManager();
        AddService<IInputManager>(inputManager);

        var timeManager = new TimeManager();
        AddService<ITimeManager>(timeManager);

        var soundManager = new SoundManager();
        AddService<ISoundManager>(soundManager);

        var scoreManager = new ScoreManager();
        AddService<IScoreManager>(scoreManager);

        // Initialize all services
        foreach (var service in serviceMap.Values)
        {
            service.Initialize();
        }
    }
}
