using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        Debug.Log("[GameLifetimeScope] Configuring DI container...");

        builder.Register<ISpawnPointService, SpawnPointService>(Lifetime.Scoped);
        builder.Register<IPlayerSpawnManager, PlayerSpawnManager>(Lifetime.Scoped);
        builder.RegisterComponentInHierarchy<GasButton>();
        builder.RegisterComponentInHierarchy<SimpleHoverChaseCam>();

        builder.RegisterBuildCallback(container =>
        {
            container.Resolve<IPlayerSpawnManager>();
            // SimpleHoverChaseCam will be auto-injected when found in hierarchy
        });
    }
}
