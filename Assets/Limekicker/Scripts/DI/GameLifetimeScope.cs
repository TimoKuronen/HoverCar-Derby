using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// Wires game-scene dependencies including scoring, spawning, and UI.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<IScoreManager, ScoreManager>(Lifetime.Scoped);

        var gameManager = FindFirstObjectByType<GameManager>();
        builder.RegisterComponent(gameManager)
               .AsImplementedInterfaces()
               .AsSelf();

        builder.RegisterEntryPoint<PlayerSpawnManager>(Lifetime.Scoped);
        builder.RegisterEntryPoint<GameUiPresenterBootstrap>(Lifetime.Scoped);
        builder.RegisterComponentInHierarchy<GasButton>();
        builder.RegisterComponentInHierarchy<CollectibleSpawner>();
        builder.RegisterBuildCallback(resolver => resolver.Resolve<CollectibleSpawner>());
    }
}
