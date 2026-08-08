using UnityEngine;
using VContainer;
using VContainer.Unity;

[DefaultExecutionOrder(-100)]
public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<IScoreManager, ScoreManager>(Lifetime.Scoped);

        var gameManager = FindObjectOfType<GameManager>();
        builder.RegisterComponent(gameManager)
               .AsImplementedInterfaces()
               .AsSelf();

        builder.RegisterEntryPoint<PlayerSpawnManager>(Lifetime.Scoped);
        builder.RegisterComponentInHierarchy<GasButton>();
    }

    protected override void Awake()
    {
        base.Awake();
        InjectSceneUiViews();
    }

    private void InjectSceneUiViews()
    {
        InjectView<ScoreDisplayView>();
        InjectView<GameHUDView>();
        InjectView<RoundResultsView>();
    }

    private void InjectView<T>() where T : Component
    {
        foreach (T view in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Container.Inject(view);
    }
}