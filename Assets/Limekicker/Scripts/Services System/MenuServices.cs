public class MenuServices : Services
{
    protected override void Initialize()
    {
        var gameManager = new GameManager();
        AddService<IGameManager>(gameManager);

        var gameStateHandler = new GameStateHandler();
        AddService<IGameStateHandler>(gameStateHandler);

        var inputManager = new InputManager();
        AddService<IInputManager>(inputManager);

        var uiNavigator = new UIControllerNavigator();
        AddService<IUIControllerNavigator>(uiNavigator);

        // Initialize all services
        foreach (var service in serviceMap.Values)
        {
            service.Initialize();
        }
    }
}
