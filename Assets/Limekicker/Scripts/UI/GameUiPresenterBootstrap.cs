using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameUiPresenterBootstrap : IStartable, IDisposable
{
    private readonly IGameManager gameManager;
    private readonly IScoreManager scoreManager;
    private readonly List<BasePresenter> presenters = new();

    [Inject]
    public GameUiPresenterBootstrap(IGameManager gameManager, IScoreManager scoreManager)
    {
        this.gameManager = gameManager;
        this.scoreManager = scoreManager;
    }

    public void Start()
    {
        InitializePresenter(FindView<PauseMenuView>(), view =>
            new PauseMenuPresenter(view, gameManager));

        InitializePresenter(FindView<RoundResultsView>(), view =>
            new RoundResultsPresenter(view, scoreManager, gameManager));

        InitializePresenter(FindView<ScoreDisplayView>(), view =>
            new ScoreDisplayPresenter(view, scoreManager, view));
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
            Debug.LogError($"[GameUiPresenterBootstrap] Missing view for {typeof(TView).Name}.");
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
