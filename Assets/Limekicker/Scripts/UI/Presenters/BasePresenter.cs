using System;

/// <summary>
/// Base class for UI presenters that subscribe to game models and views.
/// </summary>
public abstract class BasePresenter : IDisposable
{
    protected bool isInitialized = false;

    public virtual void Initialize()
    {
        if (isInitialized)
            return;

        SubscribeToModels();
        isInitialized = true;
    }

    protected abstract void SubscribeToModels();
    protected abstract void UnsubscribeFromModels();

    public virtual void Dispose()
    {
        if (!isInitialized)
            return;

        UnsubscribeFromModels();
        isInitialized = false;
    }
}
