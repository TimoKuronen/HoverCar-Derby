using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// Wires menu-scene UI presenter dependencies.
/// </summary>
[DefaultExecutionOrder(-100)]
public class MenuLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterEntryPoint<MenuUiPresenterBootstrap>(Lifetime.Scoped);
    }
}