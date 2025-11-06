using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        Debug.Log("[GameLifetimeScope] Configuring DI container...");

        builder.Register<IPlayerSpawnManager, PlayerSpawnManager>(Lifetime.Scoped);
        builder.RegisterComponentInHierarchy<GasButton>();

        builder.RegisterBuildCallback(container =>
        {
            container.Resolve<IPlayerSpawnManager>();

        });
    }
}
