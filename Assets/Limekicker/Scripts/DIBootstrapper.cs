using UnityEngine;

public class DIBootstrapper : MonoBehaviour
{
    public static DIContainer Container { get; private set; }

    void Awake()
    {
        Container = new DIContainer();

        var inputManager = new InputManager();
        var gameManager = new GameManager();
        var gameStateHandler = new GameStateHandler();
        var scoreManager = new ScoreManager();
        var timeManager = new TimeManager(gameStateHandler);
        var soundManager = new SoundManager();

        Container.Register<IInputManager>(inputManager);
        Container.Register<IGameManager>(gameManager);
        Container.Register<IGameStateHandler>(gameStateHandler);
        Container.Register<IScoreManager>(scoreManager);
        Container.Register<ITimeManager>(timeManager);
        Container.Register<ISoundManager>(soundManager);
    }
}