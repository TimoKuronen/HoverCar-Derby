using VContainer;
using VContainer.Unity;

/// <summary>
/// Registers cross-scene singleton services such as audio and input.
/// </summary>
public class BootstrapLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<IAudioService, AudioService>(Lifetime.Singleton);
        builder.Register<IInputService, TouchInputService>(Lifetime.Singleton).As<ITickable>();
    }

    private new void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        base.Awake();
    }
}
