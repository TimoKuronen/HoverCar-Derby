public class MenuServices : Services
{
    protected override void Initialize()
    {
        var gameStateHandler = new GameStateHandler();
        AddService<IGameStateHandler>(gameStateHandler);

        var inputManager = new InputManager();
        AddService<IInputManager>(inputManager);

        // Initialize all services
        foreach (var service in serviceMap.Values)
        {
            service.Initialize();
        }
    }
}
