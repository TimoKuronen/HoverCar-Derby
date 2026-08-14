using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MenuUiPresenterBootstrap : IStartable, IDisposable
{
    private readonly List<BasePresenter> presenters = new();

    public void Start()
    {
        InitializePresenter(FindView<MainMenuView>(), view => new MainMenuPresenter(view));
    }

    public void Dispose()
    {
        foreach (BasePresenter presenter in presenters)
            presenter.Dispose();

        presenters.Clear();
    }

    private void InitializePresenter<TView>(TView view, Func<TView, BasePresenter> factory)
        where TView : class
    {
        if (view == null)
        {
            Debug.LogError($"[MenuUiPresenterBootstrap] Missing view for {typeof(TView).Name}.");
            return;
        }

        BasePresenter presenter = factory(view);
        presenter.Initialize();
        presenters.Add(presenter);
    }

    private static TView FindView<TView>() where TView : Component
    {
        return UnityEngine.Object.FindFirstObjectByType<TView>(FindObjectsInactive.Include);
    }
}
