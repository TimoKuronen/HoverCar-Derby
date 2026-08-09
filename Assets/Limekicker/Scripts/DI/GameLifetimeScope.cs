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
        builder.RegisterEntryPoint<GameUiPresenterBootstrap>(Lifetime.Scoped);
        builder.RegisterComponentInHierarchy<GasButton>();
    }
}
