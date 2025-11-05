using VContainer;
using VContainer.Unity;

public class BootstrapLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<IAudioService, AudioService>(Lifetime.Singleton);
    }

    private new void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        base.Awake();
    }
}
