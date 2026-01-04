using System.Collections;
using UnityEngine;

internal class CountdownState : IGameState
{
    private readonly GameManager gameManager;

    public CountdownState(GameManager manager)
    {
        gameManager = manager;
    }

    public void Enter()
    {
        CoroutineMonoBehavior.Instance.StartCoroutine(CountdownCoroutine());
    }

    public void Exit() { }

    public void Update() { }

    private IEnumerator CountdownCoroutine()
    {
        var text = gameManager.Context.startCounterText;
        text.gameObject.SetActive(true);

        yield return AnimateText("3");
        yield return AnimateText("2");
        yield return AnimateText("1");
        yield return AnimateText("GO!");

        gameManager.ChangeState(new PlayState(gameManager));

        yield return new WaitForSeconds(0.5f);
        text.gameObject.SetActive(false);
    }

    private IEnumerator AnimateText(string value)
    {
        var text = gameManager.Context.startCounterText;
        text.text = value;

        float startSize = 100f;
        float endSize = 256f;
        float duration = 1f;
        float t = 0f;

        text.fontSize = startSize;

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;
            text.fontSize = Mathf.Lerp(startSize, endSize, lerp);
            yield return null;
        }

        text.fontSize = endSize;
    }
}